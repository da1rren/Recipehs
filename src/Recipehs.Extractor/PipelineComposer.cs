namespace Recipehs.Extractor;

using Blocks;
using System.Threading.Tasks.Dataflow;

public class PipelineComposer
{
    private readonly RangeBlock _rangeBlock;
    private readonly HttpExtractBlockFactory _httpExtractBlockFactory;
    private readonly S3UploaderBlock _s3UploaderBlock;

    public PipelineComposer(
        RangeBlock rangeBlock,
        HttpExtractBlockFactory httpExtractBlockFactory,
        S3UploaderBlock s3UploaderBlock)
    {
        _rangeBlock = rangeBlock;
        _httpExtractBlockFactory = httpExtractBlockFactory;
        _s3UploaderBlock = s3UploaderBlock;
    }

    public async Task Execute(int start, int end)
    {
        var rangeBlock = _rangeBlock.Build();
        var httpBlockPolicy = new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = 4
        };
        var httpBlock = _httpExtractBlockFactory.Build(httpBlockPolicy);

        var s3BlockPolicy = new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = 4
        };
        var s3Block = _s3UploaderBlock.Build(s3BlockPolicy);
        var linkOptions = new DataflowLinkOptions
        {
            PropagateCompletion = true
        };

        rangeBlock.LinkTo(httpBlock, linkOptions);
        httpBlock.LinkTo(s3Block, linkOptions);
    
        
        rangeBlock.Post((start, end));
        rangeBlock.Complete();
        await s3Block.Completion;
        Console.WriteLine($"Completed range {start} - {end}.");
    }
}