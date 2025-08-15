# Script de validation de configuration Railway
# Vérifie que toutes les variables d'environnement sont correctement configurées

Write-Host "🔍 Validation de la configuration Railway" -ForegroundColor Cyan

# Variables requises
$requiredVariables = @(
    "ASPNETCORE_ENVIRONMENT",
    "ASPNETCORE_URLS",
    "MYSQL_HOST",
    "MYSQL_DATABASE",
    "MYSQL_USER",
    "MYSQL_PASSWORD"
)

$missingVariables = @()
$validVariables = @()

Write-Host "`n📋 Vérification des variables d'environnement..." -ForegroundColor Yellow

foreach ($variable in $requiredVariables) {
    $value = [Environment]::GetEnvironmentVariable($variable)
    
    if ([string]::IsNullOrEmpty($value)) {
        $missingVariables += $variable
        Write-Host "❌ $variable : Non définie" -ForegroundColor Red
    } else {
        $validVariables += $variable
        if ($variable -like "*PASSWORD*") {
            Write-Host "✅ $variable : Définie (masquée)" -ForegroundColor Green
        } else {
            Write-Host "✅ $variable : $value" -ForegroundColor Green
        }
    }
}

Write-Host "`n📊 Résumé de la validation :" -ForegroundColor Cyan
Write-Host "Variables valides : $($validVariables.Count)/$($requiredVariables.Count)" -ForegroundColor Green

if ($missingVariables.Count -gt 0) {
    Write-Host "`n❌ Variables manquantes :" -ForegroundColor Red
    foreach ($missing in $missingVariables) {
        Write-Host "   - $missing" -ForegroundColor Red
    }
    
    Write-Host "`n🔧 Configuration recommandée pour Railway :" -ForegroundColor Yellow
    Write-Host "ASPNETCORE_ENVIRONMENT=Production" -ForegroundColor Gray
    Write-Host "ASPNETCORE_URLS=http://0.0.0.0:`$PORT" -ForegroundColor Gray
    Write-Host "MYSQL_HOST=mainline.proxy.rlwy.net" -ForegroundColor Gray
    Write-Host "MYSQL_DATABASE=railway" -ForegroundColor Gray
    Write-Host "MYSQL_USER=root" -ForegroundColor Gray
    Write-Host "MYSQL_PASSWORD=WCrDOYlSIPdNJriAlsizKEUSyCXmZRvE" -ForegroundColor Gray
} else {
    Write-Host "`n🎉 Toutes les variables sont correctement configurées !" -ForegroundColor Green
    Write-Host "Votre application est prête pour le déploiement Railway." -ForegroundColor Green
}

Write-Host "`n📖 Pour plus d'informations, consultez :" -ForegroundColor Cyan
Write-Host "   - railway-variables.md" -ForegroundColor Gray
Write-Host "   - DEPLOIEMENT_RAILWAY.md" -ForegroundColor Gray
