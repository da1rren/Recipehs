namespace Recipehs.Crawler;

using Amazon.S3;
using Amazon.S3.Model;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Confluent.Kafka;
using Shared;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;

public class CrawlerService : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;

    private readonly AmazonS3Client _s3Client;

    private readonly ILogger<CrawlerService> _logger;

    private readonly IConsumer<Ignore, string> _consumer;

    private readonly IProducer<Ignore, string> _producer;

    private readonly ConcurrentDictionary<Uri, byte> _previouslyVisitedUrls = new();

    public static readonly ISet<Uri> SeedUris = new HashSet<Uri> {new Uri("https://www.allrecipes.com/")};

    public CrawlerService(ConsumerConfig consumerConfig, ProducerConfig producerConfig,
        IHttpClientFactory httpClientFactory, AmazonS3Client s3Client, ILogger<CrawlerService> logger)
    {
        _httpClientFactory = httpClientFactory;

        _s3Client = s3Client;
        _logger = logger;

        _consumer = new ConsumerBuilder<Ignore, string>(consumerConfig)
            .Build();

        _producer = new ProducerBuilder<Ignore, string>(producerConfig)
            .Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(WellKnown.Kafka.Topics.URLS_TO_PROCESS);

        while (!stoppingToken.IsCancellationRequested)
        {
            var consumeResult = _consumer.Consume(stoppingToken);
#pragma warning disable CS4014
            Parse(new Uri(consumeResult.Message.Value), stoppingToken);
#pragma warning restore CS4014
        }
    }

    private async Task Parse(Uri uri, CancellationToken cancellationToken = default)
    {
        // Prevent double downloading of already processed results.
        if (!_previouslyVisitedUrls.TryAdd(uri, 0x0))
        {
            // Already added.
            return;
        }

        var client = _httpClientFactory.CreateClient();
        var stream = await client.GetStreamAsync(uri, cancellationToken);

        await Task.WhenAll(new List<Task>
        {
            UploadHtml(uri, stream, cancellationToken), ParseHtml(stream, cancellationToken)
        });
    }

    private async Task ParseHtml(Stream stream, CancellationToken cancellationToken = default)
    {
        var htmlParser = new HtmlParser();
        var document = await htmlParser.ParseDocumentAsync(stream, cancellationToken);
        var uris = document.QuerySelectorAll("a[href]")
            .OfType<IHtmlAnchorElement>()
            .Select(x => x.Href)
            .Where(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .Select(url => new Uri(url, UriKind.Absolute))
            .ToHashSet();

        // Remove previously processed keys
        uris.ExceptWith(_previouslyVisitedUrls.Keys);

        var tasks = uris.Select(uri => _producer
            .ProduceAsync(WellKnown.Kafka.Topics.URLS_TO_PROCESS,
                new Message<Ignore, string> {Value = uri.ToString()}, cancellationToken));
        
        await Task.WhenAll(tasks.Cast<Task>());
    }

    private async Task UploadHtml(Uri uri, Stream stream, CancellationToken cancellationToken = default)
    {
        var s3UploadRequest = new PutObjectRequest
        {
            BucketName = WellKnown.S3.RECIPES_BUCKET,
            Key = $"{uri.AbsoluteUri}/{uri.AbsolutePath}",
            ContentType = "application/json",
            InputStream = stream
        };

        await _s3Client.PutObjectAsync(s3UploadRequest, cancellationToken);
    }

    public override void Dispose()
    {
        _consumer.Dispose();
        _producer.Dispose();
        GC.SuppressFinalize(this);
        base.Dispose();
    }
}