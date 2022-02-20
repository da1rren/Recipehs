namespace Recipehs.Extractor.Blocks;

using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Shared;
using System.Text.Json;
using System.Threading.Tasks.Dataflow;

public class S3UploaderBlock
{
    private readonly AmazonS3Client _s3Client;
    private readonly ILogger<S3UploaderBlock> _logger;
    
    public S3UploaderBlock(AmazonS3Client s3Client, ILogger<S3UploaderBlock> logger)
    {
        _s3Client = s3Client;
        _logger = logger;
    }
    
    public ActionBlock<RecipeResponseResult[]> Build(ExecutionDataflowBlockOptions options)
    {
        return new ActionBlock<RecipeResponseResult[]>(async recipeResults =>
        {
            var rangeStart = recipeResults.Min(x => x.Id);
            var rangeEnd = recipeResults.Max(x => x.Id);
            
            _logger.LogInformation($"Uploading {rangeStart} - {rangeEnd} to s3");

            var s3UploadRequest = new PutObjectRequest
            {
                BucketName = WellKnown.S3.BUCKET_NAME,
                Key = $"all-recipes/{rangeStart}-{rangeEnd}.json",
                ContentType = "application/json",
                ContentBody = JsonSerializer.Serialize(recipeResults, WellKnown.Json.DefaultSettings)
            };

            await _s3Client.PutObjectAsync(s3UploadRequest);
        }, options);
    }
}