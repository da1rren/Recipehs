namespace Recipehs.Indexer.Extensions;

using Amazon.S3;
using Amazon.S3.Model;
using Shared;
using Shared.Models;

public static class S3Extensions
{
    public static async Task<IEnumerable<string>> ListPrefixesAsync(this AmazonS3Client client, string bucket,
        CancellationToken cancellationToken = default)
    {
        var listPrefixesRequest = new ListObjectsV2Request {BucketName = bucket, MaxKeys = 1000, Delimiter = "/"};

        var listPrefixesResponse = await client.ListObjectsV2Async(
            listPrefixesRequest, cancellationToken);

        return listPrefixesResponse.CommonPrefixes ?? Enumerable.Empty<string>();
    }

    public static async Task<IEnumerable<string>> ListKeysByPrefix(this AmazonS3Client client,
        string bucket, string prefix, CancellationToken cancellationToken = default)
    {
        var keys = new List<string>();
        string continuationToken = null;

        do
        {
            var listKeys = new ListObjectsV2Request
            {
                BucketName = bucket, MaxKeys = 1000, Prefix = prefix, ContinuationToken = continuationToken
            };

            var listKeysResponse = await client.ListObjectsV2Async(listKeys, cancellationToken);
            continuationToken = listKeysResponse.ContinuationToken;
            keys.AddRange(listKeysResponse.S3Objects.Select(x => x.Key));
        } while (!string.IsNullOrEmpty(continuationToken));

        return keys;
    }
}