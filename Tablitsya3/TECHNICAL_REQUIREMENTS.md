# 📋 Технічне завдання (ТЗ)
# Система управління меблевим виробництвом "Tablitsya3"

---

## 📌 1. Загальні відомості

### 1.1 Найменування системи
**Tablitsya3** — Система відстеження виробництва меблевих деталей з використанням QR-кодів.

### 1.2 Призначення документа
Цей документ є технічним завданням для розробки програмного забезпечення з нуля. Він містить повний опис функціональних та нефункціональних вимог до системи.

### 1.3 Замовник
Меблеве виробництво (виробничий цех).

### 1.4 Терміни виконання
| Етап | Тривалість |
|------|-----------|
| Аналіз та проектування | 2 тижні |
| Розробка MVP | 6 тижнів |
| Тестування | 2 тижні |
| Впровадження | 1 тиждень |
| **Загалом** | **11 тижнів** |

---

## 📌 2. Опис предметної області

### 2.1 Бізнес-процес виробництва

```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│  1. ПОРІЗКА │───▶│ 2. КРОМКА   │───▶│ 3. СВЕРД-   │───▶│ 4. СОРТУ-   │───▶│ 5. ПАКУ-    │
│             │    │             │    │    ЛІННЯ    │    │    ВАННЯ    │    │    ВАННЯ    │
│ ✂️ Cutting  │    │ 📦 Edge     │    │ ⚙️ Drilling │    │ 📊 Sorting  │    │ 📤 Packing  │
└─────────────┘    │    Banding  │    └─────────────┘    └─────────────┘    └─────────────┘
                   └─────────────┘
```

### 2.2 Учасники процесу

| Роль | Функції |
|------|---------|
| **Адміністратор** | Налаштування системи, імпорт проектів, перегляд статистики |
| **Оператор станції** | Сканування деталей на своєму етапі виробництва |
| **Менеджер виробництва** | Планування, аналіз KPI, моніторинг прогресу |

### 2.3 Ключові сутності

- **Проект** — XML-файл з CAD-системи, що містить інформацію про замовлення
- **Товар** — готовий виріб (шафа, тумба, полиця)
- **Деталь** — одиничний елемент виробу з унікальним QR-кодом
- **Робоча станція** — місце з обладнанням для виконання конкретного етапу
- **Працівник** — особа, що виконує операції на станції

---

## 📌 3. Функціональні вимоги

### 3.1 Модуль імпорту проектів (FR-IMPORT)

#### FR-IMPORT-01: Завантаження XML-файлів
**Опис:** Система повинна приймати XML-файли формату `.project` з CAD-системи.

**Вхідні дані:**
- XML-файл до 50 МБ
- Кодування UTF-8

**Очікувана поведінка:**
1. Валідація структури XML
2. Парсинг атрибутів проекту, товарів, деталей
3. Генерація унікальних QR-кодів для кожної деталі
4. Збереження в базу даних

**Критерії прийняття:**
- [ ] Файл валідується за 2 секунди
- [ ] Парсинг 1000 деталей займає < 5 секунд
- [ ] Дублікати проектів виявляються автоматично

#### FR-IMPORT-02: Визначення вимог до етапів
**Опис:** Для кожної деталі автоматично визначаються необхідні етапи обробки.

| Атрибут | Правило |
|---------|---------|
| Порізка | Завжди потрібна для всіх деталей |
| Кромка | Якщо `eb1`, `eb2`, `eb3` або `eb4` = "1" |
| Свердління | Якщо деталь присутня в операції типу "XNC" |
| Сортування | Завжди потрібне |
| Пакування | Завжди потрібне |

#### FR-IMPORT-03: Генерація QR-кодів
**Формат:** `{project_uuid}/{part_id}/{part_counter}`

**Приклад:** `550e8400-e29b-41d4-a716-446655440000/123/1`

---

### 3.2 Модуль сканування (FR-SCAN)

#### FR-SCAN-01: Обробка сканування
**Опис:** Система приймає відсканований QR-код і оновлює статус деталі.

**Алгоритм:**
```
1. Парсинг QR-коду → отримання project_uuid, part_id, part_counter
2. Пошук деталі в БД
3. Визначення поточного етапу деталі
4. Перевірка: чи етап станції відповідає поточному етапу деталі?
5. Якщо так → оновлення статусу, логування
6. Якщо ні → повернення помилки з поясненням
```

**Можливі результати:**

| Код | Статус | Опис |
|-----|--------|------|
| `SUCCESS` | ✅ | Етап завершено |
| `ALREADY_COMPLETED` | ⚠️ | Етап вже був завершений раніше |
| `WRONG_STAGE` | ❌ | Деталь не готова до цього етапу |
| `NOT_FOUND` | ❌ | Деталь не знайдена в базі |
| `INVALID_QR` | ❌ | Невірний формат QR-коду |

#### FR-SCAN-02: Багаторазове сканування кромки
**Опис:** Етап "Поклейка кромки" може потребувати кількох сканувань (1-4 сторони).

**Логіка:**
- Кількість необхідних сканувань = кількість сторін з кромкою
- Кожне сканування інкрементує лічильник `edge_banding_sides_completed`
- Етап завершується коли `sides_completed >= sides_required`

#### FR-SCAN-03: Автовизначення етапу
**Опис:** Якщо етап не вказано явно, система визначає його автоматично.

```
function getCurrentStage(part):
    if requires_cutting AND NOT is_cut_completed → return CUTTING
    if requires_edge_banding AND NOT fully_completed → return EDGE_BANDING
    if requires_drilling AND NOT is_drilling_completed → return DRILLING
    if requires_sorting AND NOT is_sorting_completed → return SORTING
    if requires_packing AND NOT is_packing_completed → return PACKING
    return NULL (всі етапи завершені)
```

---

### 3.3 Модуль авторизації (FR-AUTH)

#### FR-AUTH-01: Авторизація працівника на станції
**Опис:** Перед початком роботи оператор авторизується скануванням бейджа.

**Процес:**
1. Сканування коду працівника
2. Введення PIN-коду (опціонально)
3. Перевірка прав доступу до етапу станції
4. Створення сесії

#### FR-AUTH-02: Сесії та таймаути
**Опис:** Система підтримує сесії з автоматичним завершенням.

| Параметр | Значення |
|----------|----------|
| Таймаут бездіяльності | Налаштовується (15-480 хв) |
| Одночасні сесії | Заборонені (одна сесія на працівника) |
| Автоматичний logout | При закритті браузера/сканері |

#### FR-AUTH-03: Права доступу
**Опис:** Кожен працівник має список дозволених етапів.

**Формат:** Рядок `"1,2,3"` означає доступ до етапів 1, 2, 3.

---

### 3.4 Модуль управління працівниками (FR-WORKER)

#### FR-WORKER-01: CRUD операції
- Створення працівника (код, ПІБ, посада, цех)
- Редагування даних
- Деактивація (soft delete)
- Пошук та фільтрація

#### FR-WORKER-02: Генерація бейджів
**Опис:** Система генерує PDF з QR-кодом для друку бейджа.

---

### 3.5 Модуль робочих станцій (FR-STATION)

#### FR-STATION-01: Налаштування станцій
| Поле | Тип | Опис |
|------|-----|------|
| `station_code` | string | Унікальний код |
| `name` | string | Назва станції |
| `production_stage` | enum | Етап виробництва (1-5) |
| `workshop_number` | int | Номер цеху |
| `requires_auth` | bool | Чи потрібна авторизація |
| `session_timeout` | int | Таймаут сесії (хв) |

---

### 3.6 Модуль статистики та KPI (FR-STATS)

#### FR-STATS-01: Денна статистика
**Метрики:**
- Кількість оброблених деталей
- Оброблена площа (м²)
- Кількість дефектів
- Відсоток браку

#### FR-STATS-02: KPI працівників
**Метрики на працівника:**
- Деталей за зміну
- Середній час на деталь
- Відсоток браку

#### FR-STATS-03: Прогрес проекту
**Візуалізація:**
- Відсоток завершення по етапах
- Очікуваний час завершення
- Bottleneck-аналіз

---

### 3.7 Модуль планування (FR-PLAN)

#### FR-PLAN-01: Розрахунок графіку виробництва
**Вхідні дані:**
- Список замовлень з датами та площами
- Потужність цеху (м²/день)
- Календар робочих днів

**Результат:**
- Дати початку/завершення для кожного замовлення
- Завантаженість по днях
- Попередження про перевантаження

---

### 3.8 Модуль дефектів (FR-DEFECT)

#### FR-DEFECT-01: Реєстрація браку
**Поля:**
- Деталь (QR-код)
- Тип дефекту (справочник)
- Етап виявлення
- Критичність (1-5)
- Чи можна виправити

#### FR-DEFECT-02: Workflow браку
```
NEW → IN_PROGRESS → REPAIRED
                  ↘ SCRAPPED
```

---

## 📌 4. Нефункціональні вимоги

### 4.1 Продуктивність (NFR-PERF)

| Метрика | Вимога |
|---------|--------|
| Час відповіді API | < 200 мс (p95) |
| Сканування/секунду | ≥ 50 |
| Імпорт 1000 деталей | < 10 секунд |
| Одночасні користувачі | ≥ 100 |

### 4.2 Надійність (NFR-REL)

| Метрика | Вимога |
|---------|--------|
| Uptime | 99.5% |
| Втрата даних | 0% (транзакційність) |
| Backup | Щоденно, зберігання 30 днів |

### 4.3 Безпека (NFR-SEC)

- PIN-коди зберігаються як bcrypt-хеш
- HTTPS обов'язковий
- Сесійні токени — UUID v4
- Логування всіх дій

### 4.4 Масштабованість (NFR-SCALE)

- Горизонтальне масштабування API-серверів
- Read-replica для БД
- Кешування довідників

### 4.5 Локалізація (NFR-L10N)

- Основна мова: українська
- Кодування: UTF-8
- Формат дати: DD.MM.YYYY
- Формат часу: 24-годинний

---

## 📌 5. Архітектура системи

### 5.1 Компонентна діаграма

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              FRONTEND                                        │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────────────┐  │
│  │   Web UI        │  │  Scanner UI     │  │   Admin Panel               │  │
│  │  (Blazor/React) │  │  (PWA/Mobile)   │  │                             │  │
│  └────────┬────────┘  └────────┬────────┘  └─────────────┬───────────────┘  │
└───────────┼─────────────────────┼─────────────────────────┼─────────────────┘
            │                     │                         │
            └─────────────────────┼─────────────────────────┘
                                  │ HTTPS/WebSocket
                                  ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                               BACKEND                                        │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                         API Layer (REST + WebSocket)                 │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                     │                                        │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────────────┐   │
│  │ScanningService│ │WorkerService │ │ImportService │ │  StatsService    │   │
│  └──────────────┘ └──────────────┘ └──────────────┘ └──────────────────┘   │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────────────┐   │
│  │ AuthService  │ │DefectService │ │PlanningService││  KPIService      │   │
│  └──────────────┘ └──────────────┘ └──────────────┘ └──────────────────┘   │
│                                     │                                        │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                        Repository Layer                              │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────┼───────────────────────────────────────┘
                                      │
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                              DATABASE                                        │
│                        PostgreSQL / SQLite                                   │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 5.2 Технологічний стек (рекомендований)

| Компонент | Технологія | Альтернативи |
|-----------|------------|--------------|
| Backend | ASP.NET Core 8+ | Node.js, Go, Python |
| Frontend | Blazor Server/WASM | React, Vue, Angular |
| Database | PostgreSQL | MySQL, SQL Server |
| ORM | Entity Framework Core | Dapper |
| Cache | In-Memory / Redis | — |
| Real-time | SignalR | WebSocket |
| Auth | JWT + Cookie | — |

### 5.3 Структура проекту

```
/src
├── /Api
│   ├── /Controllers
│   │   ├── ScanningController.cs
│   │   ├── WorkersController.cs
│   │   ├── ProjectsController.cs
│   │   └── StatsController.cs
│   └── /Middleware
│       └── AuthMiddleware.cs
├── /Core
│   ├── /Models
│   │   ├── Part.cs
│   │   ├── Worker.cs
│   │   ├── Workstation.cs
│   │   └── ProductionStage.cs
│   ├── /Services
│   │   ├── ScanningService.cs
│   │   ├── WorkerService.cs
│   │   ├── ImportService.cs
│   │   └── PlanningService.cs
│   └── /Interfaces
│       └── IRepository.cs
├── /Infrastructure
│   ├── /Data
│   │   ├── ApplicationDbContext.cs
│   │   └── /Entities
│   │       ├── PartEntity.cs
│   │       └── ...
│   └── /Repositories
│       └── PartRepository.cs
├── /Web
│   ├── /Components
│   │   ├── ScannerPage.razor
│   │   └── DashboardPage.razor
│   └── /wwwroot
└── /Tests
    ├── /Unit
    └── /Integration
```

---

## 📌 6. Схема бази даних

### 6.1 ER-діаграма (текстова)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              IMPORTED_PROJECTS                               │
├─────────────────────────────────────────────────────────────────────────────┤
│ id (PK)              │ INT           │ AUTO_INCREMENT                        │
│ project_uuid         │ VARCHAR(50)   │ UNIQUE, NOT NULL                      │
│ file_name            │ VARCHAR(255)  │ NOT NULL                              │
│ imported_at          │ DATETIME      │ DEFAULT CURRENT_TIMESTAMP             │
│ total_cost           │ DECIMAL(12,2) │ DEFAULT 0                             │
│ material_cost        │ DECIMAL(12,2) │ DEFAULT 0                             │
│ operation_cost       │ DECIMAL(12,2) │ DEFAULT 0                             │
│ currency             │ VARCHAR(10)   │ DEFAULT 'грн'                         │
│ products_count       │ INT           │ DEFAULT 0                             │
│ parts_count          │ INT           │ DEFAULT 0                             │
│ total_square_meters  │ FLOAT         │ DEFAULT 0                             │
│ workshop_number      │ INT           │ DEFAULT 1                             │
│ is_active            │ BOOLEAN       │ DEFAULT TRUE                          │
└─────────────────────────────────────────────────────────────────────────────┘
                                      │
                                      │ 1:N
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                                   PARTS                                      │
├─────────────────────────────────────────────────────────────────────────────┤
│ id (PK)                      │ INT           │ AUTO_INCREMENT                │
│ project_uuid (FK)            │ VARCHAR(50)   │ NOT NULL                      │
│ part_id                      │ INT           │ NOT NULL                      │
│ part_counter                 │ INT           │ NOT NULL                      │
│ name                         │ VARCHAR(255)  │ NOT NULL                      │
│ code                         │ VARCHAR(100)  │                               │
│ length                       │ FLOAT         │ NOT NULL                      │
│ width                        │ FLOAT         │ NOT NULL                      │
│ thickness                    │ FLOAT         │ DEFAULT 16                    │
│ material                     │ VARCHAR(255)  │                               │
│ order_name                   │ VARCHAR(255)  │                               │
│ ─────────────────────────────┼───────────────┼───────────────────────────────│
│ requires_cutting             │ BOOLEAN       │ DEFAULT TRUE                  │
│ requires_edge_banding        │ BOOLEAN       │ DEFAULT FALSE                 │
│ requires_drilling            │ BOOLEAN       │ DEFAULT FALSE                 │
│ requires_sorting             │ BOOLEAN       │ DEFAULT TRUE                  │
│ requires_packing             │ BOOLEAN       │ DEFAULT TRUE                  │
│ ─────────────────────────────┼───────────────┼───────────────────────────────│
│ is_cut_completed             │ BOOLEAN       │ DEFAULT FALSE                 │
│ cut_completed_at             │ DATETIME      │ NULL                          │
│ is_edge_banding_completed    │ BOOLEAN       │ DEFAULT FALSE                 │
│ edge_banding_completed_at    │ DATETIME      │ NULL                          │
│ edge_banding_sides_required  │ INT           │ DEFAULT 0                     │
│ edge_banding_sides_completed │ INT           │ DEFAULT 0                     │
│ is_drilling_completed        │ BOOLEAN       │ DEFAULT FALSE                 │
│ drilling_completed_at        │ DATETIME      │ NULL                          │
│ is_sorting_completed         │ BOOLEAN       │ DEFAULT FALSE                 │
│ sorting_completed_at         │ DATETIME      │ NULL                          │
│ is_packing_completed         │ BOOLEAN       │ DEFAULT FALSE                 │
│ packing_completed_at         │ DATETIME      │ NULL                          │
│ ─────────────────────────────┼───────────────┼───────────────────────────────│
│ created_at                   │ DATETIME      │ DEFAULT CURRENT_TIMESTAMP     │
│ updated_at                   │ DATETIME      │ NULL                          │
│ ─────────────────────────────┼───────────────┼───────────────────────────────│
│ UNIQUE(project_uuid, part_id, part_counter)                                  │
└─────────────────────────────────────────────────────────────────────────────┘


┌─────────────────────────────────────────────────────────────────────────────┐
│                                  WORKERS                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│ id (PK)              │ INT           │ AUTO_INCREMENT                        │
│ worker_code          │ VARCHAR(50)   │ UNIQUE, NOT NULL                      │
│ full_name            │ VARCHAR(255)  │ NOT NULL                              │
│ first_name           │ VARCHAR(100)  │                                       │
│ last_name            │ VARCHAR(100)  │                                       │
│ middle_name          │ VARCHAR(100)  │                                       │
│ position             │ VARCHAR(100)  │                                       │
│ workshop_number      │ INT           │ DEFAULT 1                             │
│ pin_code_hash        │ VARCHAR(255)  │ NULL                                  │
│ allowed_stages       │ VARCHAR(100)  │ NULL (e.g., "1,2,3")                  │
│ phone                │ VARCHAR(20)   │                                       │
│ email                │ VARCHAR(100)  │                                       │
│ hire_date            │ DATE          │                                       │
│ is_active            │ BOOLEAN       │ DEFAULT TRUE                          │
│ created_at           │ DATETIME      │ DEFAULT CURRENT_TIMESTAMP             │
│ updated_at           │ DATETIME      │ NULL                                  │
└─────────────────────────────────────────────────────────────────────────────┘


┌─────────────────────────────────────────────────────────────────────────────┐
│                               WORKSTATIONS                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│ id (PK)                   │ INT           │ AUTO_INCREMENT                   │
│ station_code              │ VARCHAR(50)   │ UNIQUE, NOT NULL                 │
│ name                      │ VARCHAR(255)  │ NOT NULL                         │
│ description               │ TEXT          │                                  │
│ workshop_number           │ INT           │ DEFAULT 1                        │
│ production_stage          │ INT           │ NOT NULL (1-5)                   │
│ location                  │ VARCHAR(100)  │                                  │
│ is_active                 │ BOOLEAN       │ DEFAULT TRUE                     │
│ requires_worker_auth      │ BOOLEAN       │ DEFAULT TRUE                     │
│ session_timeout_minutes   │ INT           │ DEFAULT 60                       │
│ capacity                  │ DECIMAL(10,2) │ NULL (м²/день)                   │
│ device_identifier         │ VARCHAR(100)  │                                  │
│ created_at                │ DATETIME      │ DEFAULT CURRENT_TIMESTAMP        │
│ updated_at                │ DATETIME      │ NULL                             │
└─────────────────────────────────────────────────────────────────────────────┘


┌─────────────────────────────────────────────────────────────────────────────┐
│                              WORKER_SESSIONS                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│ id (PK)              │ INT           │ AUTO_INCREMENT                        │
│ worker_id (FK)       │ INT           │ REFERENCES workers(id)                │
│ workstation_id (FK)  │ INT           │ REFERENCES workstations(id)           │
│ session_token        │ VARCHAR(100)  │ UNIQUE, NOT NULL                      │
│ start_time           │ DATETIME      │ NOT NULL                              │
│ end_time             │ DATETIME      │ NULL                                  │
│ is_active            │ BOOLEAN       │ DEFAULT TRUE                          │
│ ip_address           │ VARCHAR(50)   │                                       │
│ user_agent           │ VARCHAR(500)  │                                       │
│ scans_count          │ INT           │ DEFAULT 0                             │
│ last_scan_time       │ DATETIME      │ NULL                                  │
└─────────────────────────────────────────────────────────────────────────────┘


┌─────────────────────────────────────────────────────────────────────────────┐
│                                SCAN_LOGS                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│ id (PK)              │ INT           │ AUTO_INCREMENT                        │
│ part_id (FK)         │ INT           │ REFERENCES parts(id), NULL            │
│ qr_code              │ VARCHAR(200)  │ NOT NULL                              │
│ stage                │ INT           │ NULL                                  │
│ scan_time            │ DATETIME      │ DEFAULT CURRENT_TIMESTAMP             │
│ worker_id (FK)       │ INT           │ REFERENCES workers(id), NULL          │
│ workstation_id (FK)  │ INT           │ REFERENCES workstations(id), NULL     │
│ session_id (FK)      │ INT           │ REFERENCES worker_sessions(id), NULL  │
│ device_id            │ VARCHAR(100)  │                                       │
│ success              │ BOOLEAN       │ NOT NULL                              │
│ message              │ TEXT          │                                       │
└─────────────────────────────────────────────────────────────────────────────┘


┌─────────────────────────────────────────────────────────────────────────────┐
│                                 DEFECTS                                      │
├─────────────────────────────────────────────────────────────────────────────┤
│ id (PK)                  │ INT           │ AUTO_INCREMENT                    │
│ part_id (FK)             │ INT           │ REFERENCES parts(id), NOT NULL    │
│ qr_code                  │ VARCHAR(100)  │ NOT NULL                          │
│ worker_id (FK)           │ INT           │ REFERENCES workers(id), NULL      │
│ workstation_id (FK)      │ INT           │ REFERENCES workstations(id), NULL │
│ production_stage         │ INT           │ NOT NULL                          │
│ defect_type              │ VARCHAR(100)  │ NOT NULL                          │
│ description              │ TEXT          │                                   │
│ severity                 │ INT           │ DEFAULT 1 (1-5)                   │
│ is_repairable            │ BOOLEAN       │ DEFAULT TRUE                      │
│ status                   │ VARCHAR(20)   │ DEFAULT 'new'                     │
│ repaired_by_worker_id    │ INT           │ REFERENCES workers(id), NULL      │
│ repaired_at              │ DATETIME      │ NULL                              │
│ repair_notes             │ TEXT          │                                   │
│ created_at               │ DATETIME      │ DEFAULT CURRENT_TIMESTAMP         │
│ updated_at               │ DATETIME      │ NULL                              │
└─────────────────────────────────────────────────────────────────────────────┘


┌─────────────────────────────────────────────────────────────────────────────┐
│                               WORKER_KPIS                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│ id (PK)              │ INT           │ AUTO_INCREMENT                        │
│ worker_id (FK)       │ INT           │ REFERENCES workers(id), NOT NULL      │
│ date                 │ DATE          │ NOT NULL                              │
│ production_stage     │ INT           │ NOT NULL                              │
│ parts_processed      │ INT           │ DEFAULT 0                             │
│ total_square_meters  │ FLOAT         │ DEFAULT 0                             │
│ defects_count        │ INT           │ DEFAULT 0                             │
│ work_minutes         │ INT           │ DEFAULT 0                             │
│ avg_time_per_part    │ FLOAT         │ DEFAULT 0                             │
│ updated_at           │ DATETIME      │ DEFAULT CURRENT_TIMESTAMP             │
│ ─────────────────────┼───────────────┼───────────────────────────────────────│
│ UNIQUE(worker_id, date, production_stage)                                    │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 6.2 SQL DDL (PostgreSQL)

```sql
-- Проекти
CREATE TABLE imported_projects (
    id SERIAL PRIMARY KEY,
    project_uuid VARCHAR(50) NOT NULL UNIQUE,
    file_name VARCHAR(255) NOT NULL,
    imported_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    total_cost DECIMAL(12,2) DEFAULT 0,
    material_cost DECIMAL(12,2) DEFAULT 0,
    operation_cost DECIMAL(12,2) DEFAULT 0,
    currency VARCHAR(10) DEFAULT 'грн',
    version VARCHAR(20),
    products_count INTEGER DEFAULT 0,
    parts_count INTEGER DEFAULT 0,
    total_square_meters DOUBLE PRECISION DEFAULT 0,
    workshop_number INTEGER DEFAULT 1,
    is_active BOOLEAN DEFAULT TRUE
);

-- Деталі
CREATE TABLE parts (
    id SERIAL PRIMARY KEY,
    project_external_uuid VARCHAR(50) NOT NULL,
    part_id INTEGER NOT NULL,
    part_counter INTEGER NOT NULL,
    name VARCHAR(255) NOT NULL,
    code VARCHAR(100),
    length DOUBLE PRECISION NOT NULL,
    width DOUBLE PRECISION NOT NULL,
    thickness DOUBLE PRECISION DEFAULT 16,
    material VARCHAR(255),
    order_name VARCHAR(255),
    source_file_name VARCHAR(255),
    
    requires_cutting BOOLEAN DEFAULT TRUE,
    requires_edge_banding BOOLEAN DEFAULT FALSE,
    requires_drilling BOOLEAN DEFAULT FALSE,
    requires_sorting BOOLEAN DEFAULT TRUE,
    requires_packing BOOLEAN DEFAULT TRUE,
    
    is_cut_completed BOOLEAN DEFAULT FALSE,
    cut_completed_at TIMESTAMP,
    is_edge_banding_completed BOOLEAN DEFAULT FALSE,
    edge_banding_completed_at TIMESTAMP,
    edge_banding_sides_required INTEGER DEFAULT 0,
    edge_banding_sides_completed INTEGER DEFAULT 0,
    is_drilling_completed BOOLEAN DEFAULT FALSE,
    drilling_completed_at TIMESTAMP,
    is_sorting_completed BOOLEAN DEFAULT FALSE,
    sorting_completed_at TIMESTAMP,
    is_packing_completed BOOLEAN DEFAULT FALSE,
    packing_completed_at TIMESTAMP,
    
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    
    UNIQUE(project_external_uuid, part_id, part_counter)
);

CREATE INDEX idx_parts_project ON parts(project_external_uuid);
CREATE INDEX idx_parts_lookup ON parts(project_external_uuid, part_id, part_counter);

-- Працівники
CREATE TABLE workers (
    id SERIAL PRIMARY KEY,
    worker_code VARCHAR(50) NOT NULL UNIQUE,
    full_name VARCHAR(255) NOT NULL,
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    middle_name VARCHAR(100),
    position VARCHAR(100),
    workshop_number INTEGER DEFAULT 1,
    pin_code_hash VARCHAR(255),
    allowed_stages VARCHAR(100),
    phone VARCHAR(20),
    email VARCHAR(100),
    hire_date DATE,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);

-- Станції
CREATE TABLE workstations (
    id SERIAL PRIMARY KEY,
    station_code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    workshop_number INTEGER DEFAULT 1,
    production_stage INTEGER NOT NULL,
    location VARCHAR(100),
    is_active BOOLEAN DEFAULT TRUE,
    requires_worker_auth BOOLEAN DEFAULT TRUE,
    session_timeout_minutes INTEGER DEFAULT 60,
    capacity DECIMAL(10,2),
    device_identifier VARCHAR(100),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);

-- Сесії
CREATE TABLE worker_sessions (
    id SERIAL PRIMARY KEY,
    worker_id INTEGER NOT NULL REFERENCES workers(id),
    workstation_id INTEGER NOT NULL REFERENCES workstations(id),
    session_token VARCHAR(100) NOT NULL UNIQUE,
    start_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    end_time TIMESTAMP,
    is_active BOOLEAN DEFAULT TRUE,
    ip_address VARCHAR(50),
    user_agent VARCHAR(500),
    scans_count INTEGER DEFAULT 0,
    last_scan_time TIMESTAMP
);

CREATE INDEX idx_sessions_active ON worker_sessions(workstation_id, is_active);
CREATE INDEX idx_sessions_token ON worker_sessions(session_token);

-- Логи сканувань
CREATE TABLE scan_logs (
    id SERIAL PRIMARY KEY,
    part_id INTEGER REFERENCES parts(id),
    qr_code VARCHAR(200) NOT NULL,
    stage INTEGER,
    scan_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    worker_id INTEGER REFERENCES workers(id),
    workstation_id INTEGER REFERENCES workstations(id),
    session_id INTEGER REFERENCES worker_sessions(id),
    device_id VARCHAR(100),
    success BOOLEAN NOT NULL,
    message TEXT
);

CREATE INDEX idx_scan_logs_part ON scan_logs(part_id);
CREATE INDEX idx_scan_logs_time ON scan_logs(scan_time);

-- KPI
CREATE TABLE worker_kpis (
    id SERIAL PRIMARY KEY,
    worker_id INTEGER NOT NULL REFERENCES workers(id),
    date DATE NOT NULL,
    production_stage INTEGER NOT NULL,
    parts_processed INTEGER DEFAULT 0,
    total_square_meters DOUBLE PRECISION DEFAULT 0,
    defects_count INTEGER DEFAULT 0,
    work_minutes INTEGER DEFAULT 0,
    avg_time_per_part DOUBLE PRECISION DEFAULT 0,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    UNIQUE(worker_id, date, production_stage)
);

-- Дефекти
CREATE TABLE defects (
    id SERIAL PRIMARY KEY,
    part_id INTEGER NOT NULL REFERENCES parts(id),
    qr_code VARCHAR(100) NOT NULL,
    worker_id INTEGER REFERENCES workers(id),
    workstation_id INTEGER REFERENCES workstations(id),
    production_stage INTEGER NOT NULL,
    defect_type VARCHAR(100) NOT NULL,
    description TEXT,
    severity INTEGER DEFAULT 1 CHECK (severity BETWEEN 1 AND 5),
    is_repairable BOOLEAN DEFAULT TRUE,
    status VARCHAR(20) DEFAULT 'new',
    repaired_by_worker_id INTEGER REFERENCES workers(id),
    repaired_at TIMESTAMP,
    repair_notes TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);
```

---

## 📌 7. API Специфікація

### 7.1 Базовий URL
```
https://api.tablitsya.example.com/api/v1
```

### 7.2 Аутентифікація
- **Тип:** Bearer Token (JWT) або Session Cookie
- **Header:** `Authorization: Bearer <token>`

### 7.3 Endpoints

#### 7.3.1 Сканування

**POST /scan** — Обробка сканування QR-коду

Request:
```json
{
    "qrCode": "550e8400-e29b-41d4-a716-446655440000/123/1",
    "stage": 1,
    "workstationId": 5,
    "sessionToken": "abc123-def456-..."
}
```

Response (200 OK):
```json
{
    "success": true,
    "message": "Етап 'Порізка' завершено",
    "part": {
        "id": 456,
        "projectUuid": "550e8400-e29b-41d4-a716-446655440000",
        "partId": 123,
        "partCounter": 1,
        "name": "Бічна стінка",
        "code": "S001",
        "dimensions": "800x400",
        "material": "ДСП Дуб",
        "currentStage": 2,
        "currentStageName": "Поклейка",
        "progressPercent": 20,
        "isCutCompleted": true,
        "cutCompletedAt": "2025-01-20T10:30:00Z"
    },
    "stage": 1,
    "stageName": "Порізка",
    "isFullyCompleted": false
}
```

Response (400 Bad Request):
```json
{
    "success": false,
    "message": "Не завершені попередні етапи",
    "errorCode": "WRONG_STAGE",
    "part": { ... },
    "expectedStage": 1,
    "actualStage": 2
}
```

---

#### 7.3.2 Імпорт проектів

**POST /projects/import** — Імпорт XML-файлу

Request (multipart/form-data):
- `file`: XML файл (.project)
- `clearExisting`: boolean
- `workshopNumber`: integer

Response (200 OK):
```json
{
    "success": true,
    "message": "Імпортовано 156 деталей",
    "addedCount": 156,
    "skippedCount": 0,
    "totalCount": 156,
    "project": {
        "projectUuid": "550e8400-e29b-41d4-a716-446655440000",
        "fileName": "order.project",
        "productsCount": 12,
        "partsCount": 156,
        "totalSquareMeters": 45.67,
        "totalCost": 15000.50,
        "currency": "грн"
    }
}
```

**GET /projects** — Список проектів

Query params:
- `workshopNumber`: integer (optional)
- `isActive`: boolean (optional)
- `page`: integer (default: 1)
- `pageSize`: integer (default: 20)

Response:
```json
{
    "items": [
        {
            "id": 1,
            "projectUuid": "550e8400-...",
            "fileName": "order.project",
            "importedAt": "2025-01-20T09:00:00Z",
            "partsCount": 156,
            "completedPartsCount": 45,
            "progressPercent": 28.8,
            "isActive": true
        }
    ],
    "totalCount": 10,
    "page": 1,
    "pageSize": 20
}
```

---

#### 7.3.3 Деталі

**GET /parts** — Список деталей з фільтрацією

Query params:
- `projectUuid`: string
- `stage`: integer (поточний етап)
- `completed`: boolean
- `search`: string (пошук по назві/коду)
- `page`, `pageSize`

**GET /parts/{id}** — Деталь за ID

**GET /parts/by-qr/{qrCode}** — Деталь за QR-кодом

---

#### 7.3.4 Авторизація

**POST /auth/login** — Вхід працівника

Request:
```json
{
    "workerCode": "W001",
    "pin": "1234",
    "workstationId": 5
}
```

Response:
```json
{
    "success": true,
    "sessionToken": "a1b2c3d4-e5f6-...",
    "expiresAt": "2025-01-20T18:00:00Z",
    "worker": {
        "id": 10,
        "fullName": "Іваненко О.М.",
        "position": "Оператор"
    },
    "workstation": {
        "id": 5,
        "name": "Розкрійний центр #1",
        "stage": 1,
        "stageName": "Порізка"
    }
}
```

**POST /auth/logout** — Вихід

**GET /auth/session** — Перевірка поточної сесії

---

#### 7.3.5 Працівники

**GET /workers** — Список працівників
**POST /workers** — Створення працівника
**PUT /workers/{id}** — Оновлення
**DELETE /workers/{id}** — Деактивація

---

#### 7.3.6 Станції

**GET /workstations** — Список станцій
**POST /workstations** — Створення
**PUT /workstations/{id}** — Оновлення

---

#### 7.3.7 Статистика

**GET /stats/daily** — Денна статистика

Query params:
- `date`: date (YYYY-MM-DD)
- `workshopNumber`: integer

Response:
```json
{
    "date": "2025-01-20",
    "partsProcessed": 456,
    "totalSquareMeters": 123.45,
    "byStage": {
        "1": { "name": "Порізка", "count": 120, "squareMeters": 35.2 },
        "2": { "name": "Поклейка", "count": 98, "squareMeters": 28.1 },
        "3": { "name": "Свердління", "count": 85, "squareMeters": 24.3 },
        "4": { "name": "Сортування", "count": 78, "squareMeters": 20.5 },
        "5": { "name": "Пакування", "count": 75, "squareMeters": 15.35 }
    },
    "defectsCount": 3,
    "defectRate": 0.66
}
```

**GET /stats/workers** — KPI працівників

**GET /stats/projects/{projectUuid}** — Прогрес проекту

---

## 📌 8. UI/UX вимоги

### 8.1 Екран сканування (основний)

```
┌──────────────────────────────────────────────────────────────────┐
│  🏭 Станція: Розкрійний центр #1        👤 Іваненко О.М.         │
│  📊 Етап: Порізка                       ⏱️ Сесія: 2:15:30        │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│                ┌─────────────────────────────┐                   │
│                │                             │                   │
│                │      📷 SCAN QR-CODE        │                   │
│                │                             │                   │
│                │   ┌─────────────────────┐   │                   │
│                │   │                     │   │                   │
│                │   └─────────────────────┘   │                   │
│                │                             │                   │
│                └─────────────────────────────┘                   │
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │ ✅ УСПІШНО                                                 │  │
│  │                                                            │  │
│  │ Деталь: Полиця верхня                                      │  │
│  │ Розміри: 800 × 400 мм                                      │  │
│  │ Матеріал: ДСП Дуб Сонома                                   │  │
│  │                                                            │  │
│  │ Статус: Порізка завершена ✓                                │  │
│  │ Наступний етап: Поклейка кромки                            │  │
│  │                                                            │  │
│  │ ████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ 20%                 │  │
│  └────────────────────────────────────────────────────────────┘  │
│                                                                  │
│  ┌──────────────────┐  ┌──────────────────┐                      │
│  │ 📈 Сьогодні: 47  │  │ 📐 Площа: 12.5м² │                      │
│  └──────────────────┘  └──────────────────┘                      │
│                                                                  │
│  [🚨 Брак]   [📋 Історія]   [🚪 Вихід]                           │
└──────────────────────────────────────────────────────────────────┘
```

### 8.2 Кольорова схема

| Елемент | Колір | HEX |
|---------|-------|-----|
| Успіх | Зелений | `#27ae60` |
| Попередження | Жовтий | `#f39c12` |
| Помилка | Червоний | `#e74c3c` |
| Інформація | Синій | `#3498db` |
| Фон | Світло-сірий | `#f5f6fa` |
| Текст | Темно-сірий | `#2c3e50` |

### 8.3 Етапи — кольори та іконки

| Етап | Колір | Іконка |
|------|-------|--------|
| Порізка | `#e74c3c` | ✂️ scissors |
| Поклейка | `#f39c12` | 📦 bounding-box |
| Свердління | `#3498db` | ⚙️ gear |
| Сортування | `#9b59b6` | 📊 sort-alpha-down |
| Пакування | `#27ae60` | 📤 box-seam |

### 8.4 Responsive breakpoints

| Пристрій | Ширина | Примітки |
|----------|--------|----------|
| Mobile | < 768px | Основний для сканерів |
| Tablet | 768-1024px | Планшети на станціях |
| Desktop | > 1024px | Адмін-панель |

---

## 📌 9. Алгоритми бізнес-логіки

### 9.1 Визначення поточного етапу деталі

```pseudocode
function getCurrentStage(part: Part): ProductionStage?
    
    // 1. Порізка
    if part.requires_cutting AND NOT part.is_cut_completed:
        return CUTTING
    
    // 2. Поклейка кромки
    if part.requires_edge_banding:
        if part.edge_banding_sides_required > 0:
            if part.edge_banding_sides_completed < part.edge_banding_sides_required:
                return EDGE_BANDING
        else:
            if NOT part.is_edge_banding_completed:
                return EDGE_BANDING
    
    // 3. Свердління
    if part.requires_drilling AND NOT part.is_drilling_completed:
        return DRILLING
    
    // 4. Сортування
    if part.requires_sorting AND NOT part.is_sorting_completed:
        return SORTING
    
    // 5. Пакування
    if part.requires_packing AND NOT part.is_packing_completed:
        return PACKING
    
    // Всі етапи завершені
    return NULL
```

### 9.2 Обробка сканування

```pseudocode
function processScan(qrCode, stationStage, workerId, workstationId, sessionId):
    
    // 1. Парсинг QR-коду
    parsed = parseQRCode(qrCode)  // {projectUuid, partId, partCounter}
    if parsed == NULL:
        return Error("INVALID_QR", "Невірний формат QR-коду")
    
    // 2. Пошук деталі
    part = findPart(parsed.projectUuid, parsed.partId, parsed.partCounter)
    if part == NULL:
        logScan(qrCode, stationStage, FALSE, "Деталь не знайдена")
        return Error("NOT_FOUND", "Деталь не знайдена в базі")
    
    // 3. Визначення поточного етапу
    currentStage = getCurrentStage(part)
    
    if currentStage == NULL:
        logScan(qrCode, NULL, TRUE, "Всі етапи завершені")
        return Success(part, "Всі етапи вже завершені", isFullyCompleted=TRUE)
    
    // 4. Перевірка відповідності етапу станції
    if stationStage != currentStage:
        logScan(qrCode, stationStage, FALSE, "Невірний етап")
        return Error("WRONG_STAGE", 
            "Деталь на етапі '" + currentStage.name + "', а станція для '" + stationStage.name + "'")
    
    // 5. Завершення етапу
    completeStage(part, currentStage)
    
    // 6. Оновлення KPI
    if workerId != NULL:
        updateWorkerKPI(workerId, currentStage, part)
    
    // 7. Логування
    logScan(qrCode, currentStage, TRUE, "Етап завершено", part.id, workerId, workstationId, sessionId)
    
    // 8. Результат
    nextStage = getCurrentStage(part)
    return Success(part, 
        "Етап '" + currentStage.name + "' завершено",
        isFullyCompleted = (nextStage == NULL))
```

### 9.3 Завершення етапу

```pseudocode
function completeStage(part: Part, stage: ProductionStage):
    now = currentTimestamp()
    
    switch stage:
        case CUTTING:
            part.is_cut_completed = TRUE
            part.cut_completed_at = now
        
        case EDGE_BANDING:
            if part.edge_banding_sides_required > 0:
                part.edge_banding_sides_completed += 1
                if part.edge_banding_sides_completed >= part.edge_banding_sides_required:
                    part.is_edge_banding_completed = TRUE
                    part.edge_banding_completed_at = now
            else:
                part.is_edge_banding_completed = TRUE
                part.edge_banding_completed_at = now
        
        case DRILLING:
            part.is_drilling_completed = TRUE
            part.drilling_completed_at = now
        
        case SORTING:
            part.is_sorting_completed = TRUE
            part.sorting_completed_at = now
        
        case PACKING:
            part.is_packing_completed = TRUE
            part.packing_completed_at = now
    
    part.updated_at = now
    save(part)
```

### 9.4 Парсинг QR-коду

```pseudocode
function parseQRCode(qrCode: String): QRData?
    // Підтримувані роздільники: / | -
    separators = ["/", "|", "-"]
    
    for separator in separators:
        parts = qrCode.split(separator)
        
        if parts.length >= 3:
            projectUuid = parts[0].trim()
            partId = tryParseInt(parts[1])
            partCounter = tryParseInt(parts[2])
            
            if projectUuid.length > 0 AND partId != NULL AND partCounter != NULL:
                return {
                    projectUuid: projectUuid,
                    partId: partId,
                    partCounter: partCounter
                }
    
    return NULL

// Приклади валідних QR-кодів:
// "550e8400-e29b-41d4-a716-446655440000/123/1"
// "550e8400-e29b-41d4-a716-446655440000|123|1"
```

---

## 📌 10. Тестування

### 10.1 Unit-тести

| Модуль | Мінімальне покриття | Критичні тести |
|--------|---------------------|----------------|
| ScanningService | 90% | processScan, getCurrentStage |
| ImportService | 85% | parseXml, validatePart |
| AuthService | 85% | login, validateSession |
| PlanningService | 80% | calculateSchedule |

### 10.2 Тест-кейси для сканування

| ID | Сценарій | Вхід | Очікуваний результат |
|----|----------|------|----------------------|
| SC-01 | Успішне сканування | Валідний QR, правильний етап | SUCCESS |
| SC-02 | Невірний формат QR | "abc123" | INVALID_QR |
| SC-03 | Деталь не знайдена | Валідний QR, відсутня деталь | NOT_FOUND |
| SC-04 | Невірний етап | QR на етапі 1, станція 2 | WRONG_STAGE |
| SC-05 | Повторне сканування | Вже завершений етап | ALREADY_COMPLETED |
| SC-06 | Кромка — часткове | 2/4 сторони | SUCCESS, stage=2 |
| SC-07 | Кромка — завершення | 4/4 сторони | SUCCESS, stage=3 |
| SC-08 | Всі етапи завершені | Повністю готова деталь | SUCCESS, isFullyCompleted=true |

### 10.3 Інтеграційні тести

1. **Повний цикл:** Імпорт → 5 сканувань → Завершення деталі
2. **Авторизація:** Login → Сканування → Logout → Перевірка сесії
3. **Конкурентність:** 10 одночасних сканувань однієї деталі

### 10.4 Навантажувальне тестування

| Метрика | Ціль |
|---------|------|
| Сканувань/сек | ≥ 100 |
| Час відповіді (p95) | < 200 мс |
| Імпорт 10,000 деталей | < 60 сек |
| Одночасні сесії | ≥ 50 |

---

## 📌 11. План впровадження

### Фаза 1: MVP (тижні 1-6)

| Тиждень | Задачі |
|---------|--------|
| 1-2 | Архітектура, БД, базові моделі |
| 3-4 | Імпорт XML, сканування (без авторизації) |
| 5-6 | UI сканування, базовий dashboard |

**Результат:** Працююча система імпорту та сканування

### Фаза 2: Авторизація (тижні 7-8)

| Тиждень | Задачі |
|---------|--------|
| 7 | Працівники, станції, сесії |
| 8 | UI авторизації, права доступу |

### Фаза 3: Аналітика (тижні 9-10)

| Тиждень | Задачі |
|---------|--------|
| 9 | KPI, статистика, звіти |
| 10 | Планування виробництва |

### Фаза 4: Production (тиждень 11)

- Деплой на сервер
- Міграція даних
- Навчання персоналу
- Моніторинг

---

## 📌 12. Глосарій

| Термін | Визначення |
|--------|------------|
| **Деталь (Part)** | Одиничний елемент меблевого виробу з унікальним QR-кодом |
| **QR-код** | Ідентифікатор деталі у форматі `{project_uuid}/{part_id}/{counter}` |
| **Етап (Stage)** | Крок виробничого процесу (1-Порізка, 2-Кромка, 3-Свердління, 4-Сортування, 5-Пакування) |
| **Станція (Workstation)** | Фізичне робоче місце для виконання конкретного етапу |
| **Сесія (Session)** | Період авторизованої роботи працівника на станції |
| **Проект (Project)** | XML-файл з CAD-системи, що містить товари та деталі замовлення |
| **Товар (Product)** | Готовий виріб (шафа, тумба), що складається з деталей |
| **KPI** | Ключові показники ефективності працівника |
| **Брак (Defect)** | Дефектна деталь, виявлена на етапі виробництва |

---

## 📌 13. Додатки

### Додаток А: Формат XML-файлу проекту

```xml
<?xml version="1.0" encoding="UTF-8"?>
<project 
    project.externaluuid="550e8400-e29b-41d4-a716-446655440000"
    version="2.0"
    currency="грн"
    cost="15000.50"
    costMaterial="12000.00"
    costOperation="3000.50">
    
    <!-- Товари -->
    <good typeId="product" 
          id="1" 
          name="Кухня Модерн" 
          code="KM001"
          count="1"
          cost="15000.50">
        
        <!-- Деталі -->
        <part id="101"
              name="Бічна стінка"
              part.code="S001"
              l="800"
              w="400"
              count="2"
              eb1="1" eb2="1" eb3="0" eb4="0">
        </part>
        
    </good>
    
    <!-- Операції свердління -->
    <operation typeId="XNC" name="Свердління">
        <part id="101"/>
    </operation>
    
</project>
```

### Додаток Б: Приклад конфігурації appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=tablitsya;Username=app;Password=***"
  },
  "AppSettings": {
    "DefaultWorkshopNumber": 1,
    "SessionTimeoutMinutes": 60,
    "RequireWorkerAuth": true,
    "DefaultCurrency": "грн"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

---

*Версія документа: 1.0*  
*Дата створення: Січень 2025*  
*Автор: GitHub Copilot*
