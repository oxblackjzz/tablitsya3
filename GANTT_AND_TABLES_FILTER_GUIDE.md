# ?? Виправлення фільтрів для діаграм Ганта та таблиць

## ?? Проблема

Файл `ProductionPlanning.razor` був пошкоджений при редагуванні через велику складність структури.

## ? Що потрібно зробити

### Кроки виправлення:

1. **Відкотити файл до останньої робочої версії** (якщо є Git):
   ```bash
   git checkout HEAD -- "таблиця3/Components/Pages/ProductionPlanning.razor"
   ```

2. **Або відновити з резервної копії** (якщо була створена)

3. **Потім застосувати зміни поетапно:**

---

## ?? Зміни для додавання фільтрів

### 1. Діаграми Ганта - ЗАМІНИТИ секцію (рядки ~160-240):

```razor
<!-- Діаграми Ганта -->
@if (hasSchedules)
{
    <div class="mb-4">
        <h3 class="mb-3"><i class="bi bi-bar-chart-fill"></i> Діаграми завантаження цехів</h3>
     
        @if (schedule1 != null && schedule1.Orders.Any())
        {
            var filteredOrders1Gantt = FilterOrders(schedule1.Orders).ToList();
            @if (filteredOrders1Gantt.Any())
         {
          <div class="card mb-4">
       <div class="card-header bg-primary text-white">
              <h5 class="mb-0">
  Цех №1
         <span class="badge bg-light text-dark ms-2">@filteredOrders1Gantt.Count замовлень</span>
      </h5>
           </div>
          <div class="card-body">
   <GanttChart 
      WorkshopNumber="1" 
      Orders="@filteredOrders1Gantt" 
          StartDate="@workshopData.StartDate"
    DailyCapacity="@workshopData.WorkshopCapacities[1]" />
  </div>
         </div>
  }
            else if (orderFilter != "all")
    {
                <div class="alert alert-info">
              <i class="bi bi-info-circle-fill"></i>
        <strong>Цех №1:</strong> Немає замовлень з обраним фільтром "@GetFilterName()".
    </div>
      }
        }
        
     @if (schedule3 != null && schedule3.Orders.Any())
        {
        var filteredOrders3Gantt = FilterOrders(schedule3.Orders).ToList();
      @if (filteredOrders3Gantt.Any())
    {
    <div class="card mb-4">
 <div class="card-header bg-success text-white">
<h5 class="mb-0">
      Цех №3
               <span class="badge bg-light text-dark ms-2">@filteredOrders3Gantt.Count замовлень</span>
        </h5>
          </div>
         <div class="card-body">
       <GanttChart 
     WorkshopNumber="3" 
      Orders="@filteredOrders3Gantt" 
       StartDate="@workshopData.StartDate"
  DailyCapacity="@workshopData.WorkshopCapacities[3]" />
             </div>
       </div>
        }
   else if (orderFilter != "all")
            {
          <div class="alert alert-info">
   <i class="bi bi-info-circle-fill"></i>
    <strong>Цех №3:</strong> Немає замовлень з обраним фільтром "@GetFilterName()".
   </div>
      }
        }
   
        @if (schedule6 != null && schedule6.Orders.Any())
        {
      var filteredOrders6Gantt = FilterOrders(schedule6.Orders).ToList();
 @if (filteredOrders6Gantt.Any())
    {
      <div class="card mb-4">
             <div class="card-header bg-warning text-white">
       <h5 class="mb-0">
   Цех №6
         <span class="badge bg-light text-dark ms-2">@filteredOrders6Gantt.Count замовлень</span>
          </h5>
              </div>
  <div class="card-body">
       <GanttChart 
      WorkshopNumber="6" 
     Orders="@filteredOrders6Gantt" 
  StartDate="@workshopData.StartDate"
  DailyCapacity="@workshopData.WorkshopCapacities[6]" />
      </div>
         </div>
        }
            else if (orderFilter != "all")
{
         <div class="alert alert-info">
        <i class="bi bi-info-circle-fill"></i>
       <strong>Цех №6:</strong> Немає замовлень з обраним фільтром "@GetFilterName()".
    </div>
    }
        }
    </div>
}
```

---

### 2. Таблиця Цеху №3 - ДОДАТИ колонку "Статус":

**Знайти рядок:**
```razor
<!-- Цех №3 -->
@if (schedule3 != null && schedule3.Orders.Any())
{
```

**Замінити на:**
```razor
<!-- Цех №3 -->
@if (schedule3 != null && schedule3.Orders.Any())
{
    var filteredOrders3 = FilterOrders(schedule3.Orders).ToList();
    @if (filteredOrders3.Any())
  {
        <div class="card mb-4">
      <div class="card-header bg-success text-white">
            <h5 class="mb-0">
              Замовлення для цеху №3
       <span class="badge bg-light text-dark ms-2">@filteredOrders3.Count</span>
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
```

**У циклі foreach додати:**
```razor
@foreach (var order in filteredOrders3) // ? ЗМІНЕНО: було schedule3.Orders
{
    var isEditing = editingOrder?.Day == order.Day && editingOrder?.WorkshopNumber == 3;
  var status = order.GetStatus(filterDate); // ? ДОДАНО
    var statusClass = status switch {        // ? ДОДАНО
        "В роботі" => "badge bg-success",
    "Очікує" => "badge bg-warning",
     "Завершено" => "badge bg-primary",
      _ => "badge bg-secondary"
    };
  <tr class="@(isEditing ? "table-warning" : "")">
        <td><strong>№@order.Day</strong></td>
  <td><!-- ...назва... --></td>
        <td><span class="@statusClass">@status</span></td> <!-- ? ДОДАНО -->
        <!-- ...інші колонки... -->
```

---

### 3. Таблиця Цеху №6 - АНАЛОГІЧНО до Цеху №3

---

### 4. Додати метод `GetFilterName()` в секцію `@code`:

```csharp
// Отримати назву фільтру
private string GetFilterName()
{
    return orderFilter switch
    {
        "in-production" => "В роботі",
        "not-started" => "Очікують",
        "completed" => "Завершено",
        _ => "Усі замовлення"
    };
}
```

---

## ?? Що робить кожна зміна:

### Діаграми Ганта:
- ? **Фільтрація** - показує тільки відфільтровані замовлення
- ? **Лічильник** - кількість відфільтрованих замовлень у заголовку
- ? **Повідомлення** - якщо немає замовлень з обраним фільтром

### Таблиці:
- ? **Колонка "Статус"** - показує статус кожного замовлення з кольоровим badge
- ? **Фільтрація** - показує тільки відфільтровані замовлення

---

## ?? Очікуваний результат:

### При фільтрі "В роботі" (03.11.2025):

**Діаграма Ганта Цех №1:**
```
??????????????????????????????????
?  Цех №1   [2 замовлення]    ?
??????????????????????????????????
?  [Діаграма показує тільки     ?
?   замовлення, що виконуються   ?
?   03.11.2025]      ?
??????????????????????????????????
```

**Таблиця Цех №1:**
```
???????????????????????????????????????????????
?  №  ?  Назва   ?    Статус    ? Замовлення  ?
???????????????????????????????????????????????
?  2  ? Замов #2 ? ?? В роботі? 28.10.25    ?
?  3  ? Замов #3 ? ?? В роботі  ? 29.10.25    ?
???????????????????????????????????????????????
```

---

## ?? Якщо виникли проблеми:

1. **Зробіть резервну копію** поточного файлу
2. **Відновіть з Git** або резервної копії
3. **Застосуйте зміни поетапно**, перевіряючи компіляцію після кожної зміни
4. **Використайте пошук** (Ctrl+F) для знаходження потрібних секцій

---

*Версія: 1.0 - Фільтри для діаграм Ганта та таблиць*
