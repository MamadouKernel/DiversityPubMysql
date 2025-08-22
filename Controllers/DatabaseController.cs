using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DiversityPub.Data;
using DiversityPub.Models;
using DiversityPub.Models.enums;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;
using System.Text;

namespace DiversityPub.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class DatabaseController : Controller
    {
        private readonly DiversityPubDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public DatabaseController(DiversityPubDbContext context, IConfiguration configuration, IWebHostEnvironment environment)
        {
            _context = context;
            _configuration = configuration;
            _environment = environment;
        }

        // GET: Database - Vue principale d'administration de la base de données
        public async Task<IActionResult> Index()
        {
            try
            {
                var dbStats = new
                {
                    TotalCampagnes = await _context.Campagnes.CountAsync(),
                    TotalActivations = await _context.Activations.CountAsync(),
                    TotalClients = await _context.Clients.CountAsync(),
                    TotalUtilisateurs = await _context.Utilisateurs.CountAsync(),
                    TotalAgentsTerrain = await _context.AgentsTerrain.CountAsync(),
                    TotalMedias = await _context.Medias.CountAsync(),
                    TotalIncidents = await _context.Incidents.CountAsync(),
                    TotalFeedbacks = await _context.Feedbacks.CountAsync(),
                    TotalLieux = await _context.Lieux.CountAsync(),
                    ActivationsParStatut = await _context.Activations
                        .GroupBy(a => a.Statut)
                        .Select(g => new { Statut = g.Key.ToString(), Count = g.Count() })
                        .ToListAsync(),
                    CampagnesParStatut = await _context.Campagnes
                        .GroupBy(c => c.Statut)
                        .Select(g => new { Statut = g.Key.ToString(), Count = g.Count() })
                        .ToListAsync(),
                    MediasParType = await _context.Medias
                        .GroupBy(m => m.Type)
                        .Select(g => new { Type = g.Key.ToString(), Count = g.Count() })
                        .ToListAsync()
                };

                // Récupérer l'historique des sauvegardes
                var backupHistory = GetBackupHistory();

                ViewBag.DbStats = dbStats;
                ViewBag.BackupHistory = backupHistory;
                ViewBag.ConnectionString = GetMaskedConnectionString();

                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Erreur lors du chargement des statistiques: {ex.Message}";
                return View();
            }
        }

        // POST: Database/Backup - Créer une sauvegarde de la base de données
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Backup(string? commentaire = null)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                var backupPath = Path.Combine(_environment.WebRootPath, "backups");
                
                // Créer le dossier de sauvegarde s'il n'existe pas
                if (!Directory.Exists(backupPath))
                {
                    Directory.CreateDirectory(backupPath);
                }

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupFileName = $"diversitypub_backup_{timestamp}.sql";
                var backupFilePath = Path.Combine(backupPath, backupFileName);

                // Extraire les informations de connexion MySQL
                var connectionInfo = ParseMySqlConnectionString(connectionString);
                
                // Essayer d'abord avec mysqldump
                var mysqldumpSuccess = await TryMySqlDumpBackup(connectionInfo, backupFilePath);
                
                if (!mysqldumpSuccess)
                {
                    // Si mysqldump échoue, créer un backup de structure
                    await CreateStructureBackup(backupFilePath, commentaire);
                }
                        
                        // Créer un fichier de métadonnées pour la sauvegarde
                        var metadataPath = Path.Combine(backupPath, $"metadata_{timestamp}.json");
                        var metadata = new
                        {
                            FileName = backupFileName,
                            CreatedAt = DateTime.Now,
                            CreatedBy = User.Identity?.Name,
                            Commentaire = commentaire,
                            FileSize = new FileInfo(backupFilePath).Length,
                    Database = connectionInfo.Database,
                    Type = mysqldumpSuccess ? "Complet" : "Structure"
                        };

                        await System.IO.File.WriteAllTextAsync(metadataPath, System.Text.Json.JsonSerializer.Serialize(metadata, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

                var message = mysqldumpSuccess 
                    ? $"✅ Sauvegarde complète créée avec succès : {backupFileName}"
                    : $"⚠️ Sauvegarde de structure créée : {backupFileName} (mysqldump non disponible)";
                
                TempData["Success"] = message;

            return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"❌ Erreur lors de la sauvegarde : {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        private async Task<bool> TryMySqlDumpBackup((string Host, int Port, string User, string Password, string Database) connectionInfo, string backupFilePath)
        {
            try
            {
                // Chercher mysqldump dans les chemins possibles
                var mysqldumpPaths = new[]
                {
                    "mysqldump",
                    @"C:\Program Files\MySQL\MySQL Server 8.0\bin\mysqldump.exe",
                    @"C:\Program Files\MySQL\MySQL Server 5.7\bin\mysqldump.exe",
                    @"C:\xampp\mysql\bin\mysqldump.exe",
                    @"C:\wamp64\bin\mysql\mysql8.0.31\bin\mysqldump.exe"
                };

                string? mysqldumpPath = null;
                foreach (var path in mysqldumpPaths)
                {
                    try
                    {
                        var processInfo = new ProcessStartInfo
                        {
                            FileName = path,
                            Arguments = "--version",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        };
                        
                        using var process = Process.Start(processInfo);
                        if (process != null)
                        {
                            await process.WaitForExitAsync();
                            if (process.ExitCode == 0)
                            {
                                mysqldumpPath = path;
                                break;
                            }
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }

                if (mysqldumpPath == null)
                {
                    return false;
                }

                // Commande mysqldump avec redirection de sortie
                var mysqldumpArgs = $"--host={connectionInfo.Host} --port={connectionInfo.Port} --user={connectionInfo.User} --password={connectionInfo.Password} --single-transaction --routines --triggers {connectionInfo.Database}";

                var backupProcessInfo = new ProcessStartInfo
                {
                    FileName = mysqldumpPath,
                    Arguments = mysqldumpArgs,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var backupProcess = Process.Start(backupProcessInfo);
                if (backupProcess != null)
                {
                    // Lire la sortie et l'écrire dans le fichier
                    var output = await backupProcess.StandardOutput.ReadToEndAsync();
                    var error = await backupProcess.StandardError.ReadToEndAsync();
                    
                    await backupProcess.WaitForExitAsync();
                    
                    if (backupProcess.ExitCode == 0 && !string.IsNullOrEmpty(output))
                    {
                        // Écrire le contenu dans le fichier de sauvegarde
                        await System.IO.File.WriteAllTextAsync(backupFilePath, output);
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private async Task CreateStructureBackup(string backupFilePath, string? commentaire)
        {
            var sqlContent = $@"
-- =============================================
-- Backup de la base de données DiversityPub
-- Créé le: {DateTime.Now:dd/MM/yyyy HH:mm:ss}
-- Par: {User.Identity?.Name ?? "Système"}
-- Commentaire: {commentaire ?? ""}
-- Type: Structure de base de données
-- =============================================

SET FOREIGN_KEY_CHECKS=0;
SET SQL_MODE = ""NO_AUTO_VALUE_ON_ZERO"";
SET AUTOCOMMIT = 0;
START TRANSACTION;
SET time_zone = ""+00:00"";

-- =============================================
-- Structure de la base de données
-- =============================================

CREATE DATABASE IF NOT EXISTS `railway` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE `railway`;

-- Table Utilisateur
DROP TABLE IF EXISTS `Utilisateur`;
CREATE TABLE `Utilisateur` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Nom` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `Prenom` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `Email` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `MotDePasse` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `Role` int NOT NULL,
  `DateCreation` datetime(6) NOT NULL,
  `DerniereConnexion` datetime(6) NULL,
  `EstActif` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_Utilisateur_Email` (`Email`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Table Client
DROP TABLE IF EXISTS `Client`;
CREATE TABLE `Client` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Nom` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `Email` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `Telephone` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
  `Adresse` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
  `DateCreation` datetime(6) NOT NULL,
  `EstActif` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Table Campagne
DROP TABLE IF EXISTS `Campagne`;
CREATE TABLE `Campagne` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Nom` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `Description` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
  `DateDebut` datetime(6) NOT NULL,
  `DateFin` datetime(6) NOT NULL,
  `Statut` int NOT NULL,
  `ClientId` int NOT NULL,
  `DateCreation` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Campagne_ClientId` (`ClientId`),
  CONSTRAINT `FK_Campagne_Client_ClientId` FOREIGN KEY (`ClientId`) REFERENCES `Client` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Table Lieu
DROP TABLE IF EXISTS `Lieu`;
CREATE TABLE `Lieu` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Nom` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `Adresse` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
  `Latitude` decimal(10,8) NULL,
  `Longitude` decimal(11,8) NULL,
  `CampagneId` int NOT NULL,
  `DateCreation` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Lieu_CampagneId` (`CampagneId`),
  CONSTRAINT `FK_Lieu_Campagne_CampagneId` FOREIGN KEY (`CampagneId`) REFERENCES `Campagne` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Table Activation
DROP TABLE IF EXISTS `Activation`;
CREATE TABLE `Activation` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Nom` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `Description` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
  `DateDebut` datetime(6) NOT NULL,
  `DateFin` datetime(6) NOT NULL,
  `Statut` int NOT NULL,
  `CampagneId` int NOT NULL,
  `LieuId` int NOT NULL,
  `DateCreation` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Activation_CampagneId` (`CampagneId`),
  KEY `IX_Activation_LieuId` (`LieuId`),
  CONSTRAINT `FK_Activation_Campagne_CampagneId` FOREIGN KEY (`CampagneId`) REFERENCES `Campagne` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_Activation_Lieu_LieuId` FOREIGN KEY (`LieuId`) REFERENCES `Lieu` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Table AgentTerrain
DROP TABLE IF EXISTS `AgentTerrain`;
CREATE TABLE `AgentTerrain` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `UtilisateurId` int NOT NULL,
  `Nom` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `Prenom` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `Telephone` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
  `Email` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
  `DateCreation` datetime(6) NOT NULL,
  `EstActif` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  KEY `IX_AgentTerrain_UtilisateurId` (`UtilisateurId`),
  CONSTRAINT `FK_AgentTerrain_Utilisateur_UtilisateurId` FOREIGN KEY (`UtilisateurId`) REFERENCES `Utilisateur` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Table PositionGPS
DROP TABLE IF EXISTS `PositionGPS`;
CREATE TABLE `PositionGPS` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `AgentTerrainId` int NOT NULL,
  `Latitude` decimal(10,8) NOT NULL,
  `Longitude` decimal(11,8) NOT NULL,
  `Timestamp` datetime(6) NOT NULL,
  `Precision` decimal(10,2) NULL,
  `Vitesse` decimal(10,2) NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_PositionGPS_AgentTerrainId` (`AgentTerrainId`),
  CONSTRAINT `FK_PositionGPS_AgentTerrain_AgentTerrainId` FOREIGN KEY (`AgentTerrainId`) REFERENCES `AgentTerrain` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Table Feedback
DROP TABLE IF EXISTS `Feedback`;
CREATE TABLE `Feedback` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Titre` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `Contenu` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `DateCreation` datetime(6) NOT NULL,
  `ClientId` int NOT NULL,
  `EstResolu` tinyint(1) NOT NULL DEFAULT '0',
  `Reponse` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
  `DateReponse` datetime(6) NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Feedback_ClientId` (`ClientId`),
  CONSTRAINT `FK_Feedback_Client_ClientId` FOREIGN KEY (`ClientId`) REFERENCES `Client` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Table Incident
DROP TABLE IF EXISTS `Incident`;
CREATE TABLE `Incident` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Titre` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `Description` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `DateCreation` datetime(6) NOT NULL,
  `AgentTerrainId` int NOT NULL,
  `Latitude` decimal(10,8) NULL,
  `Longitude` decimal(11,8) NULL,
  `EstResolu` tinyint(1) NOT NULL DEFAULT '0',
  `DateResolution` datetime(6) NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Incident_AgentTerrainId` (`AgentTerrainId`),
  CONSTRAINT `FK_Incident_AgentTerrain_AgentTerrainId` FOREIGN KEY (`AgentTerrainId`) REFERENCES `AgentTerrain` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Table Media
DROP TABLE IF EXISTS `Media`;
CREATE TABLE `Media` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `NomFichier` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `CheminFichier` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `Type` int NOT NULL,
  `Taille` bigint NOT NULL,
  `DateCreation` datetime(6) NOT NULL,
  `AgentTerrainId` int NULL,
  `IncidentId` int NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Media_AgentTerrainId` (`AgentTerrainId`),
  KEY `IX_Media_IncidentId` (`IncidentId`),
  CONSTRAINT `FK_Media_AgentTerrain_AgentTerrainId` FOREIGN KEY (`AgentTerrainId`) REFERENCES `AgentTerrain` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_Media_Incident_IncidentId` FOREIGN KEY (`IncidentId`) REFERENCES `Incident` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Table DemandeActivation
DROP TABLE IF EXISTS `DemandeActivation`;
CREATE TABLE `DemandeActivation` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Nom` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `Description` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL,
  `DateDebut` datetime(6) NOT NULL,
  `DateFin` datetime(6) NOT NULL,
  `Statut` int NOT NULL,
  `ClientId` int NOT NULL,
  `LieuId` int NOT NULL,
  `DateCreation` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_DemandeActivation_ClientId` (`ClientId`),
  KEY `IX_DemandeActivation_LieuId` (`LieuId`),
  CONSTRAINT `FK_DemandeActivation_Client_ClientId` FOREIGN KEY (`ClientId`) REFERENCES `Client` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_DemandeActivation_Lieu_LieuId` FOREIGN KEY (`LieuId`) REFERENCES `Lieu` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- Données de base (si nécessaire)
-- =============================================

-- Insérer un utilisateur admin par défaut si la table est vide
INSERT INTO `Utilisateur` (`Nom`, `Prenom`, `Email`, `MotDePasse`, `Role`, `DateCreation`, `EstActif`) 
SELECT 'Admin', 'System', 'admin@diversitypub.ci', 'AQAAAAEAACcQAAAAELB+...', 0, NOW(), 1
WHERE NOT EXISTS (SELECT 1 FROM `Utilisateur` WHERE `Email` = 'admin@diversitypub.ci');

-- =============================================
-- Fin du backup
-- =============================================

SET FOREIGN_KEY_CHECKS=1;
COMMIT;
";

            await System.IO.File.WriteAllTextAsync(backupFilePath, sqlContent);
        }

        // POST: Database/Restore - Restaurer une sauvegarde
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(string backupFileName)
        {
            try
        {
                if (string.IsNullOrEmpty(backupFileName))
            {
                    TempData["Error"] = "❌ Nom de fichier de sauvegarde requis";
                return RedirectToAction(nameof(Index));
            }

                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                var backupPath = Path.Combine(_environment.WebRootPath, "backups");
                var backupFilePath = Path.Combine(backupPath, backupFileName);

                if (!System.IO.File.Exists(backupFilePath))
                {
                    TempData["Error"] = $"❌ Fichier de sauvegarde non trouvé : {backupFileName}";
                    return RedirectToAction(nameof(Index));
                }

                // Extraire les informations de connexion MySQL
                var connectionInfo = ParseMySqlConnectionString(connectionString);

                // Commande mysql pour la restauration
                var mysqlArgs = $"--host={connectionInfo.Host} --port={connectionInfo.Port} --user={connectionInfo.User} --password={connectionInfo.Password} {connectionInfo.Database}";

                var restoreProcessInfo = new ProcessStartInfo
                {
                    FileName = "mysql",
                    Arguments = mysqlArgs,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var restoreProcess = Process.Start(restoreProcessInfo);
                if (restoreProcess != null)
                {
                    // Lire le contenu du fichier de sauvegarde
                    var backupContent = await System.IO.File.ReadAllTextAsync(backupFilePath);
                    
                    // Écrire le contenu dans l'entrée standard du processus
                    await restoreProcess.StandardInput.WriteAsync(backupContent);
                    await restoreProcess.StandardInput.FlushAsync();
                    restoreProcess.StandardInput.Close();
                    
                    await restoreProcess.WaitForExitAsync();
                    
                    if (restoreProcess.ExitCode == 0)
                    {
                        TempData["Success"] = $"✅ Restauration réussie depuis : {backupFileName}";
                    }
                    else
                    {
                        var error = await restoreProcess.StandardError.ReadToEndAsync();
                        TempData["Error"] = $"❌ Erreur lors de la restauration : {error}";
                    }
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"❌ Erreur lors de la restauration : {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Database/TruncateTable - Vider une table
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TruncateTable(string tableName)
        {
            try
            {
                if (string.IsNullOrEmpty(tableName))
                {
                    return Json(new { success = false, message = "❌ Nom de table requis" });
                }

                // Vérifier que la table existe
                var validTables = new[] { "Medias", "Incidents", "Feedbacks", "Activations", "Campagnes", "Clients", "Utilisateurs", "AgentsTerrain", "Lieux" };
                
                if (!validTables.Contains(tableName))
                {
                    return Json(new { success = false, message = $"❌ Table non autorisée : {tableName}" });
                }

                // Désactiver temporairement les vérifications de clés étrangères
                await _context.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 0");

                try
                {
                    // Construire la requête SQL
                    var sql = $"TRUNCATE TABLE `{tableName}`";
                    var affectedRows = await _context.Database.ExecuteSqlRawAsync(sql);

                    return Json(new { success = true, message = $"✅ Table {tableName} vidée avec succès ({affectedRows} lignes affectées)" });
                }
                finally
                {
                    // Réactiver les vérifications de clés étrangères
                    await _context.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 1");
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"❌ Erreur lors du vidage de la table : {ex.Message}" });
            }
        }

        // POST: Database/TruncateAllTables - Vider toutes les tables
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TruncateAllTables()
        {
            try
            {
                // Liste des tables dans l'ordre de suppression (en respectant les contraintes de clés étrangères)
                var tablesToTruncate = new[]
                {
                    "Medias",
                    "PositionGPS", 
                    "Incidents",
                    "Feedbacks",
                    "ActivationAgentTerrain",
                    "Activations",
                    "DemandeActivation",
                    "AgentsTerrain",
                    "Lieux",
                    "Campagnes",
                    "Clients",
                    "Utilisateurs"
                };

                // Désactiver temporairement les vérifications de clés étrangères
                await _context.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 0");

                var results = new List<object>();
                var totalAffectedRows = 0;

                try
                {
                    foreach (var tableName in tablesToTruncate)
                    {
                        try
                        {
                            // Vérifier si la table existe
                            var tableExists = await _context.Database.ExecuteSqlRawAsync(
                                $"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = '{tableName}'");
                            
                            if (tableExists > 0)
                            {
                                // Compter les lignes avant suppression
                                var countBefore = await _context.Database.ExecuteSqlRawAsync($"SELECT COUNT(*) FROM `{tableName}`");
                                
                                // Truncater la table
                                var sql = $"TRUNCATE TABLE `{tableName}`";
                                await _context.Database.ExecuteSqlRawAsync(sql);
                                
                                results.Add(new { 
                                    table = tableName, 
                                    success = true, 
                                    affectedRows = countBefore,
                                    message = $"✅ Table {tableName} vidée ({countBefore} lignes supprimées)" 
                                });
                                
                                totalAffectedRows += (int)countBefore;
                            }
                            else
                            {
                                results.Add(new { 
                                    table = tableName, 
                                    success = false, 
                                    affectedRows = 0,
                                    message = $"⚠️ Table {tableName} n'existe pas" 
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            results.Add(new { 
                                table = tableName, 
                                success = false, 
                                affectedRows = 0,
                                message = $"❌ Erreur pour {tableName}: {ex.Message}" 
                            });
                        }
                    }

                    TempData["Success"] = $"✅ Vidage général terminé ! Total: {totalAffectedRows} lignes supprimées";
                    return RedirectToAction(nameof(Index));
                }
                finally
                {
                    // Réactiver les vérifications de clés étrangères
                    await _context.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 1");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"❌ Erreur lors du vidage général : {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Database/ExecuteQuery - Exécuter une requête SQL personnalisée
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecuteQuery(string sqlQuery)
        {
            try
            {
                if (string.IsNullOrEmpty(sqlQuery))
                {
                    return Json(new { success = false, message = "❌ Requête SQL requise" });
                }

                // Vérifier que c'est une requête SELECT
                if (!sqlQuery.Trim().ToUpper().StartsWith("SELECT"))
                {
                    return Json(new { success = false, message = "❌ Seules les requêtes SELECT sont autorisées" });
                }

                // Exécuter la requête
                var result = await _context.Database.SqlQueryRaw<object>(sqlQuery).ToListAsync();

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"❌ Erreur lors de l'exécution : {ex.Message}" });
            }
        }

        // POST: Database/CreateUser - Créer un nouvel utilisateur
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(string nom, string prenom, string email, string motDePasse, int role)
        {
            try
            {
                // Vérifier que l'email n'existe pas déjà
                var existingUser = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.Email == email);
                if (existingUser != null)
                {
                    TempData["Error"] = "❌ Un utilisateur avec cet email existe déjà";
                    return RedirectToAction(nameof(Index));
                }

                // Créer le nouvel utilisateur
                var newUser = new Utilisateur
                {
                    Id = Guid.NewGuid(),
                    Nom = nom,
                    Prenom = prenom,
                    Email = email,
                    MotDePasse = BCrypt.Net.BCrypt.HashPassword(motDePasse),
                    Role = (Role)role,
                    Supprimer = 0
                };

                _context.Utilisateurs.Add(newUser);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"✅ Utilisateur {nom} {prenom} créé avec succès";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"❌ Erreur lors de la création de l'utilisateur : {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Database/GetUsers - Récupérer la liste des utilisateurs
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _context.Utilisateurs
                    .Select(u => new
                    {
                        u.Id,
                        u.Nom,
                        u.Prenom,
                        u.Email,
                        u.Role,
                        u.Supprimer
                    })
                    .OrderBy(u => u.Nom)
                    .ThenBy(u => u.Prenom)
                    .ToListAsync();

                return Json(new { success = true, data = users });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"❌ Erreur : {ex.Message}" });
            }
        }

        // GET: Database/DownloadBackup - Télécharger une sauvegarde
        public IActionResult DownloadBackup(string fileName)
        {
            try
            {
                var backupPath = Path.Combine(_environment.WebRootPath, "backups");
                var filePath = Path.Combine(backupPath, fileName);

                if (!System.IO.File.Exists(filePath))
                {
                    TempData["Error"] = $"❌ Fichier non trouvé : {fileName}";
                    return RedirectToAction(nameof(Index));
                }

                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                return File(fileBytes, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"❌ Erreur lors du téléchargement : {ex.Message}";
                    return RedirectToAction(nameof(Index));
                }
        }

        // POST: Database/DeleteBackup - Supprimer une sauvegarde
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteBackup(string fileName)
        {
            try
            {
                var backupPath = Path.Combine(_environment.WebRootPath, "backups");
                var filePath = Path.Combine(backupPath, fileName);
                var metadataPath = Path.Combine(backupPath, $"metadata_{fileName.Replace(".sql", "").Replace("diversitypub_backup_", "")}.json");

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                if (System.IO.File.Exists(metadataPath))
                {
                    System.IO.File.Delete(metadataPath);
                }

                TempData["Success"] = $"✅ Sauvegarde supprimée : {fileName}";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"❌ Erreur lors de la suppression : {ex.Message}";
            return RedirectToAction(nameof(Index));
            }
        }

        // Méthodes privées
        private List<dynamic> GetBackupHistory()
        {
            var backupPath = Path.Combine(_environment.WebRootPath, "backups");
            var backups = new List<dynamic>();

            if (Directory.Exists(backupPath))
            {
                var backupFiles = Directory.GetFiles(backupPath, "diversitypub_backup_*.sql");
                
                foreach (var file in backupFiles.OrderByDescending(f => f))
                {
                    var fileInfo = new FileInfo(file);
                    var fileName = Path.GetFileName(file);
                    var timestamp = fileName.Replace("diversitypub_backup_", "").Replace(".sql", "");
                    
                    // Chercher les métadonnées
                    var metadataPath = Path.Combine(backupPath, $"metadata_{timestamp}.json");
                    dynamic? metadata = null;
                    
                    if (System.IO.File.Exists(metadataPath))
                    {
                        try
                        {
                            var metadataJson = System.IO.File.ReadAllText(metadataPath);
                            metadata = System.Text.Json.JsonSerializer.Deserialize<dynamic>(metadataJson);
                        }
                        catch
                        {
                            // Ignorer les erreurs de métadonnées
                        }
                    }

                    backups.Add(new
                    {
                        FileName = fileName,
                        FileSize = fileInfo.Length,
                        CreatedAt = fileInfo.CreationTime,
                        Commentaire = metadata?.GetProperty("Commentaire").GetString() ?? "",
                        CreatedBy = metadata?.GetProperty("CreatedBy").GetString() ?? "Inconnu",
                        Type = metadata?.GetProperty("Type").GetString() ?? "Structure"
                    });
                }
            }

            return backups;
        }

        private string GetMaskedConnectionString()
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
                return "Non configuré";

            // Masquer le mot de passe
            var masked = connectionString.Replace("Password=", "Password=***");
            return masked;
        }

        private (string Host, int Port, string User, string Password, string Database) ParseMySqlConnectionString(string connectionString)
        {
            var parts = connectionString.Split(';');
            var host = "localhost";
            var port = 3306;
            var user = "";
                var password = "";
            var database = "";

            foreach (var part in parts)
                {
                    var keyValue = part.Split('=');
                    if (keyValue.Length == 2)
                    {
                        var key = keyValue[0].Trim().ToLower();
                        var value = keyValue[1].Trim();

                        switch (key)
                        {
                            case "server":
                            case "host":
                            host = value;
                                break;
                            case "port":
                            if (int.TryParse(value, out var portValue))
                                port = portValue;
                                break;
                            case "user id":
                            case "uid":
                            case "user":
                            user = value;
                                break;
                            case "password":
                            case "pwd":
                                password = value;
                                break;
                            case "database":
                            case "initial catalog":
                                database = value;
                                break;
                        }
                    }
                }
                
            return (host, port, user, password, database);
        }
    }
}
