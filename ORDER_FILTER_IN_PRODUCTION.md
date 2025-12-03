# ?? Додавання фільтру "В роботі" для замовлень

## ? Що було зроблено:

### 1. Доданометоди в `Order.cs`:
```csharp
/// <summary>
/// Перевіряє чи замовлення зараз в роботі
/// </summary>
public bool IsInProduction(DateTime currentDate)
{
    return currentDate.Date >= ProductionStartDate.Date && 
   currentDate.Date <= ProductionEndDate.Date;
}

/// <summary>
/// Перевіряє чи замовлення завершене
/// </summary>
public bool IsCompleted(DateTime currentDate)
{
    return currentDate.Date > ProductionEndDate.Date;
}

/// <summary>
/// Перевіряє чи замовлення ще не почалося
/// </summary>
public bool IsNotStarted(DateTime currentDate)
{
    return currentDate.Date < ProductionStartDate.Date;
}

/// <summary>
/// Отримати статус замовлення
/// </summary>
public string GetStatus(DateTime currentDate)
{
 if (IsNotStarted(currentDate)) return "Очікує";
    if (IsInProduction(currentDate)) return "В роботі";
    if (IsCompleted(currentDate)) return "Завершено";
    return "Невідомо";
}
```

---

### 2. Додано змінні в `ProductionPlanning.razor`:
```csharp
// Фільтр замовлень
private string orderFilter = "all"; // "all", "in-production", "not-started", "completed"
private DateTime filterDate = DateTime.Today;
```

---

### 3. Додано методи фільтрації:
```csharp
// Метод для фільтрації замовлень
private IEnumerable<Order> FilterOrders(IEnumerable<Order> orders)
{
    if (orders == null || !orders.Any())
        return Enumerable.Empty<Order>();

 return orderFilter switch
{
      "in-production" => orders.Where(o => o.IsInProduction(filterDate)).OrderBy(o => o.ProductionStartDate),
    "not-started" => orders.Where(o => o.IsNotStarted(filterDate)).OrderBy(o => o.StartDate),
        "completed" => orders.Where(o => o.IsCompleted(filterDate)).OrderBy(o => o.CompletionDate),
        _ => orders.OrderBy(o => o.StartDate) // "all"
    };
}

// Методи для підрахунку
private int GetTotalOrdersCount() => 
    (schedule1?.Orders.Count ?? 0) + 
 (schedule3?.Orders.Count ?? 0) + 
    (schedule6?.Orders.Count ?? 0);

private int GetInProductionCount() => 
    (schedule1?.Orders.Count(o => o.IsInProduction(filterDate)) ?? 0) +
    (schedule3?.Orders.Count(o => o.IsInProduction(filterDate)) ?? 0) +
    (schedule6?.Orders.Count(o => o.IsInProduction(filterDate)) ?? 0);

private int GetNotStartedCount() =>
    (schedule1?.Orders.Count(o => o.IsNotStarted(filterDate)) ?? 0) +
    (schedule3?.Orders.Count(o => o.IsNotStarted(filterDate)) ?? 0) +
    (schedule6?.Orders.Count(o => o.IsNotStarted(filterDate)) ?? 0);

private int GetCompletedCount() =>
    (schedule1?.Orders.Count(o => o.IsCompleted(filterDate)) ?? 0) +
    (schedule3?.Orders.Count(o => o.IsCompleted(filterDate)) ?? 0) +
    (schedule6?.Orders.Count(o => o.IsCompleted(filterDate)) ?? 0);
```

---

### 4. UI фільтру (перед таблицями):
```razor
<!-- Фільтр замовлень -->
<div class="card mb-4">
    <div class="card-header bg-light">
 <div class="row align-items-center">
         <div class="col-md-6">
   <h6 class="mb-0">
            <i class="bi bi-funnel-fill"></i> Фільтр замовлень
           </h6>
         </div>
       <div class="col-md-6">
            <div class="input-group input-group-sm">
        <span class="input-group-text">
    <i class="bi bi-calendar3"></i> Дата перевірки:
    </span>
  <input type="date" 
 class="form-control" 
             value="@filterDate.ToString("yyyy-MM-dd")"
    @onchange="@((ChangeEventArgs e) => { filterDate = DateTime.Parse(e.Value?.ToString() ?? DateTime.Today.ToString()); StateHasChanged(); })" />
        </div>
  </div>
        </div>
    </div>
    <div class="card-body">
        <div class="btn-group w-100" role="group">
        <input type="radio" class="btn-check" name="orderFilter" id="filterAll" value="all" 
    checked="@(orderFilter == "all")" 
    @onchange="@(() => { orderFilter = "all"; StateHasChanged(); })" />
     <label class="btn btn-outline-secondary" for="filterAll">
     <i class="bi bi-list-ul"></i> Усі замовлення
         <span class="badge bg-secondary ms-2">@(GetTotalOrdersCount())</span>
   </label>

            <input type="radio" class="btn-check" name="orderFilter" id="filterInProduction" value="in-production" 
        checked="@(orderFilter == "in-production")" 
           @onchange="@(() => { orderFilter = "in-production"; StateHasChanged(); })" />
            <label class="btn btn-outline-success" for="filterInProduction">
    <i class="bi bi-gear-fill"></i> В роботі
         <span class="badge bg-success ms-2">@(GetInProductionCount())</span>
  </label>

        <input type="radio" class="btn-check" name="orderFilter" id="filterNotStarted" value="not-started" 
           checked="@(orderFilter == "not-started")" 
       @onchange="@(() => { orderFilter = "not-started"; StateHasChanged(); })" />
      <label class="btn btn-outline-warning" for="filterNotStarted">
    <i class="bi bi-hourglass-split"></i> Очікують
     <span class="badge bg-warning ms-2">@(GetNotStartedCount())</span>
  </label>

            <input type="radio" class="btn-check" name="orderFilter" id="filterCompleted" value="completed" 
     checked="@(orderFilter == "completed")" 
       @onchange="@(() => { orderFilter = "completed"; StateHasChanged(); })" />
         <label class="btn-outline-primary" for="filterCompleted">
        <i class="bi bi-check-circle-fill"></i> Завершено
         <span class="badge bg-primary ms-2">@(GetCompletedCount())</span>
            </label>
        </div>
    </div>
</div>
```

---

### 5. Оновлення таблиць (для кожного цеху):

#### Для Цеху №1:
```razor
@if (schedule1 != null && schedule1.Orders.Any())
{
    var filteredOrders1 = FilterOrders(schedule1.Orders).ToList();
    @if (filteredOrders1.Any())
{
 <div class="card mb-4">
  <div class="card-header bg-primary text-white">
        <h5 class="mb-0">
 Замовлення для цеху №1
          <span class="badge bg-light text-dark ms-2">@filteredOrders1.Count</span>
   </h5>
   </div>
            <div class="card-body">
     <div class="table-responsive">
    <table class="table table-striped table-hover table-compact">
          <thead>
        <tr>
       <th>№</th>
         <th>Назва</th>
       <th>Статус</th> <!-- ? НОВА КОЛОНКА -->
     <th>Замовлення</th>
         <!-- ...інші колонки... -->
        </tr>
       </thead>
 <tbody>
 @foreach (var order in filteredOrders1)
  {
            var isEditing = editingOrder?.Day == order.Day && editingOrder?.WorkshopNumber == 1;
 var status = order.GetStatus(filterDate); // ? СТАТУС
  var statusClass = status switch {
         "В роботі" => "badge bg-success",
          "Очікує" => "badge bg-warning",
        "Завершено" => "badge bg-primary",
  _ => "badge bg-secondary"
          };
    <tr class="@(isEditing ? "table-warning" : "")">
       <td><strong>№@order.Day</strong></td>
        <td>@(string.IsNullOrWhiteSpace(order.OrderName) ? "(без назви)" : order.OrderName)</td>
     <td><span class="@statusClass">@status</span></td> <!-- ? ВІДОБРАЖЕННЯ СТАТУСУ -->
       <!-- ...інші колонки... -->
         </tr>
     }
      </tbody>
            </table>
      </div>
    </div>
        </div>
    }
}
```

#### Аналогічно для Цеху №3 та Цеху №6 (замініть `schedule1` на `schedule3` або `schedule6`)

---

## ?? Як це працює:

### Приклад використання:

1. **Поточна дата:** 03.11.2025 (понеділок)

2. **Замовлення в цеху:**
   ```
   Замовлення №1:
   - Старт виробництва: 27.10.2025
   - Фініш виробництва: 31.10.2025
   - Статус: ? Завершено (фініш < 03.11)

   Замовлення №2:
   - Старт виробництва: 28.10.2025
   - Фініш виробництва: 03.11.2025
   - Статус: ?? В роботі (28.10 ? 03.11 ? 03.11)

   Замовлення №3:
   - Старт виробництва: 29.10.2025
   - Фініш виробництва: 04.11.2025
   - Статус: ?? В роботі (29.10 ? 03.11 ? 04.11)

 Замовлення №4:
   - Старт виробництва: 05.12.2025
   - Фініш виробництва: 08.12.2025
   - Статус: ? Очікує (старт > 03.11)
   ```

3. **Фільтр "В роботі"** покаже: Замовлення №2 та №3

---

## ?? Кольори статусів:

| Статус | Колір | Badge Class |
|--------|-------|-------------|
| ?? В роботі | Зелений | `badge bg-success` |
| ? Очікує | Жовтий | `badge bg-warning` |
| ? Завершено | Синій | `badge bg-primary` |

---

## ? Завершення:

Запустіть додаток і перевірте:

1. **Фільтр "В роботі"** показує тільки замовлення в процесі виробництва
2. **Фільтр "Очікують"** показує замовлення, які ще не почалися
3. **Фільтр "Завершено"** показує завершені замовлення
4. **Кількість замовлень** відображається на кнопках фільтра
5. **Колонка "Статус"** показує поточний стан замовлення з кольоровим badge

---

*Версія: 1.0 - Фільтр замовлень за статусом виробництва*
