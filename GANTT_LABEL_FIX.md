# Виправлення відображення назв замовлень на розірваних сегментах

## Проблема
Коли замовлення розривається через вихідні дні, назва відображається на **першому сегменті**, який може бути **малим**, через що назву не видно.

### Приклад проблеми:
```
Замовлення: 14.10, 15.10, 16.10, 17.10 | [ВИХІДНІ 25.10, 26.10] | 27.10, 28.10, 29.10
Сегменти:
 - Сегмент 1: 14.10-17.10 (малий, 3 дні)
 - Сегмент 2: 27.10-29.10 (великий, 5 днів)

Було: Назва показується на Сегменті 1 (малий) ?
Стало: Назва показується на Сегменті 2 (великий) ?
```

## Рішення

### 1. Додано властивість `WidthPercent` до `OrderSegment`
```csharp
public class OrderSegment
{
    public DateTime StartDate { get; set; }
 public DateTime EndDate { get; set; }
    public double StartOffset { get; set; }
    public double EndOffset { get; set; }
    public double WidthPercent { get; set; } // ? Нове: ширина сегмента в %
}
```

### 2. Розрахунок ширини кожного сегмента
В методі `GetOrderSegmentsFromAbsoluteDates` додано розрахунок ширини:
```csharp
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
```

### 3. Метод для знаходження найбільшого сегмента
```csharp
protected OrderSegment? GetLargestSegment(List<OrderSegment> segments)
{
  if (!segments.Any()) return null;
    
    var largest = segments.OrderByDescending(s => s.WidthPercent).First();
    
    Console.WriteLine($"[GanttChart] Largest segment: Width={largest.WidthPercent:F2}%, {largest.StartDate:dd.MM.yyyy} - {largest.EndDate:dd.MM.yyyy}");
    
    return largest;
}
```

### 4. Оновлена логіка відображення назви в `GanttChart.razor`

**Було:**
```razor
@foreach (var segment in segments)
{
    var isFirstSegment = segment == segments.First();
    
    string barLabel = "";
    if (isFirstSegment) // ? Завжди на першому сегменті
    {
        barLabel = order.OrderName;
  }
}
```

**Стало:**
```razor
var largestSegment = GetLargestSegment(segments);

@foreach (var segment in segments)
{
    var isFirstSegment = segment == segments.First();
    var isLargestSegment = segment == largestSegment; // ? Визначаємо найбільший
    
    string barLabel = "";
    if (isLargestSegment) // ? Показуємо на найбільшому сегменті
    {
   barLabel = order.OrderName;
    }
}
```

### 5. Бейджі статусу теж на найбільшому сегменті
```razor
<div class="bar-label">
    @if (!string.IsNullOrWhiteSpace(barLabel))
    {
        <text>@barLabel</text>
    }
    @if (isLargestSegment) // ? Було: isFirstSegment
    {
        @if (isCompleted)
{
   <span class="status-badge completed-badge"></span>
 }
    else if (isInProgress)
        {
            <span class="status-badge progress-badge"></span>
        }
    }
</div>
```

## Як це працює

### Алгоритм:
1. **Створення сегментів**: Замовлення розбивається на сегменти по робочим дням
2. **Розрахунок ширини**: Для кожного сегмента розраховується ширина у відсотках
3. **Пошук найбільшого**: Метод `GetLargestSegment` знаходить сегмент з максимальною шириною
4. **Відображення**: Назва та бейджі показуються **тільки на найбільшому сегменті**

### Приклад роботи:
```
Замовлення №5: "Замовлення А"
Сегменти:
 - Сегмент 1: 20.10-22.10 (WidthPercent = 8.5%)
 - Сегмент 2: 27.10-31.10 (WidthPercent = 15.2%) ? НАЙБІЛЬШИЙ
 - Сегмент 3: 03.11-04.11 (WidthPercent = 5.1%)

Результат: Назва "Замовлення А" показується на Сегменті 2 (27.10-31.10)
```

## Переваги рішення

? **Назва завжди видима**: Показується на найбільшому сегменті
? **Автоматичний вибір**: Не потрібно вручну налаштовувати
? **Універсальність**: Працює для будь-якої кількості розривів
? **Логування**: Можна відстежити в консолі, який сегмент обрано
? **Зворотна сумісність**: Якщо сегмент один, працює як раніше

## Тестування

### Перевірка в консолі браузера (F12):
```
[GanttChart] Largest segment: Width=15.23%, 27.10.2025 - 31.10.2025
[GanttChart] Largest segment: Width=22.45%, 21.10.2025 - 28.10.2025
```

### Візуальна перевірка:
1. Відкрийте діаграму Ганта з фільтром, який створює розриви
2. Знайдіть замовлення, яке розривається на вихідні
3. Перевірте, що назва показується на **найдовшому сегменті**
4. Наведіть курсор на кожен сегмент - tooltip працює для всіх

## Додаткові покращення

### Адаптивне скорочення назви
Логіка скорочення назви залишилась:
```csharp
if (widthPercent > 10)
{
    barLabel = order.OrderName; // Повна назва
}
else if (widthPercent > 5)
{
    barLabel = order.OrderName.Substring(0, 5); // Скорочена
}
else if (widthPercent > 3)
{
    barLabel = order.OrderName.Substring(0, 3); // Дуже скорочена
}
```

### Позначення останнього сегмента
Маркер закінчення виробництва (`??`) та стрілка дати відвантаження **залишились на останньому сегменті**:
```razor
@if (isLastSegment)
{
    <div class="production-end-marker">...</div>
    <div class="completion-date-arrow">...</div>
}
```

## Результат

?? **Назви замовлень тепер завжди видимі**, навіть якщо замовлення розривається на кілька сегментів через вихідні дні!
