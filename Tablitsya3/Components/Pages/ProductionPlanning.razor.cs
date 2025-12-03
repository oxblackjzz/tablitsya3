using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using таблиця3.Models;
using таблиця3.Services;

namespace таблиця3.Components.Pages;

public partial class ProductionPlanning : ComponentBase
{
 [Inject] private ProductionPlanningService PlanningService { get; set; } = default!;
    [Inject] private DataStorageService StorageService { get; set; } = default!;
    [Inject] private WorkingDaysService WorkingDaysService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private LoggingService Logger { get; set; } = default!;

    // Дані
    private WorkshopData workshopData = new();
    private ProductionSchedule? schedule1;
    private ProductionSchedule? schedule3;
    private ProductionSchedule? schedule6;
    private bool hasSchedules = false;
 private string statusMessage = string.Empty;
    
    // ?? Ключ для примусового оновлення діаграм Ганта
    private int ganttChartKey = 0;

    // Фільтр замовлень (використовується і для діаграм, і для таблиць)
    private string orderFilter = "all"; // "all", "in-production", "not-started", "completed"
    private DateTime filterDate = DateTime.Today;

    // Для редагування
  private Order? editingOrder;
    private string editOrderName = string.Empty;
    private DateTime editOrderDate = DateTime.Today;
    private DateTime editProductionStart = DateTime.Today;
    private DateTime editProductionEnd = DateTime.Today;
 private DateTime editCompletionDate = DateTime.Today;

    // Властивості для явної роботи з датами
    private string EditOrderDateValue
    {
        get => editOrderDate.ToString("yyyy-MM-dd");
 set
        {
    if (DateTime.TryParse(value, out var date))
      {
    editOrderDate = date;
          Logger.LogInfo($"Дата замовлення змінена на: {date:dd.MM.yyyy}", "ProductionPlanning");
            }
        }
    }

    private string EditProductionStartValue
    {
   get => editProductionStart.ToString("yyyy-MM-dd");
        set
        {
     if (DateTime.TryParse(value, out var date))
            {
            editProductionStart = date;
     Logger.LogInfo($"Дата початку виробництва змінена на: {date:dd.MM.yyyy}", "ProductionPlanning");
            }
}
    }

    private string EditProductionEndValue
    {
 get => editProductionEnd.ToString("yyyy-MM-dd");
      set
        {
   if (DateTime.TryParse(value, out var date))
            {
       editProductionEnd = date;
     Logger.LogInfo($"Дата закінчення виробництва змінена на: {date:dd.MM.yyyy}", "ProductionPlanning");
            }
        }
    }

    private string EditCompletionDateValue
    {
        get => editCompletionDate.ToString("yyyy-MM-dd");
        set
        {
            Logger.LogInfo($"?? EditCompletionDateValue.set викликано:", "ProductionPlanning");
    Logger.LogInfo($"  • Вхідне значення (value): '{value}'", "ProductionPlanning");

        if (DateTime.TryParse(value, out var date))
            {
       Logger.LogInfo($"  • TryParse УСПІШНО: {date:dd.MM.yyyy HH:mm:ss}", "ProductionPlanning");
           editCompletionDate = date;
                Logger.LogInfo($"  • editCompletionDate встановлено на: {editCompletionDate:dd.MM.yyyy HH:mm:ss}", "ProductionPlanning");
         }
   else
        {
         Logger.LogWarning($"  • TryParse ПРОВАЛИВСЯ для значення '{value}'", "ProductionPlanning");
            }
    }
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadData();
    }

    private void GoToBulkOrderEntry()
    {
        Navigation.NavigateTo("/bulk-order-entry");
    }

    private async Task ForceRecalculate()
    {
  if (!await JSRuntime.InvokeAsync<bool>("confirm", "Це очистить всі кастомні дати завершення та перерахує графіки з нуля. Продовжити?"))
     return;

        try
        {
            Logger.LogInfo("?? ПРИМУСОВИЙ ПЕРЕРАХУНОК: Очищення кастомних дат", "ProductionPlanning");

 // Очищаємо всі кастомні дати завершення
  var customDatesCount = workshopData.CustomCompletionDates.Count;
        workshopData.CustomCompletionDates.Clear();

     Logger.LogInfo($"? Видалено {customDatesCount} кастомних дат", "ProductionPlanning");

            // Перерахунок графіків з нуля
   CalculateAllSchedules();

 // Автоматично зберігаємо
            await SaveAllData();

     statusMessage = $"? Примусовий перерахунок виконано! Очищено {customDatesCount} кастомних дат.";
   Logger.LogInfo("? Примусовий перерахунок завершено успішно", "ProductionPlanning");
        }
        catch (Exception ex)
        {
       statusMessage = $"? Помилка примусового перерахунку: {ex.Message}";
            Logger.LogError("Помилка примусового перерахунку", ex, "ProductionPlanning");
        }
    }

    private async Task LoadData()
    {
        try
  {
     Logger.LogInfo("Початок завантаження даних", "ProductionPlanning");

   var loadedData = await StorageService.LoadWorkshopDataAsync();
            if (loadedData != null)
          {
  workshopData = loadedData;

    // Переконатися що є потужності для всіх цехів
      if (!workshopData.WorkshopCapacities.ContainsKey(1))
         workshopData.WorkshopCapacities[1] = 1000;
     if (!workshopData.WorkshopCapacities.ContainsKey(3))
    workshopData.WorkshopCapacities[3] = 1000;
           if (!workshopData.WorkshopCapacities.ContainsKey(6))
   workshopData.WorkshopCapacities[6] = 1000;

     var totalOrders = workshopData.WorkshopOrders.Sum(x => x.Value.Count);
   Logger.LogInfo($"Завантажено {totalOrders} замовлень з {workshopData.WorkshopOrders.Count} цехів", "ProductionPlanning");

// Автоматично розрахувати графіки якщо є замовлення
     if (workshopData.WorkshopOrders.Any())
       {
         CalculateAllSchedules();
    }
            }
            else
       {
     Logger.LogWarning("Дані не знайдено, створено нові", "ProductionPlanning");
     }

    StateHasChanged();
        }
  catch (Exception ex)
        {
   Logger.LogError("Помилка завантаження даних", ex, "ProductionPlanning");
         statusMessage = $"Помилка завантаження: {ex.Message}";
            Console.WriteLine($"Error loading data: {ex}");
        }
    }

    private void CalculateAllSchedules()
    {
        try
    {
     Logger.LogInfo("Початок розрахунку графіка для всіх цехів", "ProductionPlanning");
       var startTime = DateTime.Now;

     // Розраховуємо графік для цеху №1
            if (workshopData.WorkshopOrders.ContainsKey(1) && workshopData.WorkshopOrders[1].Any())
   {
    var dates1 = workshopData.WorkshopOrderDates.ContainsKey(1)
       ? workshopData.WorkshopOrderDates[1]
         : workshopData.WorkshopOrders[1].Select((_, i) => workshopData.StartDate.AddDays(i)).ToList();

      var names1 = workshopData.WorkshopOrderNames.ContainsKey(1)
 ? workshopData.WorkshopOrderNames[1]
         : null;

     schedule1 = PlanningService.CalculateSchedule(
          workshopData.WorkshopOrders[1],
      workshopData.StartDate,
       workshopData.WorkshopCapacities[1],
     workshopData.ProductionLeadTime,
           dates1,
        names1,
        workshopData.CustomCompletionDates,
      1,
         workshopData.DaysBeforeProduction
  );

          // Додаємо номер цеху до кожного замовлення
     foreach (var order in schedule1.Orders)
      {
           order.WorkshopNumber = 1;
     }
       }

     // Розраховуємо графік для цеху №3
            if (workshopData.WorkshopOrders.ContainsKey(3) && workshopData.WorkshopOrders[3].Any())
    {
        var dates3 = workshopData.WorkshopOrderDates.ContainsKey(3)
         ? workshopData.WorkshopOrderDates[3]
       : workshopData.WorkshopOrders[3].Select((_, i) => workshopData.StartDate.AddDays(i)).ToList();

     var names3 = workshopData.WorkshopOrderNames.ContainsKey(3)
         ? workshopData.WorkshopOrderNames[3]
          : null;

          schedule3 = PlanningService.CalculateSchedule(
        workshopData.WorkshopOrders[3],
      workshopData.StartDate,
       workshopData.WorkshopCapacities[3],
    workshopData.ProductionLeadTime,
      dates3,
            names3,
   workshopData.CustomCompletionDates,
      3,
        workshopData.DaysBeforeProduction
           );

          foreach (var order in schedule3.Orders)
   {
        order.WorkshopNumber = 3;
  }
    }

 // Розраховуємо графік для цеху №6
            if (workshopData.WorkshopOrders.ContainsKey(6) && workshopData.WorkshopOrders[6].Any())
            {
    var dates6 = workshopData.WorkshopOrderDates.ContainsKey(6)
     ? workshopData.WorkshopOrderDates[6]
          : workshopData.WorkshopOrders[6].Select((_, i) => workshopData.StartDate.AddDays(i)).ToList();

   var names6 = workshopData.WorkshopOrderNames.ContainsKey(6)
      ? workshopData.WorkshopOrderNames[6]
        : null;

          schedule6 = PlanningService.CalculateSchedule(
           workshopData.WorkshopOrders[6],
    workshopData.StartDate,
       workshopData.WorkshopCapacities[6],
        workshopData.ProductionLeadTime,
 dates6,
       names6,
          workshopData.CustomCompletionDates,
   6,
   workshopData.DaysBeforeProduction
        );

           foreach (var order in schedule6.Orders)
     {
          order.WorkshopNumber = 6;
        }
            }

            hasSchedules = (schedule1?.Orders.Any() ?? false) ||
            (schedule3?.Orders.Any() ?? false) ||
              (schedule6?.Orders.Any() ?? false);

            if (hasSchedules)
      {
       var elapsed = DateTime.Now - startTime;
       var totalOrders = (schedule1?.Orders.Count ?? 0) + (schedule3?.Orders.Count ?? 0) + (schedule6?.Orders.Count ?? 0);
       Logger.LogInfo($"Графік успішно розрахований: {totalOrders} замовлень за {elapsed.TotalMilliseconds:F0}ms", "ProductionPlanning");

           statusMessage = "? Графіки успішно розраховані для всіх цехів!";
    
     // ?? Збільшуємо ключ для оновлення діаграм
            ganttChartKey++;
       }
   else
         {
    Logger.LogWarning("Немає замовлень для розрахунку графіків", "ProductionPlanning");
statusMessage = "?? Немає замовлень для розрахунку.";
    }

       StateHasChanged();
        }
        catch (Exception ex)
        {
 Logger.LogError("Помилка розрахунку графіка", ex, "ProductionPlanning");
 statusMessage = $"Помилка розрахунку: {ex.Message}";
            Console.WriteLine($"Error calculating schedules: {ex}");
        }
    }

    private async Task SaveAllData()
    {
        try
        {
         Logger.LogInfo("Збереження даних", "ProductionPlanning");

            await StorageService.SaveWorkshopDataAsync(workshopData);

  var totalOrders = workshopData.WorkshopOrders.Sum(x => x.Value.Count);
       Logger.LogInfo($"Дані успішно збережено: {totalOrders} замовлень", "ProductionPlanning");

   statusMessage = "? Дані успішно збережено!";
            StateHasChanged();
        }
        catch (Exception ex)
        {
   Logger.LogError("Помилка збереження даних", ex, "ProductionPlanning");
      statusMessage = $"Помилка збереження: {ex.Message}";
     Console.WriteLine($"Error saving data: {ex}");
   }
    }

    private async Task ClearAllData()
    {
if (!await JSRuntime.InvokeAsync<bool>("confirm", "Ви впевнені, що хочете видалити всі дані? Цю дію неможливо скасувати."))
   return;

  try
        {
    await StorageService.ClearAllDataAsync();
         workshopData = new WorkshopData();
      schedule1 = null;
 schedule3 = null;
    schedule6 = null;
            hasSchedules = false;
         statusMessage = "? Всі дані очищено!";
         StateHasChanged();
        }
      catch (Exception ex)
        {
     statusMessage = $"Помилка очищення: {ex.Message}";
            Console.WriteLine($"Error clearing data: {ex}");
   }
    }

    private void StartOrderEdit(int workshopNumber, Order order)
    {
        editingOrder = order;
     editingOrder.WorkshopNumber = workshopNumber;
        editOrderName = order.OrderName ?? string.Empty;
        editOrderDate = order.StartDate;
        editProductionStart = order.ProductionStartDate;
      editProductionEnd = order.ProductionEndDate;
        editCompletionDate = order.CompletionDate;

        Logger.LogInfo($"?? Редагування замовлення №{order.Day} (Цех №{workshopNumber})", "ProductionPlanning");
        Logger.LogInfo($"?? Дата замовлення: {editOrderDate:dd.MM.yyyy}", "ProductionPlanning");
        Logger.LogInfo($"?? Початок виробництва: {editProductionStart:dd.MM.yyyy}", "ProductionPlanning");
        Logger.LogInfo($"?? Кінець виробництва: {editProductionEnd:dd.MM.yyyy}", "ProductionPlanning");
        Logger.LogInfo($"?? Дата завершення: {editCompletionDate:dd.MM.yyyy}", "ProductionPlanning");
        Logger.LogInfo($"?? Назва: '{editOrderName}'", "ProductionPlanning");

        StateHasChanged();
    }

    private bool ValidateOrderEdit()
    {
        Logger.LogInfo($"? Валідація: ДОЗВОЛЕНІ БУДЬ-ЯКІ ДАТИ", "ProductionPlanning");
        Logger.LogInfo($"  • Дата завершення: {editCompletionDate:dd.MM.yyyy}", "ProductionPlanning");
        Logger.LogInfo($"? Валідація пройдена (без обмежень)", "ProductionPlanning");

        return true;
    }

    private async Task SaveOrderEdit(int workshopNumber, Order order)
    {
        try
  {
            Logger.LogInfo($"?? === ПОЧАТОК ЗБЕРЕЖЕННЯ ===", "ProductionPlanning");
     Logger.LogInfo($"?? Замовлення №{order.Day}, Цех №{workshopNumber}", "ProductionPlanning");
    Logger.LogInfo($"?? Дата замовлення: {editOrderDate:dd.MM.yyyy}", "ProductionPlanning");
        Logger.LogInfo($"?? Початок виробництва: {editProductionStart:dd.MM.yyyy}", "ProductionPlanning");
       Logger.LogInfo($"?? Кінець виробництва: {editProductionEnd:dd.MM.yyyy}", "ProductionPlanning");
       Logger.LogInfo($"?? Дата завершення: {editCompletionDate:dd.MM.yyyy}", "ProductionPlanning");
            Logger.LogInfo($"?? Назва: '{editOrderName}'", "ProductionPlanning");

if (!ValidateOrderEdit())
            {
    Logger.LogWarning($"? Валідація не пройдена", "ProductionPlanning");
                return;
            }

            var schedule = workshopNumber switch
     {
                1 => schedule1,
   3 => schedule3,
     6 => schedule6,
    _ => null
            };

       if (schedule == null || !schedule.Orders.Any())
   {
      statusMessage = "? Помилка: графік не знайдено";
      Logger.LogError($"Графік не знайдено для цеху №{workshopNumber}", null, "ProductionPlanning");
    return;
     }

    var orderToEdit = schedule.Orders.FirstOrDefault(o => o.Day == order.Day);
    if (orderToEdit == null)
            {
       statusMessage = $"? Помилка: замовлення №{order.Day} не знайдено";
      Logger.LogError($"Замовлення №{order.Day} не знайдено в графіку", null, "ProductionPlanning");
return;
            }

       var scheduleIndex = schedule.Orders.IndexOf(orderToEdit);
          Logger.LogInfo($"?? Знайдено замовлення на індексі {scheduleIndex}", "ProductionPlanning");

       // Оновлюємо назву
if (!string.IsNullOrWhiteSpace(editOrderName))
            {
       var oldName = orderToEdit.OrderName;
     orderToEdit.OrderName = editOrderName;
    Logger.LogInfo($"?? Назва змінена: '{oldName}' ? '{editOrderName}'", "ProductionPlanning");

        if (!workshopData.WorkshopOrderNames.ContainsKey(workshopNumber))
                {
              workshopData.WorkshopOrderNames[workshopNumber] = new List<string>();
    }

    while (workshopData.WorkshopOrderNames[workshopNumber].Count <= scheduleIndex)
       {
         workshopData.WorkshopOrderNames[workshopNumber].Add(string.Empty);
                }

           workshopData.WorkshopOrderNames[workshopNumber][scheduleIndex] = editOrderName;
            }

   // Оновлюємо дату замовлення
            if (editOrderDate != order.StartDate)
            {
     orderToEdit.StartDate = editOrderDate;
     Logger.LogInfo($"?? Дата замовлення змінена: {order.StartDate:dd.MM.yyyy} ? {editOrderDate:dd.MM.yyyy}", "ProductionPlanning");

  if (!workshopData.WorkshopOrderDates.ContainsKey(workshopNumber))
  {
           workshopData.WorkshopOrderDates[workshopNumber] = new List<DateTime>();
          }

        while (workshopData.WorkshopOrderDates[workshopNumber].Count <= scheduleIndex)
    {
      workshopData.WorkshopOrderDates[workshopNumber].Add(DateTime.Today);
     }

      workshopData.WorkshopOrderDates[workshopNumber][scheduleIndex] = editOrderDate;
      }

 if (editProductionStart != order.ProductionStartDate)
   {
          Logger.LogInfo($"?? Дата початку виробництва змінена: {order.ProductionStartDate:dd.MM.yyyy} ? {editProductionStart:dd.MM.yyyy}", "ProductionPlanning");
}

 if (editProductionEnd != order.ProductionEndDate)
            {
   Logger.LogInfo($"?? Дата закінчення виробництва змінена: {order.ProductionEndDate:dd.MM.yyyy} ? {editProductionEnd:dd.MM.yyyy}", "ProductionPlanning");
    }

            // Зберігаємо дату завершення
       var key = $"{workshopNumber}_{order.Day}";
            workshopData.CustomCompletionDates[key] = editCompletionDate;
  Logger.LogInfo($"?? Дата завершення збережена: {editCompletionDate:dd.MM.yyyy} (ключ: {key})", "ProductionPlanning");

      editingOrder = null;
   StateHasChanged();

 Logger.LogInfo("?? Перерахунок графіків з новою датою завершення...", "ProductionPlanning");
   CalculateAllSchedules();

            Logger.LogInfo("?? Збереження даних у файл...", "ProductionPlanning");
            await SaveAllData();

 statusMessage = $"? Замовлення №{order.Day} успішно відредаговано!";
     Logger.LogInfo($"? Замовлення №{order.Day} успішно відредаговано", "ProductionPlanning");

            StateHasChanged();
        }
        catch (Exception ex)
    {
  statusMessage = $"? Помилка збереження: {ex.Message}";
     Logger.LogError($"Помилка збереження редагування замовлення №{order.Day}", ex, "ProductionPlanning");
       Console.WriteLine($"Error saving order edit: {ex}");
 }
    }

    private void CancelOrderEdit()
    {
    editingOrder = null;
      StateHasChanged();
    }

    private async Task DeleteOrder(int workshopNumber, int orderDay)
    {
        if (!await JSRuntime.InvokeAsync<bool>("confirm", $"Видалити замовлення №{orderDay} з цеху №{workshopNumber}?"))
          return;

        try
{
         Logger.LogInfo($"Видалення замовлення №{orderDay} з цеху №{workshopNumber}", "ProductionPlanning");

       var schedule = workshopNumber switch
  {
       1 => schedule1,
        3 => schedule3,
              6 => schedule6,
           _ => null
            };

            if (schedule == null || !schedule.Orders.Any())
   {
            statusMessage = "Помилка: графік не знайдено";
     return;
            }

            var orderToDelete = schedule.Orders.FirstOrDefault(o => o.Day == orderDay);
            if (orderToDelete == null)
  {
  statusMessage = $"Помилка: замовлення №{orderDay} не знайдено";
     return;
            }

            var scheduleIndex = schedule.Orders.IndexOf(orderToDelete);

if (workshopData.WorkshopOrders.ContainsKey(workshopNumber))
            {
      var orders = workshopData.WorkshopOrders[workshopNumber];

        if (scheduleIndex >= 0 && scheduleIndex < orders.Count)
          {
    orders.RemoveAt(scheduleIndex);

     if (workshopData.WorkshopOrderDates.ContainsKey(workshopNumber) &&
          scheduleIndex < workshopData.WorkshopOrderDates[workshopNumber].Count)
       {
         workshopData.WorkshopOrderDates[workshopNumber].RemoveAt(scheduleIndex);
    }

          if (workshopData.WorkshopOrderNames.ContainsKey(workshopNumber) &&
   scheduleIndex < workshopData.WorkshopOrderNames[workshopNumber].Count)
    {
   workshopData.WorkshopOrderNames[workshopNumber].RemoveAt(scheduleIndex);
               }

               var customKey = $"{workshopNumber}_{orderDay}";
   if (workshopData.CustomCompletionDates.ContainsKey(customKey))
           {
               workshopData.CustomCompletionDates.Remove(customKey);
        }
      }
            }

            CalculateAllSchedules();
            await SaveAllData();

            Logger.LogInfo($"Замовлення №{orderDay} успішно видалено з цеху №{workshopNumber}", "ProductionPlanning");
       statusMessage = $"? Замовлення №{orderDay} видалено з цеху №{workshopNumber}";
     StateHasChanged();
        }
    catch (Exception ex)
        {
            statusMessage = $"Помилка видалення: {ex.Message}";
     Logger.LogError($"Помилка видалення замовлення №{orderDay}", ex, "ProductionPlanning");
        }
    }

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

    // Методи для підрахунку замовлень
    private int GetTotalOrdersCount()
    {
        return (schedule1?.Orders.Count ?? 0) +
  (schedule3?.Orders.Count ?? 0) +
      (schedule6?.Orders.Count ?? 0);
    }

  private int GetInProductionCount()
  {
        return (schedule1?.Orders.Count(o => o.IsInProduction(filterDate)) ?? 0) +
  (schedule3?.Orders.Count(o => o.IsInProduction(filterDate)) ?? 0) +
           (schedule6?.Orders.Count(o => o.IsInProduction(filterDate)) ?? 0);
    }

    private int GetNotStartedCount()
    {
        return (schedule1?.Orders.Count(o => o.IsNotStarted(filterDate)) ?? 0) +
          (schedule3?.Orders.Count(o => o.IsNotStarted(filterDate)) ?? 0) +
   (schedule6?.Orders.Count(o => o.IsNotStarted(filterDate)) ?? 0);
    }

    private int GetCompletedCount()
    {
        return (schedule1?.Orders.Count(o => o.IsCompleted(filterDate)) ?? 0) +
       (schedule3?.Orders.Count(o => o.IsCompleted(filterDate)) ?? 0) +
    (schedule6?.Orders.Count(o => o.IsCompleted(filterDate)) ?? 0);
    }

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
}
