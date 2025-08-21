# Script PowerShell pour backup de la base de données MySQL DiversityPub
# Compatible avec Railway et autres hébergeurs MySQL

param(
    [string]$Commentaire = "",
    [string]$OutputPath = "Backups"
)

Write-Host "=== Backup de la base de données DiversityPub ===" -ForegroundColor Green

# Configuration de la base de données (Railway)
$connectionString = "Server=mainline.proxy.rlwy.net;Port=12962;Database=railway;User=root;Password=WCrDOYlSIPdNJriAlsizKEUSyCXmZRvE;ConnectionTimeout=30;"

# Extraire les informations de connexion
$parts = $connectionString.Split(';')
$host = ""
$port = ""
$user = ""
$password = ""
$database = ""

foreach ($part in $parts) {
    $keyValue = $part.Split('=')
    if ($keyValue.Length -eq 2) {
        $key = $keyValue[0].Trim().ToLower()
        $value = $keyValue[1].Trim()
        
        switch ($key) {
            "server" { $host = $value }
            "port" { $port = $value }
            "user" { $user = $value }
            "password" { $password = $value }
            "database" { $database = $value }
        }
    }
}

Write-Host "Host: $host" -ForegroundColor Yellow
Write-Host "Port: $port" -ForegroundColor Yellow
Write-Host "Database: $database" -ForegroundColor Yellow
Write-Host "User: $user" -ForegroundColor Yellow

# Créer le dossier de backup s'il n'existe pas
if (!(Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force
    Write-Host "✓ Dossier de backup créé: $OutputPath" -ForegroundColor Green
}

# Générer le nom du fichier de backup
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupFileName = "diversitypub_backup_$timestamp.sql"
$backupFilePath = Join-Path $OutputPath $backupFileName

Write-Host "Création du backup: $backupFileName" -ForegroundColor Yellow

# Vérifier si mysqldump est disponible
$mysqldumpPath = $null
$possiblePaths = @(
    "mysqldump",
    "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysqldump.exe",
    "C:\Program Files\MySQL\MySQL Server 5.7\bin\mysqldump.exe",
    "C:\xampp\mysql\bin\mysqldump.exe",
    "C:\wamp64\bin\mysql\mysql8.0.31\bin\mysqldump.exe"
)

foreach ($path in $possiblePaths) {
    try {
        $result = Get-Command $path -ErrorAction Stop
        $mysqldumpPath = $result.Source
        Write-Host "✓ mysqldump trouvé: $mysqldumpPath" -ForegroundColor Green
        break
    }
    catch {
        continue
    }
}

if ($null -eq $mysqldumpPath) {
    Write-Host "❌ mysqldump non trouvé. Tentative avec MySQL Workbench..." -ForegroundColor Red
    
    # Essayer avec MySQL Workbench si disponible
    $workbenchPath = "C:\Program Files\MySQL\MySQL Workbench 8.0 CE\mysqldump.exe"
    if (Test-Path $workbenchPath) {
        $mysqldumpPath = $workbenchPath
        Write-Host "✓ mysqldump trouvé dans MySQL Workbench" -ForegroundColor Green
    } else {
        Write-Host "❌ Aucun outil mysqldump trouvé" -ForegroundColor Red
        Write-Host "Solutions:" -ForegroundColor Yellow
        Write-Host "1. Installez MySQL Server: https://dev.mysql.com/downloads/mysql/" -ForegroundColor White
        Write-Host "2. Installez XAMPP: https://www.apachefriends.org/" -ForegroundColor White
        Write-Host "3. Utilisez la solution alternative ci-dessous" -ForegroundColor White
        exit 1
    }
}

# Créer le backup avec mysqldump
try {
    $mysqldumpArgs = @(
        "--host=$host",
        "--port=$port",
        "--user=$user",
        "--password=$password",
        "--single-transaction",
        "--routines",
        "--triggers",
        "--add-drop-database",
        "--create-options",
        "--complete-insert",
        "--extended-insert",
        "--set-charset",
        "--default-character-set=utf8mb4",
        $database
    )
    
    Write-Host "Exécution de mysqldump..." -ForegroundColor Yellow
    
    $process = Start-Process -FilePath $mysqldumpPath -ArgumentList $mysqldumpArgs -RedirectStandardOutput $backupFilePath -RedirectStandardError "error.log" -NoNewWindow -Wait -PassThru
    
    if ($process.ExitCode -eq 0) {
        $fileSize = (Get-Item $backupFilePath).Length
        Write-Host "✅ Backup créé avec succès!" -ForegroundColor Green
        Write-Host "Fichier: $backupFileName" -ForegroundColor White
        Write-Host "Taille: $([math]::Round($fileSize / 1KB, 2)) KB" -ForegroundColor White
        
        # Créer les métadonnées
        $metadata = @{
            FileName = $backupFileName
            CreatedAt = Get-Date
            CreatedBy = $env:USERNAME
            Commentaire = $Commentaire
            FileSize = $fileSize
            Database = $database
            Host = $host
            Port = $port
        }
        
        $metadataPath = Join-Path $OutputPath "metadata_$timestamp.json"
        $metadata | ConvertTo-Json -Depth 10 | Out-File -FilePath $metadataPath -Encoding UTF8
        
        Write-Host "✅ Métadonnées créées: metadata_$timestamp.json" -ForegroundColor Green
    } else {
        $errorContent = Get-Content "error.log" -ErrorAction SilentlyContinue
        Write-Host "❌ Erreur lors du backup:" -ForegroundColor Red
        Write-Host $errorContent -ForegroundColor Red
    }
}
catch {
    Write-Host "❌ Erreur lors du backup: $($_.Exception.Message)" -ForegroundColor Red
}

# Nettoyer les fichiers temporaires
if (Test-Path "error.log") {
    Remove-Item "error.log" -Force
}

Write-Host "=== Backup terminé ===" -ForegroundColor Green
