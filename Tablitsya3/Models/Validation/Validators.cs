using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Tablitsya3.Models.Validation
{
    /// <summary>
    /// Результат валідації
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid => !Errors.Any();
        public List<ValidationError> Errors { get; set; } = new();

        public static ValidationResult Success() => new();
        
        public static ValidationResult Failure(string field, string message) => new()
        {
            Errors = new List<ValidationError> { new(field, message) }
        };

        public void AddError(string field, string message)
        {
            Errors.Add(new ValidationError(field, message));
        }
    }

    /// <summary>
    /// Помилка валідації
    /// </summary>
    public class ValidationError
    {
        public string Field { get; set; }
        public string Message { get; set; }

        public ValidationError(string field, string message)
        {
            Field = field;
            Message = message;
        }
    }

    /// <summary>
    /// Валідатор замовлення
    /// </summary>
    public class OrderValidator
    {
        public ValidationResult Validate(double squareMeters, DateTime orderDate, string? orderName = null)
        {
            var result = new ValidationResult();

            // Перевірка площі
            if (squareMeters <= 0)
            {
                result.AddError("SquareMeters", "Площа повинна бути більше 0");
            }
            else if (squareMeters > 100000)
            {
                result.AddError("SquareMeters", "Площа не може перевищувати 100,000 м²");
            }

            // Перевірка дати
            if (orderDate < new DateTime(2020, 1, 1))
            {
                result.AddError("OrderDate", "Дата замовлення не може бути раніше 2020 року");
            }
            else if (orderDate > DateTime.Today.AddYears(2))
            {
                result.AddError("OrderDate", "Дата замовлення не може бути більше ніж на 2 роки вперед");
            }

            // Перевірка назви (якщо вказана)
            if (!string.IsNullOrWhiteSpace(orderName) && orderName.Length > 500)
            {
                result.AddError("OrderName", "Назва замовлення не може перевищувати 500 символів");
            }

            return result;
        }
    }

    /// <summary>
    /// Валідатор конфігурації цеху
    /// </summary>
    public class WorkshopConfigValidator
    {
        public ValidationResult Validate(WorkshopConfig config)
        {
            var result = new ValidationResult();

            // Номер цеху
            if (config.Number < 1 || config.Number > 99)
            {
                result.AddError("Number", "Номер цеху повинен бути від 1 до 99");
            }

            // Потужність
            if (config.Capacity < 100)
            {
                result.AddError("Capacity", "Потужність повинна бути не менше 100 м²/день");
            }
            else if (config.Capacity > 50000)
            {
                result.AddError("Capacity", "Потужність не може перевищувати 50,000 м²/день");
            }

            // Тривалість виробництва
            if (config.ProductionLeadTime < 1)
            {
                result.AddError("ProductionLeadTime", "Тривалість виробництва повинна бути не менше 1 дня");
            }
            else if (config.ProductionLeadTime > 90)
            {
                result.AddError("ProductionLeadTime", "Тривалість виробництва не може перевищувати 90 днів");
            }

            // Дні до виробництва
            if (config.DaysBeforeProduction < 0)
            {
                result.AddError("DaysBeforeProduction", "Дні до виробництва не можуть бути від'ємними");
            }
            else if (config.DaysBeforeProduction > 60)
            {
                result.AddError("DaysBeforeProduction", "Дні до виробництва не можуть перевищувати 60");
            }

            // Назва
            if (string.IsNullOrWhiteSpace(config.Name))
            {
                result.AddError("Name", "Назва цеху обов'язкова");
            }
            else if (config.Name.Length > 100)
            {
                result.AddError("Name", "Назва цеху не може перевищувати 100 символів");
            }

            return result;
        }
    }

    /// <summary>
    /// Валідатор даних цеху
    /// </summary>
    public class WorkshopDataValidator
    {
        private readonly OrderValidator _orderValidator = new();

        public ValidationResult Validate(WorkshopData data)
        {
            var result = new ValidationResult();

            // Перевірка дати початку
            if (data.StartDate < new DateTime(2020, 1, 1))
            {
                result.AddError("StartDate", "Дата початку не може бути раніше 2020 року");
            }

            // Перевірка потужностей цехів
            foreach (var kvp in data.WorkshopCapacities)
            {
                if (kvp.Value < 100)
                {
                    result.AddError($"WorkshopCapacities[{kvp.Key}]", $"Потужність цеху №{kvp.Key} повинна бути не менше 100");
                }
            }

            // Перевірка узгодженості даних
            foreach (var workshopNumber in data.GetAllWorkshopNumbers())
            {
                var orders = data.WorkshopOrders.GetValueOrDefault(workshopNumber);
                var dates = data.WorkshopOrderDates.GetValueOrDefault(workshopNumber);
                var names = data.WorkshopOrderNames.GetValueOrDefault(workshopNumber);

                if (orders != null && dates != null && orders.Count != dates.Count)
                {
                    result.AddError($"Workshop{workshopNumber}", 
                        $"Кількість замовлень ({orders.Count}) не відповідає кількості дат ({dates.Count}) для цеху №{workshopNumber}");
                }

                // Валідація кожного замовлення
                if (orders != null && dates != null)
                {
                    for (int i = 0; i < orders.Count; i++)
                    {
                        var orderName = names != null && i < names.Count ? names[i] : null;
                        var orderValidation = _orderValidator.Validate(orders[i], dates[i], orderName);
                        
                        foreach (var error in orderValidation.Errors)
                        {
                            result.AddError($"Workshop{workshopNumber}.Order{i + 1}.{error.Field}", error.Message);
                        }
                    }
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Модель для масового введення замовлень з валідацією
    /// </summary>
    public class BulkOrderInput
    {
        [Required(ErrorMessage = "Дата замовлення обов'язкова")]
        public DateTime OrderDate { get; set; } = DateTime.Today;

        [Range(0, 100000, ErrorMessage = "Площа повинна бути від 0 до 100,000 м²")]
        public double SquareMeters { get; set; }

        [MaxLength(500, ErrorMessage = "Назва не може перевищувати 500 символів")]
        public string? Name { get; set; }

        public int WorkshopNumber { get; set; }
    }

    /// <summary>
    /// Валідатор масового введення
    /// </summary>
    public class BulkOrderValidator
    {
        public ValidationResult Validate(List<BulkOrderInput> orders)
        {
            var result = new ValidationResult();
            var orderValidator = new OrderValidator();

            if (!orders.Any(o => o.SquareMeters > 0))
            {
                result.AddError("Orders", "Потрібно ввести хоча б одне замовлення з площею більше 0");
                return result;
            }

            for (int i = 0; i < orders.Count; i++)
            {
                var order = orders[i];
                if (order.SquareMeters > 0)
                {
                    var orderValidation = orderValidator.Validate(order.SquareMeters, order.OrderDate, order.Name);
                    foreach (var error in orderValidation.Errors)
                    {
                        result.AddError($"Order[{i}].{error.Field}", error.Message);
                    }
                }
            }

            return result;
        }
    }
}
