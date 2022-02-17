using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Recipehs.Extractor;
using Recipehs.Extractor.Blocks;
using Microsoft.Extensions.Logging;
using Recipehs.Shared.Extensions;

var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

await using var serviceProvider = new ServiceCollection()
    .AddLogging(cfg => cfg.AddConsole())
    .AddHttpClient()
    .AddSingleton<RangeBlock>()
    .AddSingleton<HtmlParserBlock>()
    .AddSingleton<S3UploaderBlock>()
    .AddSingleton<PipelineComposer>()
    .RegisterS3(configuration)
    .BuildServiceProvider();

var composer = serviceProvider.GetRequiredService<PipelineComposer>();
await composer.Execute(20510, 20520, cleanRun: true);

