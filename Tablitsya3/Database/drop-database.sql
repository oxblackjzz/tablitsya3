-- ============================================
-- ⚠️ УВАГА: ЦЕЙ СКРИПТ ВИДАЛИТЬ ВСЮ БД!
-- ============================================
--
-- Використовуйте цей скрипт ТІЛЬКИ якщо хочете:
-- - Повністю очистити базу даних
-- - Почати з чистого аркуша
-- - Видалити всі дані та структуру
--
-- ІНСТРУКЦІЯ:
-- 1. Відкрийте Render Dashboard → PostgreSQL → Web Shell
-- 2. Скопіюйте весь цей файл
-- 3. Вставте в консоль і натисніть Enter
--
-- ⚠️ ПІСЛЯ ВИКОНАННЯ цього скрипту:
--    - Запустіть create-database.sql для створення таблиць
--    - Дані будуть втрачені безповоротно!
--
-- ============================================

-- Видаляємо всі індекси спочатку
DROP INDEX IF EXISTS "IX_custom_completion_dates_order_key_unique";
DROP INDEX IF EXISTS "IX_custom_completion_dates_order_key";
DROP INDEX IF EXISTS "IX_orders_workshop_number_order_date";
DROP INDEX IF EXISTS "IX_workshop_capacities_workshop_number";

-- Видаляємо всі таблиці (CASCADE видалить всі залежності)
DROP TABLE IF EXISTS custom_completion_dates CASCADE;
DROP TABLE IF EXISTS workshop_capacities CASCADE;
DROP TABLE IF EXISTS orders CASCADE;
DROP TABLE IF EXISTS workshop_data CASCADE;

-- ============================================
-- ПЕРЕВІРКА РЕЗУЛЬТАТІВ
-- ============================================

-- Перевіряємо що все видалено (має повернути порожній результат)
SELECT 
    '⚠️ Таблиця ще існує: ' || tablename as warning
FROM pg_tables 
WHERE schemaname = 'public' 
    AND tablename IN ('workshop_data', 'orders', 'workshop_capacities', 'custom_completion_dates');

-- Показуємо всі таблиці що залишились
SELECT 
    tablename as remaining_tables
FROM pg_tables 
WHERE schemaname = 'public';

-- ============================================
-- Готово! Тепер запустіть create-database.sql
-- щоб створити структуру БД заново
-- ============================================
