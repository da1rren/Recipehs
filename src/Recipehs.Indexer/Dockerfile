FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Recipehs.Indexer/Recipehs.Indexer.csproj", "Recipehs.Indexer/"]
RUN dotnet restore "Recipehs.Indexer/Recipehs.Indexer.csproj"
COPY . .
WORKDIR "/src/Recipehs.Indexer"
RUN dotnet build "Recipehs.Indexer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Recipehs.Indexer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Recipehs.Indexer.dll"]
