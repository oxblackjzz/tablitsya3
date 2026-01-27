using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tablitsya3.Models.Scanning;

namespace Tablitsya3.Data.Entities
{
    /// <summary>
    /// Entity для працівника цеху
    /// </summary>
    public class WorkerEntity
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Унікальний код працівника (для QR/штрих-коду бейджа)</summary>
        [Required]
        [MaxLength(50)]
        public string WorkerCode { get; set; } = string.Empty;

        /// <summary>ПІБ працівника</summary>
        [Required]
        [MaxLength(255)]
        public string FullName { get; set; } = string.Empty;

        /// <summary>Ім'я</summary>
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>Прізвище</summary>
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        /// <summary>По батькові</summary>
        [MaxLength(100)]
        public string MiddleName { get; set; } = string.Empty;

        /// <summary>Посада</summary>
        [MaxLength(100)]
        public string Position { get; set; } = string.Empty;

        /// <summary>Номер цеху</summary>
        public int WorkshopNumber { get; set; } = 1;

        /// <summary>PIN-код для авторизації (4-6 цифр)</summary>
        [MaxLength(10)]
        public string? PinCode { get; set; }

        /// <summary>Хеш PIN-коду</summary>
        [MaxLength(255)]
        public string? PinCodeHash { get; set; }

        /// <summary>Дозволені етапи (через кому: 1,2,3)</summary>
        [MaxLength(100)]
        public string AllowedStages { get; set; } = string.Empty;

        /// <summary>Телефон</summary>
        [MaxLength(20)]
        public string? Phone { get; set; }

        /// <summary>Email</summary>
        [MaxLength(100)]
        public string? Email { get; set; }

        /// <summary>Дата прийому на роботу</summary>
        public DateTime? HireDate { get; set; }

        /// <summary>Чи активний працівник</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Дата створення запису</summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>Дата останнього оновлення</summary>
        public DateTime? UpdatedDate { get; set; }

        /// <summary>Примітки</summary>
        [MaxLength(500)]
        public string? Notes { get; set; }

        // === Обчислювані поля ===
        [NotMapped]
        public List<ProductionStage> AllowedStagesList
        {
            get
            {
                if (string.IsNullOrEmpty(AllowedStages))
                    return Enum.GetValues<ProductionStage>().ToList(); // Всі етапи за замовчуванням

                return AllowedStages.Split(',')
                    .Select(s => int.TryParse(s.Trim(), out var val) ? (ProductionStage?)val : null)
                    .Where(s => s.HasValue)
                    .Select(s => s!.Value)
                    .ToList();
            }
            set
            {
                AllowedStages = string.Join(",", value.Select(s => (int)s));
            }
        }

        [NotMapped]
        public string DisplayName => !string.IsNullOrEmpty(FullName) 
            ? FullName 
            : $"{LastName} {FirstName} {MiddleName}".Trim();
    }

    /// <summary>
    /// Entity для робочої станції/станка
    /// </summary>
    public class WorkstationEntity
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Унікальний код станції (для ідентифікації пристрою)</summary>
        [Required]
        [MaxLength(50)]
        public string StationCode { get; set; } = string.Empty;

        /// <summary>Назва станції</summary>
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Опис</summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>Номер цеху</summary>
        public int WorkshopNumber { get; set; } = 1;

        /// <summary>
        /// Етап виробництва, який виконується на цій станції
        /// </summary>
        public int ProductionStage { get; set; }

        /// <summary>
        /// Потужність станції за зміну (м²/день)
        /// </summary>
        public int Capacity { get; set; } = 0;

        /// <summary>Локація в цеху</summary>
        [MaxLength(100)]
        public string? Location { get; set; }

        /// <summary>Чи активна станція</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Чи потрібна авторизація працівника</summary>
        public bool RequiresWorkerAuth { get; set; } = true;

        /// <summary>Тайм-аут сесії в хвилинах (0 = без тайм-ауту)</summary>
        public int SessionTimeoutMinutes { get; set; } = 60;

        /// <summary>IP-адреса або ідентифікатор пристрою</summary>
        [MaxLength(100)]
        public string? DeviceIdentifier { get; set; }

        /// <summary>Дата створення запису</summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>Дата останнього оновлення</summary>
        public DateTime? UpdatedDate { get; set; }

        // === Обчислювані поля ===
        [NotMapped]
        public ProductionStage Stage => (ProductionStage)ProductionStage;

        [NotMapped]
        public string StageName => Stage.GetDisplayName();
    }

    /// <summary>
    /// Entity для сесії працівника на станції
    /// </summary>
    public class WorkerSessionEntity
    {
        [Key]
        public int Id { get; set; }

        /// <summary>ID працівника</summary>
        public int WorkerId { get; set; }

        /// <summary>ID станції</summary>
        public int WorkstationId { get; set; }

        /// <summary>Токен сесії</summary>
        [Required]
        [MaxLength(100)]
        public string SessionToken { get; set; } = string.Empty;

        /// <summary>Час початку сесії</summary>
        public DateTime StartTime { get; set; } = DateTime.UtcNow;

        /// <summary>Час закінчення сесії</summary>
        public DateTime? EndTime { get; set; }

        /// <summary>Чи активна сесія</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>IP-адреса пристрою</summary>
        [MaxLength(50)]
        public string? IpAddress { get; set; }

        /// <summary>User-Agent пристрою</summary>
        [MaxLength(500)]
        public string? UserAgent { get; set; }

        /// <summary>Кількість сканувань за сесію</summary>
        public int ScansCount { get; set; } = 0;

        /// <summary>Час останнього сканування</summary>
        public DateTime? LastScanTime { get; set; }

        // === Навігаційні властивості (для EF Core) ===
        [NotMapped]
        public WorkerEntity? Worker { get; set; }

        [NotMapped]
        public WorkstationEntity? Workstation { get; set; }

        // === Обчислювані поля ===
        [NotMapped]
        public TimeSpan Duration => (EndTime ?? DateTime.UtcNow) - StartTime;

        public bool IsExpired(int timeoutMinutes)
        {
            if (timeoutMinutes <= 0) return false;
            if (!IsActive) return true;
            
            var lastActivity = LastScanTime ?? StartTime;
            return DateTime.UtcNow - lastActivity > TimeSpan.FromMinutes(timeoutMinutes);
        }
    }

    /// <summary>
    /// Entity для статистики KPI працівника
    /// </summary>
    public class WorkerKpiEntity
    {
        [Key]
        public int Id { get; set; }

        /// <summary>ID працівника</summary>
        public int WorkerId { get; set; }

        /// <summary>Дата (день)</summary>
        public DateTime Date { get; set; }

        /// <summary>Етап виробництва</summary>
        public int ProductionStage { get; set; }

        /// <summary>Кількість оброблених деталей</summary>
        public int PartsProcessed { get; set; }

        /// <summary>Загальна площа оброблених деталей (м²)</summary>
        public double TotalSquareMeters { get; set; }

        /// <summary>Кількість браку</summary>
        public int DefectsCount { get; set; }

        /// <summary>Час роботи в хвилинах</summary>
        public int WorkMinutes { get; set; }

        /// <summary>Середній час на деталь (секунди)</summary>
        public double AvgTimePerPart { get; set; }

        /// <summary>Дата оновлення</summary>
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

        // === Обчислювані поля ===
        [NotMapped]
        public double DefectRate => PartsProcessed > 0 
            ? (double)DefectsCount / PartsProcessed * 100 
            : 0;

        [NotMapped]
        public double PartsPerHour => WorkMinutes > 0 
            ? PartsProcessed / (WorkMinutes / 60.0) 
            : 0;
    }

    /// <summary>
    /// Entity для браку/дефекту
    /// </summary>
    public class DefectEntity
    {
        [Key]
        public int Id { get; set; }

        /// <summary>ID деталі</summary>
        public int PartId { get; set; }

        /// <summary>QR-код деталі</summary>
        [Required]
        [MaxLength(100)]
        public string QRCode { get; set; } = string.Empty;

        /// <summary>ID працівника, який виявив/створив брак</summary>
        public int? WorkerId { get; set; }

        /// <summary>ID станції</summary>
        public int? WorkstationId { get; set; }

        /// <summary>Етап, на якому виявлено брак</summary>
        public int ProductionStage { get; set; }

        /// <summary>Тип браку</summary>
        [MaxLength(100)]
        public string DefectType { get; set; } = string.Empty;

        /// <summary>Опис браку</summary>
        [MaxLength(1000)]
        public string? Description { get; set; }

        /// <summary>Ступінь тяжкості (1-5)</summary>
        public int Severity { get; set; } = 1;

        /// <summary>Чи підлягає виправленню</summary>
        public bool IsRepairable { get; set; } = true;

        /// <summary>Статус: new, in_progress, repaired, scrapped</summary>
        [MaxLength(20)]
        public string Status { get; set; } = "new";

        /// <summary>ID працівника, який виправив</summary>
        public int? RepairedByWorkerId { get; set; }

        /// <summary>Дата виправлення</summary>
        public DateTime? RepairedDate { get; set; }

        /// <summary>Примітки до виправлення</summary>
        [MaxLength(500)]
        public string? RepairNotes { get; set; }

        /// <summary>Дата створення</summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>Дата оновлення</summary>
        public DateTime? UpdatedDate { get; set; }
    }
}
