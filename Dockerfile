# Utiliser l'image officielle .NET 8
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Image de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copier les fichiers de projet
COPY ["DiversityPub.csproj", "./"]
RUN dotnet restore "DiversityPub.csproj"

# Copier le reste du code source
COPY . .
RUN dotnet build "DiversityPub.csproj" -c Release -o /app/build

# Publier l'application
FROM build AS publish
RUN dotnet publish "DiversityPub.csproj" -c Release -o /app/publish

# Image finale
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Variables d'environnement pour Railway
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Exposer le port
EXPOSE 8080

# Commande de démarrage
ENTRYPOINT ["dotnet", "DiversityPub.dll"]
