# ? Code-Behind Refactoring - ProductionPlanning

## ?? Що зроблено

Розділено файл `ProductionPlanning.razor` (1000+ рядків) на **2 частини**:

### ?? Структура файлів

```
Components/Pages/
??? ProductionPlanning.razor        # UI частина (Razor розмітка) - ~700 рядків
??? ProductionPlanning.razor.cs     # Logic частина (C# код) - ~500 рядків
```

---

## ?? ProductionPlanning.razor (UI)

**Що містить:**
- `@page "/production-planning"`
- `@using` директиви
- `@rendermode InteractiveServer`
- HTML розмітка (Bootstrap)
- Razor синтаксис (`@if`, `@foreach`, `@bind`)

**Що видалено:**
- ? Блок `@code { }` - перенесено в `.razor.cs`
- ? `@inject` директиви - замінено на `[Inject]` атрибути в `.razor.cs`

---

##?? ProductionPlanning.razor.cs (Logic)

```csharp
namespace таблиця3.Components.Pages;

public partial class ProductionPlanning : ComponentBase
{
    // [Inject] - Dependency Injection
    [Inject] private ProductionPlanningService PlanningService { get; set; } = default!;
    [Inject] private DataStorageService StorageService { get; set; } = default!;
[Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private LoggingService Logger { get; set; } = default!;
    
    // Приватні поля
    private WorkshopData workshopData = new();
    private ProductionSchedule? schedule1;
    private ProductionSchedule? schedule3;
    private ProductionSchedule? schedule6;
    
    // Методи життєвого циклу
    protected override async Task OnInitializedAsync() { }
    
    // Business Logic методи
    private void CalculateAllSchedules() { }
    private async Task SaveAllData() { }
    private async Task LoadData() { }
    // ... інші методи
}
```

---

## ? Переваги Code-Behind

### 1. **Кращ читабельність**
- UI логіка відокремлена від бізнес-логіки
- Легше знайти потрібний код

### 2. **Підтримка**
- Зміни в UI не впливають на логіку
- Зміни в логіці не впливають на UI

### 3. **Тестування**
- Логіку можна тестувати окремо
- Не потрібно парсити Razor файли

### 4. **Організація коду**
- Чіткий поділ відповідальностей
- Менше конфліктів при роботі в команді

### 5. **IntelliSense**
- Краща підтримка автодоповнення в `.cs` файлах
- Швидша навігація по коду

---

## ?? Як це працює?

### Partial Class
```csharp
// ProductionPlanning.razor компілюється в:
public partial class ProductionPlanning { }

// ProductionPlanning.razor.cs:
public partial class ProductionPlanning : ComponentBase { }

// ? Компілятор об'єднує обидві частини в один клас
```

### Dependency Injection

**До:**
```razor
@inject ProductionPlanningService PlanningService
```

**Після:**
```csharp
[Inject] private ProductionPlanningService PlanningService { get; set; } = default!;
```

---

## ?? Статистика

| Метрика | До | Після |
|---------|-----|--------|
| Рядків в `.razor` | 1,047 | ~700 |
| Рядків в `.razor.cs` | 0 | ~500 |
| Читабельність | ?? | ????? |
| Підтримка | ?? | ????? |
| Тестування | ? | ???? |

---

## ?? Приклад використання

### Виклик методу з Razor

```razor
<!-- ProductionPlanning.razor -->
<button @onclick="CalculateAllSchedules">
    Розрахувати
</button>
```

```csharp
// ProductionPlanning.razor.cs
private void CalculateAllSchedules()
{
    // Логіка розрахунку
}
```

### Використання полів

```razor
<!-- ProductionPlanning.razor -->
@if (hasSchedules)
{
    <div>Є графіки</div>
}
```

```csharp
// ProductionPlanning.razor.cs
private bool hasSchedules = false;
```

---

## ?? Наступні кроки

### Можливі поліпшення:

1. **Створити окремі компоненти:**
   - `OrderFilterPanel.razor` - Панель фільтрів
   - `WorkshopStatistics.razor` - Статистика цехів
   - `GanttChartSection.razor` - Діаграми
   - `OrdersTable.razor` - Таблиці замовлень

2. **Винести спільну логіку:**
   - `OrderEditService` - Редагування замовлень
   - `OrderFilterService` - Фільтрація

3. **Додати Unit Tests:**
   ```csharp
   public class ProductionPlanningTests
   {
     [Fact]
       public void FilterOrders_InProduction_ReturnsCorrectOrders() { }
 }
   ```

---

## ?? Конвенції коду

### Іменування

- **Public методи**: `PascalCase`
  ```csharp
  public void CalculateSchedules() { }
  ```

- **Private методи**: `PascalCase` (C# convention)
  ```csharp
  private void LoadData() { }
  ```

- **Private поля**: `camelCase`
  ```csharp
  private bool hasSchedules = false;
  ```

- **Event handlers**: `On` + `EventName`
  ```csharp
  private void OnFilterChanged() { }
  ```

### Організація коду в `.razor.cs`

```csharp
public partial class ProductionPlanning : ComponentBase
{
    // 1. [Inject] Dependencies
    [Inject] private Service Service { get; set; } = default!;
    
    // 2. Properties
    private WorkshopData Data { get; set; } = new();
    
    // 3. Fields
    private bool isLoading = false;
    
// 4. Lifecycle Methods
    protected override async Task OnInitializedAsync() { }
    
    // 5. Public Methods
    public void DoSomething() { }
    
// 6. Private Methods
private void HelperMethod() { }
    
    // 7. Event Handlers
    private void OnButtonClick() { }
}
```

---

## ?? Важливо

### Що НЕ можна робити:

? **Дублювати `@page` директиву**
```csharp
// ProductionPlanning.razor.cs
[Route("/production-planning")] // ? НЕ РОБИТИ!
```

? **Використовувати `@inject` та `[Inject]` одночасно**

? **Створювати конструктор з параметрами**
```csharp
// ? НЕ РОБИТИ!
public ProductionPlanning(IService service)
{
}
```

### Що МОЖНА робити:

? **Використовувати `default!` для [Inject]**
```csharp
[Inject] private IService Service { get; set; } = default!;
```

? **Створювати приватні методи**
```csharp
private void HelperMethod() { }
```

? **Використовувати будь-які C# features**
```csharp
private record OrderInfo(int Id, string Name);
```

---

## ?? Перевірка

### Компіляція
```bash
dotnet build
# ? Сборка выполнена
```

### Запуск
```bash
dotnet run
# Перейти на: http://localhost:5000/production-planning
```

---

## ?? Ресурси

- [Blazor Component Class (Microsoft Docs)](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/class-libraries)
- [Partial Classes (C#)](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/partial-classes-and-methods)
- [Blazor Best Practices](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/best-practices)

---

*Версія: 1.0 - Code-Behind Refactoring Complete ?*
