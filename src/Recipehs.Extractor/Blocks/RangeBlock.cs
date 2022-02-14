namespace Recipehs.Extractor.Blocks;

using Microsoft.Extensions.Logging;
using System.Threading.Tasks.Dataflow;

public class RangeBlock
{
    private readonly ILogger<RangeBlock> _logger;

    public RangeBlock(ILogger<RangeBlock> logger)
    {
        _logger = logger;
    }
    
    public TransformManyBlock<(int start, int end), int> Build()
    {
        var block = new TransformManyBlock<(int start, int end), int>(range =>
        {
            (int start, int end) = range;
            _logger.LogInformation($"Enumerating {start} - {end}");
            return Enumerable.Range(start, end - start);
        });

        return block;
    }
}