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
    qr_code VARCHAR(255) NOT NULL,
    stage INTEGER NOT NULL,
    scan_date TIMESTAMP WITH TIME ZONE NOT NULL,
    user_id VARCHAR(100),
    worker_id INTEGER,
    workstation_id INTEGER,
    session_id INTEGER,
    device_id VARCHAR(50),
    success BOOLEAN DEFAULT TRUE,
    message VARCHAR(500)
);

-- =============================================
-- ТАБЛИЦІ ДЛЯ ПРАЦІВНИКІВ
-- =============================================

-- Працівники
CREATE TABLE IF NOT EXISTS workers (
    id SERIAL PRIMARY KEY,
    worker_code VARCHAR(50) NOT NULL UNIQUE,
    full_name VARCHAR(255) NOT NULL,
    first_name VARCHAR(100) DEFAULT '',
    last_name VARCHAR(100) DEFAULT '',
    middle_name VARCHAR(100) DEFAULT '',
    position VARCHAR(100) DEFAULT '',
    workshop_number INTEGER DEFAULT 1,
    pin_code VARCHAR(10),
    pin_code_hash VARCHAR(255),
    allowed_stages VARCHAR(100) DEFAULT '',
    phone VARCHAR(20),
    email VARCHAR(100),
    hire_date TIMESTAMP WITH TIME ZONE,
    is_active BOOLEAN DEFAULT TRUE,
    created_date TIMESTAMP WITH TIME ZONE NOT NULL,
    updated_date TIMESTAMP WITH TIME ZONE,
    notes VARCHAR(500)
);

-- Робочі станції
CREATE TABLE IF NOT EXISTS workstations (
    id SERIAL PRIMARY KEY,
    station_code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(255) NOT NULL,
    description VARCHAR(500),
    workshop_number INTEGER DEFAULT 1,
    production_stage INTEGER DEFAULT 0,
    location VARCHAR(100),
    is_active BOOLEAN DEFAULT TRUE,
    requires_worker_auth BOOLEAN DEFAULT TRUE,
    session_timeout_minutes INTEGER DEFAULT 60,
    device_identifier VARCHAR(100),
    created_date TIMESTAMP WITH TIME ZONE NOT NULL,
    updated_date TIMESTAMP WITH TIME ZONE
);

-- Сесії працівників
CREATE TABLE IF NOT EXISTS worker_sessions (
    id SERIAL PRIMARY KEY,
    worker_id INTEGER NOT NULL,
    workstation_id INTEGER NOT NULL,
    session_token VARCHAR(100) NOT NULL UNIQUE,
    start_time TIMESTAMP WITH TIME ZONE NOT NULL,
    end_time TIMESTAMP WITH TIME ZONE,
    is_active BOOLEAN DEFAULT TRUE,
    ip_address VARCHAR(50),
    user_agent VARCHAR(500),
    scans_count INTEGER DEFAULT 0,
    last_scan_time TIMESTAMP WITH TIME ZONE
);

-- KPI працівників
CREATE TABLE IF NOT EXISTS worker_kpis (
    id SERIAL PRIMARY KEY,
    worker_id INTEGER NOT NULL,
    date TIMESTAMP WITH TIME ZONE NOT NULL,
    production_stage INTEGER NOT NULL,
    parts_processed INTEGER DEFAULT 0,
    total_square_meters DOUBLE PRECISION DEFAULT 0,
    defects_count INTEGER DEFAULT 0,
    work_minutes INTEGER DEFAULT 0,
    avg_time_per_part DOUBLE PRECISION DEFAULT 0,
    updated_date TIMESTAMP WITH TIME ZONE NOT NULL
);

-- Брак/дефекти
CREATE TABLE IF NOT EXISTS defects (
    id SERIAL PRIMARY KEY,
    part_id INTEGER NOT NULL,
    qr_code VARCHAR(100) NOT NULL,
    worker_id INTEGER,
    workstation_id INTEGER,
    production_stage INTEGER NOT NULL,
    defect_type VARCHAR(100) DEFAULT '',
    description VARCHAR(1000),
    severity INTEGER DEFAULT 1,
    is_repairable BOOLEAN DEFAULT TRUE,
    status VARCHAR(20) DEFAULT 'new',
    repaired_by_worker_id INTEGER,
    repaired_date TIMESTAMP WITH TIME ZONE,
    repair_notes VARCHAR(500),
    created_date TIMESTAMP WITH TIME ZONE NOT NULL,
    updated_date TIMESTAMP WITH TIME ZONE
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

                // =============================================
                // ДОДАВАННЯ НОВИХ КОЛОНОК ДО ІСНУЮЧИХ ТАБЛИЦЬ
                // =============================================
                var alterTablesScript = @"
-- Додаємо колонки worker_id, workstation_id, session_id до scan_logs якщо їх немає
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'scan_logs' AND column_name = 'worker_id') THEN
        ALTER TABLE scan_logs ADD COLUMN worker_id INTEGER;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'scan_logs' AND column_name = 'workstation_id') THEN
        ALTER TABLE scan_logs ADD COLUMN workstation_id INTEGER;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'scan_logs' AND column_name = 'session_id') THEN
        ALTER TABLE scan_logs ADD COLUMN session_id INTEGER;
    END IF;
END $$;

-- Збільшуємо розмір колонки qr_code з 100 до 255 символів
ALTER TABLE scan_logs ALTER COLUMN qr_code TYPE VARCHAR(255);
";

                _logger.LogInformation("🔧 Altering tables to add new columns...");
                Console.WriteLine("🔧 Altering tables to add new columns...");
                
                await using (var command = new NpgsqlCommand(alterTablesScript, connection))
                {
                    command.CommandTimeout = 60;
                    await command.ExecuteNonQueryAsync();
                }
                
                _logger.LogInformation("✅ Table alterations completed successfully");
                Console.WriteLine("✅ Table alterations completed successfully");

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
CREATE INDEX IF NOT EXISTS ""IX_scan_logs_worker_id"" 
    ON scan_logs(worker_id);
CREATE INDEX IF NOT EXISTS ""IX_scan_logs_workstation_id"" 
    ON scan_logs(workstation_id);

-- =============================================
-- ІНДЕКСИ ДЛЯ ПРАЦІВНИКІВ
-- =============================================

-- Індекси для workers
CREATE INDEX IF NOT EXISTS ""IX_workers_workshop_number"" 
    ON workers(workshop_number);
CREATE INDEX IF NOT EXISTS ""IX_workers_is_active"" 
    ON workers(is_active);

-- Індекси для workstations
CREATE INDEX IF NOT EXISTS ""IX_workstations_workshop_number"" 
    ON workstations(workshop_number);
CREATE INDEX IF NOT EXISTS ""IX_workstations_production_stage"" 
    ON workstations(production_stage);
CREATE INDEX IF NOT EXISTS ""IX_workstations_is_active"" 
    ON workstations(is_active);

-- Індекси для worker_sessions
CREATE INDEX IF NOT EXISTS ""IX_worker_sessions_worker_id"" 
    ON worker_sessions(worker_id);
CREATE INDEX IF NOT EXISTS ""IX_worker_sessions_workstation_id"" 
    ON worker_sessions(workstation_id);
CREATE INDEX IF NOT EXISTS ""IX_worker_sessions_active_workstation"" 
    ON worker_sessions(is_active, workstation_id);

-- Індекси для worker_kpis
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_worker_kpis_worker_date_stage"" 
    ON worker_kpis(worker_id, date, production_stage);
CREATE INDEX IF NOT EXISTS ""IX_worker_kpis_date"" 
    ON worker_kpis(date);

-- Індекси для defects
CREATE INDEX IF NOT EXISTS ""IX_defects_part_id"" 
    ON defects(part_id);
CREATE INDEX IF NOT EXISTS ""IX_defects_worker_id"" 
    ON defects(worker_id);
CREATE INDEX IF NOT EXISTS ""IX_defects_status"" 
    ON defects(status);
CREATE INDEX IF NOT EXISTS ""IX_defects_created_date"" 
    ON defects(created_date);
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

                // Перевіряємо що всі таблиці створені (тепер 16 таблиць!)
                var verificationScript = @"
SELECT COUNT(*) 
FROM information_schema.tables 
WHERE table_schema = 'public' 
    AND table_name IN ('workshop_data', 'orders', 'workshop_capacities', 
                       'workshop_production_lead_times', 'workshop_days_before_production',
                       'custom_completion_dates', 'original_workshops',
                       'imported_projects', 'parts', 'products', 'scan_logs',
                       'workers', 'workstations', 'worker_sessions', 'worker_kpis', 'defects');
";

                _logger.LogInformation("🔎 Verifying tables...");
                Console.WriteLine("🔎 Verifying tables...");
                
                await using (var command = new NpgsqlCommand(verificationScript, connection))
                {
                    var tableCount = (long)(await command.ExecuteScalarAsync() ?? 0L);

                    if (tableCount >= 16) // 16 таблиць (7 базових + 4 scanning + 5 workers)
                    {
                        _logger.LogInformation($"✅ All {tableCount} tables verified successfully");
                        Console.WriteLine($"✅ All {tableCount} tables verified successfully");
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning($"⚠️ Only {tableCount} out of 16 tables found");
                        Console.WriteLine($"⚠️ Only {tableCount} out of 16 tables found");
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

                // Перевіряємо ВСІ таблиці включно зі scanning та workers
                var checkScript = @"
SELECT COUNT(*) 
FROM information_schema.tables 
WHERE table_schema = 'public' 
    AND table_name IN ('workshop_data', 'orders', 'workshop_capacities', 
                       'workshop_production_lead_times', 'workshop_days_before_production',
                       'custom_completion_dates', 'original_workshops',
                       'imported_projects', 'parts', 'products', 'scan_logs',
                       'workers', 'workstations', 'worker_sessions', 'worker_kpis', 'defects');
";

                await using var command = new NpgsqlCommand(checkScript, connection);
                var tableCount = (long)(await command.ExecuteScalarAsync() ?? 0L);

                // Потрібно 16 таблиць (7 базових + 4 scanning + 5 workers)
                var allTablesExist = tableCount >= 16;
                
                if (allTablesExist)
                {
                    _logger.LogInformation($"✅ All {tableCount} tables exist");
                    Console.WriteLine($"✅ All {tableCount} tables exist");
                }
                else
                {
                    _logger.LogInformation($"⚠️ Only {tableCount} out of 16 tables exist, migration needed");
                    Console.WriteLine($"⚠️ Only {tableCount} out of 16 tables exist, migration needed");
                }
                
                return allTablesExist;
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
