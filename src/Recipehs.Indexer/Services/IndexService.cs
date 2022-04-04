namespace Recipehs.Indexer.Services;

using Amazon.S3;
using Extensions;
using Microsoft.Extensions.Logging;
using Nest;
using Shared;
using Shared.Models;
using System.Globalization;
using System.Text;
using System.Text.Json;

/// <summary>
/// This service ensures that elastic search is configured correctly by
/// 1. Ensures the index pattern exists
/// 2. Ensures all s3 prefixes have their own indexes
/// 3. Downloads all untagged json files
/// 4. Indexes them in elastic
/// </summary>
public class IndexService
{
    private readonly AmazonS3Client _s3Client;
    private readonly ElasticService _elasticService;

    public IndexService(AmazonS3Client s3Client, ElasticService elasticService)
    {
        _s3Client = s3Client;
        _elasticService = elasticService;
    }

    public async Task IndexAll(bool recreate = false, CancellationToken cancellationToken = default)
    {
        await _elasticService.TryCreateRecipeIndexPatternAsync(recreate, cancellationToken);
        var prefixes = await _s3Client.ListPrefixesAsync(WellKnown.S3.RECIPES_BUCKET, cancellationToken);
        var indexCreationTasks = prefixes.Select(prefix => 
            _elasticService.TryCreateRecipeSourceIndex(prefix, recreate, cancellationToken));

        await Task.WhenAll(indexCreationTasks);

        foreach (var prefix in prefixes)
        {
            var keys = await _s3Client.ListKeysByPrefix(
                WellKnown.S3.RECIPES_BUCKET, prefix, cancellationToken);
            
            foreach (var key in keys)
            {
                var objectResponse = await _s3Client.GetObjectAsync(
                    WellKnown.S3.RECIPES_BUCKET, key, cancellationToken);

                var recipeResults = await JsonSerializer.DeserializeAsync<IEnumerable<RecipeResponseResult>>(
                    objectResponse.ResponseStream, WellKnown.Json.DefaultSettings, cancellationToken);
                
                // Do something with the value

                var recipes = recipeResults
                    ?.Where(x => x.Status == ParseStatus.Success && x.Recipe != null)
                    ?.Select(x => x.Recipe!)
                    ?.ToList() ?? Enumerable.Empty<Recipe>();
                
                if (recipes.Any())
                {
                    await _elasticService.IndexRecipes(prefix, recipes!, cancellationToken);
                }
            }
        }
    }
}