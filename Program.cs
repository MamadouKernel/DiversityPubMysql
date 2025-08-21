
using DiversityPub.Data;
using DiversityPub.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using DiversityPub.Models;
using DiversityPub.Models.enums;

var builder = WebApplication.CreateBuilder(args);

// Forcer l'utilisation d'appsettings.json même en production
builder.Configuration.SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Logging au démarrage
Console.WriteLine("🚀 Démarrage de l'application DiversityPub...");
Console.WriteLine($"🔧 Environment: {builder.Environment.EnvironmentName}");
Console.WriteLine($"🌐 URLs configurées: {builder.Configuration["ASPNETCORE_URLS"]}");

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
        options.JsonSerializerOptions.MaxDepth = 64;
    });

// Ajouter SignalR
builder.Services.AddSignalR();

// Configuration de la base de données avec gestion d'erreur
try
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    Console.WriteLine($"🔗 Connection string: {connectionString?.Substring(0, Math.Min(50, connectionString?.Length ?? 0))}...");
    
    if (!string.IsNullOrEmpty(connectionString))
    {
builder.Services.AddDbContext<DiversityPubDbContext>(options =>
            options.UseMySql(connectionString, 
                ServerVersion.AutoDetect(connectionString)));
        
        Console.WriteLine("✅ DbContext configuré avec succès");
    }
    else
    {
        Console.WriteLine("⚠️ Pas de connection string, DbContext non configuré");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Erreur lors de la configuration de la base de données: {ex.Message}");
    Console.WriteLine("⚠️ L'application continue sans base de données...");
}

// Enregistrer les services hébergés (conditionnellement)
try
{
builder.Services.AddHostedService<CampagneExpirationService>();
builder.Services.AddHostedService<GeolocationService>();
builder.Services.AddHostedService<CampagneStatusBackgroundService>();
    Console.WriteLine("✅ Services hébergés configurés");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ Erreur lors de la configuration des services: {ex.Message}");
}

// Enregistrer les services HTTP
try
{
builder.Services.AddHttpClient<GeocodingService>();
builder.Services.AddScoped<GeocodingService>();
builder.Services.AddScoped<ICampagneStatusService, CampagneStatusService>();
builder.Services.AddScoped<IActivationValidationService, ActivationValidationService>();
    Console.WriteLine("✅ Services HTTP configurés");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ Erreur lors de la configuration des services HTTP: {ex.Message}");
}

// Configuration de l'authentification par cookies
try
{
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
            options.ExpireTimeSpan = TimeSpan.FromHours(1);
            options.SlidingExpiration = true;
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.Cookie.SameSite = SameSiteMode.Lax;
        });
    Console.WriteLine("✅ Authentification configurée");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ Erreur lors de la configuration de l'authentification: {ex.Message}");
}

var app = builder.Build();

Console.WriteLine("🏗️ Configuration du pipeline HTTP...");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

// Mapper le hub SignalR (conditionnellement)
try
{
app.MapHub<DiversityPub.Hubs.NotificationHub>("/notificationHub");
    Console.WriteLine("✅ SignalR configuré");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ Erreur lors de la configuration SignalR: {ex.Message}");
}

// Endpoint de healthcheck simple
app.MapGet("/health", () => 
{
    Console.WriteLine("🏥 Healthcheck appelé");
    return "OK";
});

// Endpoint de test de base de données (conditionnel)
app.MapGet("/db-test", async (IServiceProvider serviceProvider) =>
{
    try
    {
        var context = serviceProvider.GetService<DiversityPubDbContext>();
        if (context == null)
        {
            return "DbContext non disponible";
        }
        
        var canConnect = await context.Database.CanConnectAsync();
        return canConnect ? "Database OK" : "Database connection failed";
    }
    catch (Exception ex)
    {
        return $"Database error: {ex.Message}";
    }
});

// Endpoint de test simple
app.MapGet("/test", () => "Application is running!");

// Appliquer les migrations et créer les utilisateurs par défaut
try
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetService<DiversityPubDbContext>();
        if (context != null)
        {
            Console.WriteLine("🔄 Tentative de connexion à la base de données...");
            context.Database.Migrate();
            Console.WriteLine("✅ Migrations appliquées avec succès");

            // Créer le SuperAdmin par défaut
            var superAdminExists = await context.Utilisateurs
                .AnyAsync(u => u.Role == DiversityPub.Models.enums.Role.SuperAdmin);
            
            if (!superAdminExists)
            {
                var superAdmin = new DiversityPub.Models.Utilisateur
                {
                    Id = Guid.NewGuid(),
                    Nom = "Super",
                    Prenom = "Admin",
                    Email = "superadmin@diversitypub.ci",
                    MotDePasse = BCrypt.Net.BCrypt.HashPassword("SuperAdmin2025!"),
                    Role = DiversityPub.Models.enums.Role.SuperAdmin,
                    Supprimer = 0
                };
                
                context.Utilisateurs.Add(superAdmin);
                await context.SaveChangesAsync();
                Console.WriteLine("✅ SuperAdmin créé avec succès");
                Console.WriteLine("📧 Email: superadmin@diversitypub.ci");
                Console.WriteLine("🔑 Mot de passe: SuperAdmin2025!");
            }
            else
            {
                Console.WriteLine("ℹ️ SuperAdmin existe déjà");
            }

            // Créer l'Admin par défaut s'il n'existe pas
            var adminExists = await context.Utilisateurs
                .AnyAsync(u => u.Role == DiversityPub.Models.enums.Role.Admin);
            
            if (!adminExists)
            {
                var admin = new DiversityPub.Models.Utilisateur
                {
                    Id = Guid.NewGuid(),
                    Nom = "Admin",
                    Prenom = "Standard",
                    Email = "admin@diversitypub.ci",
                    MotDePasse = BCrypt.Net.BCrypt.HashPassword("Admin2025!"),
                    Role = DiversityPub.Models.enums.Role.Admin,
                    Supprimer = 0
                };
                
                context.Utilisateurs.Add(admin);
                await context.SaveChangesAsync();
                Console.WriteLine("✅ Admin créé avec succès");
                Console.WriteLine("📧 Email: admin@diversitypub.ci");
                Console.WriteLine("🔑 Mot de passe: Admin2025!");
            }
            else
            {
                Console.WriteLine("ℹ️ Admin existe déjà");
            }
        }
        else
        {
            Console.WriteLine("⚠️ Pas de DbContext disponible");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Erreur lors de l'initialisation de la base de données: {ex.Message}");
    Console.WriteLine($"📋 Stack trace: {ex.StackTrace}");
    Console.WriteLine("⚠️ L'application continue sans l'initialisation...");
}

Console.WriteLine("🎉 Application configurée, démarrage...");
app.Run();
