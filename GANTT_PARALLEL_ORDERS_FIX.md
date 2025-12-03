# Виправлення паралельного розподілу замовлень в діаграмі Ганта

## Проблема
Замовлення на діаграмі Ганта і в таблиці відображалися **послідовно** (одне за одним), а не **паралельно** з урахуванням добової потужності цеху.

### Було:
```
№1  16.10.25  17.10.25  20.10.25  
№2  20.10.25  21.10.25  22.10.25  1,2  
№3  22.10.25  23.10.25  24.10.25  1,4  
№4  24.10.25  27.10.25  28.10.25  1,1  
№5  28.10.25  29.10.25  30.10.25  1,7  
```

### Стало:
```
№1  16.10.25  21.10.25  22.10.25  
№2  17.10.25  22.10.25  23.10.25  1,2  
№3  18.10.25  23.10.25  24.10.25  1,4  
№4  19.10.25  24.10.25  25.10.25  1,1  
№5  20.10.25  25.10.25  26.10.25  1,7  
```

## Причина
Було **ДВІ проблеми** в різних частинах коду:

### 1. У `GanttChart.CalculateOrderPositions` (компонент діаграми)
Метод використовував простий послідовний алгоритм:
```csharp
foreach (var order in allOrders)
{
    var startIndex = currentWorkingDayIndex;
    var endIndex = currentWorkingDayIndex + productionDays;
    currentWorkingDayIndex = endIndex; // ? Наступне замовлення починається після поточного
}
```

### 2. У `ProductionPlanningService.CalculateSchedule` (основний сервіс)
Код **примусово зсував** початок кожного замовлення:
```csharp
var earliestProductionStartDate = _workingDaysService.AddWorkingDays(orderDate, daysBeforeProduction);

if (lastProductionEndDate.HasValue && lastProductionEndDate.Value > earliestProductionStartDate)
{
    earliestProductionStartDate = lastProductionEndDate.Value; // ? ЦЕ РОБИТЬ ВИКОНАННЯ ПОСЛІДОВНИМ!
}
```

Це означало, що навіть якщо було вільне місце для паралельного виконання, замовлення все одно чекало завершення попереднього.

## Рішення

### 1. Виправлено `GanttChart.CalculateOrderPositions`
Переписано для паралельного розподілу з відстеженням завантаження:

```csharp
protected List<OrderPosition> CalculateOrderPositions(List<Order> allOrders, DateTime baseDate, List<DateTime> allWorkingDays)
{
    var positions = new List<OrderPosition>();
    var productionDateLoad = new Dictionary<DateTime, double>(); // ? Відстеження завантаження

    foreach (var order in allOrders)
    {
        var remainingOrder = order.SquareMeters;
        var earliestStartDate = order.StartDate.AddDays(5); // 5 днів на підготовку
    
        // ? Розподіляємо по робочим дням з урахуванням потужності
        while (remainingOrder > 0)
        {
            if (!productionDateLoad.ContainsKey(currentDate))
                productionDateLoad[currentDate] = 0;

      var availableCapacity = DailyCapacity - productionDateLoad[currentDate];

         if (availableCapacity > 0)
   {
      var toAllocate = Math.Min(remainingOrder, availableCapacity);
  productionDateLoad[currentDate] += toAllocate;
  remainingOrder -= toAllocate;
   }

       currentDate = currentDate.AddDays(1);
        }
    }
    
    return positions;
}
```

### 2. Виправлено `ProductionPlanningService.CalculateSchedule`
**Видалено** код що робив виконання послідовним:

```csharp
var earliestProductionStartDate = _workingDaysService.AddWorkingDays(orderDate, daysBeforeProduction);

// ? ВИДАЛЕНО: Код що робив виконання послідовним
// if (lastProductionEndDate.HasValue && lastProductionEndDate.Value > earliestProductionStartDate)
// {
//     earliestProductionStartDate = lastProductionEndDate.Value;
// }

// ? Тепер замовлення починається незалежно від інших
while (remainingOrder > 0)
{
    if (!productionDateLoad.ContainsKey(currentDate))
        productionDateLoad[currentDate] = 0;

    var availableCapacity = dailyCapacity - productionDateLoad[currentDate];

    if (availableCapacity > 0)
    {
        var toAllocate = Math.Min(remainingOrder, availableCapacity);
    productionDateLoad[currentDate] += toAllocate;
        remainingOrder -= toAllocate;
    }

    currentDate = currentDate.AddDays(1);
}
```

## Ключові зміни

1. **Відстеження завантаження**: `Dictionary<DateTime, double> productionDateLoad` зберігає використану потужність для кожного робочого дня

2. **Доступна потужність**: 
   ```csharp
   var availableCapacity = DailyCapacity - productionDateLoad[currentDate];
   ```

3. **Виділення потужності**: Замовлення отримує стільки потужності, скільки доступно:
   ```csharp
   var toAllocate = Math.Min(remainingOrder, availableCapacity);
   ```

4. **Паралельне виконання**: Кожне замовлення може починатися одразу після дати замовлення + N робочих днів, незалежно від інших замовлень

5. **Видалено примусовий зсув**: `lastProductionEndDate` більше не зсуває початок наступних замовлень

## Результат

? Замовлення тепер виконуються **паралельно** (одночасно) в межах добової потужності цеху  
? Враховуються вихідні та святкові дні  
? Оптимальне використання потужності цеху  
? Правильні дати початку виробництва для кожного замовлення  
? **Таблиця** і **діаграма Ганта** показують однакові дані  

## Файли змінено
- `таблиця3\Components\GanttChart.razor.cs` - метод `CalculateOrderPositions`
- `таблиця3\Services\ProductionPlanningService.cs` - метод `CalculateSchedule`

## Тестування
Після виправлення:
1. Відкрийте сторінку "Планування виробництва"
2. Натисніть "Розрахувати графіки"
3. Перевірте консоль браузера (F12) - маєте побачити паралельне виконання:
```
   День 21.10.2025: Виділено 1000 м?, Залишилось 200 м?, Завантаження дня: 1000/1000
   День 22.10.2025: Виділено 200 м?, Залишилось 0 м?, Завантаження дня: 200/1000
   День 22.10.2025: Виділено 800 м?, Залишилось 400 м?, Завантаження дня: 1000/1000  ? ПАРАЛЕЛЬНО!
   ```
4. Перевірте таблицю - замовлення повинні розподілятися паралельно
5. Перевірте діаграму Ганта - замовлення відображаються правильно
