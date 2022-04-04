namespace Recipehs.Shared;

using System.Text.Json;
using System.Text.Json.Serialization;

public static class WellKnown
{
    public static class S3
    {
        public const string RECIPES_BUCKET = "all-recipes";
    }

    public static class ElasticSearch
    {
        public const string RECIPE_WILDCARD_INDEX = "recipes-from-*";
    }

    public static class Kafka
    {
        public static class Topics
        {
            public const string URLS_TO_PROCESS = "urls-to-process";
        }
    }
    
    public static class Json
    {
        public static JsonSerializerOptions DefaultSettings = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };

    }
}