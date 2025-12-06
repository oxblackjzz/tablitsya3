# Виправлення помилки "relation does not exist"

## Проблема
Помилка `42P01: relation "workshop_data" does not exist` означає, що таблиці в PostgreSQL базі даних не були створені.

## Рішення

### Варіант 1: Автоматичне створення через скрипт (РЕКОМЕНДОВАНО)

1. **Підключіться до PostgreSQL через Render Dashboard:**
   - Перейдіть на https://dashboard.render.com
   - Виберіть вашу PostgreSQL базу даних
   - Натисніть кнопку "Connect" → "External Connection"
   - Скопіюйте команду psql або відкрийте Web Shell

2. **Виконайте SQL скрипт:**
   - Відкрийте файл `create-database.sql` з репозиторію
   - Скопіюйте весь вміст файлу
   - Вставте в PostgreSQL консоль і виконайте (Enter)

3. **Перевірте результат:**
   ```sql
   SELECT tablename FROM pg_tables WHERE schemaname = 'public';
   ```
   Ви повинні побачити таблиці:
   - workshop_data
   - orders
   - workshop_capacities
   - custom_completion_dates

### Варіант 2: Пересоздати БД повністю

Якщо у вас є проблеми з існуючою БД, ви можете її повністю видалити і створити заново:

1. **Видаліть всі таблиці:**
   ```sql
   DROP TABLE IF EXISTS custom_completion_dates CASCADE;
   DROP TABLE IF EXISTS workshop_capacities CASCADE;
   DROP TABLE IF EXISTS orders CASCADE;
   DROP TABLE IF EXISTS workshop_data CASCADE;
   ```

2. **Створіть таблиці заново:**
   - Виконайте скрипт з файлу `create-database.sql`

### Варіант 3: Використовуйте файлове сховище (тимчасово)

Якщо вам потрібно швидко запустити додаток без БД:

1. Видаліть змінну оточення `DATABASE_URL` з Render
2. Додаток автоматично переключиться на файлове сховище
3. Дані будуть зберігатися у файлі `workshop-data.json`

## Після виправлення

1. **Перезапустіть додаток на Render:**
   - Dashboard → Services → ваш сервіс → "Manual Deploy" → "Deploy latest commit"

2. **Перевірте логи:**
   - Ви повинні побачити: `✅ Database schema created/verified`
   - І далі: `✅ Initial data seeded` або `ℹ️ Database already contains data`

3. **Перевірте роботу додатку:**
   - Відкрийте додаток в браузері
   - Спробуйте зберегти дані
   - Переконайтеся що помилка зникла

## Корисні команди для діагностики

```sql
-- Перевірити чи існують таблиці
SELECT tablename FROM pg_tables WHERE schemaname = 'public';

-- Перевірити структуру таблиці
\d workshop_data

-- Перевірити кількість записів
SELECT COUNT(*) FROM workshop_data;
SELECT COUNT(*) FROM orders;
SELECT COUNT(*) FROM workshop_capacities;
SELECT COUNT(*) FROM custom_completion_dates;

-- Видалити всі дані (але залишити таблиці)
TRUNCATE TABLE custom_completion_dates, workshop_capacities, orders, workshop_data CASCADE;
```

## Контрольний список

- [ ] Створено таблиці через `create-database.sql`
- [ ] Перевірено наявність таблиць в БД
- [ ] Перезапущено додаток на Render
- [ ] Перевірено логи (мають бути повідомлення про успішне підключення)
- [ ] Перевірено роботу додатку (збереження даних працює)

## Додаткові ресурси

- Файл для створення БД: `create-database.sql`
- Файл для видалення БД: `drop-database.sql`
- Документація PostgreSQL: https://www.postgresql.org/docs/
- Документація Render: https://render.com/docs/databases
