namespace Recipehs.Extractor.Blocks;

using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks.Dataflow;

public enum S3UploadResult
{
    Default = 0,
    Success = 1,
    Failure = 2,
    Skipped = 4
}

public class S3UploaderBlock
{
    public const string BUCKET_NAME = "all-recipes";
    public static string RecipeKey(int RecipeId) => $"all-recipes/{RecipeId}";
        
    private readonly AmazonS3Client _s3Client;
    private readonly ILogger<S3UploaderBlock> _logger;
    
    public S3UploaderBlock(AmazonS3Client s3Client, ILogger<S3UploaderBlock> logger)
    {
        _s3Client = s3Client;
        _logger = logger;
    }
    
    public ActionBlock<RecipeResult> Build(ExecutionDataflowBlockOptions options)
    {
        return new ActionBlock<RecipeResult>(async recipeResult =>
        {
            if (string.IsNullOrWhiteSpace(recipeResult.Html))
            {
                return;
            }
            
            _logger.LogInformation($@"Uploading {recipeResult.RecipeId} to s3");
            var putRequest = new PutObjectRequest
            {
                BucketName = BUCKET_NAME,
                Key = RecipeKey(recipeResult.RecipeId),
                ContentType = "text/html",
                ContentBody = recipeResult.Html
            };

            await _s3Client.PutObjectAsync(putRequest);
        }, options);
    }
}