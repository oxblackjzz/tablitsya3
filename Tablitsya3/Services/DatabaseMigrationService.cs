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

-- Створюємо таблицю workshop_production_lead_times (НОВА!)
CREATE TABLE IF NOT EXISTS workshop_production_lead_times (
    id SERIAL PRIMARY KEY,
    workshop_number INTEGER NOT NULL,
    production_lead_time INTEGER NOT NULL DEFAULT 5
);

-- Створюємо таблицю workshop_days_before_production (НОВА!)
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

-- Індекс для workshop_production_lead_times (НОВИЙ!)
CREATE INDEX IF NOT EXISTS ""IX_workshop_production_lead_times_workshop_number"" 
    ON workshop_production_lead_times(workshop_number);

-- Індекс для workshop_days_before_production (НОВИЙ!)
CREATE INDEX IF NOT EXISTS ""IX_workshop_days_before_production_workshop_number"" 
    ON workshop_days_before_production(workshop_number);

-- Видаляємо старий індекс якщо є
DROP INDEX IF EXISTS ""IX_custom_completion_dates_order_key"";

-- Створюємо унікальний індекс для order_key
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_custom_completion_dates_order_key_unique"" 
    ON custom_completion_dates(order_key);
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

                // Перевіряємо що всі таблиці створені (тепер 6 таблиць!)
                var verificationScript = @"
SELECT COUNT(*) 
FROM information_schema.tables 
WHERE table_schema = 'public' 
    AND table_name IN ('workshop_data', 'orders', 'workshop_capacities', 
                       'workshop_production_lead_times', 'workshop_days_before_production',
                       'custom_completion_dates');
";

                _logger.LogInformation("🔎 Verifying tables...");
                Console.WriteLine("🔎 Verifying tables...");
                
                await using (var command = new NpgsqlCommand(verificationScript, connection))
                {
                    var tableCount = (long)(await command.ExecuteScalarAsync() ?? 0L);

                    if (tableCount == 6)
                    {
                        _logger.LogInformation("✅ All 6 tables verified successfully");
                        Console.WriteLine("✅ All 6 tables verified successfully");
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning($"⚠️ Only {tableCount} out of 6 tables found");
                        Console.WriteLine($"⚠️ Only {tableCount} out of 6 tables found");
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

                var checkScript = @"
SELECT COUNT(*) 
FROM information_schema.tables 
WHERE table_schema = 'public' 
    AND table_name IN ('workshop_data', 'orders', 'workshop_capacities', 
                       'workshop_production_lead_times', 'workshop_days_before_production',
                       'custom_completion_dates');
";

                await using var command = new NpgsqlCommand(checkScript, connection);
                var tableCount = (long)(await command.ExecuteScalarAsync() ?? 0L);

                var exists = tableCount == 6;
                
                if (exists)
                {
                    _logger.LogInformation($"✅ All {tableCount} tables exist");
                    Console.WriteLine($"✅ All {tableCount} tables exist");
                }
                else
                {
                    _logger.LogInformation($"⚠️ Only {tableCount} out of 6 tables exist, migration needed");
                    Console.WriteLine($"⚠️ Only {tableCount} out of 6 tables exist, migration needed");
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
