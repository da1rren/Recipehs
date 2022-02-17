namespace Recipehs.Shared.Models;

using Nest;
using System.Text.Json.Serialization;

[ElasticsearchType(IdProperty = nameof(RecipeId))]
public class Recipe
{
    [JsonPropertyName("_id")]
    public string RecipeId { get; init; }
    public string Title { get; init; }
    public IEnumerable<string> Ingredient { get; init; }
    public IEnumerable<string> Steps { get; init; }
    public IEnumerable<string> Images { get; init; }

    public Recipe(string recipeId, string title, IEnumerable<string> ingredient, 
        IEnumerable<string> steps, IEnumerable<string> images)
    {
        this.RecipeId = recipeId;
        this.Title = title;
        this.Ingredient = ingredient;
        this.Steps = steps;
        this.Images = images;
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
