namespace Recipehs.Extractor.Blocks;

using Amazon.Auth.AccessControlPolicy;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Microsoft.Extensions.Logging;
using Shared.Models;
using System.Net;
using System.Text.Json.Serialization;
using System.Threading.Tasks.Dataflow;

public enum ParseStatus
{
    Default = 0,
    Success = 1,
    Failure = 2,
    NetworkFailure = 4
}

public class RecipeResponseResult
{
    public int Id { get; set; }
    
    public Uri Source { get; private init; }
    
    public ParseStatus Status { get; private init; }

    public string? Html { get; private init; }
    
    public Recipe? Recipe { get; private init; }

    private RecipeResponseResult(int id, Uri source, ParseStatus status)
    {
        Id = id;
        Source = source;
        Status = status;
    }
    
    public static RecipeResponseResult Success(int id, Uri source, string html, Recipe recipe)
    {
        return new RecipeResponseResult(id, source, ParseStatus.Success)
        {
            Html = html,
            Recipe = recipe
        };
    }

    public static RecipeResponseResult Failure(ParseStatus parseStatus, int id, Uri source)
    {
        return new RecipeResponseResult(id, source, parseStatus);
    }
    
    public static RecipeResponseResult Failure(int id, Uri source)
    {
        return new RecipeResponseResult(id, source, ParseStatus.Failure);
    }
}

public class HtmlParserBlock
{
    private readonly ILogger _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private int _index;

    public HtmlParserBlock(ILogger<HtmlParserBlock> logger, IHttpClientFactory httpClientFactory, int rangeStart)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _index = rangeStart;
    }

    public TransformBlock<int, RecipeResponseResult> Build(ExecutionDataflowBlockOptions options)
    {
        const string ingredientSelector = ".ingredients-item-name";
        const string stepSelector = ".instructions-section-item > .section-body";
        const string imageSelector = ".image-slide noscript img";

        return new TransformBlock<int, RecipeResponseResult>(async recipeId =>
        {
            var id = Interlocked.Increment(ref _index);
            var config = Configuration.Default.WithDefaultLoader();
            var uri = new Uri($"https://www.allrecipes.com/recipe/{recipeId}");
            var (success, statusCode, document) = await TryGetDocumentAsync(uri);
            
            // We could retry, it might just be network failure.
            // But for the moment lets not.
            if (!success)
            {
                return RecipeResponseResult.Failure(ParseStatus.NetworkFailure, id, uri);
            }

            if (statusCode != HttpStatusCode.OK)
            {
                _logger.LogInformation($"Recipe {recipeId} returned {document.StatusCode}");
                return RecipeResponseResult.Failure(id, uri);
            }

            var ingredients = document.QuerySelectorAll(ingredientSelector)
                .Select(static x => x.TextContent)
                .ToList();

            if (!ingredients.Any())
            {
                _logger.LogInformation($"Recipe {recipeId} has no ingredients, this could be a parse error");
                return RecipeResponseResult.Failure(id, uri);
            }

            var steps = document.QuerySelectorAll(stepSelector)
                .Select(static x => x.TextContent)
                .ToList();

            if (!steps.Any())
            {
                _logger.LogInformation($"Recipe {recipeId} has no steps, this could be a parse error");
                return RecipeResponseResult.Failure(id, uri);
            }

            var images = document.QuerySelectorAll(imageSelector)
                .Select(x => x.Attributes.GetNamedItem("src")?.Value)
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList();

            _logger.LogInformation(
                $"Recipe {recipeId} parsed with {steps.Count} steps and {ingredients.Count} ingredients");

            var formattedId = $@"{uri.Host}-{recipeId}";
            var title = document?.Title?.Replace(" | Allrecipes", string.Empty)?.Trim() ?? string.Empty;
            var summary = document?.GetElementsByClassName("recipe-summary")
                ?.FirstOrDefault()
                ?.Text()
                ?.Trim();
                
            var html = document?.GetElementsByClassName("recipe-container")
                ?.FirstOrDefault()
                ?.Html();
            
            var recipe = new Recipe(formattedId, title, summary,
                ingredients, steps, images!);

            return RecipeResponseResult.Success(id, uri, html!, recipe);
        }, options);
    }

    private async Task<(bool success, HttpStatusCode? statusCode, IHtmlDocument? document)> TryGetDocumentAsync(Uri uri)
    {
        var htmlParser = new HtmlParser();
        var client = _httpClientFactory.CreateClient();

        try
        {
            var response = await client.GetAsync(uri);

            switch (response.StatusCode)
            {
                case HttpStatusCode.BadRequest:
                case HttpStatusCode.Forbidden:
                case HttpStatusCode.Unauthorized:
                case HttpStatusCode.TooManyRequests:
                case var code when (int)code >= 500 && (int)code <= 599:
                    _logger.LogWarning($@"Received {response.StatusCode} delaying thread for 60s");
                    // Back off the thread of a minute
                    await Task.Delay(60_000);
                    break;
            }

            await using var stream = await response.Content.ReadAsStreamAsync();
            return (true, response.StatusCode, htmlParser.ParseDocument(stream));
        }
        catch (HttpRequestException)
        {
            _logger.LogWarning($@"Error loading {uri}");
            return (false, null, null);
        }
    }
}