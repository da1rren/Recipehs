using Confluent.Kafka;
using Recipehs.Crawler;
using Recipehs.Shared;
using Recipehs.Shared.Extensions;
using System.Net;

var configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>()
    .Build();

const string server = "127.0.0.1:59706";
const string group = "crawler-agents";

var consumerConfig = new ConsumerConfig
{
    BootstrapServers = server,
    GroupId = group,
    AutoOffsetReset = AutoOffsetReset.Earliest,
    ClientId = Dns.GetHostName()
};

var producerConfig = new ProducerConfig {BootstrapServers = server, ClientId = Dns.GetHostName()};

var adminConfig = new AdminClientConfig {BootstrapServers = server, ClientId = Dns.GetHostName()};
//var adminClient = new AdminClientBuilder(adminConfig).Build();

Task.Run(async () =>
{
    using var producer = new ProducerBuilder<Null, string>(producerConfig).Build();

    foreach (var domain in CrawlerService.SeedUris)
    {
        await producer.ProduceAsync(WellKnown.Kafka.Topics.URLS_TO_PROCESS, new Message<Null, string>
        {
            Value = domain.ToString()
        });
    }
});

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services
            .AddHostedService<CrawlerService>()
            .AddSingleton(consumerConfig)
            .AddSingleton(producerConfig)
            .AddSingleton(configuration)
            .AddHttpClient()
            .AddLogging(cfg => cfg.AddConsole())
            .RegisterS3(configuration);
    })
    .Build();

await host.RunAsync();