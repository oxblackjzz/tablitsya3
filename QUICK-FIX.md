# 🔧 Швидке виправлення помилки БД

## Проблема
❌ **Помилка**: `42P01: relation "workshop_data" does not exist`

## 🆓 ДЛЯ БЕЗКОШТОВНОГО RENDER (БЕЗ WEB SHELL)

### ✅ Автоматична міграція вже додана!

Якщо ви використовуєте **безкоштовний план Render**, таблиці БД будуть **автоматично створені** при старті додатку!

**Нічого робити не потрібно!** Просто:

1. **Створіть PostgreSQL БД** на Render (Free план)
2. **Скопіюйте Internal Database URL**
3. **Додайте змінну `DATABASE_URL`** у Web Service
4. **Deploy** - таблиці створяться автоматично! ✅

📖 Детальна інструкція: **`RENDER-FREE-AUTO-MIGRATION.md`**

---

## 💳 ДЛЯ ПЛАТНОГО RENDER (З WEB SHELL)

### ⚡ Ручне створення через Web Shell (2 хвилини)

### Крок 1: Створіть таблиці в PostgreSQL

1. Відкрийте [Render Dashboard](https://dashboard.render.com)
2. Виберіть вашу **PostgreSQL** базу даних
3. Натисніть **"Connect"** → **"Web Shell"**
4. Скопіюйте і виконайте скрипт з файлу **`Database/create-database.sql`**

```sql
-- Файл знаходиться в Database/create-database.sql
-- Просто скопіюйте його повністю і вставте в Web Shell
```

### Крок 2: Перезапустіть додаток

Render автоматично перезапустить додаток після deploy, або ви можете:
- Dashboard → Your Service → **"Manual Deploy"**

### Крок 3: Перевірте логи

У логах ви повинні побачити:
```
✅ Connected to database
✅ All tables already exist
✅ Initial data seeded
🚀 Application started successfully!
```

---

## ⚠️ Про помилки SQL80001 у Visual Studio

Якщо ви бачите помилки типу `SQL80001: Невірний синтаксис около "CASCADE"` у файлах `.sql`:

- ✅ **Це нормально!** Можна ігнорувати
- Visual Studio перевіряє SQL Server синтаксис
- Наші скрипти написані для PostgreSQL
- Вони **правильні** і працюватимуть у Render

Детальніше: **`Database/README-SQL-ERRORS.md`**

---

## 📚 Детальна документація

### Для безкоштовного плану:
- **`RENDER-FREE-AUTO-MIGRATION.md`** ⭐ - Автоматична міграція БД

### Для платного плану:
- **`DATABASE-FIX-GUIDE.md`** - Повна документація
- **`SQL-ERRORS-EXPLAINED.md`** - Пояснення помилок Visual Studio

---

## 🚀 Швидке розгортання

Використовуйте скрипт для автоматичного розгортання:
```powershell
.\deploy-with-db-init.ps1
```

Скрипт:
- ✅ Зробить commit змін
- ✅ Push на GitHub
- ✅ Покаже інструкції для ініціалізації БД

---

## 📁 Корисні файли

### Для безкоштовного Render:
- **`RENDER-FREE-AUTO-MIGRATION.md`** ⭐ - Інструкція з автоматичною міграцією
- **`Tablitsya3/Services/DatabaseMigrationService.cs`** - Сервіс міграції

### SQL Скрипти (для платного плану):
- **`Database/create-database.sql`** - Створення таблиць і індексів
- **`Database/drop-database.sql`** - Повне очищення БД (обережно!)
- **`Database/README-SQL-ERRORS.md`** - Чому Visual Studio показує помилки

### Документація:
- **`DATABASE-FIX-GUIDE.md`** - Повна документація
- **`SQL-ERRORS-EXPLAINED.md`** - Пояснення помилок Visual Studio
- **`deploy-with-db-init.ps1`** - Скрипт автоматичного розгортання

---

## 🆘 Якщо нічого не допомагає

### Для безкоштовного плану:
1. Перевірте що `DATABASE_URL` додано в Environment Variables
2. Перевірте логи - має бути "Database migration completed"
3. Спробуйте **Clear build cache & deploy**

### Для платного плану:
1. **Видаліть БД повністю**:
   ```sql
   -- Виконайте Database/drop-database.sql
   ```

2. **Створіть заново**:
   ```sql
   -- Виконайте Database/create-database.sql
   ```

3. **Перезапустіть додаток**

---

## 💡 Порада

### Безкоштовний план:
- ✅ Таблиці створюються **автоматично** при першому deploy
- ✅ Дані зберігаються **90 днів**
- ✅ **Не потрібен Web Shell**

### Платний план:
- ✅ Таблиці створюються **один раз вручну**
- ✅ Дані зберігаються **назавжди**
- ✅ Є доступ до Web Shell
