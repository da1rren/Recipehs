namespace Recipehs.Extractor.Blocks;

using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Microsoft.Extensions.Logging;
using Shared.Models;
using System.Net;
using System.Threading.Tasks.Dataflow;

public enum ParseStatus
{
    Default = 0,
    Success = 1,
    Failure = 2
}

public record RecipeResponseResult(ParseStatus Status, Recipe? Recipe);

public class HtmlParserBlock
{
    private readonly ILogger _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public HtmlParserBlock(ILogger<HtmlParserBlock> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public TransformBlock<int, RecipeResponseResult> Build(ExecutionDataflowBlockOptions options)
    {
        const string ingredientSelector = ".ingredients-item-name";
        const string stepSelector = ".instructions-section-item > .section-body";
        const string imageSelector = ".image-slide noscript img";
        //noscript img

        return new TransformBlock<int, RecipeResponseResult>(async recipeId =>
        {
            var config = Configuration.Default.WithDefaultLoader();
            var uri = new Uri($"https://www.allrecipes.com/recipe/{recipeId}");
            var document = await TryGetDocumentAsync(uri);

            // We could retry, it might just be network failure.
            // But for the moment lets not.
            if (document == null)
            {
                return new RecipeResponseResult(ParseStatus.Failure, null);
            }
            
            if (document.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogInformation($"Recipe {recipeId} returned {document.StatusCode}");
                return new RecipeResponseResult(ParseStatus.Failure, null);
            }

            var ingredients = document.QuerySelectorAll(ingredientSelector)
                .Select(static x => x.TextContent)
                .ToList();

            if (!ingredients.Any())
            {
                _logger.LogInformation($"Recipe {recipeId} has no ingredients, this could be a parse error");
                return new RecipeResponseResult(ParseStatus.Failure, null);
            }
            
            var steps = document.QuerySelectorAll(stepSelector)
                .Select(static x => x.TextContent)
                .ToList();

            if (!steps.Any())
            {
                _logger.LogInformation($"Recipe {recipeId} has no steps, this could be a parse error");
                return new RecipeResponseResult(ParseStatus.Failure, null);
            }
            
            var images = document.QuerySelectorAll(imageSelector)
                .Select(x => x.Attributes.GetNamedItem("src")?.Value)
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList();

            _logger.LogInformation($"Recipe {recipeId} parsed with {steps.Count} steps and {ingredients.Count} ingredients");

            var formattedId = $@"ar-{recipeId}";
            var title = document?.Title?.Replace(" | Allrecipes", string.Empty) ?? string.Empty;
            
            var recipe = new Recipe(formattedId, title,
                ingredients, steps, images!);

            return new RecipeResponseResult(ParseStatus.Success, recipe);
        }, options);
    }

    private async Task<IHtmlDocument?> TryGetDocumentAsync(Uri uri)
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
            return htmlParser.ParseDocument(stream);
        }
        catch(HttpRequestException)
        {
            _logger.LogWarning($@"Error loading {uri}");
            return null;
        }
    }
}