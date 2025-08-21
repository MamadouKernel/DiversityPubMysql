using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DiversityPub.Data;
using DiversityPub.Models;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;
using System.Text;

namespace DiversityPub.Controllers
{
    [Authorize(Roles = "Admin")]
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
                
                // Commande mysqldump
                var mysqldumpArgs = $"--host={connectionInfo.Host} --port={connectionInfo.Port} --user={connectionInfo.User} --password={connectionInfo.Password} --single-transaction --routines --triggers {connectionInfo.Database} > \"{backupFilePath}\"";

                var processInfo = new ProcessStartInfo
                {
                    FileName = "mysqldump",
                    Arguments = mysqldumpArgs,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    
                    if (process.ExitCode == 0)
                    {
                        // Créer un fichier de métadonnées pour la sauvegarde
                        var metadataPath = Path.Combine(backupPath, $"metadata_{timestamp}.json");
                        var metadata = new
                        {
                            FileName = backupFileName,
                            CreatedAt = DateTime.Now,
                            CreatedBy = User.Identity?.Name,
                            Commentaire = commentaire,
                            FileSize = new FileInfo(backupFilePath).Length,
                            Database = connectionInfo.Database
                        };

                        await System.IO.File.WriteAllTextAsync(metadataPath, System.Text.Json.JsonSerializer.Serialize(metadata, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

                        TempData["Success"] = $"✅ Sauvegarde créée avec succès : {backupFileName}";
                    }
                    else
                    {
                        var error = await process.StandardError.ReadToEndAsync();
                        TempData["Error"] = $"❌ Erreur lors de la sauvegarde : {error}";
                    }
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"❌ Erreur lors de la sauvegarde : {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
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
                var mysqlArgs = $"--host={connectionInfo.Host} --port={connectionInfo.Port} --user={connectionInfo.User} --password={connectionInfo.Password} {connectionInfo.Database} < \"{backupFilePath}\"";

                var processInfo = new ProcessStartInfo
                {
                    FileName = "mysql",
                    Arguments = mysqlArgs,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    
                    if (process.ExitCode == 0)
                    {
                        TempData["Success"] = $"✅ Restauration réussie depuis : {backupFileName}";
                    }
                    else
                    {
                        var error = await process.StandardError.ReadToEndAsync();
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

                // Construire la requête SQL
                var sql = $"TRUNCATE TABLE `{tableName}`";
                var affectedRows = await _context.Database.ExecuteSqlRawAsync(sql);

                return Json(new { success = true, message = $"✅ Table {tableName} vidée avec succès ({affectedRows} lignes affectées)" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"❌ Erreur lors du vidage de la table : {ex.Message}" });
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

                // Vérifier que la requête ne contient pas de commandes dangereuses
                var dangerousKeywords = new[] { "DROP", "DELETE", "TRUNCATE", "ALTER", "CREATE", "INSERT", "UPDATE" };
                var upperQuery = sqlQuery.ToUpper();
                
                if (dangerousKeywords.Any(keyword => upperQuery.Contains(keyword)))
                {
                    return Json(new { success = false, message = "❌ Requête non autorisée (commandes dangereuses détectées)" });
                }

                // Exécuter la requête
                var result = await _context.Database.SqlQueryRaw<object>(sqlQuery).ToListAsync();

                return Json(new { success = true, message = $"✅ Requête exécutée avec succès ({result.Count} résultats)", data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"❌ Erreur lors de l'exécution de la requête : {ex.Message}" });
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
                        CreatedBy = metadata?.GetProperty("CreatedBy").GetString() ?? "Inconnu"
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
