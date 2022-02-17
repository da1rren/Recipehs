namespace Recipehs.Shared.Models;

public record Recipe(string RecipeId, string Title, IEnumerable<string> Ingredient, 
    IEnumerable<string> Steps, IEnumerable<string> Images);
