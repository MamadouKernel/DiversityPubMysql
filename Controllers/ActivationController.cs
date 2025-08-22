using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DiversityPub.Data;
using DiversityPub.Models;
using DiversityPub.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace DiversityPub.Controllers
{
    [Authorize(Roles = "Admin,ChefProjet,SuperAdmin")]
    public class ActivationController : Controller
    {
        private readonly DiversityPubDbContext _context;
        private readonly ICampagneStatusService _campagneStatusService;
        private readonly IActivationValidationService _validationService;

        public ActivationController(DiversityPubDbContext context, ICampagneStatusService campagneStatusService, IActivationValidationService validationService)
        {
            _context = context;
            _campagneStatusService = campagneStatusService;
            _validationService = validationService;
        }

        // GET: Activation
        public async Task<IActionResult> Index()
        {
            try
            {
                var activations = await _context.Activations
                    .Include(a => a.Campagne)
                        .ThenInclude(c => c.Client)
                    .Include(a => a.Lieu)
                    .Include(a => a.Responsable)
                        .ThenInclude(r => r.Utilisateur)
                    .Include(a => a.AgentsTerrain)
                        .ThenInclude(at => at.Utilisateur)
                    .Include(a => a.Medias)
                    .Include(a => a.Incidents)
                    .Include(a => a.Feedbacks)
                    .OrderByDescending(a => a.DateCreation)
                    .ToListAsync();

                return View(activations);
            }
            catch (Exception ex)
            {
                return View("Error", new { Message = $"Erreur lors du chargement des activations: {ex.Message}" });
            }
        }

        // GET: Activation/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
                return NotFound();

            var activation = await _context.Activations
                .Include(a => a.Campagne)
                    .ThenInclude(c => c.Client)
                .Include(a => a.Lieu)
                .Include(a => a.Responsable)
                    .ThenInclude(r => r.Utilisateur)
                .Include(a => a.AgentsTerrain)
                    .ThenInclude(at => at.Utilisateur)
                .Include(a => a.Medias)
                .Include(a => a.Incidents)
                .Include(a => a.Feedbacks)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (activation == null)
                return NotFound();

            return View(activation);
        }

        // GET: Activation/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                // Récupérer les campagnes avec plus de détails
                var campagnes = await _context.Campagnes
                    .Include(c => c.Client)
                    .Where(c => c.Statut == DiversityPub.Models.enums.StatutCampagne.EnCours || 
                                c.Statut == DiversityPub.Models.enums.StatutCampagne.EnPreparation)
                    .OrderByDescending(c => c.DateCreation)
                    .ToListAsync();

                // Récupérer les lieux avec plus d'informations
                var lieux = await _context.Lieux
                    .OrderBy(l => l.Nom)
                    .ToListAsync();

                // Récupérer les agents terrain avec leurs compétences
                var agentsTerrain = await _context.AgentsTerrain
                    .Include(at => at.Utilisateur)
                    .OrderBy(at => at.Utilisateur.Nom ?? "")
                    .ThenBy(at => at.Utilisateur.Prenom ?? "")
                    .ToListAsync();

                // Récupérer les responsables (agents terrain qui peuvent être responsables)
                var responsables = await _context.AgentsTerrain
                    .Include(at => at.Utilisateur)
                    .OrderBy(at => at.Utilisateur.Nom ?? "")
                    .ThenBy(at => at.Utilisateur.Prenom ?? "")
                    .ToListAsync();

                // Récupérer les activations existantes pour validation des conflits
                var activationsExistantes = await _context.Activations
                    .Include(a => a.Lieu)
                    .Include(a => a.Responsable)
                    .Where(a => a.DateActivation >= DateTime.Today)
                    .ToListAsync();

                ViewBag.Campagnes = campagnes;
                ViewBag.Lieux = lieux;
                ViewBag.AgentsTerrain = agentsTerrain;
                ViewBag.Responsables = responsables;
                ViewBag.ActivationsExistantes = activationsExistantes;
                ViewBag.StatutsActivation = Enum.GetValues(typeof(DiversityPub.Models.enums.StatutActivation));

                return View();
            }
            catch (Exception ex)
            {
                return View("Error", new { Message = $"Erreur lors du chargement des données: {ex.Message}" });
            }
        }

        // POST: Activation/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Description,Instructions,DateActivation,HeureDebut,HeureFin,CampagneId,LieuId,ResponsableId")] Activation activation, List<Guid> AgentsTerrainIds)
        {
            try
            {
                // Validation avec le service
                var validationErrors = new List<string>();

                // 1. Validation de la date par rapport à la campagne
                var campagneError = await _validationService.ValidateCampagneDateAsync(activation.CampagneId, activation.DateActivation);
                if (campagneError != null)
                {
                    ModelState.AddModelError("DateActivation", campagneError);
                }

                // 2. Validation des heures
                var heuresError = _validationService.ValidateHeuresAsync(activation.HeureDebut, activation.HeureFin);
                if (heuresError != null)
                {
                    ModelState.AddModelError("HeureFin", heuresError);
                }

                // 3. Validation des conflits de lieu
                var lieuErrors = await _validationService.ValidateLieuAvailabilityAsync(activation.LieuId, activation.DateActivation, activation.HeureDebut, activation.HeureFin);
                foreach (var error in lieuErrors)
                {
                    ModelState.AddModelError("HeureDebut", error);
                }

                // 4. Validation des conflits de responsable
                if (activation.ResponsableId.HasValue)
                {
                    var responsableErrors = await _validationService.ValidateResponsableAvailabilityAsync(activation.ResponsableId.Value, activation.DateActivation, activation.HeureDebut, activation.HeureFin);
                    foreach (var error in responsableErrors)
                    {
                        ModelState.AddModelError("ResponsableId", error);
                    }
                }

                // 5. Validation des conflits d'agents terrain (IMPORTANT !)
                if (AgentsTerrainIds != null && AgentsTerrainIds.Any())
                {
                    Console.WriteLine($"🔍 Validation des agents terrain: {AgentsTerrainIds.Count} agents sélectionnés");
                    var agentErrors = await _validationService.ValidateAgentAvailabilityAsync(AgentsTerrainIds, activation.DateActivation, activation.HeureDebut, activation.HeureFin);
                    Console.WriteLine($"🔍 Résultat validation: {agentErrors.Count} erreurs trouvées");
                    
                    foreach (var error in agentErrors)
                    {
                        Console.WriteLine($"❌ Erreur de validation: {error}");
                        ModelState.AddModelError("AgentsTerrainIds", error);
                    }
                }
                else
                {
                    Console.WriteLine("⚠️ Aucun agent terrain sélectionné");
                }

                Console.WriteLine($"🔍 Validation ModelState: {ModelState.IsValid}");
                if (!ModelState.IsValid)
                {
                    Console.WriteLine("❌ Erreurs de validation détectées:");
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        Console.WriteLine($"❌ {error.ErrorMessage}");
                    }
                }

                if (ModelState.IsValid)
                {
                    activation.Id = Guid.NewGuid();
                    activation.DateCreation = DateTime.Now;
                    activation.Statut = DiversityPub.Models.enums.StatutActivation.Planifiee;

                    // Générer automatiquement le nom s'il est vide
                    if (string.IsNullOrWhiteSpace(activation.Nom))
                    {
                        var campagneNom = await _context.Campagnes
                            .Include(c => c.Client)
                            .FirstOrDefaultAsync(c => c.Id == activation.CampagneId);
                        var lieu = await _context.Lieux
                            .FirstOrDefaultAsync(l => l.Id == activation.LieuId);

                        if (campagneNom != null && lieu != null)
                        {
                            var periode = $"({campagneNom.DateDebut:dd/MM/yyyy} - {campagneNom.DateFin:dd/MM/yyyy})";
                            activation.Nom = $"{lieu.Nom}-{lieu.Adresse}-{campagneNom.Nom} {periode}";
                        }
                        else
                        {
                            activation.Nom = $"Activation-{DateTime.Now:yyyyMMdd_HHmmss}";
                        }
                    }

                    _context.Add(activation);
                    await _context.SaveChangesAsync();

                    // Associer les agents terrain sélectionnés (optionnel)
                    if (AgentsTerrainIds != null && AgentsTerrainIds.Any())
                    {
                        var agentsSelectionnes = await _context.AgentsTerrain
                            .Where(at => AgentsTerrainIds.Contains(at.Id))
                            .ToListAsync();

                        foreach (var agent in agentsSelectionnes)
                        {
                            activation.AgentsTerrain.Add(agent);
                        }
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        // Aucun agent sélectionné - c'est autorisé
                        Console.WriteLine("Aucun agent terrain sélectionné pour cette activation");
                    }

                    TempData["Success"] = "✅ Activation créée avec succès !";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Erreur lors de la création: {ex.Message}");
            }

            // Recharger les données en cas d'erreur
            var campagnes = await _context.Campagnes
                .Include(c => c.Client)
                .Where(c => c.Statut == DiversityPub.Models.enums.StatutCampagne.EnCours || 
                            c.Statut == DiversityPub.Models.enums.StatutCampagne.EnPreparation)
                .OrderByDescending(c => c.DateCreation)
                .ToListAsync();

            var lieux = await _context.Lieux
                .OrderBy(l => l.Nom)
                .ToListAsync();

            var agentsTerrain = await _context.AgentsTerrain
                .Include(at => at.Utilisateur)
                .OrderBy(at => at.Utilisateur.Nom)
                .ThenBy(at => at.Utilisateur.Prenom)
                .ToListAsync();

            ViewBag.Campagnes = campagnes;
            ViewBag.Lieux = lieux;
            ViewBag.AgentsTerrain = agentsTerrain;
            ViewBag.StatutsActivation = Enum.GetValues(typeof(DiversityPub.Models.enums.StatutActivation));

            return View(activation);
        }

        // GET: Activation/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
                return NotFound();

            var activation = await _context.Activations
                .Include(a => a.Campagne)
                .Include(a => a.Lieu)
                .Include(a => a.Responsable)
                    .ThenInclude(r => r.Utilisateur)
                .Include(a => a.AgentsTerrain)
                    .ThenInclude(at => at.Utilisateur)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (activation == null)
                return NotFound();

            // Récupérer tous les agents terrain pour la sélection
            var agentsTerrain = await _context.AgentsTerrain
                .Include(at => at.Utilisateur)
                .OrderBy(at => at.Utilisateur.Nom)
                .ThenBy(at => at.Utilisateur.Prenom)
                .ToListAsync();

            ViewBag.AgentsTerrain = agentsTerrain;
            ViewBag.Responsables = agentsTerrain; // Initialement tous les agents peuvent être responsables
            return View(activation);
        }

        // POST: Activation/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Description,Instructions,DateActivation,HeureDebut,HeureFin,ResponsableId")] Activation activation, List<Guid> AgentsTerrainIds)
        {
            if (id != activation.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Récupérer l'activation existante avec ses agents
                    var activationExistante = await _context.Activations
                        .Include(a => a.Campagne)
                        .Include(a => a.Lieu)
                        .Include(a => a.AgentsTerrain)
                        .FirstOrDefaultAsync(a => a.Id == id);

                    if (activationExistante == null)
                        return NotFound();

                    // Mettre à jour seulement les champs modifiables
                    activationExistante.Description = activation.Description;
                    activationExistante.Instructions = activation.Instructions;
                    activationExistante.DateActivation = activation.DateActivation;
                    activationExistante.HeureDebut = activation.HeureDebut;
                    activationExistante.HeureFin = activation.HeureFin;
                    activationExistante.ResponsableId = activation.ResponsableId;

                    // Gérer les agents terrain
                    // Supprimer les anciens agents
                    activationExistante.AgentsTerrain.Clear();
                    
                    if (AgentsTerrainIds != null && AgentsTerrainIds.Any())
                    {
                        // Ajouter les nouveaux agents
                        var agentsSelectionnes = await _context.AgentsTerrain
                            .Where(at => AgentsTerrainIds.Contains(at.Id))
                            .ToListAsync();

                        foreach (var agent in agentsSelectionnes)
                        {
                            activationExistante.AgentsTerrain.Add(agent);
                        }
                    }

                    // Validation avec le service
                    var validationErrors = new List<string>();

                    // 1. Validation de la date par rapport à la campagne
                    var campagneError = await _validationService.ValidateCampagneDateAsync(activationExistante.CampagneId, activationExistante.DateActivation);
                    if (campagneError != null)
                    {
                        ModelState.AddModelError("DateActivation", campagneError);
                    }

                    // 2. Validation des heures
                    var heuresError = _validationService.ValidateHeuresAsync(activationExistante.HeureDebut, activationExistante.HeureFin);
                    if (heuresError != null)
                    {
                        ModelState.AddModelError("HeureFin", heuresError);
                    }

                    // 3. Validation des conflits de lieu
                    var lieuErrors = await _validationService.ValidateLieuAvailabilityAsync(activationExistante.LieuId, activationExistante.DateActivation, activationExistante.HeureDebut, activationExistante.HeureFin, activationExistante.Id);
                    foreach (var error in lieuErrors)
                    {
                        ModelState.AddModelError("HeureDebut", error);
                    }

                    // 4. Validation des conflits de responsable
                    if (activationExistante.ResponsableId.HasValue)
                    {
                        var responsableErrors = await _validationService.ValidateResponsableAvailabilityAsync(activationExistante.ResponsableId.Value, activationExistante.DateActivation, activationExistante.HeureDebut, activationExistante.HeureFin, activationExistante.Id);
                        foreach (var error in responsableErrors)
                        {
                            ModelState.AddModelError("ResponsableId", error);
                        }
                    }

                    // 5. Validation des conflits d'agents terrain (IMPORTANT !)
                    if (AgentsTerrainIds != null && AgentsTerrainIds.Any())
                    {
                        var agentErrors = await _validationService.ValidateAgentAvailabilityAsync(AgentsTerrainIds, activationExistante.DateActivation, activationExistante.HeureDebut, activationExistante.HeureFin, activationExistante.Id);
                        foreach (var error in agentErrors)
                        {
                            ModelState.AddModelError("AgentsTerrainIds", error);
                        }
                    }

                    // Validation que le responsable fait partie des agents sélectionnés
                    if (activationExistante.ResponsableId.HasValue && AgentsTerrainIds != null && AgentsTerrainIds.Any())
                    {
                        if (!AgentsTerrainIds.Contains(activationExistante.ResponsableId.Value))
                        {
                            ModelState.AddModelError("ResponsableId", "Le responsable doit être choisi parmi les agents sélectionnés.");
                        }
                    }

                    if (ModelState.IsValid)
                    {
                        await _context.SaveChangesAsync();
                        TempData["Success"] = "✅ Activation mise à jour avec succès !";
                        return RedirectToAction(nameof(Index));
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ActivationExists(activation.Id))
                        return NotFound();
                    else
                        throw;
                }
            }

            // Récupérer l'activation complète pour la vue
            var activationComplete = await _context.Activations
                .Include(a => a.Campagne)
                .Include(a => a.Lieu)
                .Include(a => a.Responsable)
                    .ThenInclude(r => r.Utilisateur)
                .Include(a => a.AgentsTerrain)
                    .ThenInclude(at => at.Utilisateur)
                .FirstOrDefaultAsync(a => a.Id == id);

            // Récupérer tous les agents terrain pour la sélection
            var agentsTerrain = await _context.AgentsTerrain
                .Include(at => at.Utilisateur)
                .OrderBy(at => at.Utilisateur.Nom)
                .ThenBy(at => at.Utilisateur.Prenom)
                .ToListAsync();

            ViewBag.AgentsTerrain = agentsTerrain;
            ViewBag.Responsables = agentsTerrain;
            return View(activationComplete);
        }

        // GET: Activation/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
                return NotFound();

            var activation = await _context.Activations
                .Include(a => a.Campagne)
                .Include(a => a.Lieu)
                .Include(a => a.Responsable)
                    .ThenInclude(r => r.Utilisateur)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (activation == null)
                return NotFound();

            return View(activation);
        }

        // POST: Activation/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var activation = await _context.Activations.FindAsync(id);
            if (activation != null)
            {
                _context.Activations.Remove(activation);
                await _context.SaveChangesAsync();
                TempData["Success"] = "✅ Activation supprimée avec succès !";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Activation/ChangerStatut/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangerStatut(Guid id, DiversityPub.Models.enums.StatutActivation nouveauStatut, string? motifSuspension = null)
        {
            var activation = await _context.Activations.FindAsync(id);
            if (activation == null)
                return NotFound();

            // Validation pour la suspension
            if (nouveauStatut == DiversityPub.Models.enums.StatutActivation.Suspendue)
            {
                if (string.IsNullOrWhiteSpace(motifSuspension))
                {
                    TempData["Error"] = "❌ Le motif de suspension est obligatoire.";
                    return RedirectToAction("Edit", "Assignation", new { id });
                }
            }

            activation.Statut = nouveauStatut;
            
            if (nouveauStatut == DiversityPub.Models.enums.StatutActivation.Suspendue)
            {
                activation.DateSuspension = DateTime.Now;
                activation.MotifSuspension = motifSuspension?.Trim();
            }
            else if (nouveauStatut == DiversityPub.Models.enums.StatutActivation.Terminee)
            {
                activation.DateSuspension = null;
                activation.MotifSuspension = null;
            }

            _context.Update(activation);
            await _context.SaveChangesAsync();
            
            // Mettre à jour le statut de la campagne automatiquement
            await _campagneStatusService.UpdateCampagneStatusAsync(activation.CampagneId);
            
            TempData["Success"] = $"✅ Statut de l'activation changé vers {nouveauStatut} !";
            return RedirectToAction("Index", "Assignation");
        }

        private bool ActivationExists(Guid id)
        {
            return _context.Activations.Any(e => e.Id == id);
        }
    }
} 