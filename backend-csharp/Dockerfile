FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY *.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "NexusEngine.Api.dll"]

# Multi-stage build: l'immagine finale contiene solo il runtime ASP.NET, non l'SDK. 
# Riduce l'immagine da ~800MB a ~200MB. 
