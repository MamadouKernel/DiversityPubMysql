using Microsoft.EntityFrameworkCore;
using DiversityPub.Data;
using DiversityPub.Models;
using DiversityPub.Models.enums;

namespace DiversityPub.Services
{
    public interface IActivationValidationService
    {
        Task<List<string>> ValidateAgentAvailabilityAsync(List<Guid> agentIds, DateTime dateActivation, TimeSpan heureDebut, TimeSpan heureFin, Guid? excludeActivationId = null);
        Task<bool> HasAgentConflictAsync(Guid agentId, DateTime dateActivation, TimeSpan heureDebut, TimeSpan heureFin, Guid? excludeActivationId = null);
        Task<List<Activation>> GetConflictingActivationsAsync(List<Guid> agentIds, DateTime dateActivation, TimeSpan heureDebut, TimeSpan heureFin, Guid? excludeActivationId = null);
        Task<List<string>> ValidateLieuAvailabilityAsync(Guid lieuId, DateTime dateActivation, TimeSpan heureDebut, TimeSpan heureFin, Guid? excludeActivationId = null);
        Task<List<string>> ValidateResponsableAvailabilityAsync(Guid responsableId, DateTime dateActivation, TimeSpan heureDebut, TimeSpan heureFin, Guid? excludeActivationId = null);
        Task<string?> ValidateCampagneDateAsync(Guid campagneId, DateTime dateActivation);
        string? ValidateHeuresAsync(TimeSpan heureDebut, TimeSpan heureFin);
    }

    public class ActivationValidationService : IActivationValidationService
    {
        private readonly DiversityPubDbContext _context;

        public ActivationValidationService(DiversityPubDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Valide la disponibilité des agents pour une activation donnée
        /// </summary>
        /// <param name="agentIds">Liste des IDs des agents à valider</param>
        /// <param name="dateActivation">Date de l'activation</param>
        /// <param name="heureDebut">Heure de début</param>
        /// <param name="heureFin">Heure de fin</param>
        /// <param name="excludeActivationId">ID de l'activation à exclure (pour les modifications)</param>
        /// <returns>Liste des messages d'erreur</returns>
        public async Task<List<string>> ValidateAgentAvailabilityAsync(List<Guid> agentIds, DateTime dateActivation, TimeSpan heureDebut, TimeSpan heureFin, Guid? excludeActivationId = null)
        {
            var errors = new List<string>();

            if (agentIds == null || !agentIds.Any())
            {
                return errors; // Aucun agent sélectionné, pas de conflit
            }

            foreach (var agentId in agentIds)
            {
                var hasConflict = await HasAgentConflictAsync(agentId, dateActivation, heureDebut, heureFin, excludeActivationId);
                
                if (hasConflict)
                {
                    var agent = await _context.AgentsTerrain
                        .Include(at => at.Utilisateur)
                        .FirstOrDefaultAsync(at => at.Id == agentId);

                    if (agent != null)
                    {
                        var nomAgent = $"{agent.Utilisateur.Prenom} {agent.Utilisateur.Nom}";
                        var conflits = await GetConflictingActivationsAsync(new List<Guid> { agentId }, dateActivation, heureDebut, heureFin, excludeActivationId);
                        
                        var nomsActivations = string.Join(", ", conflits.Select(a => $"'{a.Nom}' ({a.HeureDebut:hh\\:mm}-{a.HeureFin:hh\\:mm} à {a.Lieu?.Nom})"));
                        
                        errors.Add($"❌ L'agent {nomAgent} est déjà affecté à d'autres activations le {dateActivation:dd/MM/yyyy} : {nomsActivations}");
                    }
                }
            }

            return errors;
        }

        /// <summary>
        /// Vérifie si un agent a un conflit d'horaires
        /// </summary>
        /// <param name="agentId">ID de l'agent</param>
        /// <param name="dateActivation">Date de l'activation</param>
        /// <param name="heureDebut">Heure de début</param>
        /// <param name="heureFin">Heure de fin</param>
        /// <param name="excludeActivationId">ID de l'activation à exclure</param>
        /// <returns>True si conflit, False sinon</returns>
        public async Task<bool> HasAgentConflictAsync(Guid agentId, DateTime dateActivation, TimeSpan heureDebut, TimeSpan heureFin, Guid? excludeActivationId = null)
        {
            var conflits = await GetConflictingActivationsAsync(new List<Guid> { agentId }, dateActivation, heureDebut, heureFin, excludeActivationId);
            return conflits.Any();
        }

        /// <summary>
        /// Récupère les activations en conflit pour les agents donnés
        /// </summary>
        /// <param name="agentIds">Liste des IDs des agents</param>
        /// <param name="dateActivation">Date de l'activation</param>
        /// <param name="heureDebut">Heure de début</param>
        /// <param name="heureFin">Heure de fin</param>
        /// <param name="excludeActivationId">ID de l'activation à exclure</param>
        /// <returns>Liste des activations en conflit</returns>
        public async Task<List<Activation>> GetConflictingActivationsAsync(List<Guid> agentIds, DateTime dateActivation, TimeSpan heureDebut, TimeSpan heureFin, Guid? excludeActivationId = null)
        {
            var query = _context.Activations
                .Include(a => a.AgentsTerrain)
                .Include(a => a.Lieu)
                .Where(a => a.DateActivation == dateActivation
                           && a.Statut != StatutActivation.Terminee // Exclure les activations terminées
                           && a.AgentsTerrain.Any(at => agentIds.Contains(at.Id)));

            // Exclure l'activation en cours de modification
            if (excludeActivationId.HasValue)
            {
                query = query.Where(a => a.Id != excludeActivationId.Value);
            }

            var activations = await query.ToListAsync();

            // Filtrer les activations avec chevauchement d'horaires
            var conflits = activations.Where(a => 
                (heureDebut < a.HeureFin && heureFin > a.HeureDebut) // Chevauchement d'horaires
            ).ToList();

            return conflits;
        }

        /// <summary>
        /// Valide la disponibilité d'un lieu pour une activation
        /// </summary>
        /// <param name="lieuId">ID du lieu</param>
        /// <param name="dateActivation">Date de l'activation</param>
        /// <param name="heureDebut">Heure de début</param>
        /// <param name="heureFin">Heure de fin</param>
        /// <param name="excludeActivationId">ID de l'activation à exclure</param>
        /// <returns>Liste des messages d'erreur</returns>
        public async Task<List<string>> ValidateLieuAvailabilityAsync(Guid lieuId, DateTime dateActivation, TimeSpan heureDebut, TimeSpan heureFin, Guid? excludeActivationId = null)
        {
            var errors = new List<string>();

            var query = _context.Activations
                .Where(a => a.LieuId == lieuId
                           && a.DateActivation == dateActivation
                           && a.Statut != StatutActivation.Terminee);

            if (excludeActivationId.HasValue)
            {
                query = query.Where(a => a.Id != excludeActivationId.Value);
            }

            var activationsConflitantes = await query.ToListAsync();

            foreach (var activation in activationsConflitantes)
            {
                if (heureDebut < activation.HeureFin && heureFin > activation.HeureDebut)
                {
                    errors.Add($"❌ Conflit d'horaires avec l'activation '{activation.Nom}' au même lieu ({activation.HeureDebut:hh\\:mm}-{activation.HeureFin:hh\\:mm})");
                }
            }

            return errors;
        }

        /// <summary>
        /// Valide la disponibilité d'un responsable pour une activation
        /// </summary>
        /// <param name="responsableId">ID du responsable</param>
        /// <param name="dateActivation">Date de l'activation</param>
        /// <param name="heureDebut">Heure de début</param>
        /// <param name="heureFin">Heure de fin</param>
        /// <param name="excludeActivationId">ID de l'activation à exclure</param>
        /// <returns>Liste des messages d'erreur</returns>
        public async Task<List<string>> ValidateResponsableAvailabilityAsync(Guid responsableId, DateTime dateActivation, TimeSpan heureDebut, TimeSpan heureFin, Guid? excludeActivationId = null)
        {
            var errors = new List<string>();

            var query = _context.Activations
                .Where(a => a.ResponsableId == responsableId
                           && a.DateActivation == dateActivation
                           && a.Statut != StatutActivation.Terminee);

            if (excludeActivationId.HasValue)
            {
                query = query.Where(a => a.Id != excludeActivationId.Value);
            }

            var activationsConflitantes = await query.ToListAsync();

            foreach (var activation in activationsConflitantes)
            {
                if (heureDebut < activation.HeureFin && heureFin > activation.HeureDebut)
                {
                    errors.Add($"❌ Le responsable est déjà occupé avec l'activation '{activation.Nom}' ({activation.HeureDebut:hh\\:mm}-{activation.HeureFin:hh\\:mm})");
                }
            }

            return errors;
        }

        /// <summary>
        /// Valide la date d'activation par rapport à la campagne
        /// </summary>
        /// <param name="campagneId">ID de la campagne</param>
        /// <param name="dateActivation">Date de l'activation</param>
        /// <returns>Message d'erreur ou null si valide</returns>
        public async Task<string?> ValidateCampagneDateAsync(Guid campagneId, DateTime dateActivation)
        {
            var campagne = await _context.Campagnes.FindAsync(campagneId);
            
            if (campagne == null)
            {
                return "❌ Campagne non trouvée";
            }

            if (dateActivation < campagne.DateDebut || dateActivation > campagne.DateFin)
            {
                return $"❌ La date d'activation doit être comprise entre {campagne.DateDebut:dd/MM/yyyy} et {campagne.DateFin:dd/MM/yyyy}";
            }

            return null;
        }

        /// <summary>
        /// Valide les heures de début et de fin
        /// </summary>
        /// <param name="heureDebut">Heure de début</param>
        /// <param name="heureFin">Heure de fin</param>
        /// <returns>Message d'erreur ou null si valide</returns>
        public string? ValidateHeuresAsync(TimeSpan heureDebut, TimeSpan heureFin)
        {
            if (heureDebut >= heureFin)
            {
                return "❌ L'heure de fin doit être postérieure à l'heure de début";
            }

            return null;
        }
    }
}
