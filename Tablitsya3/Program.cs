using Tablitsya3.Components;
using Tablitsya3.Services;
using Tablitsya3.Data;
using Tablitsya3.Middleware;
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
     
     // ✅ ДОДАЄМО UTF-8 якщо його немає
     if (!connectionString.Contains("Client Encoding") && !connectionString.Contains("Encoding"))
     {
         connectionString = connectionString.TrimEnd(';') + ";Client Encoding=UTF8";
         Console.WriteLine("✅ Added UTF-8 encoding to connection string");
     }
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

   connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true;Client Encoding=UTF8";
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
  // ✅ НАЛАШТОВУЄМО NPGSQL ДЛЯ UTF-8
  AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
  
  builder.Services.AddDbContext<ApplicationDbContext>(options =>
      {
 options.UseNpgsql(connectionString, npgsqlOptions =>
     {
         // ✅ Встановлюємо таймаут для команд
         npgsqlOptions.CommandTimeout(60);
     });
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

// ✅ ДОДАЄМО КЕШУВАННЯ
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<CachedStorageService>();
Console.WriteLine("✅ Memory cache configured");

// ✅ ДОДАЄМО СЕРВІС МІГРАЦІЇ
builder.Services.AddSingleton<DatabaseMigrationService>();

// ✅ ДОДАЄМО СЕРВІС КОНФІГУРАЦІЇ ЦЕХІВ
builder.Services.AddScoped<WorkshopConfigService>();
Console.WriteLine("✅ WorkshopConfigService registered");

// ✅ ДОДАЄМО СЕРВІС БЕКАПІВ
builder.Services.AddSingleton<BackupService>();
Console.WriteLine("✅ BackupService registered");

// ✅ ДОДАЄМО DRAG & DROP INTEROP
builder.Services.AddScoped<DragDropInterop>();
Console.WriteLine("✅ DragDropInterop registered");

// ✅ ДОДАЄМО UNDO/REDO СЕРВІС
builder.Services.AddScoped<UndoRedoService>();
Console.WriteLine("✅ UndoRedoService registered");

// ✅ ДОДАЄМО СЕРВІС ТЕМИ
builder.Services.AddScoped<ThemeService>();
Console.WriteLine("✅ ThemeService registered");

// ✅ ДОДАЄМО СЕРВІСИ СКАНУВАННЯ ДЕТАЛЕЙ
builder.Services.AddScoped<ProjectFileParserService>();
builder.Services.AddScoped<ScanningService>();
builder.Services.AddScoped<ScanningProgressService>();
Console.WriteLine("✅ Scanning services registered");

// ✅ ДОДАЄМО SIGNALR
builder.Services.AddSignalR();
builder.Services.AddSingleton<Tablitsya3.Hubs.ScanningHubService>();
Console.WriteLine("✅ SignalR configured");

// ✅ ГЛОБАЛЬНА ОБРОБКА ПОМИЛОК
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ✅ ДОДАЄМО CONTROLLERS ДЛЯ API
builder.Services.AddControllers();

builder.Services.AddScoped<ProductionPlanningService>();
builder.Services.AddSingleton<WorkingDaysService>();
builder.Services.AddSingleton<LoggingService>();
builder.Services.AddSingleton<DataSeedService>();

var app = builder.Build();

// ✅ ВИКОРИСТАННЯ EXCEPTION HANDLER
app.UseExceptionHandler();

// ✅ АВТОМАТИЧНА МІГРАЦІЯ БД при старті
if (isDatabaseConfigured && !string.IsNullOrEmpty(connectionString))
{
    using (var scope = app.Services.CreateScope())
    {
   try
        {
      var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
 var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var migrationService = scope.ServiceProvider.GetRequiredService<DatabaseMigrationService>();

    Console.WriteLine("============================================");
    Console.WriteLine("🔄 STARTING DATABASE INITIALIZATION");
    Console.WriteLine("============================================");
   logger.LogInformation("🔄 Starting database initialization");

      // ✅ ПЕРЕВІРЯЄМО ЧИ ІСНУЄ БД
    Console.WriteLine("🔄 Checking database connection...");
    logger.LogInformation("🔄 Checking database connection...");

   var canConnect = await dbContext.Database.CanConnectAsync();

 if (!canConnect)
 {
 Console.WriteLine("❌ Cannot connect to database!");
                logger.LogError("❌ Cannot connect to database!");
         throw new Exception("Database connection failed");
        }

       Console.WriteLine("✅ Connected to database");
            logger.LogInformation("✅ Connected to database");

   // ✅ ПЕРЕВІРЯЄМО ЧИ ІСНУЮТЬ ТАБЛИЦІ
        var tablesExist = await migrationService.CheckTablesExistAsync(connectionString);

   if (!tablesExist)
   {
     Console.WriteLine("============================================");
   Console.WriteLine("📋 TABLES NOT FOUND - STARTING MIGRATION");
   Console.WriteLine("============================================");
      logger.LogWarning("📋 Tables not found. Running automatic migration...");

          // ✅ АВТОМАТИЧНО СТВОРЮЄМО ТАБЛИЦІ
   var migrationSuccess = await migrationService.MigrateDatabaseAsync(connectionString);

 if (migrationSuccess)
     {
   Console.WriteLine("============================================");
         Console.WriteLine("✅ DATABASE MIGRATION COMPLETED!");
        Console.WriteLine("============================================");
logger.LogInformation("✅ Database migration completed successfully!");
  }
   else
      {
          Console.WriteLine("============================================");
          Console.WriteLine("⚠️ MIGRATION FAILED - TRYING EnsureCreated");
     Console.WriteLine("============================================");
       logger.LogWarning("⚠️ Database migration failed. Trying EnsureCreated...");
        
            await dbContext.Database.EnsureCreatedAsync();
              
        Console.WriteLine("✅ EnsureCreated completed");
     logger.LogInformation("✅ EnsureCreated completed");
     }
  }
      else
       {
   Console.WriteLine("✅ All tables already exist - skipping migration");
      logger.LogInformation("✅ All tables already exist");
  }

      var dbStorage = scope.ServiceProvider.GetRequiredService<DatabaseStorageService>();
      var seedService = scope.ServiceProvider.GetRequiredService<DataSeedService>();

   // Перевіряємо чи є дані в БД
         Console.WriteLine("🔍 Checking for existing data...");
 logger.LogInformation("🔍 Checking for existing data...");

            if (!await dbStorage.HasSavedDataAsync())
            {
      Console.WriteLine("🌱 Seeding initial data...");
 logger.LogInformation("🌱 Seeding initial data...");
  
      await seedService.SeedInitialDataIfEmpty();
    
                Console.WriteLine("✅ Initial data seeded");
                logger.LogInformation("✅ Initial data seeded");
  }
      else
  {
      Console.WriteLine("ℹ️ Database already contains data, skipping seed");
             logger.LogInformation("ℹ️ Database already contains data, skipping seed");
   }

     Console.WriteLine("============================================");
    Console.WriteLine("✅ DATABASE INITIALIZATION COMPLETE");
            Console.WriteLine("============================================");
        }
   catch (Exception ex)
        {
   var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "❌ Error during database setup or seeding");
            
            Console.WriteLine("============================================");
       Console.WriteLine("❌ DATABASE INITIALIZATION FAILED");
            Console.WriteLine("============================================");
            Console.WriteLine($"❌ Error: {ex.Message}");
   Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
       Console.WriteLine("");
      Console.WriteLine("🔧 TROUBLESHOOTING:");
      Console.WriteLine("   1. Check DATABASE_URL environment variable");
   Console.WriteLine("   2. Verify database connection settings");
       Console.WriteLine("   3. Check Render PostgreSQL logs");
        Console.WriteLine("   4. Ensure PostgreSQL is running and accessible");
      Console.WriteLine("============================================");
            
    // ⚠️ НЕ КИДАЄМО ПОМИЛКУ - дозволяємо додатку запуститися
// Він працюватиме з файловим сховищем
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

// ✅ Маппінг API Controllers
app.MapControllers();

// ✅ Маппінг SignalR Hub
app.MapHub<Tablitsya3.Hubs.ScanningHub>("/hubs/scanning");

app.MapRazorComponents<App>()
  .AddInteractiveServerRenderMode();

Console.WriteLine("🚀 Application started successfully!");
app.Run();
