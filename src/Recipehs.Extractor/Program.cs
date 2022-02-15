using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Recipehs.Extractor;
using Recipehs.Extractor.Blocks;
using Microsoft.Extensions.Logging;

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
    .AddSingleton(new AmazonS3Config
    {
        ServiceURL = configuration["S3_ENDPOINT"]
    })
    .AddSingleton(x =>
    {
        var config = x.GetService<AmazonS3Config>();
        var accessKey = configuration["S3_ACCESS_KEY"];
        var secretKey = configuration["S3_SECRET_KEY"];
        return new AmazonS3Client(accessKey, secretKey, config);
    })
    .BuildServiceProvider();

var composer = serviceProvider.GetRequiredService<PipelineComposer>();
await composer.Execute(20510, 20520, cleanRun: true);

