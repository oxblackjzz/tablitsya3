using Microsoft.EntityFrameworkCore;
using Npgsql;
using System;
using System.Threading.Tasks;

namespace Tablitsya3.Services
{
    /// <summary>
    /// Сервіс для автоматичного створення схеми БД при старті додатку
    /// Використовується для безкоштовного Render (без Web Shell доступу)
    /// </summary>
    public class DatabaseMigrationService
    {
        private readonly ILogger<DatabaseMigrationService> _logger;

        public DatabaseMigrationService(ILogger<DatabaseMigrationService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Виконує міграцію БД - створює всі таблиці та індекси
        /// </summary>
        public async Task<bool> MigrateDatabaseAsync(string connectionString)
        {
            try
            {
                _logger.LogInformation("🔧 Starting automatic database migration...");
                Console.WriteLine("🔧 Starting automatic database migration...");

                await using var connection = new NpgsqlConnection(connectionString);
                
                _logger.LogInformation("📡 Opening connection to database...");
                Console.WriteLine("📡 Opening connection to database...");
                await connection.OpenAsync();
                _logger.LogInformation("✅ Connection opened successfully");
                Console.WriteLine("✅ Connection opened successfully");

                // ✅ ВСТАНОВЛЮЄМО UTF-8 КОДУВАННЯ ДЛЯ СЕСІЇ
                var setEncodingScript = @"
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
";
                
                await using (var command = new NpgsqlCommand(setEncodingScript, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
                
                _logger.LogInformation("✅ UTF-8 encoding set for session");
                Console.WriteLine("✅ UTF-8 encoding set for session");
                
                // ✅ ПЕРЕВІРЯЄМО КОДУВАННЯ БАЗИ ДАНИХ
                var checkEncodingScript = @"
SELECT pg_encoding_to_char(encoding) as encoding 
FROM pg_database 
WHERE datname = current_database();
";
                
                await using (var command = new NpgsqlCommand(checkEncodingScript, connection))
                {
                    var dbEncoding = await command.ExecuteScalarAsync();
                    _logger.LogInformation($"📊 Database encoding: {dbEncoding}");
                    Console.WriteLine($"📊 Database encoding: {dbEncoding}");
                    
                    if (dbEncoding?.ToString() != "UTF8")
                    {
                        _logger.LogWarning($"⚠️ Database is not UTF8! Current: {dbEncoding}");
                        Console.WriteLine($"⚠️ WARNING: Database encoding is {dbEncoding}, should be UTF8!");
                    }
                }

                // SQL скрипт для створення таблиць
                var createTablesScript = @"
-- Створюємо таблицю workshop_data
CREATE TABLE IF NOT EXISTS workshop_data (
    id SERIAL PRIMARY KEY,
    last_updated TIMESTAMP WITH TIME ZONE NOT NULL,
    start_date TIMESTAMP WITH TIME ZONE NOT NULL,
    production_lead_time INTEGER NOT NULL DEFAULT 5,
    days_before_production INTEGER NOT NULL DEFAULT 16
);

-- Створюємо таблицю orders
CREATE TABLE IF NOT EXISTS orders (
    id SERIAL PRIMARY KEY,
    workshop_number INTEGER NOT NULL,
    order_date TIMESTAMP WITH TIME ZONE NOT NULL,
    square_meters DOUBLE PRECISION NOT NULL,
    order_name VARCHAR(500) NOT NULL DEFAULT ''
);

-- Створюємо таблицю workshop_capacities
CREATE TABLE IF NOT EXISTS workshop_capacities (
    id SERIAL PRIMARY KEY,
    workshop_number INTEGER NOT NULL,
    capacity INTEGER NOT NULL
);

-- Створюємо таблицю workshop_production_lead_times
CREATE TABLE IF NOT EXISTS workshop_production_lead_times (
    id SERIAL PRIMARY KEY,
    workshop_number INTEGER NOT NULL,
    production_lead_time INTEGER NOT NULL DEFAULT 5
);

-- Створюємо таблицю workshop_days_before_production
CREATE TABLE IF NOT EXISTS workshop_days_before_production (
    id SERIAL PRIMARY KEY,
    workshop_number INTEGER NOT NULL,
    days_before_production INTEGER NOT NULL DEFAULT 16
);

-- Створюємо таблицю custom_completion_dates
CREATE TABLE IF NOT EXISTS custom_completion_dates (
    id SERIAL PRIMARY KEY,
    order_key VARCHAR(200) NOT NULL,
    completion_date TIMESTAMP WITH TIME ZONE NOT NULL
);

-- Створюємо таблицю original_workshops (для збереження кольору при переміщенні)
CREATE TABLE IF NOT EXISTS original_workshops (
    id SERIAL PRIMARY KEY,
    order_key VARCHAR(200) NOT NULL,
    original_workshop_number INTEGER NOT NULL
);

-- =============================================
-- ТАБЛИЦІ ДЛЯ СКАНУВАННЯ ДЕТАЛЕЙ
-- =============================================

-- Імпортовані проекти
CREATE TABLE IF NOT EXISTS imported_projects (
    id SERIAL PRIMARY KEY,
    project_uuid VARCHAR(100) NOT NULL,
    file_name VARCHAR(255) DEFAULT '',
    imported_date TIMESTAMP WITH TIME ZONE NOT NULL,
    total_cost DECIMAL(18,2) DEFAULT 0,
    material_cost DECIMAL(18,2) DEFAULT 0,
    operation_cost DECIMAL(18,2) DEFAULT 0,
    currency VARCHAR(10) DEFAULT 'грн',
    version VARCHAR(50) DEFAULT '',
    products_count INTEGER DEFAULT 0,
    parts_count INTEGER DEFAULT 0,
    total_square_meters DOUBLE PRECISION DEFAULT 0,
    is_active BOOLEAN DEFAULT TRUE,
    workshop_number INTEGER DEFAULT 1
);

-- Деталі
CREATE TABLE IF NOT EXISTS parts (
    id SERIAL PRIMARY KEY,
    project_external_uuid VARCHAR(100) NOT NULL,
    part_id INTEGER NOT NULL,
    part_counter INTEGER NOT NULL,
    name VARCHAR(255) NOT NULL,
    code VARCHAR(100) DEFAULT '',
    length DOUBLE PRECISION DEFAULT 0,
    width DOUBLE PRECISION DEFAULT 0,
    thickness DOUBLE PRECISION DEFAULT 16,
    material VARCHAR(100) DEFAULT '',
    quantity INTEGER DEFAULT 1,
    created_date TIMESTAMP WITH TIME ZONE NOT NULL,
    source_file_name VARCHAR(255) DEFAULT '',
    order_name VARCHAR(255) DEFAULT '',
    is_cut_completed BOOLEAN DEFAULT FALSE,
    cut_completed_date TIMESTAMP WITH TIME ZONE,
    is_edge_banding_completed BOOLEAN DEFAULT FALSE,
    edge_banding_completed_date TIMESTAMP WITH TIME ZONE,
    is_drilling_completed BOOLEAN DEFAULT FALSE,
    drilling_completed_date TIMESTAMP WITH TIME ZONE,
    is_sorting_completed BOOLEAN DEFAULT FALSE,
    sorting_completed_date TIMESTAMP WITH TIME ZONE,
    is_packing_completed BOOLEAN DEFAULT FALSE,
    packing_completed_date TIMESTAMP WITH TIME ZONE,
    requires_cutting BOOLEAN DEFAULT TRUE,
    requires_edge_banding BOOLEAN DEFAULT TRUE,
    requires_drilling BOOLEAN DEFAULT TRUE,
    requires_sorting BOOLEAN DEFAULT TRUE,
    requires_packing BOOLEAN DEFAULT TRUE,
    edge_banding_sides_required INTEGER DEFAULT 0,
    edge_banding_sides_completed INTEGER DEFAULT 0,
    edge_banding_completed_dates VARCHAR(500) DEFAULT ''
);

-- Товари (products)
CREATE TABLE IF NOT EXISTS products (
    id SERIAL PRIMARY KEY,
    project_uuid VARCHAR(100) NOT NULL,
    product_id INTEGER NOT NULL,
    name VARCHAR(255) NOT NULL,
    code VARCHAR(100) DEFAULT '',
    description VARCHAR(255) DEFAULT '',
    count INTEGER DEFAULT 1,
    cost DECIMAL(18,2) DEFAULT 0,
    material_cost DECIMAL(18,2) DEFAULT 0,
    operation_cost DECIMAL(18,2) DEFAULT 0,
    order_date TIMESTAMP WITH TIME ZONE,
    created_date TIMESTAMP WITH TIME ZONE NOT NULL
);

-- Логи сканувань
CREATE TABLE IF NOT EXISTS scan_logs (
    id SERIAL PRIMARY KEY,
    part_id INTEGER NOT NULL,
    qr_code VARCHAR(100) NOT NULL,
    stage INTEGER NOT NULL,
    scan_date TIMESTAMP WITH TIME ZONE NOT NULL,
    user_id VARCHAR(100),
    device_id VARCHAR(50),
    success BOOLEAN DEFAULT TRUE,
    message VARCHAR(500)
);
";

                // Виконуємо створення таблиць
                _logger.LogInformation("📋 Creating tables...");
                Console.WriteLine("📋 Creating tables...");
                
                await using (var command = new NpgsqlCommand(createTablesScript, connection))
                {
                    command.CommandTimeout = 60;
                    await command.ExecuteNonQueryAsync();
                }
                
                _logger.LogInformation("✅ Database tables created successfully");
                Console.WriteLine("✅ Database tables created successfully");

                // SQL скрипт для створення індексів
                var createIndexesScript = @"
-- Індекс для workshop_capacities
CREATE INDEX IF NOT EXISTS ""IX_workshop_capacities_workshop_number"" 
    ON workshop_capacities(workshop_number);

-- Індекс для orders
CREATE INDEX IF NOT EXISTS ""IX_orders_workshop_number_order_date"" 
    ON orders(workshop_number, order_date);

-- Індекс для workshop_production_lead_times
CREATE INDEX IF NOT EXISTS ""IX_workshop_production_lead_times_workshop_number"" 
    ON workshop_production_lead_times(workshop_number);

-- Індекс для workshop_days_before_production
CREATE INDEX IF NOT EXISTS ""IX_workshop_days_before_production_workshop_number"" 
    ON workshop_days_before_production(workshop_number);

-- Індекс для custom_completion_dates
DROP INDEX IF EXISTS ""IX_custom_completion_dates_order_key"";
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_custom_completion_dates_order_key_unique"" 
    ON custom_completion_dates(order_key);

-- Індекс для original_workshops
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_original_workshops_order_key"" 
    ON original_workshops(order_key);

-- =============================================
-- ІНДЕКСИ ДЛЯ СКАНУВАННЯ ДЕТАЛЕЙ
-- =============================================

-- Індекси для imported_projects
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_imported_projects_project_uuid"" 
    ON imported_projects(project_uuid);
CREATE INDEX IF NOT EXISTS ""IX_imported_projects_file_name"" 
    ON imported_projects(file_name);

-- Індекси для parts
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_parts_qr_code"" 
    ON parts(project_external_uuid, part_id, part_counter);
CREATE INDEX IF NOT EXISTS ""IX_parts_source_file"" 
    ON parts(source_file_name);
CREATE INDEX IF NOT EXISTS ""IX_parts_order_name"" 
    ON parts(order_name);
CREATE INDEX IF NOT EXISTS ""IX_parts_completion"" 
    ON parts(is_cut_completed, is_edge_banding_completed, is_drilling_completed, is_sorting_completed, is_packing_completed);

-- Індекси для products
CREATE INDEX IF NOT EXISTS ""IX_products_project_uuid"" 
    ON products(project_uuid, product_id);
CREATE INDEX IF NOT EXISTS ""IX_products_name"" 
    ON products(name);

-- Індекси для scan_logs
CREATE INDEX IF NOT EXISTS ""IX_scan_logs_qr_code"" 
    ON scan_logs(qr_code);
CREATE INDEX IF NOT EXISTS ""IX_scan_logs_scan_date"" 
    ON scan_logs(scan_date);
CREATE INDEX IF NOT EXISTS ""IX_scan_logs_part_id"" 
    ON scan_logs(part_id);
";

                // Виконуємо створення індексів
                _logger.LogInformation("🔍 Creating indexes...");
                Console.WriteLine("🔍 Creating indexes...");
                
                await using (var command = new NpgsqlCommand(createIndexesScript, connection))
                {
                    command.CommandTimeout = 60;
                    await command.ExecuteNonQueryAsync();
                }
                
                _logger.LogInformation("✅ Database indexes created successfully");
                Console.WriteLine("✅ Database indexes created successfully");

                // Перевіряємо що всі таблиці створені (тепер 11 таблиць!)
                var verificationScript = @"
SELECT COUNT(*) 
FROM information_schema.tables 
WHERE table_schema = 'public' 
    AND table_name IN ('workshop_data', 'orders', 'workshop_capacities', 
                       'workshop_production_lead_times', 'workshop_days_before_production',
                       'custom_completion_dates', 'original_workshops',
                       'imported_projects', 'parts', 'products', 'scan_logs');
";

                _logger.LogInformation("🔎 Verifying tables...");
                Console.WriteLine("🔎 Verifying tables...");
                
                await using (var command = new NpgsqlCommand(verificationScript, connection))
                {
                    var tableCount = (long)(await command.ExecuteScalarAsync() ?? 0L);

                    if (tableCount >= 7) // Мінімум 7 базових таблиць
                    {
                        _logger.LogInformation($"✅ {tableCount} tables verified successfully");
                        Console.WriteLine($"✅ {tableCount} tables verified successfully");
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning($"⚠️ Only {tableCount} tables found");
                        Console.WriteLine($"⚠️ Only {tableCount} tables found");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during database migration");
                Console.WriteLine($"❌ Error during database migration: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Перевіряє чи існують таблиці в БД
        /// </summary>
        public async Task<bool> CheckTablesExistAsync(string connectionString)
        {
            try
            {
                _logger.LogInformation("🔍 Checking if tables exist...");
                Console.WriteLine("🔍 Checking if tables exist...");

                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                // Перевіряємо базові таблиці (без scanning - вони опціональні)
                var checkScript = @"
SELECT COUNT(*) 
FROM information_schema.tables 
WHERE table_schema = 'public' 
    AND table_name IN ('workshop_data', 'orders', 'workshop_capacities', 
                       'workshop_production_lead_times', 'workshop_days_before_production',
                       'custom_completion_dates', 'original_workshops');
";

                await using var command = new NpgsqlCommand(checkScript, connection);
                var tableCount = (long)(await command.ExecuteScalarAsync() ?? 0L);

                var exists = tableCount >= 7;
                
                if (exists)
                {
                    _logger.LogInformation($"✅ Base tables exist ({tableCount})");
                    Console.WriteLine($"✅ Base tables exist ({tableCount})");
                    
                    // Перевіряємо чи є scanning таблиці
                    var checkScanningScript = @"
SELECT COUNT(*) 
FROM information_schema.tables 
WHERE table_schema = 'public' 
    AND table_name IN ('imported_projects', 'parts', 'products', 'scan_logs');
";
                    await using var scanCommand = new NpgsqlCommand(checkScanningScript, connection);
                    var scanTableCount = (long)(await scanCommand.ExecuteScalarAsync() ?? 0L);
                    
                    if (scanTableCount < 4)
                    {
                        _logger.LogInformation($"⚠️ Scanning tables missing ({scanTableCount}/4), will create them");
                        Console.WriteLine($"⚠️ Scanning tables missing ({scanTableCount}/4), will create them");
                        return false; // Потрібна міграція для створення scanning таблиць
                    }
                }
                else
                {
                    _logger.LogInformation($"⚠️ Only {tableCount} out of 7 base tables exist, migration needed");
                    Console.WriteLine($"⚠️ Only {tableCount} out of 7 base tables exist, migration needed");
                }
                
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error checking if tables exist");
                Console.WriteLine($"❌ Error checking if tables exist: {ex.Message}");
                return false;
            }
        }
    }
}
