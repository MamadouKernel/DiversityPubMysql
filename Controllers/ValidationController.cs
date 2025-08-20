using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DiversityPub.Data;
using DiversityPub.Models;
using Microsoft.AspNetCore.Authorization;
using DiversityPub.Models.enums;
using OfficeOpenXml;
using System.IO;

namespace DiversityPub.Controllers
{
    [Authorize(Roles = "Admin,ChefProjet")]
    public class ValidationController : Controller
    {
        private readonly DiversityPubDbContext _context;

        public ValidationController(DiversityPubDbContext context)
        {
            _context = context;
        }

        // GET: Validation - Vue principale de validation
        public async Task<IActionResult> Index()
        {
            try
            {
                var activationsEnAttente = await _context.Activations
                    .Include(a => a.Campagne)
                    .Include(a => a.Lieu)
                    .Include(a => a.AgentsTerrain)
                        .ThenInclude(at => at.Utilisateur)
                    .Include(a => a.Medias.Where(m => !m.Valide))
                    .Include(a => a.Incidents.Where(i => i.Statut == "Ouvert" || i.Statut == "EnCours"))
                    .Where(a => a.Statut == StatutActivation.Terminee && !a.PreuvesValidees)
                    .OrderByDescending(a => a.DateActivation)
                    .ToListAsync();

                return View(activationsEnAttente);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Erreur lors du chargement des validations: {ex.Message}";
                return View(new List<Activation>());
            }
        }

        // GET: Validation/Details/5 - Détails d'une activation à valider
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
                return NotFound();

            var activation = await _context.Activations
                .Include(a => a.Campagne)
                .Include(a => a.Lieu)
                .Include(a => a.AgentsTerrain)
                    .ThenInclude(at => at.Utilisateur)
                .Include(a => a.Medias.OrderByDescending(m => m.DateUpload))
                .Include(a => a.Incidents.OrderByDescending(i => i.DateCreation))
                .Include(a => a.Responsable)
                    .ThenInclude(r => r.Utilisateur)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (activation == null)
                return NotFound();

            return View(activation);
        }

        // POST: Validation/ValiderMedia - Valider un média
        [HttpPost]
        public async Task<IActionResult> ValiderMedia(Guid mediaId, bool valide, string? commentaire = null)
        {
            try
            {
                var userEmail = User.Identity?.Name;
                var utilisateur = await _context.Utilisateurs
                    .FirstOrDefaultAsync(u => u.Email == userEmail);

                if (utilisateur == null)
                {
                    return Json(new { success = false, message = "Utilisateur non trouvé." });
                }

                var media = await _context.Medias
                    .Include(m => m.Activation)
                    .FirstOrDefaultAsync(m => m.Id == mediaId);

                if (media == null)
                {
                    return Json(new { success = false, message = "Média non trouvé." });
                }

                media.Valide = valide;
                media.DateValidation = DateTime.Now;
                media.ValideParId = utilisateur.Id;
                media.CommentaireValidation = commentaire;

                await _context.SaveChangesAsync();

                var message = valide ? "Média validé avec succès." : "Média rejeté.";
                return Json(new { success = true, message = message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Erreur lors de la validation: {ex.Message}" });
            }
        }

        // POST: Validation/ValiderActivation - Valider toutes les preuves d'une activation
        [HttpPost]
        public async Task<IActionResult> ValiderActivation(Guid activationId, bool validee, string? commentaire = null)
        {
            try
            {
                var userEmail = User.Identity?.Name;
                var utilisateur = await _context.Utilisateurs
                    .FirstOrDefaultAsync(u => u.Email == userEmail);

                if (utilisateur == null)
                {
                    return Json(new { success = false, message = "Utilisateur non trouvé." });
                }

                var activation = await _context.Activations
                    .Include(a => a.Medias)
                    .FirstOrDefaultAsync(a => a.Id == activationId);

                if (activation == null)
                {
                    return Json(new { success = false, message = "Activation non trouvée." });
                }

                // Valider tous les médias de l'activation
                foreach (var media in activation.Medias.Where(m => !m.Valide))
                {
                    media.Valide = validee;
                    media.DateValidation = DateTime.Now;
                    media.ValideParId = utilisateur.Id;
                    media.CommentaireValidation = commentaire;
                }

                // Marquer l'activation comme validée
                activation.PreuvesValidees = validee;
                activation.DateValidationPreuves = DateTime.Now;
                activation.ValideParId = utilisateur.Id;

                await _context.SaveChangesAsync();

                var message = validee ? "Activation validée avec succès. Les preuves sont maintenant visibles par le client." : "Activation rejetée.";
                return Json(new { success = true, message = message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Erreur lors de la validation: {ex.Message}" });
            }
        }

        // GET: Validation/Media - Vue pour valider les médias
        public async Task<IActionResult> Media()
        {
            try
            {
                var mediasEnAttente = await _context.Medias
                    .Include(m => m.Activation)
                        .ThenInclude(a => a.Campagne)
                    .Include(m => m.Activation)
                        .ThenInclude(a => a.Lieu)
                    .Include(m => m.AgentTerrain)
                        .ThenInclude(at => at.Utilisateur)
                    .Where(m => !m.Valide)
                    .OrderByDescending(m => m.DateUpload)
                    .ToListAsync();

                return View(mediasEnAttente);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Erreur lors du chargement des médias: {ex.Message}";
                return View(new List<Media>());
            }
        }

        // GET: Validation/Incidents - Vue pour gérer les incidents
        public async Task<IActionResult> Incidents()
        {
            try
            {
                var incidents = await _context.Incidents
                    .Include(i => i.Activation)
                        .ThenInclude(a => a.Campagne)
                    .Include(i => i.Activation)
                        .ThenInclude(a => a.Lieu)
                    .Include(i => i.AgentTerrain)
                        .ThenInclude(at => at.Utilisateur)
                    .Where(i => i.Statut == "Ouvert" || i.Statut == "EnCours")
                    .OrderByDescending(i => i.DateCreation)
                    .ToListAsync();

                return View(incidents);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Erreur lors du chargement des incidents: {ex.Message}";
                return View(new List<Incident>());
            }
        }

        // POST: Validation/ResoudreIncident - Résoudre un incident
        [HttpPost]
        public async Task<IActionResult> ResoudreIncident(Guid incidentId, string statut, string? commentaire = null)
        {
            try
            {
                var incident = await _context.Incidents
                    .FirstOrDefaultAsync(i => i.Id == incidentId);

                if (incident == null)
                {
                    return Json(new { success = false, message = "Incident non trouvé." });
                }

                incident.Statut = statut;
                incident.DateResolution = DateTime.Now;
                incident.CommentaireResolution = commentaire;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Incident mis à jour avec succès." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Erreur lors de la mise à jour: {ex.Message}" });
            }
        }

        // GET: Validation/ImportPreuves - Vue pour importer des preuves
        [Authorize(Roles = "Admin")]
        public IActionResult ImportPreuves()
        {
            return View();
        }

        // GET: Validation/DownloadTemplatePreuves - Télécharger le template Excel pour les preuves
        [Authorize(Roles = "Admin")]
        public IActionResult DownloadTemplatePreuves()
        {
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Preuves");
                    
                    // En-têtes
                    var headers = new[] { "ActivationId", "Type", "Description", "Url", "DateUpload", "Valide", "CommentaireValidation" };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        worksheet.Cells[1, i + 1].Value = headers[i];
                        worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                        worksheet.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                    }
                    
                    // Données d'exemple
                    var sampleData = new[]
                    {
                        new { ActivationId = "GUID_ACTIVATION_1", Type = "Photo", Description = "Photo de la mission", Url = "/uploads/photo1.jpg", DateUpload = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Valide = "true", CommentaireValidation = "Preuve validée" },
                        new { ActivationId = "GUID_ACTIVATION_1", Type = "Video", Description = "Vidéo de la mission", Url = "/uploads/video1.mp4", DateUpload = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Valide = "false", CommentaireValidation = "Vidéo floue" },
                        new { ActivationId = "GUID_ACTIVATION_2", Type = "Document", Description = "Rapport de mission", Url = "/uploads/rapport1.pdf", DateUpload = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Valide = "true", CommentaireValidation = "Document complet" }
                    };
                    
                    for (int row = 0; row < sampleData.Length; row++)
                    {
                        var data = sampleData[row];
                        worksheet.Cells[row + 2, 1].Value = data.ActivationId;
                        worksheet.Cells[row + 2, 2].Value = data.Type;
                        worksheet.Cells[row + 2, 3].Value = data.Description;
                        worksheet.Cells[row + 2, 4].Value = data.Url;
                        worksheet.Cells[row + 2, 5].Value = data.DateUpload;
                        worksheet.Cells[row + 2, 6].Value = data.Valide;
                        worksheet.Cells[row + 2, 7].Value = data.CommentaireValidation;
                    }
                    
                    // Feuille d'instructions
                    var instructionsSheet = package.Workbook.Worksheets.Add("Instructions");
                    instructionsSheet.Cells[1, 1].Value = "INSTRUCTIONS D'IMPORT DES PREUVES";
                    instructionsSheet.Cells[1, 1].Style.Font.Bold = true;
                    instructionsSheet.Cells[1, 1].Style.Font.Size = 16;
                    
                    instructionsSheet.Cells[3, 1].Value = "COLONNES OBLIGATOIRES :";
                    instructionsSheet.Cells[3, 1].Style.Font.Bold = true;
                    instructionsSheet.Cells[4, 1].Value = "• ActivationId : ID de l'activation (GUID)";
                    instructionsSheet.Cells[5, 1].Value = "• Type : Photo, Video, ou Document";
                    instructionsSheet.Cells[6, 1].Value = "• Description : Description de la preuve";
                    instructionsSheet.Cells[7, 1].Value = "• Url : Chemin vers le fichier";
                    
                    instructionsSheet.Cells[9, 1].Value = "COLONNES OPTIONNELLES :";
                    instructionsSheet.Cells[9, 1].Style.Font.Bold = true;
                    instructionsSheet.Cells[10, 1].Value = "• DateUpload : Date d'upload (format: yyyy-MM-dd HH:mm:ss)";
                    instructionsSheet.Cells[11, 1].Value = "• Valide : true/false pour la validation";
                    instructionsSheet.Cells[12, 1].Value = "• CommentaireValidation : Commentaire de validation";
                    
                    instructionsSheet.Cells[14, 1].Value = "TYPES DISPONIBLES :";
                    instructionsSheet.Cells[14, 1].Style.Font.Bold = true;
                    instructionsSheet.Cells[15, 1].Value = "• Photo : Images (JPG, PNG, GIF)";
                    instructionsSheet.Cells[16, 1].Value = "• Video : Vidéos (MP4, AVI)";
                    instructionsSheet.Cells[17, 1].Value = "• Document : Documents (PDF, DOC, DOCX)";
                    
                    instructionsSheet.Cells[19, 1].Value = "IMPORTANT :";
                    instructionsSheet.Cells[19, 1].Style.Font.Bold = true;
                    instructionsSheet.Cells[20, 1].Value = "• L'ActivationId doit exister dans la base de données";
                    instructionsSheet.Cells[21, 1].Value = "• Les fichiers doivent être accessibles via l'URL fournie";
                    instructionsSheet.Cells[22, 1].Value = "• Supprimez les lignes d'exemple avant l'import";
                    
                    // Ajuster la largeur des colonnes
                    worksheet.Cells.AutoFitColumns();
                    instructionsSheet.Cells.AutoFitColumns();
                    
                    // Convertir en bytes
                    var bytes = package.GetAsByteArray();
                    
                    return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Template_Preuves.xlsx");
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Erreur lors de la création du template : {ex.Message}");
            }
        }

        // POST: Validation/ImportPreuves - Importer des preuves depuis Excel
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ImportPreuves(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ViewBag.Error = "Veuillez sélectionner un fichier Excel.";
                return View();
            }

            if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
            {
                ViewBag.Error = "Veuillez sélectionner un fichier Excel (.xlsx ou .xls).";
                return View();
            }

            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                var importResults = new List<string>();
                var successCount = 0;
                var errorCount = 0;

                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets[0];
                        var rowCount = worksheet.Dimension?.Rows ?? 0;
                        var colCount = worksheet.Dimension?.Columns ?? 0;

                        // Vérifier les en-têtes
                        var headers = new List<string>();
                        for (int col = 1; col <= colCount; col++)
                        {
                            headers.Add(worksheet.Cells[1, col].Value?.ToString() ?? "");
                        }

                        // Vérifier les colonnes requises
                        var requiredColumns = new[] { "ActivationId", "Type", "Description", "Url" };
                        var missingColumns = requiredColumns.Where(col => !headers.Contains(col)).ToList();

                        if (missingColumns.Any())
                        {
                            ViewBag.Error = $"Colonnes manquantes : {string.Join(", ", missingColumns)}";
                            return View();
                        }

                        // Traiter chaque ligne
                        for (int row = 2; row <= rowCount; row++)
                        {
                            try
                            {
                                var activationIdStr = worksheet.Cells[row, headers.IndexOf("ActivationId") + 1].Value?.ToString();
                                var typeStr = worksheet.Cells[row, headers.IndexOf("Type") + 1].Value?.ToString();
                                var description = worksheet.Cells[row, headers.IndexOf("Description") + 1].Value?.ToString();
                                var url = worksheet.Cells[row, headers.IndexOf("Url") + 1].Value?.ToString();
                                var dateUploadStr = worksheet.Cells[row, headers.IndexOf("DateUpload") + 1].Value?.ToString();
                                var valideStr = worksheet.Cells[row, headers.IndexOf("Valide") + 1].Value?.ToString();
                                var commentaireValidation = worksheet.Cells[row, headers.IndexOf("CommentaireValidation") + 1].Value?.ToString();

                                // Validation des données
                                if (string.IsNullOrEmpty(activationIdStr) || string.IsNullOrEmpty(typeStr) || 
                                    string.IsNullOrEmpty(description) || string.IsNullOrEmpty(url))
                                {
                                    importResults.Add($"❌ Ligne {row}: Données manquantes");
                                    errorCount++;
                                    continue;
                                }

                                // Vérifier que l'activation existe
                                if (!Guid.TryParse(activationIdStr, out var activationId))
                                {
                                    importResults.Add($"❌ Ligne {row}: ActivationId invalide");
                                    errorCount++;
                                    continue;
                                }

                                var activation = await _context.Activations.FindAsync(activationId);
                                if (activation == null)
                                {
                                    importResults.Add($"❌ Ligne {row}: Activation {activationIdStr} non trouvée");
                                    errorCount++;
                                    continue;
                                }

                                // Vérifier le type
                                if (!Enum.TryParse<TypeMedia>(typeStr, out var type))
                                {
                                    importResults.Add($"❌ Ligne {row}: Type {typeStr} invalide");
                                    errorCount++;
                                    continue;
                                }

                                // Créer le média
                                var media = new Media
                                {
                                    Id = Guid.NewGuid(),
                                    ActivationId = activationId,
                                    Type = type,
                                    Description = description,
                                    Url = url,
                                    DateUpload = !string.IsNullOrEmpty(dateUploadStr) && DateTime.TryParse(dateUploadStr, out var dateUpload) 
                                        ? dateUpload : DateTime.Now,
                                    Valide = !string.IsNullOrEmpty(valideStr) && bool.TryParse(valideStr, out var valide) ? valide : false,
                                    CommentaireValidation = commentaireValidation
                                };

                                _context.Medias.Add(media);
                                importResults.Add($"✅ Ligne {row}: Preuve créée pour l'activation {activation.Nom}");
                                successCount++;
                            }
                            catch (Exception ex)
                            {
                                importResults.Add($"❌ Ligne {row}: {ex.Message}");
                                errorCount++;
                            }
                        }

                        await _context.SaveChangesAsync();
                    }
                }

                ViewBag.ImportResults = importResults;
                ViewBag.SuccessCount = successCount;
                ViewBag.ErrorCount = errorCount;
                ViewBag.Success = $"Import terminé : {successCount} preuves créées, {errorCount} erreurs";

                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Erreur lors de l'import : {ex.Message}";
                return View();
            }
        }

        // GET: Validation/ExportPreuves - Exporter les preuves en Excel
        [Authorize(Roles = "Admin,ChefProjet")]
        public async Task<IActionResult> ExportPreuves()
        {
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                
                var preuves = await _context.Medias
                    .Include(m => m.Activation)
                        .ThenInclude(a => a.Campagne)
                    .Include(m => m.AgentTerrain)
                        .ThenInclude(at => at.Utilisateur)
                    .Include(m => m.ValidePar)
                    .OrderByDescending(m => m.DateUpload)
                    .ToListAsync();

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Preuves");
                    
                    // En-têtes
                    var headers = new[] { 
                        "ID", "Activation", "Campagne", "Agent", "Type", "Description", "URL", 
                        "Date Upload", "Validé", "Date Validation", "Validé par", "Commentaire" 
                    };
                    
                    for (int i = 0; i < headers.Length; i++)
                    {
                        worksheet.Cells[1, i + 1].Value = headers[i];
                        worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                        worksheet.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                    }
                    
                    // Données
                    for (int row = 0; row < preuves.Count; row++)
                    {
                        var preuve = preuves[row];
                        var rowIndex = row + 2;
                        
                        worksheet.Cells[rowIndex, 1].Value = preuve.Id.ToString();
                        worksheet.Cells[rowIndex, 2].Value = preuve.Activation?.Nom ?? "";
                        worksheet.Cells[rowIndex, 3].Value = preuve.Activation?.Campagne?.Nom ?? "";
                        worksheet.Cells[rowIndex, 4].Value = preuve.AgentTerrain?.Utilisateur != null 
                            ? $"{preuve.AgentTerrain.Utilisateur.Prenom} {preuve.AgentTerrain.Utilisateur.Nom}" : "";
                        worksheet.Cells[rowIndex, 5].Value = preuve.Type.ToString();
                        worksheet.Cells[rowIndex, 6].Value = preuve.Description ?? "";
                        worksheet.Cells[rowIndex, 7].Value = preuve.Url ?? "";
                        worksheet.Cells[rowIndex, 8].Value = preuve.DateUpload.ToString("yyyy-MM-dd HH:mm:ss");
                        worksheet.Cells[rowIndex, 9].Value = preuve.Valide ? "Oui" : "Non";
                        worksheet.Cells[rowIndex, 10].Value = preuve.DateValidation?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                        worksheet.Cells[rowIndex, 11].Value = preuve.ValidePar != null 
                            ? $"{preuve.ValidePar.Prenom} {preuve.ValidePar.Nom}" : "";
                        worksheet.Cells[rowIndex, 12].Value = preuve.CommentaireValidation ?? "";
                    }
                    
                    // Ajuster la largeur des colonnes
                    worksheet.Cells.AutoFitColumns();
                    
                    // Convertir en bytes
                    var bytes = package.GetAsByteArray();
                    
                    return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Preuves_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Erreur lors de l'export : {ex.Message}");
            }
        }
    }
} 