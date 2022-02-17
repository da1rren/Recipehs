using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Recipehs.Indexer.Services;
using Recipehs.Shared.Extensions;

var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

await using var serviceProvider = new ServiceCollection()
    .AddLogging(cfg => cfg.AddConsole())
    .RegisterS3(configuration)
    .RegisterElasticSearch(configuration)
    .AddSingleton<ElasticService>()
    .AddSingleton<IndexService>()
    .BuildServiceProvider();

var indexService = serviceProvider.GetRequiredService<IndexService>();
await indexService.IndexAll(recreate: true);

var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Index complete.");