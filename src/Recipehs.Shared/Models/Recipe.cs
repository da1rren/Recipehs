namespace Recipehs.Shared.Models;

using Nest;
using System.Text.Json.Serialization;

[ElasticsearchType(IdProperty = nameof(RecipeId))]
public class Recipe
{
    [JsonPropertyName("_id")]
    public string RecipeId { get; init; }
    
    public string Title { get; init; }

    public string? Summary { get; init; }
    
    public IEnumerable<string> Ingredient { get; init; }
    
    public IEnumerable<string> Steps { get; init; }
    
    public IEnumerable<string> Images { get; init; }

    public Recipe(string recipeId, string title, string? summary, IEnumerable<string> ingredient, 
        IEnumerable<string> steps, IEnumerable<string> images)
    {
        RecipeId = recipeId;
        Title = title;
        Summary = summary;
        Ingredient = ingredient;
        Steps = steps;
        Images = images;
    }

    public void Deconstruct(out string recipeId, out string title, out IEnumerable<string> ingredient, out IEnumerable<string> steps, out IEnumerable<string> images)
    {
        recipeId = this.RecipeId;
        title = this.Title;
        ingredient = this.Ingredient;
        steps = this.Steps;
        images = this.Images;
    }
}
