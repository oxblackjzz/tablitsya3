-- ⚠️ УВАГА: ЦЕЙ СКРИПТ ВИДАЛИТЬ ВСЮ БД!
-- Виконайте його в Render PostgreSQL Web Shell

-- Видаляємо всі таблиці
DROP TABLE IF EXISTS custom_completion_dates CASCADE;
DROP TABLE IF EXISTS workshop_capacities CASCADE;
DROP TABLE IF EXISTS orders CASCADE;
DROP TABLE IF EXISTS workshop_data CASCADE;

-- Видаляємо всі індекси (якщо залишились)
DROP INDEX IF EXISTS "IX_workshop_capacities_workshop_number";
DROP INDEX IF EXISTS "IX_custom_completion_dates_order_key";
DROP INDEX IF EXISTS "IX_orders_workshop_number_order_date";

-- Перевіряємо що все видалено
SELECT tablename FROM pg_tables WHERE schemaname = 'public';
