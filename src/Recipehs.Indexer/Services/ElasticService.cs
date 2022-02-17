namespace Recipehs.Indexer.Services;

using Microsoft.Extensions.Logging;
using Nest;
using Shared.Models;

public class ElasticService
{
    private readonly ElasticClient _client;

    private readonly ILogger<ElasticClient> _logger;

    private const string IndexPatternName = "recipes-from";

    private const string RecipeWildcardIndex = "recipes-from-*";
    private static string GenerateIndexName(string source) => $"recipes-from-{source.TrimEnd('/')}";

    public ElasticService(ElasticClient client, ILogger<ElasticClient> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task TryCreateRecipeSourceIndex(string source,
        bool recreate = false, CancellationToken cancellationToken = default)
    {
        var indexName = GenerateIndexName(source);
        _logger.LogInformation($"Creating index {indexName}");

        var isExistingResponse = await _client.Indices.ExistsAsync(indexName, ct: cancellationToken);

        if (isExistingResponse.Exists && recreate)
        {
            _logger.LogInformation($"Index {indexName} will be recreated");
            await _client.Indices.DeleteAsync(indexName, ct: cancellationToken);
        }
        else if (isExistingResponse.Exists)
        {
            _logger.LogInformation($"Index {indexName} already exists");
            return;
        }

        var indexResponse = await _client.Indices.CreateAsync(indexName, c =>
        {
            return c
                .Settings(s => s
                    .Analysis(a => a
                        .TokenFilters(tf => tf
                            .Stop("english_stop", st => st
                                .StopWords("_english_"))
                            .Stemmer("english_stemmer", st => st
                                .Language("english"))
                            .Stemmer("light_english_stemmer", st => st
                                .Language("light_english")
                            )
                            .Stemmer("english_possessive_stemmer", st => st
                                .Language("possessive_english")
                            )
                        )
                        .Analyzers(aa => aa
                            .Custom("light_english", ca => ca
                                .Tokenizer("standard")
                                .Filters("light_english_stemmer",
                                    "english_possessive_stemmer",
                                    "lowercase",
                                    "asciifolding")
                            )
                        )))
                .Map<Recipe>(m => m
                    .AutoMap()
                    .Properties(p => p
                        .Text(t => t
                            .Name(n => n.Title)
                            .Analyzer("light_english"))
                        .Text(t => t
                            .Name(n => n.Ingredient)
                            .Analyzer("light_english"))
                    ));
        }, cancellationToken);

        if (indexResponse.Acknowledged)
        {
            _logger.LogInformation($"Index {indexName} was Acknowledged");
        }
        else
        {
            _logger.LogInformation($"Index {indexName} failed to create");
        }
    }

    public async Task TryCreateRecipeIndexPatternAsync(bool recreate, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"Ensuring index pattern {RecipeWildcardIndex} exists.");
        var isExistingResponse = await _client.Indices.TemplateExistsAsync(
            IndexPatternName, ct: cancellationToken);

        if (recreate && isExistingResponse.Exists)
        {
            _logger.LogInformation("Index pattern will be recreated.");
            await _client.Indices.DeleteTemplateV2Async(RecipeWildcardIndex, ct: cancellationToken);
        }
        else if (isExistingResponse.Exists)
        {
            _logger.LogInformation("Index pattern already exists.");
            return;
        }

        var response = await _client.Indices
            .PutTemplateV2Async(IndexPatternName, p => p
                    .IndexPatterns(RecipeWildcardIndex),
                cancellationToken);

        if (response.Acknowledged)
        {
            _logger.LogInformation($"Create request for index pattern was Acknowledged");
        }
        else
        {
            _logger.LogWarning($"Create request for index pattern has failed.");
        }
    }

    public async Task IndexRecipes(string source, IEnumerable<Recipe> recipes, CancellationToken cancellationToken)
    {
        var indexName = GenerateIndexName(source);
        _logger.LogInformation($@"Indexing {recipes.Count()} documents into the {indexName} index");

        var indexResult = await _client.IndexManyAsync(recipes, indexName, cancellationToken: cancellationToken);

        if (indexResult.Errors)
        {
            _logger.LogWarning($@"Indexing completed with errors {indexResult.ItemsWithErrors.Count()}");
        }
        else
        {
            _logger.LogInformation("Indexing completed without errors");
        }
    }
}