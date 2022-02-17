namespace Recipehs.Extractor.Blocks;

using AngleSharp;
using AngleSharp.Dom;
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

    public HtmlParserBlock(ILogger<HtmlParserBlock> logger)
    {
        _logger = logger;
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
            var uri = $"https://www.allrecipes.com/recipe/{recipeId}";
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(uri);

            if (document.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogInformation($"Recipe {recipeId} returned {document.StatusCode}");
                return new RecipeResponseResult(ParseStatus.Failure, null);
            }

            var ingredients = document.QuerySelectorAll(ingredientSelector)
                .Select(static x => x.TextContent)
                .ToList();

            var steps = document.QuerySelectorAll(stepSelector)
                .Select(static x => x.TextContent)
                .ToList();

            var images = document.QuerySelectorAll(imageSelector)
                .Select(x => x.Attributes.GetNamedItem("src")?.Value)
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList();
            
            _logger.LogInformation($"Recipe {recipeId} parsed with {steps.Count} steps and {ingredients.Count} steps");

            var formattedId = $@"ar-{recipeId}";
            var title = document?.Title?.Replace(" | Allrecipes", string.Empty) ?? string.Empty;
            var recipe = new Recipe(formattedId, title, ingredients, steps, images!);
                
            return new RecipeResponseResult(ParseStatus.Success, recipe);
        }, options);
    }
}