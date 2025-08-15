# Script de configuration MySQL pour DiversityPub
# Assurez-vous que MySQL est installé et en cours d'exécution

Write-Host "=== Configuration MySQL pour DiversityPub ===" -ForegroundColor Green

# Vérifier si MySQL est installé
try {
    $mysqlVersion = mysql --version 2>$null
    if ($mysqlVersion) {
        Write-Host "✅ MySQL détecté: $mysqlVersion" -ForegroundColor Green
    } else {
        Write-Host "❌ MySQL non trouvé. Veuillez installer MySQL Server." -ForegroundColor Red
        Write-Host "Téléchargez depuis: https://dev.mysql.com/downloads/mysql/" -ForegroundColor Yellow
        exit 1
    }
} catch {
    Write-Host "❌ Erreur lors de la vérification de MySQL" -ForegroundColor Red
    exit 1
}

# Demander les informations de connexion
$mysqlUser = Read-Host "Nom d'utilisateur MySQL (défaut: root)"
if ([string]::IsNullOrEmpty($mysqlUser)) {
    $mysqlUser = "root"
}

$mysqlPassword = Read-Host "Mot de passe MySQL" -AsSecureString
$mysqlPasswordPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($mysqlPassword))

# Créer la base de données
Write-Host "Création de la base de données DB_Diversity..." -ForegroundColor Yellow

$createDbScript = @"
CREATE DATABASE IF NOT EXISTS DB_Diversity 
CHARACTER SET utf8mb4 
COLLATE utf8mb4_unicode_ci;
"@

try {
    if ([string]::IsNullOrEmpty($mysqlPasswordPlain)) {
        echo $createDbScript | mysql -u $mysqlUser
    } else {
        echo $createDbScript | mysql -u $mysqlUser -p$mysqlPasswordPlain
    }
    Write-Host "✅ Base de données créée avec succès!" -ForegroundColor Green
} catch {
    Write-Host "❌ Erreur lors de la création de la base de données" -ForegroundColor Red
    Write-Host "Assurez-vous que MySQL est en cours d'exécution et que les identifiants sont corrects." -ForegroundColor Yellow
    exit 1
}

# Mettre à jour appsettings.json
Write-Host "Mise à jour de appsettings.json..." -ForegroundColor Yellow

$appSettingsPath = "appsettings.json"
$appSettings = Get-Content $appSettingsPath | ConvertFrom-Json

if ([string]::IsNullOrEmpty($mysqlPasswordPlain)) {
    $connectionString = "Server=localhost;Database=DB_Diversity;User=$mysqlUser;CharSet=utf8mb4;"
} else {
    $connectionString = "Server=localhost;Database=DB_Diversity;User=$mysqlUser;Password=$mysqlPasswordPlain;CharSet=utf8mb4;"
}

$appSettings.ConnectionStrings.DefaultConnection = $connectionString
$appSettings | ConvertTo-Json -Depth 10 | Set-Content $appSettingsPath

Write-Host "✅ appsettings.json mis à jour!" -ForegroundColor Green

# Instructions pour les migrations
Write-Host "`n=== Prochaines étapes ===" -ForegroundColor Cyan
Write-Host "1. Supprimez les anciennes migrations SQL Server:" -ForegroundColor White
Write-Host "   Remove-Item Migrations\*.cs" -ForegroundColor Gray
Write-Host "`n2. Créez une nouvelle migration MySQL:" -ForegroundColor White
Write-Host "   dotnet ef migrations add InitialCreate" -ForegroundColor Gray
Write-Host "`n3. Appliquez la migration:" -ForegroundColor White
Write-Host "   dotnet ef database update" -ForegroundColor Gray
Write-Host "`n4. Lancez l'application:" -ForegroundColor White
Write-Host "   dotnet run" -ForegroundColor Gray

Write-Host "`n✅ Configuration MySQL terminée!" -ForegroundColor Green
