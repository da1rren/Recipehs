namespace Recipehs.Extractor.Blocks;

using Amazon.S3;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Threading.Tasks.Dataflow;

public record RecipeResult(int RecipeId, string Html);

public class HttpExtractBlockFactory
{
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<HttpExtractBlockFactory> _logger;
    private readonly AmazonS3Client _s3Client;

    public HttpExtractBlockFactory(IHttpClientFactory factory, ILogger<HttpExtractBlockFactory> logger,
        AmazonS3Client _s3Client)
    {
        _factory = factory;
        _logger = logger;
        this._s3Client = _s3Client;
    }
    
    public TransformBlock<int, RecipeResult> Build(ExecutionDataflowBlockOptions options)
    {
        return new TransformBlock<int, RecipeResult>( async recipeId =>
        {
            try
            {
                var metadata = await _s3Client.GetObjectMetadataAsync(S3UploaderBlock.BUCKET_NAME, 
                    S3UploaderBlock.RecipeKey(recipeId));
                _logger.LogInformation("Skipping {0}", recipeId);

                return new RecipeResult(recipeId, null);
            }
            catch
            {
                // File doesn't exist
            }
            
            var uri = $"https://www.allrecipes.com/recipe/{recipeId}";
            _logger.LogInformation("Downloading {0}", recipeId);
            
            var client = _factory.CreateClient();
            var response = await client.GetAsync(uri);

            if (!response.IsSuccessStatusCode)
            {
                return new RecipeResult(recipeId,$"Error: {response.StatusCode}" );
            }

            var body = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Downloaded {0}", recipeId);
            return new RecipeResult(recipeId, body);
        }, options);
    }
}