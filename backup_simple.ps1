# Script PowerShell simple pour backup de structure MySQL
param(
    [string]$Commentaire = "",
    [string]$OutputPath = "Backups"
)

Write-Host "=== Backup simple de la base de données DiversityPub ===" -ForegroundColor Green

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

# Créer le contenu SQL du backup
$currentDate = Get-Date -Format "dd/MM/yyyy HH:mm:ss"
$currentUser = $env:USERNAME

$sqlContent = @"
-- =============================================
-- Backup de la base de données DiversityPub
-- Créé le: $currentDate
-- Par: $currentUser
-- Commentaire: $Commentaire
-- Type: Structure de base de données
-- =============================================

SET FOREIGN_KEY_CHECKS=0;
SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
SET AUTOCOMMIT = 0;
START TRANSACTION;
SET time_zone = "+00:00";

-- Créer la base de données
CREATE DATABASE IF NOT EXISTS \`railway\` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE \`railway\`;

-- Table Utilisateur
DROP TABLE IF EXISTS \`Utilisateur\`;
CREATE TABLE \`Utilisateur\` (
  \`Id\` int NOT NULL AUTO_INCREMENT,
  \`Nom\` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  \`Prenom\` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  \`Email\` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  \`MotDePasse\` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  \`Role\` int NOT NULL,
  \`DateCreation\` datetime(6) NOT NULL,
  \`DerniereConnexion\` datetime(6) NULL,
  \`EstActif\` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (\`Id\`),
  UNIQUE KEY \`IX_Utilisateur_Email\` (\`Email\`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Table Client
DROP TABLE IF EXISTS \`Client\`;
CREATE TABLE \`Client\` (
  \`Id\` int NOT NULL AUTO_INCREMENT,
  \`Nom\` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  \`Email\` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  \`Telephone\` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
  \`Adresse\` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
  \`DateCreation\` datetime(6) NOT NULL,
  \`EstActif\` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (\`Id\`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Table Campagne
DROP TABLE IF EXISTS \`Campagne\`;
CREATE TABLE \`Campagne\` (
  \`Id\` int NOT NULL AUTO_INCREMENT,
  \`Nom\` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  \`Description\` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
  \`DateDebut\` datetime(6) NOT NULL,
  \`DateFin\` datetime(6) NOT NULL,
  \`Statut\` int NOT NULL,
  \`ClientId\` int NOT NULL,
  \`DateCreation\` datetime(6) NOT NULL,
  PRIMARY KEY (\`Id\`),
  KEY \`IX_Campagne_ClientId\` (\`ClientId\`),
  CONSTRAINT \`FK_Campagne_Client_ClientId\` FOREIGN KEY (\`ClientId\`) REFERENCES \`Client\` (\`Id\`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Table Lieu
DROP TABLE IF EXISTS \`Lieu\`;
CREATE TABLE \`Lieu\` (
  \`Id\` int NOT NULL AUTO_INCREMENT,
  \`Nom\` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  \`Adresse\` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
  \`Latitude\` decimal(10,8) NULL,
  \`Longitude\` decimal(11,8) NULL,
  \`CampagneId\` int NOT NULL,
  \`DateCreation\` datetime(6) NOT NULL,
  PRIMARY KEY (\`Id\`),
  KEY \`IX_Lieu_CampagneId\` (\`CampagneId\`),
  CONSTRAINT \`FK_Lieu_Campagne_CampagneId\` FOREIGN KEY (\`CampagneId\`) REFERENCES \`Campagne\` (\`Id\`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Table Activation
DROP TABLE IF EXISTS \`Activation\`;
CREATE TABLE \`Activation\` (
  \`Id\` int NOT NULL AUTO_INCREMENT,
  \`Nom\` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  \`Description\` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
  \`DateDebut\` datetime(6) NOT NULL,
  \`DateFin\` datetime(6) NOT NULL,
  \`Statut\` int NOT NULL,
  \`CampagneId\` int NOT NULL,
  \`LieuId\` int NOT NULL,
  \`DateCreation\` datetime(6) NOT NULL,
  PRIMARY KEY (\`Id\`),
  KEY \`IX_Activation_CampagneId\` (\`CampagneId\`),
  KEY \`IX_Activation_LieuId\` (\`LieuId\`),
  CONSTRAINT \`FK_Activation_Campagne_CampagneId\` FOREIGN KEY (\`CampagneId\`) REFERENCES \`Campagne\` (\`Id\`) ON DELETE CASCADE,
  CONSTRAINT \`FK_Activation_Lieu_LieuId\` FOREIGN KEY (\`LieuId\`) REFERENCES \`Lieu\` (\`Id\`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Table AgentTerrain
DROP TABLE IF EXISTS \`AgentTerrain\`;
CREATE TABLE \`AgentTerrain\` (
  \`Id\` int NOT NULL AUTO_INCREMENT,
  \`UtilisateurId\` int NOT NULL,
  \`Nom\` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  \`Prenom\` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  \`Telephone\` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
  \`Email\` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
  \`DateCreation\` datetime(6) NOT NULL,
  \`EstActif\` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (\`Id\`),
  KEY \`IX_AgentTerrain_UtilisateurId\` (\`UtilisateurId\`),
  CONSTRAINT \`FK_AgentTerrain_Utilisateur_UtilisateurId\` FOREIGN KEY (\`UtilisateurId\`) REFERENCES \`Utilisateur\` (\`Id\`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Table PositionGPS
DROP TABLE IF EXISTS \`PositionGPS\`;
CREATE TABLE \`PositionGPS\` (
  \`Id\` int NOT NULL AUTO_INCREMENT,
  \`AgentTerrainId\` int NOT NULL,
  \`Latitude\` decimal(10,8) NOT NULL,
  \`Longitude\` decimal(11,8) NOT NULL,
  \`Timestamp\` datetime(6) NOT NULL,
  \`Precision\` decimal(10,2) NULL,
  \`Vitesse\` decimal(10,2) NULL,
  PRIMARY KEY (\`Id\`),
  KEY \`IX_PositionGPS_AgentTerrainId\` (\`AgentTerrainId\`),
  CONSTRAINT \`FK_PositionGPS_AgentTerrain_AgentTerrainId\` FOREIGN KEY (\`AgentTerrainId\`) REFERENCES \`AgentTerrain\` (\`Id\`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Table Feedback
DROP TABLE IF EXISTS \`Feedback\`;
CREATE TABLE \`Feedback\` (
  \`Id\` int NOT NULL AUTO_INCREMENT,
  \`Titre\` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  \`Contenu\` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  \`DateCreation\` datetime(6) NOT NULL,
  \`ClientId\` int NOT NULL,
  \`EstResolu\` tinyint(1) NOT NULL DEFAULT '0',
  \`Reponse\` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
  \`DateReponse\` datetime(6) NULL,
  PRIMARY KEY (\`Id\`),
  KEY \`IX_Feedback_ClientId\` (\`ClientId\`),
  CONSTRAINT \`FK_Feedback_Client_ClientId\` FOREIGN KEY (\`ClientId\`) REFERENCES \`Client\` (\`Id\`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Table Incident
DROP TABLE IF EXISTS \`Incident\`;
CREATE TABLE \`Incident\` (
  \`Id\` int NOT NULL AUTO_INCREMENT,
  \`Titre\` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  \`Description\` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  \`DateCreation\` datetime(6) NOT NULL,
  \`AgentTerrainId\` int NOT NULL,
  \`Latitude\` decimal(10,8) NULL,
  \`Longitude\` decimal(11,8) NULL,
  \`EstResolu\` tinyint(1) NOT NULL DEFAULT '0',
  \`DateResolution\` datetime(6) NULL,
  PRIMARY KEY (\`Id\`),
  KEY \`IX_Incident_AgentTerrainId\` (\`AgentTerrainId\`),
  CONSTRAINT \`FK_Incident_AgentTerrain_AgentTerrainId\` FOREIGN KEY (\`AgentTerrainId\`) REFERENCES \`AgentTerrain\` (\`Id\`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Table Media
DROP TABLE IF EXISTS \`Media\`;
CREATE TABLE \`Media\` (
  \`Id\` int NOT NULL AUTO_INCREMENT,
  \`NomFichier\` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  \`CheminFichier\` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  \`Type\` int NOT NULL,
  \`Taille\` bigint NOT NULL,
  \`DateCreation\` datetime(6) NOT NULL,
  \`AgentTerrainId\` int NULL,
  \`IncidentId\` int NULL,
  PRIMARY KEY (\`Id\`),
  KEY \`IX_Media_AgentTerrainId\` (\`AgentTerrainId\`),
  KEY \`IX_Media_IncidentId\` (\`IncidentId\`),
  CONSTRAINT \`FK_Media_AgentTerrain_AgentTerrainId\` FOREIGN KEY (\`AgentTerrainId\`) REFERENCES \`AgentTerrain\` (\`Id\`) ON DELETE SET NULL,
  CONSTRAINT \`FK_Media_Incident_IncidentId\` FOREIGN KEY (\`IncidentId\`) REFERENCES \`Incident\` (\`Id\`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Table DemandeActivation
DROP TABLE IF EXISTS \`DemandeActivation\`;
CREATE TABLE \`DemandeActivation\` (
  \`Id\` int NOT NULL AUTO_INCREMENT,
  \`Nom\` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  \`Description\` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
  \`DateDebut\` datetime(6) NOT NULL,
  \`DateFin\` datetime(6) NOT NULL,
  \`Statut\` int NOT NULL,
  \`ClientId\` int NOT NULL,
  \`LieuId\` int NOT NULL,
  \`DateCreation\` datetime(6) NOT NULL,
  PRIMARY KEY (\`Id\`),
  KEY \`IX_DemandeActivation_ClientId\` (\`ClientId\`),
  KEY \`IX_DemandeActivation_LieuId\` (\`LieuId\`),
  CONSTRAINT \`FK_DemandeActivation_Client_ClientId\` FOREIGN KEY (\`ClientId\`) REFERENCES \`Client\` (\`Id\`) ON DELETE CASCADE,
  CONSTRAINT \`FK_DemandeActivation_Lieu_LieuId\` FOREIGN KEY (\`LieuId\`) REFERENCES \`Lieu\` (\`Id\`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Données de base
INSERT INTO \`Utilisateur\` (\`Nom\`, \`Prenom\`, \`Email\`, \`MotDePasse\`, \`Role\`, \`DateCreation\`, \`EstActif\`) 
SELECT 'Admin', 'System', 'admin@diversitypub.ci', 'AQAAAAEAACcQAAAAELB+...', 0, NOW(), 1
WHERE NOT EXISTS (SELECT 1 FROM \`Utilisateur\` WHERE \`Email\` = 'admin@diversitypub.ci');

SET FOREIGN_KEY_CHECKS=1;
COMMIT;
"@

# Écrire le contenu dans le fichier de backup
try {
    $sqlContent | Out-File -FilePath $backupFilePath -Encoding UTF8
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
        Database = "railway"
        Type = "Structure + Données de base"
        Method = "Script PowerShell simple"
    }
    
    $metadataPath = Join-Path $OutputPath "metadata_$timestamp.json"
    $metadata | ConvertTo-Json -Depth 10 | Out-File -FilePath $metadataPath -Encoding UTF8
    
    Write-Host "✅ Métadonnées créées: metadata_$timestamp.json" -ForegroundColor Green
}
catch {
    Write-Host "❌ Erreur lors du backup: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "=== Backup simple terminé ===" -ForegroundColor Green
Write-Host "Note: Ce backup contient la structure de la base de données." -ForegroundColor Yellow
Write-Host "Pour un backup complet avec toutes les données, installez mysqldump." -ForegroundColor Yellow
