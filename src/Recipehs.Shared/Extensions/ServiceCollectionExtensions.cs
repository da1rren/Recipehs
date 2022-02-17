namespace Recipehs.Shared.Extensions;

using Amazon.S3;
using Elasticsearch.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nest;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterS3(this IServiceCollection services, 
        IConfiguration configuration)
    {
        return services.AddSingleton(new AmazonS3Config
        {
            ServiceURL = configuration["S3_ENDPOINT"]
        })
        .AddSingleton(sp =>
        {
            var config = sp.GetService<AmazonS3Config>();
            var accessKey = configuration["S3_ACCESS_KEY"];
            var secretKey = configuration["S3_SECRET_KEY"];
            
            ArgumentNullException.ThrowIfNull(accessKey);
            ArgumentNullException.ThrowIfNull(secretKey);
            
            return new AmazonS3Client(accessKey, secretKey, config);
        });
    }

    public static IServiceCollection RegisterElasticSearch(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddSingleton(_ =>
        {
            var cloudId = configuration["ES_CLOUD_ID"];
            var keyId = configuration["ES_KEY_ID"];
            var key = configuration["ES_KEY"];

            ArgumentNullException.ThrowIfNull(cloudId);
            ArgumentNullException.ThrowIfNull(keyId);
            ArgumentNullException.ThrowIfNull(key);

            var apiCredential = new ApiKeyAuthenticationCredentials(keyId, key);
            return new ElasticClient(cloudId, apiCredential);
        });
    }
}