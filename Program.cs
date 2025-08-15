
using DiversityPub.Data;
using DiversityPub.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
        options.JsonSerializerOptions.MaxDepth = 64;
    });

// Ajouter SignalR
builder.Services.AddSignalR();

builder.Services.AddDbContext<DiversityPubDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"), 
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))));

// Enregistrer les services hébergés
builder.Services.AddHostedService<CampagneExpirationService>();
builder.Services.AddHostedService<GeolocationService>();

// Enregistrer les services HTTP
builder.Services.AddHttpClient<GeocodingService>();
builder.Services.AddScoped<GeocodingService>();

// Configuration de l'authentification par cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(1); // Session de 8 heures
        options.SlidingExpiration = true; // Renouvellement automatique
        options.Cookie.HttpOnly = true; // Sécurité
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // HTTPS en production
        options.Cookie.SameSite = SameSiteMode.Lax; // Compatibilité cross-site
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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

// Mapper le hub SignalR
app.MapHub<DiversityPub.Hubs.NotificationHub>("/notificationHub");

// Appliquer les migrations automatiquement en production
if (app.Environment.IsProduction())
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<DiversityPubDbContext>();
        try
        {
            Console.WriteLine("🔄 Tentative de connexion à la base de données...");
            context.Database.Migrate();
            Console.WriteLine("✅ Migrations appliquées avec succès");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Erreur lors de l'application des migrations: {ex.Message}");
            Console.WriteLine($"📋 Stack trace: {ex.StackTrace}");
            
            // En production, on continue même si les migrations échouent
            // pour éviter que l'application ne démarre pas du tout
            Console.WriteLine("⚠️ L'application continue sans les migrations...");
        }
    }
}

// Ajouter un endpoint de healthcheck simple
app.MapGet("/health", () => "OK");

app.Run();
