using Tablitsya3.Components;
using Tablitsya3.Services;
using Tablitsya3.Data;
using Tablitsya3.Middleware;
using Tablitsya3.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

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

     // ✅ ДОДАЄМО CONNECTION POOLING якщо його немає
     if (!connectionString.Contains("Pooling=") && !connectionString.Contains("MaxPoolSize="))
     {
         connectionString = connectionString.TrimEnd(';') + ";Pooling=true;MinPoolSize=5;MaxPoolSize=100;Connection Idle Lifetime=300";
         Console.WriteLine("✅ Added connection pooling to connection string");
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

   connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true;Client Encoding=UTF8;Pooling=true;MinPoolSize=5;MaxPoolSize=100;Connection Idle Lifetime=300";
         Console.WriteLine($"✅ Converted Postgres URL to Npgsql format (with connection pooling)");
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

// ✅ ДОДАЄМО СЕРВІСИ СКАНУВАННЯ ДЕТАЛЕЙ
builder.Services.AddScoped<ProjectFileParserService>();
builder.Services.AddScoped<ScanningService>();
builder.Services.AddScoped<ScanningProgressService>();
Console.WriteLine("✅ Scanning services registered");

// ✅ ДОДАЄМО СЕРВІСИ ПРАЦІВНИКІВ
builder.Services.AddScoped<WorkerService>();
builder.Services.AddScoped<WorkstationService>();
builder.Services.AddScoped<WorkerAuthService>();
Console.WriteLine("✅ Worker services registered");

// ✅ ДОДАЄМО СЕРВІС ДЕФЕКТІВ (БРАКУ)
builder.Services.AddScoped<DefectService>();
Console.WriteLine("✅ DefectService registered");

// ✅ ДОДАЄМО СЕРВІС ДРУКУ БІРОК
builder.Services.AddScoped<LabelPrintService>();
Console.WriteLine("✅ LabelPrintService registered");

// ✅ ДОДАЄМО AI АНАЛІТИКУ (ML.NET)
builder.Services.AddScoped<AIAnalyticsService>();
Console.WriteLine("✅ AIAnalyticsService registered (ML.NET)");

// ✅ ДОДАЄМО SIGNALR
builder.Services.AddSignalR();
builder.Services.AddSingleton<Tablitsya3.Hubs.ScanningHubService>();
Console.WriteLine("✅ SignalR configured");

// ✅ ДОДАЄМО BACKGROUND SERVICE ДЛЯ АВТОМАТИЧНОГО ОБСЛУГОВУВАННЯ БД
builder.Services.AddHostedService<MaintenanceBackgroundService>();
Console.WriteLine("✅ MaintenanceBackgroundService registered (auto-archive old logs)");

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

// === Authentication & Authorization ===
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<RoleService>();
builder.Services.AddScoped<WorkstationScannerService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider,
    Tablitsya3.Services.HttpContextAuthenticationStateProvider>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "Tablitsya3.Auth";
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthPolicies.AdminOnly, p => p.RequireRole(nameof(UserRole.Admin)));
    options.AddPolicy(AuthPolicies.ManagerOrAbove, p => p.RequireRole(nameof(UserRole.Admin), nameof(UserRole.Manager)));
    options.AddPolicy(AuthPolicies.OperatorOrAbove, p => p.RequireRole(nameof(UserRole.Admin), nameof(UserRole.Manager), nameof(UserRole.Operator)));
    options.AddPolicy(AuthPolicies.AnyAuthenticated, p => p.RequireAuthenticatedUser());
});

builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider,
    Tablitsya3.Services.PermissionPolicyProvider>();
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler,
    Tablitsya3.Services.PermissionAuthorizationHandler>();

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
   Console.WriteLine("✅ All tables already exist - checking for pending migrations...");
      logger.LogInformation("✅ All tables already exist - checking for pending migrations...");
      
      // ✅ ЗАСТОСОВУЄМО PENDING МІГРАЦІЇ (нові колонки, таблиці тощо)
      var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
      if (pendingMigrations.Any())
      {
          Console.WriteLine($"🔄 Found {pendingMigrations.Count()} pending migrations, applying...");
          logger.LogInformation($"🔄 Found {pendingMigrations.Count()} pending migrations: {string.Join(", ", pendingMigrations)}");
          
          try
          {
              await dbContext.Database.MigrateAsync();
              Console.WriteLine("✅ Pending migrations applied successfully");
              logger.LogInformation("✅ Pending migrations applied successfully");
          }
          catch (Exception migEx)
          {
              Console.WriteLine($"⚠️ Migration failed: {migEx.Message}");
              logger.LogWarning($"⚠️ Migration failed: {migEx.Message}");
              
              // Fallback: додаємо колонки вручну якщо вони не існують
              await EnsureColumnsExist(dbContext, logger);
          }
      }
      else
      {
          Console.WriteLine("✅ No pending migrations");
          logger.LogInformation("✅ No pending migrations");
          
          // Перевіряємо чи існують нові колонки
          await EnsureColumnsExist(dbContext, logger);
      }
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

            // ✅ AUTH: створення таблиці app_users + seed адміна
            try
            {
                await EnsureAuthSchemaAndSeedAsync(scope.ServiceProvider, logger);
            }
            catch (Exception authEx)
            {
                logger.LogError(authEx, "❌ Auth schema/seed initialization failed");
                Console.WriteLine($"❌ Auth init failed: {authEx.Message}");
            }

            // ✅ SCANNER: додавання колонок сканера до workstations
            try
            {
                await EnsureWorkstationScannerColumnsAsync(scope.ServiceProvider, logger);
            }
            catch (Exception scnEx)
            {
                logger.LogError(scnEx, "❌ Workstation scanner columns init failed");
                Console.WriteLine($"❌ Scanner columns init failed: {scnEx.Message}");
            }
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
app.MapStaticAssets();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// ✅ Маппінг API Controllers
app.MapControllers();

// ✅ Маппінг SignalR Hub
app.MapHub<Tablitsya3.Hubs.ScanningHub>("/hubs/scanning");

app.MapRazorComponents<App>()
  .AddInteractiveServerRenderMode();

Console.WriteLine("🚀 Application started successfully!");
app.Run();

// ✅ Створення таблиці app_users та seed адміна
static async Task EnsureAuthSchemaAndSeedAsync(IServiceProvider services, ILogger logger)
{
    var dbContext = services.GetRequiredService<ApplicationDbContext>();

    const string createTableSql = @"
        CREATE TABLE IF NOT EXISTS app_users (
            id SERIAL PRIMARY KEY,
            username VARCHAR(64) NOT NULL,
            password_hash TEXT NOT NULL DEFAULT '',
            password_salt TEXT NOT NULL DEFAULT '',
            display_name VARCHAR(128) NOT NULL DEFAULT '',
            role INTEGER NOT NULL DEFAULT 0,
            is_active BOOLEAN NOT NULL DEFAULT TRUE,
            created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
            last_login_at TIMESTAMPTZ NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS ix_app_users_username ON app_users (username);

        ALTER TABLE app_users ADD COLUMN IF NOT EXISTS role_id INTEGER NULL;
        ALTER TABLE app_users ADD COLUMN IF NOT EXISTS worker_id INTEGER NULL;
        CREATE INDEX IF NOT EXISTS ix_app_users_role_id ON app_users (role_id);

        CREATE TABLE IF NOT EXISTS app_roles (
            id SERIAL PRIMARY KEY,
            code VARCHAR(64) NOT NULL,
            display_name VARCHAR(128) NOT NULL,
            description VARCHAR(500) NULL,
            badge_color VARCHAR(16) NULL,
            is_system BOOLEAN NOT NULL DEFAULT FALSE,
            created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
            updated_at TIMESTAMPTZ NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS ix_app_roles_code ON app_roles (code);

        CREATE TABLE IF NOT EXISTS app_role_permissions (
            id SERIAL PRIMARY KEY,
            role_id INTEGER NOT NULL,
            permission_key VARCHAR(128) NOT NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS ix_app_role_permissions_role_key ON app_role_permissions (role_id, permission_key);

        CREATE TABLE IF NOT EXISTS app_user_permission_overrides (
            id SERIAL PRIMARY KEY,
            user_id INTEGER NOT NULL,
            permission_key VARCHAR(128) NOT NULL,
            is_granted BOOLEAN NOT NULL DEFAULT TRUE
        );
        CREATE UNIQUE INDEX IF NOT EXISTS ix_app_user_perm_overrides_user_key ON app_user_permission_overrides (user_id, permission_key);

        ALTER TABLE workers ADD COLUMN IF NOT EXISTS app_user_id INTEGER NULL;
    ";

    await dbContext.Database.ExecuteSqlRawAsync(createTableSql);
    logger.LogInformation("✅ Auth schema ensured (users/roles/permissions/overrides)");

    // Засіяти системні ролі та матриці прав
    var roleService = services.GetRequiredService<RoleService>();
    foreach (var (code, defaults) in Tablitsya3.Models.Permissions.DefaultRolePermissions)
    {
        var role = await roleService.GetRoleByCodeAsync(code);
        if (role == null)
        {
            var displayName = code switch
            {
                "Admin" => "Адміністратор",
                "Manager" => "Менеджер",
                "Operator" => "Оператор",
                "Viewer" => "Спостерігач",
                _ => code,
            };
            var color = code switch
            {
                "Admin" => "#dc3545",
                "Manager" => "#0d6efd",
                "Operator" => "#198754",
                "Viewer" => "#6c757d",
                _ => "#6c757d",
            };
            role = await roleService.CreateRoleAsync(code, displayName, "Системна роль", color);
            // Позначаємо як системну
            role.IsSystem = true;
            await dbContext.SaveChangesAsync();
            await roleService.SetRolePermissionsAsync(role.Id, defaults);
            logger.LogInformation("✅ Seeded system role {Code}", code);
        }
    }

    // Прив'язати legacy users до RoleId
    var unlinked = await dbContext.AppUsers.Where(u => u.RoleId == null).ToListAsync();
    if (unlinked.Count > 0)
    {
        var rolesByCode = await dbContext.Roles.ToDictionaryAsync(r => r.Code, r => r.Id, StringComparer.OrdinalIgnoreCase);
        foreach (var u in unlinked)
        {
            var legacy = ((UserRole)u.Role).ToString();
            if (rolesByCode.TryGetValue(legacy, out var rid))
                u.RoleId = rid;
        }
        await dbContext.SaveChangesAsync();
        logger.LogInformation("✅ Linked {Count} legacy users to dynamic roles", unlinked.Count);
    }

    var auth = services.GetRequiredService<AuthService>();
    if (!await auth.AnyUserExistsAsync())
    {
        await auth.CreateAsync("admin", "admin123", "Адміністратор", UserRole.Admin, isActive: true);
        Console.WriteLine("============================================");
        Console.WriteLine("👤 Default admin created: admin / admin123");
        Console.WriteLine("⚠️  Change this password after first login!");
        Console.WriteLine("============================================");
        logger.LogWarning("👤 Default admin user created with password 'admin123' - change it!");
    }
    else
    {
        var admin = await auth.GetByUsernameAsync("admin");
        if (admin != null && string.IsNullOrEmpty(admin.PasswordSalt))
        {
            await auth.SetPasswordAsync(admin.Id, "admin123");
            logger.LogWarning("👤 Admin password reset to default (was missing salt)");
            Console.WriteLine("👤 Admin password was reset to: admin123");
        }
    }
}

// ✅ Метод для додавання колонок якщо міграція не спрацювала
static async Task EnsureColumnsExist(ApplicationDbContext dbContext, ILogger logger)
{
    try
    {
        // Перевіряємо та додаємо колонку capacity в workstations
        var checkCapacityColumn = @"
            SELECT column_name 
            FROM information_schema.columns 
            WHERE table_name = 'workstations' AND column_name = 'capacity'";
        
        var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync();
        
        using var checkCmd = connection.CreateCommand();
        checkCmd.CommandText = checkCapacityColumn;
        var result = await checkCmd.ExecuteScalarAsync();
        
        if (result == null)
        {
            Console.WriteLine("🔧 Adding missing 'capacity' column to workstations...");
            logger.LogInformation("🔧 Adding missing 'capacity' column to workstations...");
            
            await dbContext.Database.ExecuteSqlRawAsync(
                "ALTER TABLE workstations ADD COLUMN IF NOT EXISTS capacity numeric DEFAULT 0");
            
            Console.WriteLine("✅ Added 'capacity' column");
            logger.LogInformation("✅ Added 'capacity' column");
        }
        
        // Перевіряємо та додаємо колонку use_auto_capacity в workshop_capacities
        checkCmd.CommandText = @"
            SELECT column_name 
            FROM information_schema.columns 
            WHERE table_name = 'workshop_capacities' AND column_name = 'use_auto_capacity'";
        result = await checkCmd.ExecuteScalarAsync();
        
        if (result == null)
        {
            Console.WriteLine("🔧 Adding missing 'use_auto_capacity' column to workshop_capacities...");
            logger.LogInformation("🔧 Adding missing 'use_auto_capacity' column to workshop_capacities...");
            
            await dbContext.Database.ExecuteSqlRawAsync(
                "ALTER TABLE workshop_capacities ADD COLUMN IF NOT EXISTS use_auto_capacity boolean DEFAULT false");
            
            Console.WriteLine("✅ Added 'use_auto_capacity' column");
            logger.LogInformation("✅ Added 'use_auto_capacity' column");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Error ensuring columns exist: {ex.Message}");
        logger.LogWarning($"⚠️ Error ensuring columns exist: {ex.Message}");
    }
}

// ✅ Додавання колонок сканера до workstations
static async Task EnsureWorkstationScannerColumnsAsync(IServiceProvider services, ILogger logger)
{
    var dbContext = services.GetRequiredService<ApplicationDbContext>();
    const string sql = @"
        ALTER TABLE workstations ADD COLUMN IF NOT EXISTS scanner_model INTEGER NOT NULL DEFAULT 0;
        ALTER TABLE workstations ADD COLUMN IF NOT EXISTS scanner_connection_type INTEGER NOT NULL DEFAULT 0;
        ALTER TABLE workstations ADD COLUMN IF NOT EXISTS scanner_enabled BOOLEAN NOT NULL DEFAULT FALSE;
        ALTER TABLE workstations ADD COLUMN IF NOT EXISTS scanner_device_name VARCHAR(150);
        ALTER TABLE workstations ADD COLUMN IF NOT EXISTS scanner_serial_number VARCHAR(100);
        ALTER TABLE workstations ADD COLUMN IF NOT EXISTS scanner_usb_vid VARCHAR(10);
        ALTER TABLE workstations ADD COLUMN IF NOT EXISTS scanner_usb_pid VARCHAR(10);
        ALTER TABLE workstations ADD COLUMN IF NOT EXISTS scanner_com_port VARCHAR(20);
        ALTER TABLE workstations ADD COLUMN IF NOT EXISTS scanner_baud_rate INTEGER;
        ALTER TABLE workstations ADD COLUMN IF NOT EXISTS scanner_bluetooth_mac VARCHAR(50);
        ALTER TABLE workstations ADD COLUMN IF NOT EXISTS scanner_ip_address VARCHAR(50);
        ALTER TABLE workstations ADD COLUMN IF NOT EXISTS scanner_tcp_port INTEGER;
        ALTER TABLE workstations ADD COLUMN IF NOT EXISTS scanner_webhook_url VARCHAR(500);
        ALTER TABLE workstations ADD COLUMN IF NOT EXISTS scanner_prefix VARCHAR(20);
        ALTER TABLE workstations ADD COLUMN IF NOT EXISTS scanner_suffix VARCHAR(20);
        ALTER TABLE workstations ADD COLUMN IF NOT EXISTS scanner_extra_json TEXT;
    ";
    await dbContext.Database.ExecuteSqlRawAsync(sql);
    logger.LogInformation("✅ Workstation scanner columns ensured");

    // Створюємо нову таблицю для декількох сканерів на станцію
    const string scannersTable = @"
        CREATE TABLE IF NOT EXISTS app_workstation_scanners (
            id SERIAL PRIMARY KEY,
            workstation_id INTEGER NOT NULL,
            name VARCHAR(150) NOT NULL DEFAULT '',
            role INTEGER NOT NULL DEFAULT 0,
            is_primary BOOLEAN NOT NULL DEFAULT FALSE,
            is_enabled BOOLEAN NOT NULL DEFAULT TRUE,
            scanner_model INTEGER NOT NULL DEFAULT 0,
            connection_type INTEGER NOT NULL DEFAULT 0,
            serial_number VARCHAR(100),
            usb_vid VARCHAR(10),
            usb_pid VARCHAR(10),
            com_port VARCHAR(20),
            baud_rate INTEGER,
            bluetooth_mac VARCHAR(50),
            ip_address VARCHAR(50),
            tcp_port INTEGER,
            webhook_url VARCHAR(500),
            prefix VARCHAR(20),
            suffix VARCHAR(20),
            extra_json TEXT,
            created_date TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
            updated_date TIMESTAMP WITH TIME ZONE
        );
        CREATE INDEX IF NOT EXISTS ix_app_workstation_scanners_ws ON app_workstation_scanners(workstation_id);
    ";
    await dbContext.Database.ExecuteSqlRawAsync(scannersTable);

    // Одноразова міграція legacy сканера зі станції в нову таблицю,
    // якщо у станції увімкнено сканер, але в новій таблиці ще немає записів.
    const string migrateLegacy = @"
        INSERT INTO app_workstation_scanners (
            workstation_id, name, role, is_primary, is_enabled,
            scanner_model, connection_type, serial_number,
            usb_vid, usb_pid, com_port, baud_rate, bluetooth_mac,
            ip_address, tcp_port, webhook_url, prefix, suffix, extra_json,
            created_date)
        SELECT
            w.id,
            COALESCE(NULLIF(w.scanner_device_name,''), 'Сканер ' || w.name),
            0, TRUE, COALESCE(w.scanner_enabled, FALSE),
            COALESCE(w.scanner_model, 0), COALESCE(w.scanner_connection_type, 0),
            w.scanner_serial_number, w.scanner_usb_vid, w.scanner_usb_pid,
            w.scanner_com_port, w.scanner_baud_rate, w.scanner_bluetooth_mac,
            w.scanner_ip_address, w.scanner_tcp_port, w.scanner_webhook_url,
            w.scanner_prefix, w.scanner_suffix, w.scanner_extra_json,
            NOW()
        FROM workstations w
        WHERE w.scanner_enabled = TRUE
          AND NOT EXISTS (SELECT 1 FROM app_workstation_scanners s WHERE s.workstation_id = w.id);
    ";
    try
    {
        await dbContext.Database.ExecuteSqlRawAsync(migrateLegacy);
        logger.LogInformation("✅ Legacy scanner data migrated to app_workstation_scanners");
    }
    catch (Exception ex)
    {
        logger.LogWarning("⚠️ Legacy scanner migration skipped: {Message}", ex.Message);
    }
}
