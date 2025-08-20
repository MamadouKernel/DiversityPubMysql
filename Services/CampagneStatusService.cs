using Microsoft.EntityFrameworkCore;
using DiversityPub.Data;
using DiversityPub.Models;
using DiversityPub.Models.enums;

namespace DiversityPub.Services
{
    public interface ICampagneStatusService
    {
        Task UpdateCampagneStatusAsync(Guid campagneId);
        Task UpdateAllCampagnesStatusAsync();
    }

    public class CampagneStatusService : ICampagneStatusService
    {
        private readonly DiversityPubDbContext _context;

        public CampagneStatusService(DiversityPubDbContext context)
        {
            _context = context;
        }

        public async Task UpdateCampagneStatusAsync(Guid campagneId)
        {
            var campagne = await _context.Campagnes
                .Include(c => c.Activations)
                .FirstOrDefaultAsync(c => c.Id == campagneId);

            if (campagne == null) return;

            var activations = campagne.Activations?.ToList() ?? new List<Activation>();
            
            if (!activations.Any())
            {
                // Aucune activation, la campagne reste en statut Planifiée
                return;
            }

            var now = DateTime.Now;
            var dateFinAtteinte = campagne.DateFin <= now;

            // Compter les activations par statut
            var activationsEnCours = activations.Count(a => a.Statut == StatutActivation.EnCours);
            var activationsTerminees = activations.Count(a => a.Statut == StatutActivation.Terminee);
            var activationsPlanifiees = activations.Count(a => a.Statut == StatutActivation.Planifiee);
            var totalActivations = activations.Count;

            // Logique de mise à jour du statut
            if (dateFinAtteinte && activationsTerminees == totalActivations)
            {
                // Toutes les activations sont terminées ET la date de fin est atteinte
                campagne.Statut = StatutCampagne.Terminee;
            }
            else if (activationsEnCours > 0)
            {
                // Au moins une activation est en cours
                campagne.Statut = StatutCampagne.EnCours;
            }
            else if (activationsTerminees == totalActivations && !dateFinAtteinte)
            {
                // Toutes les activations sont terminées mais la date de fin n'est pas atteinte
                campagne.Statut = StatutCampagne.EnCours;
            }
            else if (activationsPlanifiees == totalActivations)
            {
                // Toutes les activations sont planifiées
                campagne.Statut = StatutCampagne.EnPreparation;
            }

            await _context.SaveChangesAsync();
        }

        public async Task UpdateAllCampagnesStatusAsync()
        {
            var campagnes = await _context.Campagnes
                .Include(c => c.Activations)
                .Where(c => c.Statut != StatutCampagne.Terminee) // Optimisation : ne traiter que les campagnes non terminées
                .ToListAsync();

            foreach (var campagne in campagnes)
            {
                await UpdateCampagneStatusAsync(campagne.Id);
            }
        }
    }
}
