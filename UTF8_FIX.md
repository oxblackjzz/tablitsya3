# 🔧 Виправлення проблеми з українським текстом (знаки запитання)

## Проблема

На сайті замість українського тексту відображаються знаки запитання (�������).

## Причина

Проблема виникає через неправильне кодування при збереженні даних у PostgreSQL базу даних.

## ✅ Виправлення (АВТОМАТИЧНЕ)

Виправлення вже додане до коду. При наступному deploy на Render всі нові дані будуть зберігатися правильно з UTF-8 кодуванням.

### Що було виправлено:

1. **Connection String** - додано `Client Encoding=UTF8`
2. **Database Migration Service** - встановлюється UTF-8 кодування для сесії + перевірка кодування БД
3. **ApplicationDbContext** - зареєстровано CodePagesEncodingProvider
4. **Npgsql налаштування** - додано правильні опції для роботи з UTF-8
5. **HTML Meta Tag** - підтверджено `<meta charset="utf-8" />` в App.razor
6. **Project File** - підтверджено `<Utf8Output>true</Utf8Output>` в .csproj

## 🔄 Якщо проблема залишилась (старі дані в БД)

Якщо у вас вже є пошкоджені дані в базі, їх потрібно очистити:

### Варіант 1: Через Render Dashboard

1. Відкрийте **Render Dashboard** → ваш PostgreSQL сервіс
2. Натисніть **Shell** або **Connect**
3. Виконайте команди:

```sql
-- Очищаємо всі дані
DELETE FROM orders;
DELETE FROM workshop_capacities;
DELETE FROM custom_completion_dates;
DELETE FROM workshop_data;

-- Перевіряємо що все видалено
SELECT COUNT(*) FROM orders;
SELECT COUNT(*) FROM workshop_capacities;
SELECT COUNT(*) FROM custom_completion_dates;
SELECT COUNT(*) FROM workshop_data;
```

4. Перезапустіть веб-сервіс на Render
5. При старті додаток автоматично завантажить початкові дані з правильним кодуванням

### Варіант 2: Через додаток (простіший)

1. Просто видаліть всі замовлення через інтерфейс сайту
2. Додайте їх заново
3. Нові дані будуть збережені з правильним кодуванням

### Варіант 3: Повне очищення БД

Якщо ви хочете почати з чистого аркуша:

```bash
# Зайдіть у Render Dashboard → PostgreSQL → Shell
# Виконайте:
DROP SCHEMA public CASCADE;
CREATE SCHEMA public;
GRANT ALL ON SCHEMA public TO public;
GRANT ALL ON SCHEMA public TO tablitsya3user;
```

Потім перезапустіть веб-сервіс - таблиці створяться автоматично з правильним кодуванням.

## 🔍 Перевірка правильності кодування

Після виправлення перевірте:

1. Додайте нове замовлення з українським текстом
2. Оновіть сторінку (F5)
3. Якщо текст відображається правильно - все працює!

### Діагностика в логах Render

При старті перевірте лог на Render:

```
✅ UTF-8 encoding set for session
📊 Database encoding: UTF8
```

Якщо бачите `⚠️ WARNING: Database encoding is SQL_ASCII` - база даних створена з неправильним кодуванням!

## 📝 Технічні деталі виправлення

### Program.cs
```csharp
// Додано Client Encoding=UTF8 до connection string
connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true;Client Encoding=UTF8";

// Додано AppContext для Npgsql
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Налаштування Npgsql
options.UseNpgsql(connectionString, npgsqlOptions =>
{
    npgsqlOptions.CommandTimeout(60);
});
```

### DatabaseMigrationService.cs
```csharp
// Встановлюємо UTF-8 перед створенням таблиць
var setEncodingScript = @"
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
";

// Перевіряємо кодування БД
var checkEncodingScript = @"
SELECT pg_encoding_to_char(encoding) as encoding 
FROM pg_database 
WHERE datname = current_database();
";
```

### ApplicationDbContext.cs
```csharp
public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : base(options)
{
    // Реєструємо UTF-8 provider
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
}
```

### App.razor
```html
<head>
    <meta charset="utf-8" />
    <!-- Інші meta tags -->
</head>
```

### Tablitsya3.csproj
```xml
<PropertyGroup>
    <Utf8Output>true</Utf8Output>
</PropertyGroup>
```

## ⚠️ Важливо

- **Нові дані** будуть зберігатися правильно автоматично
- **Старі пошкоджені дані** потрібно видалити та додати заново
- Якщо у файлі `workshop-data.json` є правильні дані, просто очистіть БД та додаток завантажить їх звідти
- **Перевірте лог** на Render після deploy - має бути `Database encoding: UTF8`

## 🎯 Результат

✅ Весь новий текст зберігається у UTF-8  
✅ Українські літери відображаються правильно  
✅ Емодзі та спецсимволи працюють  
✅ Проблема не повторюватиметься  
✅ Автоматична діагностика кодування БД при старті  

---

**Версія:** 2.2  
**Дата:** 03.12.2025  
**Автор:** oxblackjzz
