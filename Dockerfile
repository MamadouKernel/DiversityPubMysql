# Utiliser l'image officielle .NET 8 Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Image de build avec SDK
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copier le fichier de projet et restaurer les dépendances
COPY ["DiversityPub.csproj", "./"]
RUN dotnet restore "DiversityPub.csproj"

# Copier tout le code source
COPY . .

# Build de l'application
RUN dotnet build "DiversityPub.csproj" -c Release -o /app/build

# Publier l'application
FROM build AS publish
RUN dotnet publish "DiversityPub.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Image finale
FROM base AS final
WORKDIR /app

# Copier les fichiers publiés
COPY --from=publish /app/publish .

# Variables d'environnement pour Railway
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Exposer le port
EXPOSE 8080

# Commande de démarrage
ENTRYPOINT ["dotnet", "DiversityPub.dll"]
