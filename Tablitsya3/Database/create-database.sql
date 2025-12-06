-- ============================================
-- Створення таблиць БД для Tablitsya3
-- PostgreSQL Database Script
-- ============================================
-- 
-- ⚠️  ІНСТРУКЦІЯ:
-- 1. Відкрийте Render Dashboard (https://dashboard.render.com)
-- 2. Виберіть вашу PostgreSQL базу даних
-- 3. Натисніть "Connect" → "Web Shell"
-- 4. Скопіюйте весь цей файл і вставте в консоль
-- 5. Натисніть Enter для виконання
--
-- ✅ Цей скрипт безпечний:
--    - Використовує IF NOT EXISTS - не видалить існуючі дані
--    - Можна запускати багато разів без ризику
--  - Створює тільки відсутні таблиці та індекси
--
-- ⚠️ ПРИМІТКА для Visual Studio:
--    Помилки SQL80001 можна ігнорувати - це PostgreSQL синтаксис,
--    а Visual Studio перевіряє SQL Server синтаксис
--
-- ============================================

-- Створюємо таблицю workshop_data (основні налаштування)
CREATE TABLE IF NOT EXISTS workshop_data (
    id SERIAL PRIMARY KEY,
    last_updated TIMESTAMP WITH TIME ZONE NOT NULL,
    start_date TIMESTAMP WITH TIME ZONE NOT NULL,
    production_lead_time INTEGER NOT NULL,
    days_before_production INTEGER NOT NULL
);

-- Створюємо таблицю orders (замовлення по цехах)
CREATE TABLE IF NOT EXISTS orders (
    id SERIAL PRIMARY KEY,
    workshop_number INTEGER NOT NULL,
    order_date TIMESTAMP WITH TIME ZONE NOT NULL,
    square_meters DOUBLE PRECISION NOT NULL,
    order_name VARCHAR(500) NOT NULL DEFAULT ''
);

-- Створюємо таблицю workshop_capacities (потужності цехів)
CREATE TABLE IF NOT EXISTS workshop_capacities (
    id SERIAL PRIMARY KEY,
    workshop_number INTEGER NOT NULL,
    capacity INTEGER NOT NULL
);

-- Створюємо таблицю custom_completion_dates (кастомні дати завершення)
CREATE TABLE IF NOT EXISTS custom_completion_dates (
    id SERIAL PRIMARY KEY,
    order_key VARCHAR(200) NOT NULL,
    completion_date TIMESTAMP WITH TIME ZONE NOT NULL
);

-- Створюємо індекси для оптимізації запитів
CREATE INDEX IF NOT EXISTS "IX_workshop_capacities_workshop_number" 
    ON workshop_capacities(workshop_number);

CREATE INDEX IF NOT EXISTS "IX_orders_workshop_number_order_date" 
 ON orders(workshop_number, order_date);

-- Унікальний індекс для order_key
DROP INDEX IF EXISTS "IX_custom_completion_dates_order_key";
CREATE UNIQUE INDEX IF NOT EXISTS "IX_custom_completion_dates_order_key_unique" 
    ON custom_completion_dates(order_key);

-- ============================================
-- ПЕРЕВІРКА РЕЗУЛЬТАТІВ
-- ============================================

-- Перевіряємо що всі таблиці створені
SELECT 
    '✅ Таблиця існує: ' || tablename as status
FROM pg_tables 
WHERE schemaname = 'public' 
    AND tablename IN ('workshop_data', 'orders', 'workshop_capacities', 'custom_completion_dates')
ORDER BY tablename;

-- Перевіряємо кількість записів
SELECT 
    'workshop_data' as table_name,
    COUNT(*) as record_count 
FROM workshop_data
UNION ALL
SELECT 
    'orders' as table_name,
 COUNT(*) as record_count 
FROM orders
UNION ALL
SELECT 
    'workshop_capacities' as table_name,
    COUNT(*) as record_count 
FROM workshop_capacities
UNION ALL
SELECT 
    'custom_completion_dates' as table_name,
    COUNT(*) as record_count 
FROM custom_completion_dates;

-- ============================================
-- Готово! Тепер можете перезапустити додаток на Render
-- ============================================
