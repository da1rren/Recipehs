namespace Recipehs.Shared.Models;

public enum ParseStatus
{
    Default = 0,
    Success = 1,
    Failure = 2,
    NetworkFailure = 4,
    ParseFailure = 8
}