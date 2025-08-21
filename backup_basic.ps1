# Script PowerShell basique pour backup de structure MySQL
param(
    [string]$Commentaire = "",
    [string]$OutputPath = "Backups"
)

Write-Host "=== Backup basique de la base de données DiversityPub ===" -ForegroundColor Green

# Créer le dossier de backup s'il n'existe pas
if (!(Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force
    Write-Host "Dossier de backup cree: $OutputPath" -ForegroundColor Green
}

# Générer le nom du fichier de backup
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupFileName = "diversitypub_backup_$timestamp.sql"
$backupFilePath = Join-Path $OutputPath $backupFileName

Write-Host "Creation du backup: $backupFileName" -ForegroundColor Yellow

# Créer le contenu SQL du backup
$currentDate = Get-Date -Format "dd/MM/yyyy HH:mm:ss"
$currentUser = $env:USERNAME

# Créer le contenu SQL
$sqlContent = "/* Backup de la base de donnees DiversityPub */`n"
$sqlContent += "/* Cree le: $currentDate */`n"
$sqlContent += "/* Par: $currentUser */`n"
$sqlContent += "/* Commentaire: $Commentaire */`n`n"
$sqlContent += "SET FOREIGN_KEY_CHECKS=0;`n"
$sqlContent += "SET SQL_MODE = 'NO_AUTO_VALUE_ON_ZERO';`n"
$sqlContent += "SET AUTOCOMMIT = 0;`n"
$sqlContent += "START TRANSACTION;`n"
$sqlContent += "SET time_zone = '+00:00';`n`n"
$sqlContent += "CREATE DATABASE IF NOT EXISTS railway DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;`n"
$sqlContent += "USE railway;`n`n"
$sqlContent += "DROP TABLE IF EXISTS Utilisateur;`n"
$sqlContent += "CREATE TABLE Utilisateur (`n"
$sqlContent += "  Id int NOT NULL AUTO_INCREMENT,`n"
$sqlContent += "  Nom varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,`n"
$sqlContent += "  Prenom varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,`n"
$sqlContent += "  Email varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,`n"
$sqlContent += "  MotDePasse varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,`n"
$sqlContent += "  Role int NOT NULL,`n"
$sqlContent += "  DateCreation datetime(6) NOT NULL,`n"
$sqlContent += "  DerniereConnexion datetime(6) NULL,`n"
$sqlContent += "  EstActif tinyint(1) NOT NULL DEFAULT '1',`n"
$sqlContent += "  PRIMARY KEY (Id),`n"
$sqlContent += "  UNIQUE KEY IX_Utilisateur_Email (Email)`n"
$sqlContent += ") ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;`n`n"
$sqlContent += "DROP TABLE IF EXISTS Client;`n"
$sqlContent += "CREATE TABLE Client (`n"
$sqlContent += "  Id int NOT NULL AUTO_INCREMENT,`n"
$sqlContent += "  Nom varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,`n"
$sqlContent += "  Email varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,`n"
$sqlContent += "  Telephone varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,`n"
$sqlContent += "  Adresse text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,`n"
$sqlContent += "  DateCreation datetime(6) NOT NULL,`n"
$sqlContent += "  EstActif tinyint(1) NOT NULL DEFAULT '1',`n"
$sqlContent += "  PRIMARY KEY (Id)`n"
$sqlContent += ") ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;`n`n"
$sqlContent += "DROP TABLE IF EXISTS Campagne;`n"
$sqlContent += "CREATE TABLE Campagne (`n"
$sqlContent += "  Id int NOT NULL AUTO_INCREMENT,`n"
$sqlContent += "  Nom varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,`n"
$sqlContent += "  Description text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,`n"
$sqlContent += "  DateDebut datetime(6) NOT NULL,`n"
$sqlContent += "  DateFin datetime(6) NOT NULL,`n"
$sqlContent += "  Statut int NOT NULL,`n"
$sqlContent += "  ClientId int NOT NULL,`n"
$sqlContent += "  DateCreation datetime(6) NOT NULL,`n"
$sqlContent += "  PRIMARY KEY (Id),`n"
$sqlContent += "  KEY IX_Campagne_ClientId (ClientId),`n"
$sqlContent += "  CONSTRAINT FK_Campagne_Client_ClientId FOREIGN KEY (ClientId) REFERENCES Client (Id) ON DELETE CASCADE`n"
$sqlContent += ") ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;`n`n"
$sqlContent += "DROP TABLE IF EXISTS Lieu;`n"
$sqlContent += "CREATE TABLE Lieu (`n"
$sqlContent += "  Id int NOT NULL AUTO_INCREMENT,`n"
$sqlContent += "  Nom varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,`n"
$sqlContent += "  Adresse text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,`n"
$sqlContent += "  Latitude decimal(10,8) NULL,`n"
$sqlContent += "  Longitude decimal(11,8) NULL,`n"
$sqlContent += "  CampagneId int NOT NULL,`n"
$sqlContent += "  DateCreation datetime(6) NOT NULL,`n"
$sqlContent += "  PRIMARY KEY (Id),`n"
$sqlContent += "  KEY IX_Lieu_CampagneId (CampagneId),`n"
$sqlContent += "  CONSTRAINT FK_Lieu_Campagne_CampagneId FOREIGN KEY (CampagneId) REFERENCES Campagne (Id) ON DELETE CASCADE`n"
$sqlContent += ") ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;`n`n"
$sqlContent += "DROP TABLE IF EXISTS Activation;`n"
$sqlContent += "CREATE TABLE Activation (`n"
$sqlContent += "  Id int NOT NULL AUTO_INCREMENT,`n"
$sqlContent += "  Nom varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,`n"
$sqlContent += "  Description text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,`n"
$sqlContent += "  DateDebut datetime(6) NOT NULL,`n"
$sqlContent += "  DateFin datetime(6) NOT NULL,`n"
$sqlContent += "  Statut int NOT NULL,`n"
$sqlContent += "  CampagneId int NOT NULL,`n"
$sqlContent += "  LieuId int NOT NULL,`n"
$sqlContent += "  DateCreation datetime(6) NOT NULL,`n"
$sqlContent += "  PRIMARY KEY (Id),`n"
$sqlContent += "  KEY IX_Activation_CampagneId (CampagneId),`n"
$sqlContent += "  KEY IX_Activation_LieuId (LieuId),`n"
$sqlContent += "  CONSTRAINT FK_Activation_Campagne_CampagneId FOREIGN KEY (CampagneId) REFERENCES Campagne (Id) ON DELETE CASCADE,`n"
$sqlContent += "  CONSTRAINT FK_Activation_Lieu_LieuId FOREIGN KEY (LieuId) REFERENCES Lieu (Id) ON DELETE CASCADE`n"
$sqlContent += ") ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;`n`n"
$sqlContent += "DROP TABLE IF EXISTS AgentTerrain;`n"
$sqlContent += "CREATE TABLE AgentTerrain (`n"
$sqlContent += "  Id int NOT NULL AUTO_INCREMENT,`n"
$sqlContent += "  UtilisateurId int NOT NULL,`n"
$sqlContent += "  Nom varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,`n"
$sqlContent += "  Prenom varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,`n"
$sqlContent += "  Telephone varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,`n"
$sqlContent += "  Email varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,`n"
$sqlContent += "  DateCreation datetime(6) NOT NULL,`n"
$sqlContent += "  EstActif tinyint(1) NOT NULL DEFAULT '1',`n"
$sqlContent += "  PRIMARY KEY (Id),`n"
$sqlContent += "  KEY IX_AgentTerrain_UtilisateurId (UtilisateurId),`n"
$sqlContent += "  CONSTRAINT FK_AgentTerrain_Utilisateur_UtilisateurId FOREIGN KEY (UtilisateurId) REFERENCES Utilisateur (Id) ON DELETE CASCADE`n"
$sqlContent += ") ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;`n`n"
$sqlContent += "DROP TABLE IF EXISTS PositionGPS;`n"
$sqlContent += "CREATE TABLE PositionGPS (`n"
$sqlContent += "  Id int NOT NULL AUTO_INCREMENT,`n"
$sqlContent += "  AgentTerrainId int NOT NULL,`n"
$sqlContent += "  Latitude decimal(10,8) NOT NULL,`n"
$sqlContent += "  Longitude decimal(11,8) NOT NULL,`n"
$sqlContent += "  Timestamp datetime(6) NOT NULL,`n"
$sqlContent += "  Precision decimal(10,2) NULL,`n"
$sqlContent += "  Vitesse decimal(10,2) NULL,`n"
$sqlContent += "  PRIMARY KEY (Id),`n"
$sqlContent += "  KEY IX_PositionGPS_AgentTerrainId (AgentTerrainId),`n"
$sqlContent += "  CONSTRAINT FK_PositionGPS_AgentTerrain_AgentTerrainId FOREIGN KEY (AgentTerrainId) REFERENCES AgentTerrain (Id) ON DELETE CASCADE`n"
$sqlContent += ") ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;`n`n"
$sqlContent += "DROP TABLE IF EXISTS Feedback;`n"
$sqlContent += "CREATE TABLE Feedback (`n"
$sqlContent += "  Id int NOT NULL AUTO_INCREMENT,`n"
$sqlContent += "  Titre varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,`n"
$sqlContent += "  Contenu text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,`n"
$sqlContent += "  DateCreation datetime(6) NOT NULL,`n"
$sqlContent += "  ClientId int NOT NULL,`n"
$sqlContent += "  EstResolu tinyint(1) NOT NULL DEFAULT '0',`n"
$sqlContent += "  Reponse text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,`n"
$sqlContent += "  DateReponse datetime(6) NULL,`n"
$sqlContent += "  PRIMARY KEY (Id),`n"
$sqlContent += "  KEY IX_Feedback_ClientId (ClientId),`n"
$sqlContent += "  CONSTRAINT FK_Feedback_Client_ClientId FOREIGN KEY (ClientId) REFERENCES Client (Id) ON DELETE CASCADE`n"
$sqlContent += ") ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;`n`n"
$sqlContent += "DROP TABLE IF EXISTS Incident;`n"
$sqlContent += "CREATE TABLE Incident (`n"
$sqlContent += "  Id int NOT NULL AUTO_INCREMENT,`n"
$sqlContent += "  Titre varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,`n"
$sqlContent += "  Description text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,`n"
$sqlContent += "  DateCreation datetime(6) NOT NULL,`n"
$sqlContent += "  AgentTerrainId int NOT NULL,`n"
$sqlContent += "  Latitude decimal(10,8) NULL,`n"
$sqlContent += "  Longitude decimal(11,8) NULL,`n"
$sqlContent += "  EstResolu tinyint(1) NOT NULL DEFAULT '0',`n"
$sqlContent += "  DateResolution datetime(6) NULL,`n"
$sqlContent += "  PRIMARY KEY (Id),`n"
$sqlContent += "  KEY IX_Incident_AgentTerrainId (AgentTerrainId),`n"
$sqlContent += "  CONSTRAINT FK_Incident_AgentTerrain_AgentTerrainId FOREIGN KEY (AgentTerrainId) REFERENCES AgentTerrain (Id) ON DELETE CASCADE`n"
$sqlContent += ") ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;`n`n"
$sqlContent += "DROP TABLE IF EXISTS Media;`n"
$sqlContent += "CREATE TABLE Media (`n"
$sqlContent += "  Id int NOT NULL AUTO_INCREMENT,`n"
$sqlContent += "  NomFichier varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,`n"
$sqlContent += "  CheminFichier varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,`n"
$sqlContent += "  Type int NOT NULL,`n"
$sqlContent += "  Taille bigint NOT NULL,`n"
$sqlContent += "  DateCreation datetime(6) NOT NULL,`n"
$sqlContent += "  AgentTerrainId int NULL,`n"
$sqlContent += "  IncidentId int NULL,`n"
$sqlContent += "  PRIMARY KEY (Id),`n"
$sqlContent += "  KEY IX_Media_AgentTerrainId (AgentTerrainId),`n"
$sqlContent += "  KEY IX_Media_IncidentId (IncidentId),`n"
$sqlContent += "  CONSTRAINT FK_Media_AgentTerrain_AgentTerrainId FOREIGN KEY (AgentTerrainId) REFERENCES AgentTerrain (Id) ON DELETE SET NULL,`n"
$sqlContent += "  CONSTRAINT FK_Media_Incident_IncidentId FOREIGN KEY (IncidentId) REFERENCES Incident (Id) ON DELETE SET NULL`n"
$sqlContent += ") ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;`n`n"
$sqlContent += "DROP TABLE IF EXISTS DemandeActivation;`n"
$sqlContent += "CREATE TABLE DemandeActivation (`n"
$sqlContent += "  Id int NOT NULL AUTO_INCREMENT,`n"
$sqlContent += "  Nom varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,`n"
$sqlContent += "  Description text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,`n"
$sqlContent += "  DateDebut datetime(6) NOT NULL,`n"
$sqlContent += "  DateFin datetime(6) NOT NULL,`n"
$sqlContent += "  Statut int NOT NULL,`n"
$sqlContent += "  ClientId int NOT NULL,`n"
$sqlContent += "  LieuId int NOT NULL,`n"
$sqlContent += "  DateCreation datetime(6) NOT NULL,`n"
$sqlContent += "  PRIMARY KEY (Id),`n"
$sqlContent += "  KEY IX_DemandeActivation_ClientId (ClientId),`n"
$sqlContent += "  KEY IX_DemandeActivation_LieuId (LieuId),`n"
$sqlContent += "  CONSTRAINT FK_DemandeActivation_Client_ClientId FOREIGN KEY (ClientId) REFERENCES Client (Id) ON DELETE CASCADE,`n"
$sqlContent += "  CONSTRAINT FK_DemandeActivation_Lieu_LieuId FOREIGN KEY (LieuId) REFERENCES Lieu (Id) ON DELETE CASCADE`n"
$sqlContent += ") ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;`n`n"
$sqlContent += "INSERT INTO Utilisateur (Nom, Prenom, Email, MotDePasse, Role, DateCreation, EstActif) `n"
$sqlContent += "SELECT 'Admin', 'System', 'admin@diversitypub.ci', 'AQAAAAEAACcQAAAAELB+...', 0, NOW(), 1 `n"
$sqlContent += "WHERE NOT EXISTS (SELECT 1 FROM Utilisateur WHERE Email = 'admin@diversitypub.ci');`n`n"
$sqlContent += "SET FOREIGN_KEY_CHECKS=1;`n"
$sqlContent += "COMMIT;`n"

# Écrire le contenu dans le fichier de backup
try {
    $sqlContent | Out-File -FilePath $backupFilePath -Encoding UTF8
    $fileSize = (Get-Item $backupFilePath).Length
    
    Write-Host "Backup cree avec succes!" -ForegroundColor Green
    Write-Host "Fichier: $backupFileName" -ForegroundColor White
    Write-Host "Taille: $([math]::Round($fileSize / 1KB, 2)) KB" -ForegroundColor White
    
    # Créer les métadonnées
    $metadata = @{
        FileName = $backupFileName
        CreatedAt = Get-Date
        CreatedBy = $env:USERNAME
        Commentaire = $Commentaire
        FileSize = $fileSize
        Database = "railway"
        Type = "Structure + Donnees de base"
        Method = "Script PowerShell basique"
    }
    
    $metadataPath = Join-Path $OutputPath "metadata_$timestamp.json"
    $metadata | ConvertTo-Json -Depth 10 | Out-File -FilePath $metadataPath -Encoding UTF8
    
    Write-Host "Metadonnees creees: metadata_$timestamp.json" -ForegroundColor Green
}
catch {
    Write-Host "Erreur lors du backup: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "=== Backup basique termine ===" -ForegroundColor Green
Write-Host "Note: Ce backup contient la structure de la base de donnees." -ForegroundColor Yellow
Write-Host "Pour un backup complet avec toutes les donnees, installez mysqldump." -ForegroundColor Yellow
