# Виправлення: Дата відвантаження на день пізніше фінішу виробництва

## Проблема
Дата відвантаження (`CompletionDate`) була **рівна** даті закінчення виробництва (`ProductionEndDate`), що є нелогічним. 

### Було:
```
ProductionEndDate (Фініш):  22.10.2025
CompletionDate (Відвантаж): 22.10.2025  ? Однакові дати
```

### Логіка:
1. **Виробництво завершується** (ProductionEndDate)
2. **Наступного дня відбувається відвантаження** (CompletionDate)

## Рішення

### 1. Виправлення базової логіки
**Було:**
```csharp
var completionDate = productionEndDate; // ? Однакова дата
lastProductionEndDate = completionDate;
```

**Стало:**
```csharp
// CompletionDate (дата відвантаження) = ProductionEndDate (фініш виробництва) + 1 робочий день
var completionDate = _workingDaysService.GetNextWorkingDay(productionEndDate);
lastProductionEndDate = completionDate;
```

### 2. Виправлення логіки для кастомної дати відвантаження

**Проблема:** Якщо користувач задає дату відвантаження `25.10.2025`, виробництво повинно завершитись **24.10.2025** (або раніше).

**Було:**
```csharp
if (customCompletionDate.HasValue)
{
    var targetEndDate = customCompletionDate.Value; // ? Виробництво до дати відвантаження
    
    // Розподіл виробництва...
    while (remainingOrder > 0 && currentDate <= targetEndDate)
    {
 // ...
    }
}
```

**Стало:**
```csharp
if (customCompletionDate.HasValue)
{
    // ВАЖЛИВО: targetEndDate = customCompletionDate - 1 робочий день
    // Виробництво повинно завершитись за день до відвантаження
    var targetEndDate = _workingDaysService.AddWorkingDays(customCompletionDate.Value, -1);
 
    // Розподіл виробництва...
    currentDate = earliestProductionStartDate;
    while (remainingOrder > 0 && currentDate <= targetEndDate)
    {
        // ...
        productionEndDate = currentDate; // Фініш виробництва
    }
}

// Після циклу:
var completionDate = _workingDaysService.GetNextWorkingDay(productionEndDate);
```

## Приклади

### Приклад 1: Автоматичний розрахунок
```
Замовлення №5:
 - Старт виробництва:  20.10.2025 (Пн)
 - Фініш виробництва:  22.10.2025 (Ср)  ? ProductionEndDate
 - Дата відвантаження: 23.10.2025 (Чт)  ? CompletionDate = ProductionEndDate + 1 день ?
```

### Приклад 2: Кастомна дата відвантаження
```
Замовлення №8:
 - Задана дата відвантаження: 30.10.2025 (Чт)
 - targetEndDate = 30.10.2025 - 1 день = 29.10.2025 (Ср) ? Фініш виробництва
 - Розподіл виробництва: 27.10 - 29.10.2025
 - ProductionEndDate: 29.10.2025 (Ср)
 - CompletionDate: 30.10.2025 (Чт) ?
```

### Приклад 3: З урахуванням вихідних
```
Замовлення №12:
 - Фініш виробництва: 24.10.2025 (Пт)  ? ProductionEndDate
 - Наступний робочий день: 27.10.2025 (Пн)  ? Пропущено Сб, Нд
 - Дата відвантаження: 27.10.2025 (Пн)  ? CompletionDate ?
```

## Вплив на систему

### До виправлення:
| Замовлення | Фініш | Відвантаж | Різниця |
|------------|-------|-----------|---------|
| №5 | 22.10.25 | 22.10.25 | 0 днів ? |
| №8 | 23.10.25 | 23.10.25 | 0 днів ? |
| №12 | 30.10.25 | 30.10.25 | 0 днів ? |

### Після виправлення:
| Замовлення | Фініш | Відвантаж | Різниця |
|------------|-------|-----------|---------|
| №5 | 22.10.25 | 23.10.25 | 1 день ? |
| №8 | 23.10.25 | 24.10.25 | 1 день ? |
| №12 | 24.10.25 | 27.10.25 | 1 роб. день ? |

## Технічні деталі

### Використання `WorkingDaysService`
```csharp
// Отримати наступний робочий день
var nextWorkingDay = _workingDaysService.GetNextWorkingDay(productionEndDate);

// Віднімаємо 1 робочий день від дати відвантаження
var targetEndDate = _workingDaysService.AddWorkingDays(customCompletionDate.Value, -1);
```

### Оновлені поля в `Order`:
```csharp
var order = new Order
{
    ProductionStartDate = productionStartDate,  // Початок виробництва
    ProductionEndDate = productionEndDate,      // Фініш виробництва
    CompletionDate = completionDate,      // Відвантаження (productionEndDate + 1 роб. день)
    // ...
};
```

## Тестування

### Автоматична перевірка:
```csharp
// Для кожного замовлення:
Assert.True(order.CompletionDate > order.ProductionEndDate);

// Різниця повинна бути >= 1 день
var diff = (order.CompletionDate - order.ProductionEndDate).Days;
Assert.True(diff >= 1);

// Якщо враховувати тільки робочі дні:
var workingDaysDiff = _workingDaysService.GetWorkingDaysBetween(
    order.ProductionEndDate, 
    order.CompletionDate
);
Assert.Equal(1, workingDaysDiff); // Рівно 1 робочий день
```

### Візуальна перевірка на діаграмі Ганта:
1. Відкрийте сторінку планування
2. Перегляньте таблицю замовлень
3. Перевірте, що **Відвантаж.** > **Фініш** для всіх замовлень
4. На діаграмі Ганта:
   - Синій блок закінчується на **Фініш** (ProductionEndDate)
   - Стрілка вказує на **Відвантаж.** (CompletionDate) - наступний день

## Додаткові покращення

### Логування для діагностики
Можна додати логування в `ProductionPlanningService`:
```csharp
Console.WriteLine($"Order #{day}: Production {productionStartDate:dd.MM.yy} - {productionEndDate:dd.MM.yy}, Completion: {completionDate:dd.MM.yy}");
```

### Валідація в UI
В `ProductionPlanning.razor` можна додати перевірку:
```csharp
@foreach (var order in schedule.Orders)
{
    if (order.CompletionDate <= order.ProductionEndDate)
  {
        <div class="alert alert-danger">
      ?? Помилка: Замовлення №@order.Day має некоректні дати!
        </div>
    }
}
```

## Результат

? **Дата відвантаження завжди на 1 робочий день пізніше фінішу виробництва**  
? **Враховуються вихідні та святкові дні**  
? **Коректна робота з кастомними датами відвантаження**  
? **Логічна послідовність подій: Виробництво ? Підготовка ? Відвантаження**

## Міграція даних

?? **Увага:** Після цього виправлення всі існуючі дати відвантаження зміняться на +1 день. 

Рекомендується:
1. Зберегти backup даних
2. Перерахувати всі графіки (кнопка "Розрахувати графік")
3. Перевірити критичні замовлення вручну
