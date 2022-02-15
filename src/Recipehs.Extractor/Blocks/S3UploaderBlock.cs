namespace Recipehs.Extractor.Blocks;

using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Tasks.Dataflow;

public class S3UploaderBlock
{
    public const string BUCKET_NAME = "all-recipes";
    public static string RecipeKey(int recipeId) => $"all-recipes/{recipeId}";
    
    private readonly AmazonS3Client _s3Client;
    private readonly ILogger<S3UploaderBlock> _logger;
    
    public S3UploaderBlock(AmazonS3Client s3Client, ILogger<S3UploaderBlock> logger)
    {
        _s3Client = s3Client;
        _logger = logger;
    }
    
    public ActionBlock<RecipeResponseResult[]> Build(ExecutionDataflowBlockOptions options)
    {
        var settings = new JsonSerializerOptions {PropertyNamingPolicy = JsonNamingPolicy.CamelCase};
            
        return new ActionBlock<RecipeResponseResult[]>(async recipeResults =>
        {
            var recipes = recipeResults
                .Where(x => x.Status == ParseStatus.Success)
                .Select(x => x.Recipe!)
                .ToArray();
            
            var rangeStart = recipes.Min(x => x.RecipeId);
            var rangeEnd = recipes.Max(x => x.RecipeId);

            _logger.LogInformation($"Uploading {rangeStart} - {rangeEnd} to s3");

            var s3UploadRequest = new PutObjectRequest
            {
                BucketName = BUCKET_NAME,
                Key = $"all-recipes/{rangeStart}-{rangeEnd}.json",
                ContentType = "application/json",
                ContentBody = JsonSerializer.Serialize(recipes, settings)
            };

            await _s3Client.PutObjectAsync(s3UploadRequest);
        }, options);
    }
}