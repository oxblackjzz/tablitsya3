# Виправлення: Підтримка від'ємних значень у AddWorkingDays

## Проблема
Після останнього оновлення з'явилась помилка:
```
Помилка розрахунку: Кількість робочих днів не може бути від'ємною (Parameter 'workingDays')
```

### Причина:
В `ProductionPlanningService.cs` додано виклик:
```csharp
var targetEndDate = _workingDaysService.AddWorkingDays(customCompletionDate.Value, -1);
```

Метод `AddWorkingDays` **не підтримував від'ємні значення** і викидав виключення:
```csharp
if (workingDays < 0)
    throw new ArgumentException("Кількість робочих днів не може бути від'ємною", nameof(workingDays));
```

## Рішення

### 1. Додано метод `GetPreviousWorkingDay`
Метод для отримання **попереднього робочого дня**:

```csharp
// Метод для отримання попереднього робочого дня
public DateTime GetPreviousWorkingDay(DateTime date)
{
var previousDay = date.AddDays(-1);
    while (!IsWorkingDay(previousDay))
    {
        previousDay = previousDay.AddDays(-1);
    }
    return previousDay;
}
```

### 2. Оновлено метод `AddWorkingDays`
Тепер підтримує **від'ємні значення** (віднімання робочих днів):

**Було:**
```csharp
public DateTime AddWorkingDays(DateTime startDate, int workingDays)
{
    if (workingDays < 0)
    throw new ArgumentException("Кількість робочих днів не може бути від'ємною", nameof(workingDays)); // ?

    if (workingDays == 0)
    return startDate;

    var currentDate = startDate;
    var daysAdded = 0;

    while (daysAdded < workingDays)
    {
        currentDate = GetNextWorkingDay(currentDate);
   daysAdded++;
    }

    return currentDate;
}
```

**Стало:**
```csharp
public DateTime AddWorkingDays(DateTime startDate, int workingDays)
{
    if (workingDays == 0)
        return startDate;

    var currentDate = startDate;

 // Підтримка від'ємних значень (віднімання робочих днів)
    if (workingDays < 0)
    {
        var daysToSubtract = Math.Abs(workingDays);
        var daysSubtracted = 0;

        while (daysSubtracted < daysToSubtract)
    {
      currentDate = GetPreviousWorkingDay(currentDate); // ?
          daysSubtracted++;
        }

        return currentDate;
    }

    // Додавання робочих днів (позитивне значення)
    var daysAdded = 0;
    while (daysAdded < workingDays)
    {
        currentDate = GetNextWorkingDay(currentDate);
        daysAdded++;
    }

    return currentDate;
}
```

## Приклади використання

### Додавання робочих днів (позитивне значення)
```csharp
var startDate = new DateTime(2025, 10, 20); // Понеділок
var result = _workingDaysService.AddWorkingDays(startDate, 3);
// Результат: 23.10.2025 (Четвер) ?
```

### Віднімання робочих днів (від'ємне значення)
```csharp
var startDate = new DateTime(2025, 10, 30); // Четвер (дата відвантаження)
var result = _workingDaysService.AddWorkingDays(startDate, -1); // Фініш виробництва
// Результат: 29.10.2025 (Середа) ?
```

### З урахуванням вихідних
```csharp
var startDate = new DateTime(2025, 10, 27); // Понеділок
var result = _workingDaysService.AddWorkingDays(startDate, -1);
// Результат: 24.10.2025 (Пятниця) - пропущено Сб, Нд ?
```

### З урахуванням свят
```csharp
var startDate = new DateTime(2025, 12, 26); // П'ятниця
var result = _workingDaysService.AddWorkingDays(startDate, -1);
// Результат: 24.12.2025 (Середа) - пропущено 25.12 (Різдво) ?
```

## Алгоритм роботи

### Для позитивних значень (`workingDays > 0`):
1. Починаємо з `startDate`
2. Викликаємо `GetNextWorkingDay()` в циклі `workingDays` разів
3. Пропускаємо вихідні та свята
4. Повертаємо результат

### Для від'ємних значень (`workingDays < 0`):
1. Беремо абсолютне значення: `Math.Abs(workingDays)`
2. Викликаємо `GetPreviousWorkingDay()` в циклі `|workingDays|` разів
3. Пропускаємо вихідні та свята **назад у часі**
4. Повертаємо результат

### Для нуля (`workingDays == 0`):
Повертаємо `startDate` без змін

## Вплив на систему

### ProductionPlanningService
Тепер коректно працює розрахунок фінішу виробництва:
```csharp
if (customCompletionDate.HasValue)
{
    // targetEndDate = customCompletionDate - 1 робочий день
    var targetEndDate = _workingDaysService.AddWorkingDays(customCompletionDate.Value, -1); // ? Працює!
    
    // Розподіл виробництва до targetEndDate...
}
```

### Приклад в контексті:
```
Користувач задає дату відвантаження: 30.10.2025 (Четвер)
targetEndDate = AddWorkingDays(30.10.2025, -1) = 29.10.2025 (Середа)
Виробництво розподіляється до 29.10.2025
CompletionDate = 30.10.2025 (Четвер)

Результат:
 - Фініш виробництва: 29.10.2025 ?
 - Дата відвантаження: 30.10.2025 ?
```

## Тестування

### Unit тести
```csharp
[Fact]
public void AddWorkingDays_NegativeValue_ReturnsCorrectDate()
{
    var service = new WorkingDaysService();
    var startDate = new DateTime(2025, 10, 30); // Четвер
    
    var result = service.AddWorkingDays(startDate, -1);
    
  Assert.Equal(new DateTime(2025, 10, 29), result); // Середа
}

[Fact]
public void AddWorkingDays_NegativeValueOverWeekend_SkipsWeekend()
{
    var service = new WorkingDaysService();
    var startDate = new DateTime(2025, 10, 27); // Понеділок
    
    var result = service.AddWorkingDays(startDate, -1);
    
    Assert.Equal(new DateTime(2025, 10, 24), result); // П'ятниця
}

[Fact]
public void AddWorkingDays_Zero_ReturnsSameDate()
{
    var service = new WorkingDaysService();
    var startDate = new DateTime(2025, 10, 20);
    
    var result = service.AddWorkingDays(startDate, 0);
    
    Assert.Equal(startDate, result);
}

[Fact]
public void GetPreviousWorkingDay_SkipsWeekend()
{
    var service = new WorkingDaysService();
    var monday = new DateTime(2025, 10, 27); // Понеділок
    
    var result = service.GetPreviousWorkingDay(monday);
    
    Assert.Equal(new DateTime(2025, 10, 24), result); // П'ятниця
}
```

## Додаткові методи

Тепер в `WorkingDaysService` є повний набір методів для роботи з робочими днями:

| Метод | Опис | Приклад |
|-------|------|---------|
| `IsWorkingDay(date)` | Перевірка чи день робочий | `IsWorkingDay(сб) ? false` |
| `GetNextWorkingDay(date)` | Наступний робочий день | `GetNextWorkingDay(пт) ? пн` |
| `GetPreviousWorkingDay(date)` | **Попередній робочий день** | `GetPreviousWorkingDay(пн) ? пт` |
| `AddWorkingDays(date, +n)` | Додати n робочих днів | `AddWorkingDays(пн, +3) ? чт` |
| `AddWorkingDays(date, -n)` | **Відняти n робочих днів** | `AddWorkingDays(чт, -1) ? ср` |
| `GetWorkingDaysBetween(start, end)` | Кількість робочих днів між датами | `GetWorkingDaysBetween(пн, пт) ? 5` |

## Результат

? **Помилка виправлена** - метод `AddWorkingDays` тепер підтримує від'ємні значення  
? **Додано `GetPreviousWorkingDay`** - новий метод для отримання попереднього робочого дня  
? **Коректний розрахунок фінішу виробництва** - виробництво завершується за день до відвантаження  
? **Враховуються вихідні та свята** - при відніманні днів також пропускаються неробочі дні  

## Міграція

?? **Увага:** Це виправлення **зворотньо сумісне**. Всі існуючі виклики з позитивними значеннями працюють як раніше.

Нова функціональність:
- ? `AddWorkingDays(date, -1)` - тепер працює!
- ? `GetPreviousWorkingDay(date)` - новий публічний метод
