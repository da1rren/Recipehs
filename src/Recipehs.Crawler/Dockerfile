FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Recipehs.Crawler/Recipehs.Crawler.csproj", "Recipehs.Crawler/"]
RUN dotnet restore "Recipehs.Crawler/Recipehs.Crawler.csproj"
COPY . .
WORKDIR "/src/Recipehs.Crawler"
RUN dotnet build "Recipehs.Crawler.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Recipehs.Crawler.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Recipehs.Crawler.dll"]
