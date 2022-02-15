namespace Recipehs.Extractor;

using Amazon.S3;
using Amazon.S3.Model;
using Blocks;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks.Dataflow;

public class PipelineComposer
{
    private readonly RangeBlock _rangeBlock;
    private readonly HtmlParserBlock _htmlParserBlock;
    private readonly S3UploaderBlock _s3UploaderBlock;
    private readonly AmazonS3Client _s3Client;
    private readonly ILogger<PipelineComposer> _logger;

    public PipelineComposer(
        RangeBlock rangeBlock,
        HtmlParserBlock htmlParserBlock,
        S3UploaderBlock s3UploaderBlock,
        AmazonS3Client s3Client,
        ILogger<PipelineComposer> logger)
    {
        _rangeBlock = rangeBlock;
        _htmlParserBlock = htmlParserBlock;
        _s3UploaderBlock = s3UploaderBlock;
        _s3Client = s3Client;
        _logger = logger;
    }

    public async Task Execute(int start, int end, int batchSize = 1000, bool cleanRun = false)
    {
        if (cleanRun)
        {
            _logger.LogInformation("Cleaning up all objects.");
            await CleanUp();
        }

        var rangeBlock = _rangeBlock.Build();
        var httpBlockPolicy = new ExecutionDataflowBlockOptions {MaxDegreeOfParallelism = 4};
        var httpBlock = _htmlParserBlock.Build(httpBlockPolicy);

        var s3BlockPolicy = new ExecutionDataflowBlockOptions {MaxDegreeOfParallelism = 16};
        var s3Block = _s3UploaderBlock.Build(s3BlockPolicy);
        var linkOptions = new DataflowLinkOptions {PropagateCompletion = true};

        var batchBlock = new BatchBlock<RecipeResponseResult>(batchSize);
        
        rangeBlock.LinkTo(httpBlock, linkOptions);
        
        httpBlock.LinkTo(batchBlock, linkOptions, 
            x => x.Status == ParseStatus.Success);
        
        // Discard failed.
        httpBlock.LinkTo(DataflowBlock.NullTarget<RecipeResponseResult>(),
            x => x.Status != ParseStatus.Success);
        
        batchBlock.LinkTo(s3Block, linkOptions);
        
        rangeBlock.Post((start, end));
        rangeBlock.Complete();
        await s3Block.Completion;
        Console.WriteLine($"Completed range {start} - {end}.");
    }

    private async Task CleanUp()
    {
        var listObjectsRequest = new ListObjectsV2Request {BucketName = S3UploaderBlock.BUCKET_NAME, MaxKeys = 1000};
        var tasks = new List<Task>();

        while (true)
        {
            var s3Response = await _s3Client.ListObjectsV2Async(listObjectsRequest);

            foreach (var @object in s3Response.S3Objects)
            {
                _logger.LogInformation($"Deleting {s3Response.KeyCount}");
                tasks.Add(_s3Client.DeleteObjectAsync(S3UploaderBlock.BUCKET_NAME, @object.Key));
            }
            
            if (s3Response.KeyCount == 0)
            {
                break;
            }
        }
        
        await Task.WhenAll(tasks);
        _logger.LogInformation("Cleanup complete.");
    }
}