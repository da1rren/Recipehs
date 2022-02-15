FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Recipehs.Web/Recipehs.Web.csproj", "Recipehs.Web/"]
RUN dotnet restore "Recipehs.Web/Recipehs.Web.csproj"
COPY . .
WORKDIR "/src/Recipehs.Web"
RUN dotnet build "Recipehs.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Recipehs.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Recipehs.Web.dll"]
