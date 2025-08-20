using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DiversityPub.Services
{
    public class CampagneStatusBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CampagneStatusBackgroundService> _logger;
        private readonly TimeSpan _period = TimeSpan.FromMinutes(5); // Vérifier toutes les 5 minutes

        public CampagneStatusBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<CampagneStatusBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 Service de mise à jour des statuts de campagnes démarré");

            using var timer = new PeriodicTimer(_period);

            while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await UpdateCampagneStatusesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Erreur lors de la mise à jour automatique des statuts de campagnes");
                }
            }
        }

        private async Task UpdateCampagneStatusesAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var campagneStatusService = scope.ServiceProvider.GetRequiredService<ICampagneStatusService>();

            try
            {
                await campagneStatusService.UpdateAllCampagnesStatusAsync();
                _logger.LogInformation("✅ Mise à jour automatique des statuts de campagnes effectuée");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la mise à jour des statuts de campagnes");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("🛑 Service de mise à jour des statuts de campagnes arrêté");
            await base.StopAsync(cancellationToken);
        }
    }
}
