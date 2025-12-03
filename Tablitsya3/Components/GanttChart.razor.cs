using Microsoft.AspNetCore.Components;
using таблиця3.Models;
using таблиця3.Services;

namespace таблиця3.Components;

public partial class GanttChart
{
    [Parameter]
    public List<Order>? Orders { get; set; }

    [Parameter]
    public Dictionary<int, double> DailyLoad { get; set; } = new();

    [Parameter]
    public int DailyCapacity { get; set; } = 1000;

    [Parameter]
    public int WorkshopNumber { get; set; } = 1;

    [Parameter]
    public DateTime? StartDate { get; set; }

    [Parameter]
 public DateTime? FilterFromDate { get; set; }

    [Parameter]
    public DateTime? FilterToDate { get; set; }
  
    [Inject]
    protected WorkingDaysService WorkingDays { get; set; } = default!;

    private List<OrderPosition> cachedOrderPositions = new();
    private DateTime? cachedPositionsDate = null;
    
    public enum ViewMode
{
        Pending,     // Незавершені
        InProduction,  // В роботі (новий!)
        All,  // Усі
        CustomDate// Користувацький період
    }
 
    private ViewMode currentViewMode = ViewMode.Pending;
    private DateTime? customFromDate = null;
    private DateTime? customToDate = null;
    
    protected void SetViewMode(ViewMode mode)
    {
      if (mode != ViewMode.CustomDate && mode != currentViewMode)
      {
   customFromDate = null;
            customToDate = null;
        }
  
currentViewMode = mode;
        StateHasChanged();
    }
  
    protected void ApplyCustomDateRange()
    {
        if (customFromDate.HasValue || customToDate.HasValue)
     {
  Console.WriteLine($"[GanttChart Workshop#{WorkshopNumber}] Applying custom date range: {customFromDate?.ToString("dd.MM.yyyy") ?? "null"} - {customToDate?.ToString("dd.MM.yyyy") ?? "null"}");
            currentViewMode = ViewMode.CustomDate;
    StateHasChanged();
        }
    }
  
    protected void ClearCustomDateRange()
    {
    customFromDate = null;
     customToDate = null;
  currentViewMode = ViewMode.All;
      StateHasChanged();
    }
    
    protected string GetCurrentViewDescription()
    {
 return currentViewMode switch
        {
     ViewMode.Pending => "Незавершені замовлення",
          ViewMode.InProduction => "Замовлення в роботі",
     ViewMode.CustomDate => customFromDate.HasValue || customToDate.HasValue 
 ? $"{customFromDate?.ToString("dd.MM.yyyy") ?? "Початок"} - {customToDate?.ToString("dd.MM.yyyy") ?? "Кінець"}"
     : "Користувацький період",
 ViewMode.All => "Всі замовлення",
   _ => "Невідомо"
     };
    }
 
    protected List<Order> ApplyViewModeFilter(List<Order> orders)
    {
     if (Orders != null && Orders.Any() && cachedPositionsDate != DateTime.Today)
     {
   var sortedOrders = Orders.OrderBy(o => o.StartDate).ThenBy(o => o.Day).ToList();
          var actualFirstOrderDate = sortedOrders.Min(o => o.StartDate);
  var latestOrderDate = sortedOrders.Max(o => o.CompletionDate);
       var totalProductionDays = Orders.Sum(o => o.SquareMeters / DailyCapacity);
     
    var firstWorkingDay = actualFirstOrderDate;
       while (!WorkingDays.IsWorkingDay(firstWorkingDay))
     {
       firstWorkingDay = firstWorkingDay.AddDays(1);
  }
    firstWorkingDay = firstWorkingDay.AddDays(5);
 while (!WorkingDays.IsWorkingDay(firstWorkingDay))
    {
       firstWorkingDay = firstWorkingDay.AddDays(1);
            }
   
          var allWorkingDaysGlobal = GenerateCalendarDatesRange(
firstWorkingDay, 
        latestOrderDate.AddDays((int)Math.Ceiling(totalProductionDays * 0.2)))
 .Where(d => WorkingDays.IsWorkingDay(d))
 .ToList();
   
      cachedOrderPositions = CalculateOrderPositions(sortedOrders, firstWorkingDay, allWorkingDaysGlobal);
          cachedPositionsDate = DateTime.Today;
   
    Console.WriteLine($"[GanttChart Workshop#{WorkshopNumber}] Cache updated: {cachedOrderPositions.Count} positions");
 }
     
   var result = currentViewMode switch
 {
 ViewMode.Pending => orders.Where(o => !IsOrderCompleted(o)).ToList(),
    ViewMode.InProduction => orders.Where(o => IsOrderInProgress(o)).ToList(),
         ViewMode.CustomDate => orders
  .Where(o => 
     {
           var position = cachedOrderPositions.FirstOrDefault(p => p.OrderDay == o.Day);
  if (position == null) 
       {
         Console.WriteLine($"[GanttChart Workshop#{WorkshopNumber}] No position found for order #{o.Day}");
          return false;
       }
         
 // ВИПРАВЛЕННЯ: Перевіряємо чи замовлення ПЕРЕТИНАЄТЬСЯ з періодом, а не чи воно повністю входить
        // Замовлення відображається якщо:
     // - Воно починається до кінця періоду (або немає обмеження "До")
         // - Воно закінчується після початку періоду (або немає обмеження "Від")
    bool startsBeforeEnd = !customToDate.HasValue || position.ProductionStartDate.Date <= customToDate.Value.Date;
         bool endsAfterStart = !customFromDate.HasValue || position.ProductionEndDate.Date >= customFromDate.Value.Date;
         
             bool matches = startsBeforeEnd && endsAfterStart;
   
        if (!matches)
     {
             Console.WriteLine($"[GanttChart Workshop#{WorkshopNumber}] Order #{o.Day} filtered out: Production {position.ProductionStartDate:dd.MM.yyyy}-{position.ProductionEndDate:dd.MM.yyyy}, Filter {customFromDate?.ToString("dd.MM.yyyy") ?? "null"}-{customToDate?.ToString("dd.MM.yyyy") ?? "null"}");
          }
 
          return matches;
            })
                .ToList(),
            ViewMode.All => orders,
            _ => orders
        };
     
   Console.WriteLine($"[GanttChart Workshop#{WorkshopNumber}] Filter result: {result.Count} orders from {orders.Count} (Mode: {currentViewMode})");
     
     return result;
    }
    
    protected (DateTime startDate, DateTime endDate) CalculateOptimalDateRange(List<Order> allOrders, List<Order> displayOrders)
    {
        if (!displayOrders.Any())
     {
        return (DateTime.Today, DateTime.Today.AddDays(30));
        }
  
        switch (currentViewMode)
        {
        case ViewMode.Pending:
       {
       if (!displayOrders.Any())
         {
  return (DateTime.Today, DateTime.Today.AddDays(30));
        }
 
      var baseDate = allOrders.Min(o => o.StartDate);
      
       var firstWorkingDay = baseDate;
    while (!WorkingDays.IsWorkingDay(firstWorkingDay))
            {
     firstWorkingDay = firstWorkingDay.AddDays(1);
}
  firstWorkingDay = firstWorkingDay.AddDays(5);
       while (!WorkingDays.IsWorkingDay(firstWorkingDay))
     {
    firstWorkingDay = firstWorkingDay.AddDays(1);
  }
 
         var endDate = allOrders.Max(o => o.CompletionDate);
   var workingDays = GenerateCalendarDatesRange(firstWorkingDay, endDate.AddDays(30))
     .Where(d => WorkingDays.IsWorkingDay(d))
      .ToList();
   
var allPositions = CalculateOrderPositions(allOrders, firstWorkingDay, workingDays);
  
     var displayOrderDays = displayOrders.Select(o => o.Day).ToHashSet();
 var positions = allPositions.Where(p => displayOrderDays.Contains(p.OrderDay)).ToList();
   
    if (!positions.Any())
      {
       return (displayOrders.Min(o => o.StartDate), displayOrders.Max(o => o.CompletionDate));
  }
  
   var filteredMinDate = positions.Min(p => p.ProductionStartDate);
     var filteredMaxDate = positions.Max(p => p.ProductionEndDate);

  return (filteredMinDate, filteredMaxDate);
}

        case ViewMode.InProduction:
  {
    if (!displayOrders.Any())
        {
            return (DateTime.Today.AddDays(-7), DateTime.Today.AddDays(7));
    }
           
    var baseDate = allOrders.Min(o => o.StartDate);
            
          var firstWorkingDay = baseDate;
    while (!WorkingDays.IsWorkingDay(firstWorkingDay))
     {
            firstWorkingDay = firstWorkingDay.AddDays(1);
                }
                firstWorkingDay = firstWorkingDay.AddDays(5);
while (!WorkingDays.IsWorkingDay(firstWorkingDay))
  {
firstWorkingDay = firstWorkingDay.AddDays(1);
           }
   
        var endDate = allOrders.Max(o => o.CompletionDate);
 var workingDays = GenerateCalendarDatesRange(firstWorkingDay, endDate.AddDays(30))
       .Where(d => WorkingDays.IsWorkingDay(d))
           .ToList();
   
var allPositions = CalculateOrderPositions(allOrders, firstWorkingDay, workingDays);
     
          var displayOrderDays = displayOrders.Select(o => o.Day).ToHashSet();
     var positions = allPositions.Where(p => displayOrderDays.Contains(p.OrderDay)).ToList();
  
                if (!positions.Any())
        {
       return (displayOrders.Min(o => o.StartDate), displayOrders.Max(o => o.CompletionDate));
       }
       
        var filteredMinDate = positions.Min(p => p.ProductionStartDate);
    var filteredMaxDate = positions.Max(p => p.ProductionEndDate);
              
 return (filteredMinDate, filteredMaxDate);
   }

        case ViewMode.CustomDate:
       {
      // Використовуємо дати виробництва з позицій
       if (displayOrders.Any() && cachedOrderPositions.Any())
       {
 var displayOrderDays = displayOrders.Select(o => o.Day).ToHashSet();
          var positions = cachedOrderPositions.Where(p => displayOrderDays.Contains(p.OrderDay)).ToList();
     
     if (positions.Any())
     {
            var minDate = customFromDate ?? positions.Min(p => p.ProductionStartDate);
              var maxDate = customToDate ?? positions.Max(p => p.ProductionEndDate);
  return (minDate, maxDate);
     }
   }
    
         // Fallback якщо немає позицій
    var fallbackMinDate = customFromDate ?? displayOrders.Min(o => o.StartDate);
    var fallbackMaxDate = customToDate ?? displayOrders.Max(o => o.CompletionDate);
    return (fallbackMinDate, fallbackMaxDate);
      }

default:
            {
   var baseDate = allOrders.Min(o => o.StartDate);
            var endDate = allOrders.Max(o => o.CompletionDate);
   
  var firstWorkingDay = baseDate;
                while (!WorkingDays.IsWorkingDay(firstWorkingDay))
     {
  firstWorkingDay = firstWorkingDay.AddDays(1);
        }
       firstWorkingDay = firstWorkingDay.AddDays(5);
   while (!WorkingDays.IsWorkingDay(firstWorkingDay))
    {
          firstWorkingDay = firstWorkingDay.AddDays(1);
      }
    
             var workingDays = GenerateCalendarDatesRange(firstWorkingDay, endDate.AddDays(30))
      .Where(d => WorkingDays.IsWorkingDay(d))
             .ToList();
        
    var allPositions = CalculateOrderPositions(allOrders, firstWorkingDay, workingDays);
    
                if (allPositions.Any())
         {
            var firstProductionStart = allPositions.Min(p => p.ProductionStartDate);
  var lastProductionEnd = allPositions.Max(p => p.ProductionEndDate);
 
return (firstProductionStart, lastProductionEnd);
         }
 
      return (allOrders.Min(o => o.StartDate), allOrders.Max(o => o.CompletionDate));
      }
        }
  }
    
    public class OrderSegment
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
  public double StartOffset { get; set; }
        public double EndOffset { get; set; }
        public double WidthPercent { get; set; } // Ширина сегмента в процентах
    }
    
    public class OrderPosition
    {
    public int OrderDay { get; set; }
        public DateTime ProductionStartDate { get; set; }
    public DateTime ProductionEndDate { get; set; }
        public double StartWorkingDayIndex { get; set; }
        public double EndWorkingDayIndex { get; set; }
    }
  
    protected List<OrderPosition> CalculateOrderPositions(List<Order> allOrders, DateTime baseDate, List<DateTime> allWorkingDays)
    {
        var positions = new List<OrderPosition>();
        double currentWorkingDayIndex = 0.0;
   
        foreach (var order in allOrders)
     {
            var productionDays = order.SquareMeters / (double)DailyCapacity;
            var startIndex = currentWorkingDayIndex;
     var endIndex = currentWorkingDayIndex + productionDays;
 
            var startDayFloor = (int)Math.Floor(startIndex);
            var endDayFloor = (int)Math.Floor(endIndex);
            var startOffset = startIndex - startDayFloor;
            var endOffset = endIndex - endDayFloor;
     
            if (endOffset == 0.0 && endDayFloor > 0)
            {
      endDayFloor--;
      endOffset = 1.0;
            }
   
      if (startDayFloor < allWorkingDays.Count && endDayFloor < allWorkingDays.Count)
    {
        var productionStartDate = allWorkingDays[startDayFloor];
        var productionEndDate = allWorkingDays[Math.Min(endDayFloor, allWorkingDays.Count - 1)];
       
       positions.Add(new OrderPosition
     {
     OrderDay = order.Day,
   ProductionStartDate = productionStartDate,
   ProductionEndDate = productionEndDate,
       StartWorkingDayIndex = startIndex,
   EndWorkingDayIndex = endIndex
         });
        }
     
  currentWorkingDayIndex = endIndex;
        }
   
        return positions;
    }
    
    protected List<OrderSegment> GetOrderSegmentsFromAbsoluteDates(DateTime startDate, DateTime endDate, 
    List<DateTime> calendarDates, double startWorkingDayIndex, double endWorkingDayIndex, 
        List<DateTime> allWorkingDays)
    {
      var segments = new List<OrderSegment>();

  var startDayFloor = (int)Math.Floor(startWorkingDayIndex);
        var endDayFloor = (int)Math.Floor(endWorkingDayIndex);
    var startOffset = startWorkingDayIndex - startDayFloor;
    var endOffset = endWorkingDayIndex - endDayFloor;
      
    if (endOffset == 0.0 && endDayFloor > 0)
    {
            endDayFloor--;
   endOffset = 1.0;
   }
   
        if (startDayFloor >= allWorkingDays.Count || endDayFloor >= allWorkingDays.Count)
        {
            return segments;
  }
  
        DateTime? currentSegmentStart = null;
        DateTime? currentSegmentEnd = null;
double currentSegmentStartOffset = 0.0;
        double currentSegmentEndOffset = 1.0;

        for (int i = startDayFloor; i <= endDayFloor && i < allWorkingDays.Count; i++)
        {
  var workingDay = allWorkingDays[i];
          var dayStartOffset = (i == startDayFloor) ? startOffset : 0.0;
       var dayEndOffset = (i == endDayFloor) ? endOffset : 1.0;
     
  if (!calendarDates.Contains(workingDay))
      {
            if (currentSegmentStart.HasValue && currentSegmentEnd.HasValue)
 {
   segments.Add(new OrderSegment
   {
    StartDate = currentSegmentStart.Value,
    EndDate = currentSegmentEnd.Value,
   StartOffset = currentSegmentStartOffset,
     EndOffset = currentSegmentEndOffset
 });
          
 currentSegmentStart = null;
   currentSegmentEnd = null;
        }
        continue;
  }
       
    if (currentSegmentStart == null)
        {
       currentSegmentStart = workingDay;
    currentSegmentStartOffset = dayStartOffset;
   }
 
   currentSegmentEnd = workingDay;
     currentSegmentEndOffset = dayEndOffset;
 
     var nextWorkingDay = (i + 1 < allWorkingDays.Count) ? allWorkingDays[i + 1] : DateTime.MaxValue;
        var daysDiff = (nextWorkingDay - workingDay).Days;

       if (i == endDayFloor || daysDiff > 1 || (i + 1 < allWorkingDays.Count && !calendarDates.Contains(nextWorkingDay)))
      {
       segments.Add(new OrderSegment
     {
      StartDate = currentSegmentStart.Value,
      EndDate = currentSegmentEnd.Value,
    StartOffset = currentSegmentStartOffset,
EndOffset = currentSegmentEndOffset
     });

   currentSegmentStart = null;
     currentSegmentEnd = null;
         currentSegmentStartOffset = 0.0;
     currentSegmentEndOffset = 1.0;
      }
      }
     
    // Розрахунок ширини кожного сегмента
        foreach (var segment in segments)
        {
var startCalendarIndex = calendarDates.IndexOf(segment.StartDate);
            var endCalendarIndex = calendarDates.IndexOf(segment.EndDate);
      
    if (startCalendarIndex != -1 && endCalendarIndex != -1)
     {
  var dayWidth = 100.0 / calendarDates.Count;
             var leftPercent = (startCalendarIndex + segment.StartOffset) * dayWidth;
 var rightPercent = (endCalendarIndex + segment.EndOffset) * dayWidth;
      segment.WidthPercent = rightPercent - leftPercent;
            }
        }
        
        return segments;
    }
    
    // Метод для знаходження сегмента з найбільшою шириною
    protected OrderSegment? GetLargestSegment(List<OrderSegment> segments)
    {
        if (!segments.Any()) return null;
        
        var largest = segments.OrderByDescending(s => s.WidthPercent).First();
  
      Console.WriteLine($"[GanttChart] Largest segment: Width={largest.WidthPercent:F2}%, {largest.StartDate:dd.MM.yyyy} - {largest.EndDate:dd.MM.yyyy}");
        
        return largest;
    }
  
    protected List<DateTime> GenerateCalendarDates(DateTime startDate, int totalDays)
    {
        var dates = new List<DateTime>();
        for (int i = 0; i < totalDays; i++)
        {
    dates.Add(startDate.AddDays(i));
        }
        return dates;
    }
    
  protected List<DateTime> GenerateCalendarDatesRange(DateTime startDate, DateTime endDate)
    {
        var dates = new List<DateTime>();
        var currentDate = startDate.Date;
   
        while (currentDate <= endDate.Date)
        {
  dates.Add(currentDate);
            currentDate = currentDate.AddDays(1);
        }
    
    return dates;
    }

    protected bool IsOrderCompleted(Order order)
    {
        return order.CompletionDate < DateTime.Today;
    }

    protected bool IsOrderInProgress(Order order)
 {
    // Просто використовуємо метод з Order
    return order.IsInProduction(DateTime.Today);
}
  
    protected string GetOrderColor(int orderDay)
    {
        var colors = new[]
        {
   "#4A90E2", "#E74C3C", "#2ECC71", "#FFA500", "#9B59B6",
        "#E91E63", "#00BCD4", "#8B5CF6", "#EC4899", "#10B981"
        };
     return colors[(orderDay - 1) % colors.Length];
    }

  protected string GetDayOfWeekShort(DateTime date)
    {
        return date.DayOfWeek switch
        {
            DayOfWeek.Monday => "Пн",
 DayOfWeek.Tuesday => "Вт",
            DayOfWeek.Wednesday => "Ср",
    DayOfWeek.Thursday => "Чт",
     DayOfWeek.Friday => "Пт",
            DayOfWeek.Saturday => "Сб",
 DayOfWeek.Sunday => "Нд",
  _ => ""
        };
    }
}
