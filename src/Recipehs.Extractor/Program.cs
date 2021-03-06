using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Recipehs.Extractor;
using Recipehs.Extractor.Blocks;
using Microsoft.Extensions.Logging;
using Recipehs.Shared.Extensions;

var configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>()
    .Build();

var start = configuration.GetValue<int>("RANGE_START");
var end = configuration.GetValue<int>("RANGE_END");

await using var serviceProvider = new ServiceCollection()
    .AddLogging(cfg => cfg.AddConsole())
    .AddHttpClient()
    .AddSingleton(sp =>
    {
        var logger = sp.GetRequiredService<ILogger<HtmlParserBlock>>();
        var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
        return new HtmlParserBlock(logger, httpClientFactory, start);
    })
    .AddSingleton<S3UploaderBlock>()
    .AddSingleton<PipelineComposer>()
    .RegisterS3(configuration)
    .BuildServiceProvider();

var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

if (end - start <= 0)
{
    logger.LogInformation($"Range {start} to {end} is invalid.");
    return;
}

logger.LogInformation($"Processing range {start} to {end}.");
var composer = serviceProvider.GetRequiredService<PipelineComposer>();
await composer.Execute(start, end, cleanRun: false);

