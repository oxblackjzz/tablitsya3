using Tablitsya3.Components;
using Tablitsya3.Services;
using Tablitsya3.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Підключення PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? Environment.GetEnvironmentVariable("DATABASE_URL");

if (!string.IsNullOrEmpty(connectionString))
{
    // Якщо це Render/Heroku формат (postgres://user:pass@host:port/db)
    if (connectionString.StartsWith("postgres://"))
    {
        var uri = new Uri(connectionString);
        var host = uri.Host;
      var port = uri.Port;
    var username = uri.UserInfo.Split(':')[0];
      var password = uri.UserInfo.Split(':')[1];
    var database = uri.AbsolutePath.Trim('/');

        connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
    }

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));
    
    builder.Services.AddScoped<DatabaseStorageService>();
    builder.Logging.AddConsole();
    
    Console.WriteLine("✅ PostgreSQL Database configured");
}
else
{
    // Fallback до файлової системи якщо немає БД
    builder.Services.AddSingleton<DataStorageService>();
    Console.WriteLine("⚠️ Using file storage (no database configured)");
}

// Додаємо універсальний сервіс
builder.Services.AddSingleton<UnifiedStorageService>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<ProductionPlanningService>();
builder.Services.AddSingleton<WorkingDaysService>();
builder.Services.AddSingleton<LoggingService>();
builder.Services.AddSingleton<DataSeedService>();

var app = builder.Build();

// Migrate database and seed data
if (!string.IsNullOrEmpty(connectionString))
{
    using (var scope = app.Services.CreateScope())
    {
    try
   {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
 
        Console.WriteLine("🔄 Running database migrations...");
            await dbContext.Database.MigrateAsync();
         Console.WriteLine("✅ Database migrations completed");
          
     var dbStorage = scope.ServiceProvider.GetRequiredService<DatabaseStorageService>();
            var seedService = scope.ServiceProvider.GetRequiredService<DataSeedService>();
 
            // Перевіряємо чи є дані в БД
   if (!await dbStorage.HasSavedDataAsync())
   {
Console.WriteLine("🌱 Seeding initial data...");
        await seedService.SeedInitialDataIfEmpty();
         Console.WriteLine("✅ Initial data seeded");
        }
            else
        {
             Console.WriteLine("ℹ️ Database already contains data, skipping seed");
       }
        }
   catch (Exception ex)
 {
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
         logger.LogError(ex, "❌ Error during database migration or seeding");
      Console.WriteLine($"❌ Database error: {ex.Message}");
        }
    }
}
else
{
    // Fallback seed для файлової системи
    using (var scope = app.Services.CreateScope())
    {
        var seedService = scope.ServiceProvider.GetRequiredService<DataSeedService>();
        await seedService.SeedInitialDataIfEmpty();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

Console.WriteLine("🚀 Application started successfully!");
app.Run();
