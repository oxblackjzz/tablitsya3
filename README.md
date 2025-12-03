# ?? Система Планування Виробництва

> Веб-додаток для планування та візуалізації завантаження виробничих цехів з діаграмами Ганта

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/Blazor-Server-512BD4?logo=blazor)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Status](https://img.shields.io/badge/Status-Beta-yellow.svg)](https://github.com)

---

## ?? Скріншоти

### Діаграма Ганта
![Gantt Chart](docs/screenshots/gantt-chart.png)

### Планування виробництва
![Production Planning](docs/screenshots/production-planning.png)

### Додавання замовлень
![Add Orders](docs/screenshots/add-orders.png)

---

## ? Основні функції

- ?? **Діаграми Ганта** - візуалізація завантаження 3 цехів (№1, №3, №6)
- ?? **Календарне планування** - урахування робочих/вихідних днів та свят
- ?? **Автоматичний розрахунок** - завантаження цехів в реальному часі
- ?? **Управління замовленнями** - додавання, редагування, видалення
- ?? **Гнучкі фільтри** - за статусом (Усі / В роботі / Незавершені) та періодом
- ?? **Локальне збереження** - дані зберігаються в браузері
- ?? **Адаптивний дизайн** - працює на desktop, tablet, mobile

---

## ?? Демо

**Beta-версія:** https://tablitsya3-beta.azurewebsites.net

?? **Перше завантаження може зайняти 10-30 секунд** (безкоштовний сервер)

---

## ??? Технології

- **Frontend:** Blazor Server
- **Backend:** ASP.NET Core 9.0
- **Styling:** Bootstrap 5 + Custom CSS
- **Icons:** Bootstrap Icons
- **Deployment:** Azure App Service / Railway

---

## ?? Встановлення

### Вимоги

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Windows / macOS / Linux
- Сучасний браузер (Chrome, Edge, Firefox)

### Локальний запуск

```bash
# 1. Клонуйте репозиторій
git clone https://github.com/[ваш-username]/tablitsya3.git
cd tablitsya3

# 2. Перейдіть в папку проекту
cd таблиця3

# 3. Встановіть залежності (автоматично при build)
dotnet restore

# 4. Запустіть проект
dotnet run

# 5. Відкрийте браузер
# https://localhost:7056
```

---

## ?? Розгортання

### Швидкий спосіб (Railway)

[![Deploy on Railway](https://railway.app/button.svg)](https://railway.app/new/template?template=https://github.com/[ваш-repo])

### Azure App Service

```bash
# Використайте готовий скрипт
.\deploy-to-azure.ps1
```

### Детальні інструкції

Дивіться повну документацію:
- [Deployment Guide](DEPLOYMENT_GUIDE.md) - Azure
- [Deployment Alternatives](DEPLOYMENT_ALTERNATIVES.md) - Інші варіанти
- [Quick Start](QUICK_START_GUIDE.md) - За 30 хвилин

---

## ?? Документація

### Для розробників:
- [Quick Start Guide](QUICK_START_GUIDE.md) - Швидкий старт
- [Deployment Guide](DEPLOYMENT_GUIDE.md) - Розгортання на Azure
- [Launch Checklist](LAUNCH_CHECKLIST.md) - Чеклист запуску

### Для користувачів:
- [Beta README](BETA_README.md) - Інструкція для бета-тестерів
- [FAQ](BETA_README.md#-часті-питання-faq) - Часті питання

### Архітектура:
- [Code Behind Refactoring](CODE_BEHIND_REFACTORING.md) - Рефакторинг коду
- [Tooltip Data Sync](TOOLTIP_DATA_SYNC_FINAL.md) - Синхронізація тултіпів

---

## ?? Використання

### 1. Додавання замовлень

```
Меню ? "Додати замовлення" ? Оберіть цех ? Заповніть форму ? Додати
```

### 2. Розрахунок графіків

```
"Планування виробництва" ? "Розрахувати графіки"
```

### 3. Перегляд діаграм

```
Прокрутіть до діаграм ? Наведіть курсор для деталей
```

### 4. Фільтрація

```
Фільтри: "Усі" / "В роботі" / "Незавершені"
Період: Оберіть дати ? "Застосувати"
```

---

## ?? Структура проекту

```
таблиця3/
??? Components/
?   ??? Pages/
?   ?   ??? Home.razor           # Головна сторінка
?   ?   ??? ProductionPlanning.razor # Планування виробництва
?   ?   ??? BulkOrderEntry.razor    # Додавання замовлень
?   ?   ??? LogViewer.razor         # Перегляд логів
?   ??? Layout/
?   ?   ??? MainLayout.razor        # Головний layout
?   ?   ??? NavMenu.razor     # Навігаційне меню
?   ??? GanttChart.razor# Компонент діаграми Ганта
??? Models/
?   ??? Order.cs     # Модель замовлення
???? WorkshopData.cs       # Дані цеху
?   ??? ProductionSchedule.cs       # Розклад виробництва
?   ??? LogEntry.cs      # Запис логу
??? Services/
?   ??? ProductionPlanningService.cs # Сервіс планування
?   ??? DataStorageService.cs        # Збереження даних
?   ??? WorkingDaysService.cs # Робочі дні
?   ??? LoggingService.cs  # Логування
??? wwwroot/
?   ??? app.css  # Глобальні стилі
?   ??? js/app.js         # JavaScript
??? Program.cs          # Точка входу

```

---

## ?? Тестування

### Запуск тестів

```bash
dotnet test
```

### Мануальне тестування

Дивіться [Launch Checklist](LAUNCH_CHECKLIST.md#-тестування-після-розгортання)

---

## ?? Відомі обмеження (Beta)

### Що працює ?
- Планування до 50 замовлень на цех
- Розрахунок завантаження
- Візуалізація діаграм Ганта
- Редагування замовлень
- Фільтрація за статусом
- Збереження даних локально

### В розробці ?
- Експорт в PDF/Excel
- Багатокористувацький режим
- Сповіщення про дедлайни
- Drag & Drop перепланування
- Покращена мобільна версія
- Статистика та аналітика

---

## ?? Внесок в проект

Вітаємо внесок в проект! Ось як ви можете допомогти:

### Повідомлення про помилки

1. Перевірте [Issues](https://github.com/[username]/tablitsya3/issues) чи вже є така помилка
2. Створіть новий Issue з детальним описом
3. Додайте скріншоти та кроки для відтворення

### Пропозиції покращень

1. Створіть Issue з міткою `enhancement`
2. Опишіть що хочете додати і навіщо
3. Чекайте на відповідь від команди

### Pull Requests

1. Fork репозиторій
2. Створіть feature branch (`git checkout -b feature/amazing-feature`)
3. Commit зміни (`git commit -m 'Add amazing feature'`)
4. Push в branch (`git push origin feature/amazing-feature`)
5. Створіть Pull Request

---

## ?? Changelog

### [0.9.0] - 2024-11 - Beta Release

#### Added
- ?? Діаграми Ганта для 3 цехів
- ?? Календарне планування
- ?? Автоматичний розрахунок завантаження
- ?? Управління замовленнями (CRUD)
- ?? Фільтри за статусом та періодом
- ?? Локальне збереження даних
- ?? Базовий адаптивний дизайн

#### Fixed
- ?? Синхронізація тултіпів з даними
- ?? Розтягування діаграм в режимі "В роботі"
- ?? Відображення дат відвантаження

---

## ?? Ліцензія

Цей проект розповсюджується під ліцензією MIT. Дивіться [LICENSE](LICENSE) для деталей.

---

## ?? Автори

- **Ваше ім'я** - *Initial work* - [GitHub](https://github.com/[username])

Дивіться також список [контриб'юторів](https://github.com/[username]/tablitsya3/contributors).

---

## ?? Подяки

- [Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor) - Framework
- [Bootstrap](https://getbootstrap.com/) - CSS Framework
- [Bootstrap Icons](https://icons.getbootstrap.com/) - Icons
- [Azure](https://azure.microsoft.com/) - Hosting
- [Railway](https://railway.app/) - Альтернативний hosting

---

## ?? Контакти

**Email:** [ваш-email]  
**Telegram:** [@ваш-telegram]  
**Website:** [ваш-сайт]

**Issues:** [GitHub Issues](https://github.com/[username]/tablitsya3/issues)  
**Discussions:** [GitHub Discussions](https://github.com/[username]/tablitsya3/discussions)

---

## ?? Статистика

![GitHub stars](https://img.shields.io/github/stars/[username]/tablitsya3?style=social)
![GitHub forks](https://img.shields.io/github/forks/[username]/tablitsya3?style=social)
![GitHub issues](https://img.shields.io/github/issues/[username]/tablitsya3)
![GitHub pull requests](https://img.shields.io/github/issues-pr/[username]/tablitsya3)

---

## ??? Roadmap

### Q4 2024 - Beta
- [x] Базовий функціонал
- [x] Діаграми Ганта
- [x] Управління замовленнями
- [ ] Бета-тестування

### Q1 2025 - Production
- [ ] Експорт в PDF/Excel
- [ ] Збереження на сервері
- [ ] Статистика та звіти
- [ ] Production запуск

### Q2 2025 - Features
- [ ] Сповіщення
- [ ] Drag & Drop
- [ ] Багатокористувацький режим
- [ ] Мобільний додаток

### Q3 2025 - Enterprise
- [ ] AI рекомендації
- [ ] Інтеграція з 1С
- [ ] Управління ресурсами
- [ ] Розширена аналітика

---

## ? Star History

[![Star History Chart](https://api.star-history.com/svg?repos=[username]/tablitsya3&type=Date)](https://star-history.com/#[username]/tablitsya3&Date)

---

<div align="center">

**Зроблено з ?? для виробничих підприємств**

[Website](https://tablitsya3-beta.azurewebsites.net) • 
[Documentation](BETA_README.md) • 
[Report Bug](https://github.com/[username]/tablitsya3/issues) • 
[Request Feature](https://github.com/[username]/tablitsya3/issues)

</div>
```

Створено **[Ваше ім'я]** • © 2024
