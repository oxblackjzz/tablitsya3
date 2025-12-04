using Tablitsya3.Components;
using Tablitsya3.Services;
using Tablitsya3.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Підключення PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
  ?? Environment.GetEnvironmentVariable("DATABASE_URL");

Console.WriteLine($"🔍 DATABASE_URL present: {!string.IsNullOrEmpty(connectionString)}");

bool isDatabaseConfigured = false;

if (!string.IsNullOrEmpty(connectionString) && connectionString.Length > 10)
{
    Console.WriteLine($"🔍 DATABASE_URL length: {connectionString.Length}");
 Console.WriteLine($"🔍 First 20 chars: {connectionString.Substring(0, Math.Min(20, connectionString.Length))}...");
    
 try
  {
      // Check if it's already in Npgsql format (Host=...)
    if (connectionString.Contains("Host=") && connectionString.Contains("Database="))
    {
         Console.WriteLine("✅ Connection string is already in Npgsql format");
    }
        // Якщо це Render/Heroku формат (postgres:// або postgresql://)
        else if (connectionString.StartsWith("postgres://") || connectionString.StartsWith("postgresql://"))
 {
      var uri = new Uri(connectionString);
     var host = uri.Host;
     var port = uri.Port > 0 ? uri.Port : 5432; // Default PostgreSQL port
      
    string username = "";
    string password = "";
 
            if (!string.IsNullOrEmpty(uri.UserInfo))
      {
   var userInfoParts = uri.UserInfo.Split(':');
         username = userInfoParts[0];
  password = userInfoParts.Length > 1 ? userInfoParts[1] : "";
   }
     
   var database = uri.AbsolutePath.Trim('/');

    Console.WriteLine($"🔍 Parsed - Host: {host}, Port: {port}, Database: {database}, User: {username}");

          connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
         Console.WriteLine($"✅ Converted Postgres URL to Npgsql format");
        }
    else
     {
    // Unknown format
   Console.WriteLine($"⚠️ Unknown connection string format. Expected 'postgres://' or 'Host='");
    Console.WriteLine($"⚠️ Please use the 'Internal Database URL' from Render dashboard");
        connectionString = null;
      }

     if (!string.IsNullOrEmpty(connectionString))
  {
  builder.Services.AddDbContext<ApplicationDbContext>(options =>
      {
 options.UseNpgsql(connectionString);
     // Suppress pending model changes warning in production
    options.ConfigureWarnings(warnings => 
  warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
       });
  
     builder.Services.AddScoped<DatabaseStorageService>();
     builder.Logging.AddConsole();
   
   isDatabaseConfigured = true;
   Console.WriteLine("✅ PostgreSQL Database configured");
        }
    }
    catch (Exception ex)
    {
     Console.WriteLine($"❌ Error parsing DATABASE_URL: {ex.Message}");
     Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
    Console.WriteLine("⚠️ Falling back to file storage");
        connectionString = null;
    }
}

if (!isDatabaseConfigured)
{
  // Fallback до файлової системи якщо немає БД
    builder.Services.AddSingleton<DataStorageService>();
    Console.WriteLine("⚠️ Using file storage (no database configured)");
    connectionString = null;
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
if (isDatabaseConfigured && !string.IsNullOrEmpty(connectionString))
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
   Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
        }
    }
}
else
{
    // Fallback seed для файлової системи
  Console.WriteLine("🌱 Using file storage mode, seeding initial data if needed...");
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
