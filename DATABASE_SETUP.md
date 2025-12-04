# 🗄️ Підключення PostgreSQL бази даних

## Що змінилося?

Тепер проєкт підтримує **PostgreSQL базу даних** для збереження даних про замовлення. Дані більше не зберігаються у файлі `workshop-data.json`, тому **не губляться при оновленні коду** на Render!

## ✅ Автоматичне підключення на Render

Render автоматично створить PostgreSQL базу даних згідно з конфігурацією в `render.yaml`:

```yaml
databases:
  - name: tablitsya3-db
    databaseName: tablitsya3
 user: tablitsya3user
    region: frankfurt
    plan: free
```

Після деплою:
1. Render створить безкоштовну PostgreSQL БД
2. Автоматично прив'яже її до веб-сервісу через змінну `DATABASE_URL`
3. При старті додаток:
   - Виконає міграції (створить таблиці)
   - Завантажить початкові дані (seed data)

## 🏠 Локальна розробка

### Варіант 1: Без БД (файлове сховище)

Просто запустіть проєкт - він автоматично використає файлове сховище:

```bash
cd Tablitsya3
dotnet run
```

### Варіант 2: З PostgreSQL

1. **Встановіть PostgreSQL**:
   - Windows: https://www.postgresql.org/download/windows/
   - Створіть базу `tablitsya3`

2. **Налаштуйте connection string** в `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=tablitsya3;Username=postgres;Password=ВАШ_ПАРОЛЬ"
  }
}
```

3. **Запустіть додаток**:

```bash
cd Tablitsya3
dotnet run
```

Міграції виконаються автоматично при старті!

## 📊 Структура бази даних

### Таблиці:

1. **workshop_data** - основні налаштування
   - `id`, `last_updated`, `start_date`
   - `production_lead_time`, `days_before_production`

2. **orders** - замовлення
   - `id`, `workshop_number`, `order_date`
   - `square_meters`, `order_name`

3. **workshop_capacities** - потужність цехів
   - `id`, `workshop_number`, `capacity`

4. **custom_completion_dates** - кастомні дати
   - `id`, `order_key`, `completion_date`

## 🔄 Міграція існуючих даних

Якщо у вас вже є дані у файлі `workshop-data.json`:

1. Додаток автоматично завантажить дані з файлу при першому запуску з БД
2. Після цього дані будуть зберігатися тільки в БД
3. Файл більше не використовується

## 🛠️ Перевірка підключення

При старті додатку дивіться лог:

```
✅ PostgreSQL Database configured
🔄 Running database migrations...
✅ Database migrations completed
🌱 Seeding initial data...
✅ Initial data seeded
🚀 Application started successfully!
```

Або (без БД):

```
⚠️ Using file storage (no database configured)
```

## 🔍 Troubleshooting

### Помилка підключення до БД

1. Перевірте connection string
2. Переконайтеся що PostgreSQL запущений
3. Перевірте що база даних створена
4. Перевірте username/password

### Міграції не виконуються

```bash
cd Tablitsya3
dotnet ef database update
```

### Скинути БД та почати заново

```bash
cd Tablitsya3
dotnet ef database drop
dotnet run  # автоматично створить нову БД
```

## 📝 Зміни в коді

### Program.cs
- Додано підтримку PostgreSQL з автоматичними міграціями
- Fallback до файлового сховища якщо немає БД

### Нові сервіси:
- `DatabaseStorageService` - робота з PostgreSQL
- `UnifiedStorageService` - автоматичний вибір БД/файл

### Нові файли:
- `Data/ApplicationDbContext.cs` - EF Core DbContext
- `Data/Entities/WorkshopEntities.cs` - Entity моделі
- `Migrations/20251203_InitialCreate.cs` - початкова міграція

## ⚙️ Змінні оточення

- `DATABASE_URL` - PostgreSQL connection string (Render формат)
- `ConnectionStrings__DefaultConnection` - .NET формат connection string

## 🎯 Переваги PostgreSQL

✅ Дані не губляться при deploy  
✅ Можливість резервного копіювання  
✅ Швидша робота з великим обсягом даних  
✅ Безкоштовно на Render (Free tier: 90 днів історії)  
✅ Автоматичне масштабування  

---

**Версія:** 2.0  
**Дата:** 03.12.2025  
**Автор:** oxblackjzz
