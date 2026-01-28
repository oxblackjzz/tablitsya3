# 🔧 Tablitsya3 — Специфікація для портування

## Документація для розробників, які хочуть реалізувати систему на іншій мові програмування

---

## Зміст
- [1. Загальний огляд](#1-загальний-огляд)
- [2. Архітектурні патерни](#2-архітектурні-патерни)
- [3. Структури даних](#3-структури-даних)
- [4. Алгоритми бізнес-логіки](#4-алгоритми-бізнес-логіки)
- [5. API Специфікація](#5-api-специфікація)
- [6. Формат XML проекту](#6-формат-xml-проекту)
- [7. Формат QR-коду](#7-формат-qr-коду)
- [8. Схема бази даних](#8-схема-бази-даних)
- [9. Стан-машина етапів](#9-стан-машина-етапів)
- [10. Алгоритм планування](#10-алгоритм-планування)
- [11. Рекомендації щодо реалізації](#11-рекомендації-щодо-реалізації)

---

## 1. Загальний огляд

### 1.1 Призначення системи
Система управління меблевим виробництвом з відстеженням деталей через QR-коди на кожному етапі виробництва.

### 1.2 Основні модулі

| Модуль | Відповідальність |
|--------|------------------|
| **Project Importer** | Парсинг XML файлів CAD-системи |
| **Scanning Engine** | Обробка сканувань QR-кодів |
| **Worker Management** | Управління працівниками та авторизацією |
| **Workstation Management** | Управління робочими станціями |
| **Production Planning** | Розрахунок графіків виробництва |
| **KPI Tracker** | Збір та аналіз статистики |

### 1.3 Рекомендована архітектура

```
┌─────────────────────────────────────────────────────────────────┐
│                        API / Web Layer                          │
│         (REST API, WebSocket для real-time updates)             │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                       Service Layer                             │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────────────────┐ │
│  │ ScanService  │ │ WorkerService│ │ ProductionPlanningService│ │
│  └──────────────┘ └──────────────┘ └──────────────────────────┘ │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────────────────┐ │
│  │ AuthService  │ │ ImportService│ │ KPIService               │ │
│  └──────────────┘ └──────────────┘ └──────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Repository Layer                           │
│     (абстракція доступу до даних, підтримка транзакцій)        │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                        Database Layer                           │
│           (PostgreSQL, MySQL, SQLite, MongoDB, etc.)            │
└─────────────────────────────────────────────────────────────────┘
```

---

## 2. Архітектурні патерни

### 2.1 Рекомендовані патерни

| Патерн | Застосування |
|--------|--------------|
| **Repository Pattern** | Абстракція доступу до даних |
| **Service Layer** | Бізнес-логіка |
| **State Machine** | Управління етапами виробництва |
| **Factory Pattern** | Створення об'єктів сканування |
| **Observer Pattern** | Real-time оновлення UI |
| **Strategy Pattern** | Різні алгоритми планування |

### 2.2 Принципи

- **Іммутабельність станів** — зміна етапу створює новий запис в логах
- **Ідемпотентність API** — повторний запит дає той самий результат
- **Eventual Consistency** — для KPI та статистики
- **Strong Consistency** — для станів деталей

---

## 3. Структури даних

### 3.1 Part (Деталь)

```
Part {
    // Ідентифікація
    id: Integer                      // Унікальний ID в БД
    project_uuid: String             // UUID проекту (з XML)
    part_id: Integer                 // ID деталі в межах проекту (з XML)
    part_counter: Integer            // Лічильник для унікальності (якщо part_id повторюється)
    
    // Властивості
    name: String                     // Назва деталі
    code: String                     // Код деталі
    length: Float                    // Довжина в мм
    width: Float                     // Ширина в мм
    thickness: Float                 // Товщина в мм (default: 16)
    material: String                 // Назва матеріалу
    order_name: String               // Назва замовлення (товару)
    source_file: String              // Ім'я файлу-джерела
    
    // Прапорці вимог до етапів
    requires_cutting: Boolean        // Чи потрібна порізка (default: true)
    requires_edge_banding: Boolean   // Чи потрібна поклейка кромки
    requires_drilling: Boolean       // Чи потрібне свердління
    requires_sorting: Boolean        // Чи потрібне сортування (default: true)
    requires_packing: Boolean        // Чи потрібне пакування (default: true)
    
    // Статуси етапів
    is_cut_completed: Boolean        // Порізка завершена
    cut_completed_at: DateTime?      // Час завершення порізки
    
    is_edge_banding_completed: Boolean
    edge_banding_completed_at: DateTime?
    edge_banding_sides_required: Integer  // К-сть сторін для обклейки (0-4)
    edge_banding_sides_completed: Integer // К-сть оброблених сторін
    
    is_drilling_completed: Boolean
    drilling_completed_at: DateTime?
    
    is_sorting_completed: Boolean
    sorting_completed_at: DateTime?
    
    is_packing_completed: Boolean
    packing_completed_at: DateTime?
    
    // Метадані
    created_at: DateTime
    updated_at: DateTime
}
```

### 3.2 Worker (Працівник)

```
Worker {
    id: Integer
    worker_code: String              // Унікальний код для бейджа (QR/штрих-код)
    full_name: String                // ПІБ
    first_name: String
    last_name: String
    middle_name: String
    position: String                 // Посада
    workshop_number: Integer         // Номер цеху
    pin_code_hash: String?           // Хеш PIN-коду (SHA256 або bcrypt)
    allowed_stages: String           // Дозволені етапи через кому: "1,2,3"
    phone: String?
    email: String?
    hire_date: Date?
    is_active: Boolean
    created_at: DateTime
    updated_at: DateTime?
}
```

### 3.3 Workstation (Робоча станція)

```
Workstation {
    id: Integer
    station_code: String             // Унікальний код станції
    name: String                     // Назва ("Розкрійний центр #1")
    description: String?
    workshop_number: Integer         // Номер цеху
    production_stage: Integer        // Етап виробництва (1-5)
    location: String?                // Фізична локація в цеху
    is_active: Boolean
    requires_worker_auth: Boolean    // Чи потрібна авторизація працівника
    session_timeout_minutes: Integer // Тайм-аут сесії (0 = без тайм-ауту)
    capacity: Decimal?               // Потужність (м²/день)
    device_identifier: String?       // IP або унікальний ID пристрою
    created_at: DateTime
    updated_at: DateTime?
}
```

### 3.4 WorkerSession (Сесія працівника)

```
WorkerSession {
    id: Integer
    worker_id: Integer               // FK → Worker
    workstation_id: Integer          // FK → Workstation
    session_token: String            // Унікальний токен (UUID v4)
    start_time: DateTime
    end_time: DateTime?
    is_active: Boolean
    ip_address: String?
    user_agent: String?
    scans_count: Integer             // К-сть сканувань за сесію
    last_scan_time: DateTime?
}
```

### 3.5 ScanLog (Лог сканування)

```
ScanLog {
    id: Integer
    part_id: Integer?                // FK → Part (nullable якщо деталь не знайдена)
    qr_code: String                  // Відсканований QR-код
    stage: Integer                   // Етап (1-5)
    scan_time: DateTime
    worker_id: Integer?              // FK → Worker
    workstation_id: Integer?         // FK → Workstation
    session_id: Integer?             // FK → WorkerSession
    device_id: String?               // ID пристрою-сканера
    success: Boolean                 // Чи успішне сканування
    message: String?                 // Повідомлення (помилка або результат)
}
```

### 3.6 Defect (Дефект/Брак)

```
Defect {
    id: Integer
    part_id: Integer                 // FK → Part
    qr_code: String
    worker_id: Integer?              // Хто виявив
    workstation_id: Integer?
    production_stage: Integer        // На якому етапі виявлено
    defect_type: String              // Тип дефекту
    description: String?
    severity: Integer                // 1-5 (1 = мінімальна, 5 = критична)
    is_repairable: Boolean           // Чи можна виправити
    status: Enum                     // NEW, IN_PROGRESS, REPAIRED, SCRAPPED
    repaired_by_worker_id: Integer?
    repaired_at: DateTime?
    repair_notes: String?
    created_at: DateTime
    updated_at: DateTime?
}

Enum DefectStatus {
    NEW = "new"
    IN_PROGRESS = "in_progress"
    REPAIRED = "repaired"
    SCRAPPED = "scrapped"
}
```

### 3.7 ImportedProject (Імпортований проект)

```
ImportedProject {
    id: Integer
    project_uuid: String             // Унікальний UUID з XML
    file_name: String
    imported_at: DateTime
    total_cost: Decimal
    material_cost: Decimal
    operation_cost: Decimal
    currency: String                 // "грн", "USD", etc.
    version: String                  // Версія формату файлу
    products_count: Integer          // К-сть товарів
    parts_count: Integer             // К-сть деталей
    total_square_meters: Float       // Загальна площа
    workshop_number: Integer
    is_active: Boolean
}
```

### 3.8 WorkerKPI (Статистика працівника)

```
WorkerKPI {
    id: Integer
    worker_id: Integer
    date: Date                       // День
    production_stage: Integer
    parts_processed: Integer         // К-сть оброблених деталей
    total_square_meters: Float       // Оброблена площа
    defects_count: Integer           // К-сть браку
    work_minutes: Integer            // Час роботи
    avg_time_per_part: Float         // Середній час на деталь (секунди)
    updated_at: DateTime
}
```

---

## 4. Алгоритми бізнес-логіки

### 4.1 Етапи виробництва (Production Stages)

```
Enum ProductionStage {
    CUTTING = 1       // Порізка
    EDGE_BANDING = 2  // Поклейка кромки
    DRILLING = 3      // Свердління
    SORTING = 4       // Сортування
    PACKING = 5       // Пакування
}
```

### 4.2 Визначення поточного етапу

```pseudocode
function getCurrentStage(part: Part): ProductionStage?
    if part.requires_cutting AND NOT part.is_cut_completed:
        return CUTTING
    
    if part.requires_edge_banding AND NOT isEdgeBandingFullyCompleted(part):
        return EDGE_BANDING
    
    if part.requires_drilling AND NOT part.is_drilling_completed:
        return DRILLING
    
    if part.requires_sorting AND NOT part.is_sorting_completed:
        return SORTING
    
    if part.requires_packing AND NOT part.is_packing_completed:
        return PACKING
    
    return NULL  // Всі етапи завершені

function isEdgeBandingFullyCompleted(part: Part): Boolean
    if NOT part.requires_edge_banding:
        return TRUE
    
    if part.edge_banding_sides_required <= 0:
        return part.is_edge_banding_completed
    
    return part.edge_banding_sides_completed >= part.edge_banding_sides_required
```

### 4.3 Перевірка можливості переходу до етапу

```pseudocode
function canAdvanceToStage(part: Part, stage: ProductionStage): Boolean
    switch stage:
        case CUTTING:
            return part.requires_cutting AND NOT part.is_cut_completed
        
        case EDGE_BANDING:
            // Попередній етап (порізка) має бути завершений
            cutting_done = NOT part.requires_cutting OR part.is_cut_completed
            return part.requires_edge_banding 
                   AND canPerformEdgeBandingScan(part) 
                   AND cutting_done
        
        case DRILLING:
            cutting_done = NOT part.requires_cutting OR part.is_cut_completed
            edging_done = NOT part.requires_edge_banding OR isEdgeBandingFullyCompleted(part)
            return part.requires_drilling 
                   AND NOT part.is_drilling_completed 
                   AND cutting_done 
                   AND edging_done
        
        case SORTING:
            cutting_done = NOT part.requires_cutting OR part.is_cut_completed
            edging_done = NOT part.requires_edge_banding OR isEdgeBandingFullyCompleted(part)
            drilling_done = NOT part.requires_drilling OR part.is_drilling_completed
            return part.requires_sorting 
                   AND NOT part.is_sorting_completed 
                   AND cutting_done 
                   AND edging_done 
                   AND drilling_done
        
        case PACKING:
            cutting_done = NOT part.requires_cutting OR part.is_cut_completed
            edging_done = NOT part.requires_edge_banding OR isEdgeBandingFullyCompleted(part)
            drilling_done = NOT part.requires_drilling OR part.is_drilling_completed
            sorting_done = NOT part.requires_sorting OR part.is_sorting_completed
            return part.requires_packing 
                   AND NOT part.is_packing_completed 
                   AND cutting_done 
                   AND edging_done 
                   AND drilling_done 
                   AND sorting_done
    
    return FALSE

function canPerformEdgeBandingScan(part: Part): Boolean
    if NOT part.requires_edge_banding:
        return FALSE
    
    if part.edge_banding_sides_required <= 0:
        return NOT part.is_edge_banding_completed
    
    return part.edge_banding_sides_completed < part.edge_banding_sides_required
```

### 4.4 Завершення етапу

```pseudocode
function completeStage(part: Part, stage: ProductionStage): void
    now = currentDateTime()
    
    switch stage:
        case CUTTING:
            part.is_cut_completed = TRUE
            part.cut_completed_at = now
        
        case EDGE_BANDING:
            if part.edge_banding_sides_required > 0:
                part.edge_banding_sides_completed += 1
                // Зберігаємо час кожної сторони
                appendEdgeBandingDate(part, now)
                
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
```

### 4.5 Обробка сканування

```pseudocode
function processScan(
    qrCode: String,
    stage: ProductionStage?,
    workerId: Integer?,
    workstationId: Integer?,
    sessionId: Integer?,
    deviceId: String?
): ScanResult

    result = new ScanResult(success: FALSE)
    
    // 1. Парсинг QR-коду
    parsedData = parseQRCode(qrCode)
    if parsedData == NULL:
        result.message = "Невірний формат QR-коду"
        logScan(NULL, qrCode, stage, FALSE, result.message, ...)
        return result
    
    // 2. Пошук деталі в БД
    part = findPart(
        projectUuid: parsedData.projectUuid,
        partId: parsedData.partId,
        partCounter: parsedData.partCounter
    )
    
    if part == NULL:
        result.message = "Деталь не знайдена в базі даних"
        logScan(NULL, qrCode, stage, FALSE, result.message, ...)
        return result
    
    // 3. Визначення етапу (якщо не вказано)
    if stage == NULL:
        stage = getCurrentStage(part)
        
        if stage == NULL:
            result.success = TRUE
            result.part = part
            result.message = "Всі етапи вже завершені"
            result.isFullyCompleted = TRUE
            logScan(part.id, qrCode, NULL, TRUE, result.message, ...)
            return result
    
    // 4. Перевірка можливості переходу
    if NOT canAdvanceToStage(part, stage):
        result.part = part
        result.stage = stage
        
        if NOT isStageRequired(part, stage):
            result.message = "Етап не потрібен для цієї деталі"
        else if isStageCompleted(part, stage):
            result.message = "Етап вже завершений"
        else:
            result.message = "Не завершені попередні етапи"
        
        logScan(part.id, qrCode, stage, FALSE, result.message, ...)
        return result
    
    // 5. Завершення етапу
    completeStage(part, stage)
    savePart(part)
    
    // 6. Оновлення KPI (якщо є працівник)
    if workerId != NULL:
        updateWorkerKPI(workerId, stage, part)
    
    // 7. Логування
    logScan(part.id, qrCode, stage, TRUE, "Етап завершено", ...)
    
    // 8. Результат
    result.success = TRUE
    result.part = part
    result.stage = stage
    result.message = "Етап '" + getStageName(stage) + "' завершено"
    result.isFullyCompleted = isPartFullyCompleted(part)
    
    return result
```

### 4.6 Авторизація працівника

```pseudocode
function loginWorker(
    workerCode: String,
    pin: String?,
    workstationId: Integer,
    ipAddress: String?,
    userAgent: String?
): AuthResult

    result = new AuthResult(success: FALSE)
    
    // 1. Перевірка станції
    workstation = findWorkstationById(workstationId)
    if workstation == NULL OR NOT workstation.is_active:
        result.message = "Станцію не знайдено або вона неактивна"
        return result
    
    // 2. Пошук працівника
    worker = findWorkerByCode(workerCode)
    if worker == NULL:
        result.message = "Працівника не знайдено"
        return result
    
    // 3. Перевірка PIN (якщо потрібно)
    if workstation.requires_worker_auth AND worker.pin_code_hash != NULL:
        if pin == NULL OR NOT verifyPin(worker, pin):
            result.message = "Невірний PIN-код"
            return result
    
    // 4. Перевірка дозволу на етап
    stage = workstation.production_stage
    if NOT canWorkerPerformStage(worker, stage):
        result.message = "Працівник не має доступу до цього етапу"
        return result
    
    // 5. Закриття попередніх сесій
    closeWorkerSessions(worker.id)
    closeWorkstationSession(workstationId)
    
    // 6. Створення нової сесії
    session = new WorkerSession(
        worker_id: worker.id,
        workstation_id: workstationId,
        session_token: generateUUID(),
        start_time: now(),
        is_active: TRUE,
        ip_address: ipAddress,
        user_agent: userAgent,
        scans_count: 0
    )
    saveSession(session)
    
    // 7. Результат
    result.success = TRUE
    result.worker = worker
    result.workstation = workstation
    result.session = session
    result.sessionToken = session.session_token
    
    return result

function canWorkerPerformStage(worker: Worker, stage: Integer): Boolean
    if worker.allowed_stages == NULL OR worker.allowed_stages == "":
        return TRUE  // Всі етапи за замовчуванням
    
    allowedStages = parseIntList(worker.allowed_stages)  // "1,2,3" → [1,2,3]
    return stage IN allowedStages

function verifyPin(worker: Worker, pin: String): Boolean
    // Використовуйте безпечне хешування (bcrypt, Argon2, або SHA256 з сіллю)
    return secureCompare(hashPin(pin), worker.pin_code_hash)
```

---

## 5. API Специфікація

### 5.1 Сканування

#### POST /api/scan
Обробка сканування QR-коду.

**Request:**
```json
{
    "qrCode": "550e8400-e29b-41d4-a716-446655440000/123/1",
    "stage": 1,
    "workerId": 10,
    "workstationId": 5,
    "sessionId": 100,
    "deviceId": "scanner-001"
}
```

**Response (success):**
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
        "isCutCompleted": true,
        "cutCompletedAt": "2025-01-20T10:30:00Z",
        "currentStage": 2,
        "progressPercent": 20
    },
    "stage": 1,
    "isFullyCompleted": false
}
```

**Response (error):**
```json
{
    "success": false,
    "message": "Не завершені попередні етапи",
    "part": { ... },
    "stage": 3
}
```

### 5.2 Імпорт проекту

#### POST /api/projects/import
Імпорт XML файлу проекту.

**Request (multipart/form-data):**
- `file`: XML файл (.project)
- `clearExisting`: boolean (очистити існуючі дані)
- `workshopNumber`: integer

**Response:**
```json
{
    "success": true,
    "message": "Імпортовано 156 деталей, пропущено 0",
    "addedCount": 156,
    "skippedCount": 0,
    "totalCount": 156,
    "project": {
        "projectUuid": "550e8400-e29b-41d4-a716-446655440000",
        "fileName": "order.project",
        "productsCount": 12,
        "partsCount": 156,
        "totalSquareMeters": 45.67
    }
}
```

### 5.3 Авторизація

#### POST /api/auth/login
Авторизація працівника на станції.

**Request:**
```json
{
    "workerCode": "W001",
    "pin": "1234",
    "workstationId": 5
}
```

**Response:**
```json
{
    "success": true,
    "message": "Авторизація успішна",
    "sessionToken": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "worker": {
        "id": 10,
        "fullName": "Іваненко О.М.",
        "position": "Оператор"
    },
    "workstation": {
        "id": 5,
        "name": "Розкрійний центр #1",
        "stage": 1
    }
}
```

#### POST /api/auth/logout
```json
{
    "sessionToken": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
}
```

### 5.4 Деталі

#### GET /api/parts
Отримання списку деталей з фільтрацією.

**Query Parameters:**
- `projectUuid`: string
- `stage`: integer (поточний етап)
- `completed`: boolean
- `workshopNumber`: integer
- `page`: integer
- `pageSize`: integer

#### GET /api/parts/{id}
Отримання деталі за ID.

#### GET /api/parts/by-qr/{qrCode}
Отримання деталі за QR-кодом.

### 5.5 Статистика

#### GET /api/stats/daily
Денна статистика.

**Query Parameters:**
- `date`: date (YYYY-MM-DD)
- `workshopNumber`: integer

**Response:**
```json
{
    "date": "2025-01-20",
    "partsProcessed": 456,
    "totalSquareMeters": 123.45,
    "byStage": {
        "1": { "count": 120, "squareMeters": 35.2 },
        "2": { "count": 98, "squareMeters": 28.1 },
        "3": { "count": 85, "squareMeters": 24.3 },
        "4": { "count": 78, "squareMeters": 20.5 },
        "5": { "count": 75, "squareMeters": 15.35 }
    },
    "defectsCount": 3,
    "defectRate": 0.66
}
```

---

## 6. Формат XML проекту

### 6.1 Структура XML

```xml
<?xml version="1.0" encoding="UTF-8"?>
<project 
    project.externaluuid="550e8400-e29b-41d4-a716-446655440000"
    version="2.0"
    currency="грн"
    cost="15000.50"
    costMaterial="12000.00"
    costOperation="3000.50">
    
    <!-- Товари (вироби) -->
    <good typeId="product" 
          id="1" 
          name="Кухня Модерн" 
          code="KM001"
          count="1"
          cost="15000.50"
          costMaterial="12000.00"
          costOperation="3000.50"
          orderDate="2025-01-15">
        
        <!-- Деталі -->
        <part id="101"
              name="Бічна стінка"
              part.code="S001"
              l="800"
              w="400"
              count="2"
              usedCount="2"
              eb1="1" eb2="1" eb3="0" eb4="0">
        </part>
        
        <part id="102"
              name="Полиця"
              part.code="P001"
              l="600"
              w="300"
              count="3">
        </part>
    </good>
    
    <!-- Операції (для визначення свердління) -->
    <operation typeId="XNC" name="Свердління">
        <part id="101"/>
        <part id="103"/>
    </operation>
    
</project>
```

### 6.2 Парсинг деталі

```pseudocode
function parsePartElement(element, projectUuid, orderName, drillingPartIds):
    partId = parseInt(element.attr("id"))
    
    // Розміри
    length = parseFloat(element.attr("l")) OR parseFloat(element.attr("dl"))
    width = parseFloat(element.attr("w")) OR parseFloat(element.attr("dw"))
    
    // Пропускаємо деталі без розмірів
    if length == 0 OR width == 0:
        return NULL
    
    // Кількість
    count = max(1, parseInt(element.attr("count")))
    usedCount = parseInt(element.attr("usedCount"))
    actualCount = max(count, usedCount)
    
    // Кількість сторін для кромки
    edgeBandingSides = 0
    if element.attr("eb1") == "1": edgeBandingSides++
    if element.attr("eb2") == "1": edgeBandingSides++
    if element.attr("eb3") == "1": edgeBandingSides++
    if element.attr("eb4") == "1": edgeBandingSides++
    
    // Чи потрібне свердління
    requiresDrilling = partId IN drillingPartIds
    
    // Створюємо actualCount деталей
    parts = []
    for i = 1 to actualCount:
        counter = getNextCounter(partId)  // Унікальний лічильник
        
        part = new Part(
            project_uuid: projectUuid,
            part_id: partId,
            part_counter: counter,
            name: element.attr("name"),
            code: element.attr("part.code"),
            length: length,
            width: width,
            order_name: orderName,
            edge_banding_sides_required: edgeBandingSides,
            requires_cutting: TRUE,
            requires_edge_banding: edgeBandingSides > 0,
            requires_drilling: requiresDrilling,
            requires_sorting: TRUE,
            requires_packing: TRUE
        )
        parts.append(part)
    
    return parts
```

### 6.3 Отримання деталей зі свердлінням

```pseudocode
function getDrillingPartIds(xmlRoot):
    partIds = new Set()
    
    // Шукаємо операції типу XNC
    for operation in xmlRoot.elements("operation"):
        if operation.attr("typeId") == "XNC":
            for partRef in operation.elements("part"):
                partId = parseInt(partRef.attr("id"))
                partIds.add(partId)
    
    return partIds
```

---

## 7. Формат QR-коду

### 7.1 Структура

```
{project_uuid}/{part_id}/{part_counter}
```

**Приклад:**
```
550e8400-e29b-41d4-a716-446655440000/123/1
```

### 7.2 Альтернативні роздільники

Система підтримує роздільники: `/`, `|`, `-`

```
550e8400-e29b-41d4-a716-446655440000|123|1
550e8400-e29b-41d4-a716-446655440000-123-1
```

### 7.3 Парсинг

```pseudocode
function parseQRCode(qrCode: String): QRData?
    // Спробувати різні роздільники
    for separator in ["/", "|", "-"]:
        parts = qrCode.split(separator)
        
        if parts.length >= 3:
            projectUuid = parts[0]
            partId = tryParseInt(parts[1])
            partCounter = tryParseInt(parts[2])
            
            if partId != NULL AND partCounter != NULL:
                return new QRData(
                    projectUuid: projectUuid,
                    partId: partId,
                    partCounter: partCounter
                )
    
    return NULL
```

### 7.4 Генерація

```pseudocode
function generateQRCode(part: Part): String
    return part.project_uuid + "/" + part.part_id + "/" + part.part_counter
```

---

## 8. Схема бази даних

### 8.1 SQL DDL (PostgreSQL)

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

-- Товари
CREATE TABLE products (
    id SERIAL PRIMARY KEY,
    project_uuid VARCHAR(50) NOT NULL,
    product_id INTEGER NOT NULL,
    name VARCHAR(255) NOT NULL,
    code VARCHAR(100),
    description TEXT,
    count INTEGER DEFAULT 1,
    cost DECIMAL(12,2) DEFAULT 0,
    material_cost DECIMAL(12,2) DEFAULT 0,
    operation_cost DECIMAL(12,2) DEFAULT 0,
    order_date DATE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
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
    
    -- Вимоги до етапів
    requires_cutting BOOLEAN DEFAULT TRUE,
    requires_edge_banding BOOLEAN DEFAULT FALSE,
    requires_drilling BOOLEAN DEFAULT FALSE,
    requires_sorting BOOLEAN DEFAULT TRUE,
    requires_packing BOOLEAN DEFAULT TRUE,
    
    -- Статуси етапів
    is_cut_completed BOOLEAN DEFAULT FALSE,
    cut_completed_at TIMESTAMP,
    
    is_edge_banding_completed BOOLEAN DEFAULT FALSE,
    edge_banding_completed_at TIMESTAMP,
    edge_banding_sides_required INTEGER DEFAULT 0,
    edge_banding_sides_completed INTEGER DEFAULT 0,
    edge_banding_completed_dates TEXT,  -- JSON array або ";" separated
    
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

-- Індекси для швидкого пошуку
CREATE INDEX idx_parts_project ON parts(project_external_uuid);
CREATE INDEX idx_parts_lookup ON parts(project_external_uuid, part_id, part_counter);
CREATE INDEX idx_parts_stage_cutting ON parts(is_cut_completed) WHERE requires_cutting = TRUE;
CREATE INDEX idx_parts_stage_edging ON parts(is_edge_banding_completed) WHERE requires_edge_banding = TRUE;

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
    allowed_stages VARCHAR(100),  -- "1,2,3"
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
    production_stage INTEGER NOT NULL,  -- 1-5
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

CREATE INDEX idx_sessions_active ON worker_sessions(workstation_id, is_active) WHERE is_active = TRUE;
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
CREATE INDEX idx_scan_logs_worker ON scan_logs(worker_id, scan_time);

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
    status VARCHAR(20) DEFAULT 'new' CHECK (status IN ('new', 'in_progress', 'repaired', 'scrapped')),
    repaired_by_worker_id INTEGER REFERENCES workers(id),
    repaired_at TIMESTAMP,
    repair_notes TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);
```

---

## 9. Стан-машина етапів

### 9.1 Діаграма станів

```
                    ┌─────────────────────────────────────────────────────────────────┐
                    │                    PART STATE MACHINE                            │
                    └─────────────────────────────────────────────────────────────────┘
                    
                                            ┌─────────┐
                                            │  START  │
                                            └────┬────┘
                                                 │
                    ┌────────────────────────────┼────────────────────────────┐
                    │ requires_cutting?          │                            │
                    │                            ▼                            │
                    │                    ┌───────────────┐                    │
                    │               YES  │   CUTTING     │                    │
                    │          ┌────────▶│   PENDING     │                    │
                    │          │         └───────┬───────┘                    │
                    │          │                 │ scan                       │
                    │          │                 ▼                            │
                    │          │         ┌───────────────┐                    │
                    │          │         │   CUTTING     │                    │
                    │          │         │   COMPLETED   │                    │
                    │          │         └───────┬───────┘                    │
                    │          │                 │                            │
                    └──────────┼─────────────────┼────────────────────────────┘
                               │ NO              │
                               │                 │
                    ┌──────────┼─────────────────┼────────────────────────────┐
                    │          │                 │                            │
                    │          │                 ▼                            │
                    │          │  requires  ┌───────────────┐                 │
                    │          │  _edge_    │ EDGE_BANDING  │                 │
                    │          │  banding?  │   PENDING     │                 │
                    │          └───────────▶│ sides: 0/N    │                 │
                    │               YES     └───────┬───────┘                 │
                    │                               │ scan (repeat N times)   │
                    │                               ▼                         │
                    │                       ┌───────────────┐                 │
                    │                       │ EDGE_BANDING  │                 │
                    │                       │   COMPLETED   │                 │
                    │                       │ sides: N/N    │                 │
                    │                       └───────┬───────┘                 │
                    │                               │                         │
                    └───────────────────────────────┼─────────────────────────┘
                                                    │
                    ┌───────────────────────────────┼─────────────────────────┐
                    │ requires_drilling?            │                         │
                    │                               ▼                         │
                    │                       ┌───────────────┐                 │
                    │                  YES  │   DRILLING    │                 │
                    │              ┌───────▶│   PENDING     │                 │
                    │              │        └───────┬───────┘                 │
                    │              │                │ scan                    │
                    │              │                ▼                         │
                    │              │        ┌───────────────┐                 │
                    │              │        │   DRILLING    │                 │
                    │              │        │   COMPLETED   │                 │
                    │              │        └───────┬───────┘                 │
                    │              │                │                         │
                    └──────────────┼────────────────┼─────────────────────────┘
                                   │ NO             │
                                   │                │
                    ┌──────────────┼────────────────┼─────────────────────────┐
                    │              │                ▼                         │
                    │              │ requires  ┌───────────────┐              │
                    │              │ _sorting? │   SORTING     │              │
                    │              └──────────▶│   PENDING     │              │
                    │                   YES    └───────┬───────┘              │
                    │                                  │ scan                 │
                    │                                  ▼                      │
                    │                          ┌───────────────┐              │
                    │                          │   SORTING     │              │
                    │                          │   COMPLETED   │              │
                    │                          └───────┬───────┘              │
                    │                                  │                      │
                    └──────────────────────────────────┼──────────────────────┘
                                                       │
                    ┌──────────────────────────────────┼──────────────────────┐
                    │ requires_packing?                │                      │
                    │                                  ▼                      │
                    │                          ┌───────────────┐              │
                    │                     YES  │   PACKING     │              │
                    │                 ┌───────▶│   PENDING     │              │
                    │                 │        └───────┬───────┘              │
                    │                 │                │ scan                 │
                    │                 │                ▼                      │
                    │                 │        ┌───────────────┐              │
                    │                 │        │   PACKING     │              │
                    │                 │        │   COMPLETED   │              │
                    │                 │        └───────┬───────┘              │
                    │                 │                │                      │
                    └─────────────────┼────────────────┼──────────────────────┘
                                      │ NO             │
                                      │                │
                                      └────────┬───────┘
                                               │
                                               ▼
                                        ┌─────────────┐
                                        │    DONE     │
                                        │ (all stages │
                                        │  completed) │
                                        └─────────────┘
```

### 9.2 Правила переходів

| Поточний стан | Умова переходу | Наступний стан |
|---------------|----------------|----------------|
| START | requires_cutting = true | CUTTING_PENDING |
| START | requires_cutting = false | (skip to next) |
| CUTTING_PENDING | scan received | CUTTING_COMPLETED |
| CUTTING_COMPLETED | requires_edge_banding = true | EDGE_BANDING_PENDING |
| EDGE_BANDING_PENDING | scan received, sides < required | EDGE_BANDING_PENDING (increment) |
| EDGE_BANDING_PENDING | scan received, sides = required | EDGE_BANDING_COMPLETED |
| ... | ... | ... |
| PACKING_COMPLETED | - | DONE |

---

## 10. Алгоритм планування

### 10.1 Вхідні дані

```
Input:
    orders: List<Order>         // Замовлення (дата, площа в м²)
    dailyCapacity: Integer      // Потужність цеху (м²/день)
    productionLeadTime: Integer // Термін виробництва (робочих днів)
    daysBeforeProduction: Integer // Днів на підготовку
    workingDaysService          // Сервіс робочих днів (враховує вихідні)
```

### 10.2 Алгоритм

```pseudocode
function calculateSchedule(orders, dailyCapacity, leadTime, prepDays):
    schedule = new ProductionSchedule()
    dateLoad = new Map<Date, Float>()  // Завантаження по днях
    
    for each order in orders:
        // 1. Визначити найраніший старт
        earliestStart = addWorkingDays(order.date, prepDays)
        
        // 2. Знайти день з вільною потужністю
        actualStart = findNextAvailableDate(earliestStart, order.squareMeters, 
                                            dailyCapacity, dateLoad)
        
        // 3. Розподілити замовлення по днях
        remaining = order.squareMeters
        currentDate = actualStart
        allocation = new Map<Date, Float>()
        
        while remaining > 0:
            // Скільки можемо зробити в цей день
            currentLoad = dateLoad.get(currentDate) OR 0
            available = dailyCapacity - currentLoad
            
            if available > 0:
                toProcess = min(remaining, available)
                allocation[currentDate] = toProcess
                dateLoad[currentDate] = currentLoad + toProcess
                remaining -= toProcess
            
            currentDate = nextWorkingDay(currentDate)
        
        // 4. Визначити дату завершення
        productionEndDate = lastKey(allocation)
        completionDate = addWorkingDays(productionEndDate, leadTime)
        
        // 5. Зберегти результат
        schedule.add(new OrderSchedule(
            order: order,
            productionStart: actualStart,
            productionEnd: productionEndDate,
            completionDate: completionDate,
            dailyAllocation: allocation
        ))
    
    return schedule

function findNextAvailableDate(startDate, requiredCapacity, dailyCapacity, dateLoad):
    currentDate = startDate
    
    while true:
        currentLoad = dateLoad.get(currentDate) OR 0
        
        if currentLoad < dailyCapacity:
            return currentDate
        
        currentDate = nextWorkingDay(currentDate)
        
        // Захист від нескінченного циклу
        if currentDate > startDate + 365 days:
            return startDate  // Fallback
    
function nextWorkingDay(date):
    next = date + 1 day
    
    while isWeekend(next) OR isHoliday(next):
        next = next + 1 day
    
    return next

function addWorkingDays(date, days):
    result = date
    
    for i = 1 to days:
        result = nextWorkingDay(result)
    
    return result
```

---

## 11. Рекомендації щодо реалізації

### 11.1 Вибір технологій

| Мова | Рекомендовані фреймворки |
|------|-------------------------|
| **Python** | FastAPI/Django + SQLAlchemy + PostgreSQL |
| **Java** | Spring Boot + JPA/Hibernate + PostgreSQL |
| **Go** | Gin/Echo + GORM + PostgreSQL |
| **Node.js** | NestJS/Express + TypeORM/Prisma + PostgreSQL |
| **Rust** | Actix-web + Diesel + PostgreSQL |
| **PHP** | Laravel + Eloquent + PostgreSQL/MySQL |

### 11.2 Критичні компоненти

1. **Транзакційність сканування**
   - Оновлення статусу деталі + створення логу = одна транзакція
   - Використовуйте оптимістичне блокування для конкурентного доступу

2. **Унікальність QR-кодів**
   - Комбінація (project_uuid, part_id, part_counter) ЗАВЖДИ унікальна
   - part_counter генерується послідовно при імпорті

3. **Безпека авторизації**
   - PIN-коди зберігайте як хеш (bcrypt з cost ≥ 10)
   - Токени сесій — UUID v4 або криптографічно безпечний random

4. **Real-time оновлення**
   - WebSocket для push-нотифікацій про сканування
   - Server-Sent Events як альтернатива

### 11.3 Масштабування

```
                    ┌─────────────────┐
                    │  Load Balancer  │
                    └────────┬────────┘
                             │
           ┌─────────────────┼─────────────────┐
           │                 │                 │
           ▼                 ▼                 ▼
    ┌─────────────┐   ┌─────────────┐   ┌─────────────┐
    │  API Node 1 │   │  API Node 2 │   │  API Node N │
    └──────┬──────┘   └──────┬──────┘   └──────┬──────┘
           │                 │                 │
           └─────────────────┼─────────────────┘
                             │
                    ┌────────┴────────┐
                    │                 │
                    ▼                 ▼
             ┌──────────┐      ┌──────────┐
             │ Primary  │      │ Replica  │
             │ Database │ ───▶ │ Database │
             └──────────┘      └──────────┘
```

### 11.4 Тестування

| Тип | Що тестувати |
|-----|--------------|
| **Unit** | Алгоритми canAdvanceToStage, parseQRCode, планування |
| **Integration** | API endpoints, транзакції БД |
| **E2E** | Повний цикл: імпорт → сканування → статистика |
| **Load** | Конкурентні сканування (100+ req/sec) |

### 11.5 Моніторинг

Метрики для відстеження:
- Кількість сканувань/хв
- Час відповіді API (p50, p95, p99)
- Кількість помилок сканування
- Розмір черги необроблених деталей

---

## Контрольний список для портування

- [ ] Реалізувати парсер XML проектів
- [ ] Реалізувати генерацію/парсинг QR-кодів
- [ ] Створити схему БД та міграції
- [ ] Реалізувати State Machine етапів
- [ ] Реалізувати API сканування
- [ ] Реалізувати авторизацію працівників
- [ ] Реалізувати алгоритм планування
- [ ] Реалізувати збір KPI
- [ ] Додати WebSocket для real-time оновлень
- [ ] Написати тести
- [ ] Налаштувати моніторинг

---

*Специфікація версії 1.0 | Січень 2025*
