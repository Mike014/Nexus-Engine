FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY backend-csharp/*.csproj backend-csharp/
RUN dotnet restore backend-csharp/NexusEngine.Api.csproj

COPY backend-csharp/ backend-csharp/
WORKDIR /src/backend-csharp
RUN dotnet publish NexusEngine.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "NexusEngine.Api.dll"]

# Multi-stage build: l'immagine finale contiene solo il runtime ASP.NET, non l'SDK. 
# Riduce l'immagine da ~800MB a ~200MB.
