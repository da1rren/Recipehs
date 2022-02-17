# Recipehs

I'm using this project to teach myself Elasticsearch & TPL Dataflows.  It consists of three major parts.

## Extractor

The extractor uses TPL dataflows to build a parallel pipeline that ingests data from allrecipes.com and creates a batched json file from the webpage.

The json file is then upload to S3 for future processing.

## Indexer

The indexer initializes the Elasticsearch search indexes, patterns and text processing (Stemming, Stop words, Ascii folding and pluralizer).

Once the indexes are built the json documents are downloaded from s3 then indexed into elasticsearch.

## Blazor Web App

The blazor web app is based on the server side model to keep things simple.  All the webapp does is reach out to elasticsearch and queries it.