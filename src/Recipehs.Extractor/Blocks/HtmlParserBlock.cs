namespace Recipehs.Extractor.Blocks;

using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Microsoft.Extensions.Logging;
using Shared.Models;
using System.Net;
using System.Threading.Tasks.Dataflow;

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
        const string ingredientSelector = ".ingredients-item-name, .recipe-ingred_txt";
        const string stepSelector = ".instructions-section-item > .section-body, .recipe-directions__list--item";
        const string imageSelector = ".image-slide noscript img";
        const string documentSelector = ".recipe-container, .recipe-container-outer";

        return new TransformBlock<int, RecipeResponseResult>(async recipeId =>
        {
            var id = Interlocked.Increment(ref _index);
            var uri = new Uri($"https://www.allrecipes.com/recipe/{recipeId}");
            var (success, statusCode, document) = await TryGetDocumentAsync(uri);
            var html = document?.QuerySelectorAll(documentSelector)
                ?.FirstOrDefault()
                ?.Html()
                .Trim();
            
            // We could retry, it might just be network failure.
            // But for the moment lets not.
            if (!success)
            {
                return RecipeResponseResult.Failure(ParseStatus.NetworkFailure, id, uri);
            }

            if (statusCode != HttpStatusCode.OK)
            {
                _logger.LogInformation($"Recipe {recipeId} returned {document.StatusCode}");
                return RecipeResponseResult.Failure(ParseStatus.NetworkFailure, id, uri);
            }

            var ingredients = document.QuerySelectorAll(ingredientSelector)
                .Select(static x => x.TextContent)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            if (!ingredients.Any())
            {
                _logger.LogInformation($"Recipe {recipeId} has no ingredients, this could be a parse error");
                return RecipeResponseResult.Failure(ParseStatus.ParseFailure, id, uri, html: html);
            }

            var steps = document.QuerySelectorAll(stepSelector)
                .Select(static x => x.TextContent)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            if (!steps.Any())
            {
                _logger.LogInformation($"Recipe {recipeId} has no steps, this could be a parse error");
                return RecipeResponseResult.Failure(ParseStatus.ParseFailure, id, uri, html: html);
            }

            var images = document.QuerySelectorAll(imageSelector)
                .Select(x => x.Attributes.GetNamedItem("src")?.Value)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            _logger.LogInformation(
                $"Recipe {recipeId} parsed with {steps.Count} steps and {ingredients.Count} ingredients");

            var formattedId = $@"{uri.Host}-{recipeId}";
            var title = document?.Title?.Replace(" | Allrecipes", string.Empty)?.Trim() ?? string.Empty;
            var summary = document?.GetElementsByClassName("recipe-summary")
                ?.FirstOrDefault()
                ?.Text()
                ?.Trim();
            
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