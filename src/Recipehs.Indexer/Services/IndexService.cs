namespace Recipehs.Indexer.Services;

using Amazon.S3;
using Extensions;
using Microsoft.Extensions.Logging;
using Nest;
using Shared;
using Shared.Models;
using System.Globalization;
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
    private readonly ILogger<IndexService> _logger;

    public IndexService(AmazonS3Client s3Client, ElasticService elasticService, ILogger<IndexService> logger)
    {
        _s3Client = s3Client;
        _elasticService = elasticService;
        _logger = logger;
    }

    public async Task IndexAll(bool recreate = false, CancellationToken cancellationToken = default)
    {
        await _elasticService.TryCreateRecipeIndexPatternAsync(recreate, cancellationToken);
        var prefixes = await _s3Client.ListPrefixesAsync(WellKnown.S3.BUCKET_NAME, cancellationToken);
        var indexCreationTasks = prefixes.Select(prefix => 
            _elasticService.TryCreateRecipeSourceIndex(prefix, recreate, cancellationToken));

        await Task.WhenAll(indexCreationTasks);

        foreach (var prefix in prefixes)
        {
            var keys = await _s3Client.ListKeysByPrefix(
                WellKnown.S3.BUCKET_NAME, prefix, cancellationToken);

            foreach (var key in keys)
            {
                var objectResponse = await _s3Client.GetObjectAsync(
                    WellKnown.S3.BUCKET_NAME, key, cancellationToken);

                var recipes = await JsonSerializer.DeserializeAsync<IEnumerable<Recipe>>(
                    objectResponse.ResponseStream, WellKnown.Json.DefaultSettings, cancellationToken);

                await _elasticService.IndexRecipes(prefix, recipes ?? Enumerable.Empty<Recipe>(), cancellationToken);
            }
        }
    }
}