FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Recipehs.Extractor/Recipehs.Extractor.csproj", "Recipehs.Extractor/"]
COPY ["Recipehs.Shared/Recipehs.Shared.csproj", "Recipehs.Shared/"]
RUN dotnet restore "Recipehs.Extractor/Recipehs.Extractor.csproj"
COPY . .
WORKDIR "/src/Recipehs.Extractor"
RUN dotnet build "Recipehs.Extractor.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Recipehs.Extractor.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Recipehs.Extractor.dll"]
