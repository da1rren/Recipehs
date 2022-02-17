namespace Recipehs.Shared;

using System.Text.Json;

public static class WellKnown
{
    public static class S3
    {
        public const string BUCKET_NAME = "all-recipes";
    }

    public static class Json
    {
        public static JsonSerializerOptions DefaultSettings = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

    }
}