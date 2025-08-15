# Script de test de configuration Railway
# Teste la configuration avant déploiement

Write-Host "🧪 Test de configuration Railway" -ForegroundColor Cyan

# Variables d'environnement de test
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:ASPNETCORE_URLS = "http://0.0.0.0:8080"
$env:MYSQL_HOST = "mainline.proxy.rlwy.net"
$env:MYSQL_DATABASE = "railway"
$env:MYSQL_USER = "root"
$env:MYSQL_PASSWORD = "WCrDOYlSIPdNJriAlsizKEUSyCXmZRvE"

Write-Host "`n🔧 Variables d'environnement configurées :" -ForegroundColor Yellow
Write-Host "ASPNETCORE_ENVIRONMENT: $env:ASPNETCORE_ENVIRONMENT" -ForegroundColor Green
Write-Host "ASPNETCORE_URLS: $env:ASPNETCORE_URLS" -ForegroundColor Green
Write-Host "MYSQL_HOST: $env:MYSQL_HOST" -ForegroundColor Green
Write-Host "MYSQL_DATABASE: $env:MYSQL_DATABASE" -ForegroundColor Green
Write-Host "MYSQL_USER: $env:MYSQL_USER" -ForegroundColor Green
Write-Host "MYSQL_PASSWORD: [MASQUÉ]" -ForegroundColor Green

Write-Host "`n🚀 Test de build..." -ForegroundColor Yellow
dotnet build -c Release

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Build réussi" -ForegroundColor Green
} else {
    Write-Host "❌ Build échoué" -ForegroundColor Red
    exit 1
}

Write-Host "`n📦 Test de publication..." -ForegroundColor Yellow
dotnet publish -c Release -o ./test-publish

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Publication réussie" -ForegroundColor Green
} else {
    Write-Host "❌ Publication échouée" -ForegroundColor Red
    exit 1
}

Write-Host "`n🎉 Configuration testée avec succès !" -ForegroundColor Green
Write-Host "Prêt pour le déploiement Railway." -ForegroundColor Green

# Nettoyer
Remove-Item -Recurse -Force ./test-publish -ErrorAction SilentlyContinue
