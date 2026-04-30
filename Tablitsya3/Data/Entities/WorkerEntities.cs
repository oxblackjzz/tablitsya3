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

        /// <summary>Опційне посилання на обліковий запис web-користувача (AppUserEntity).</summary>
        public int? AppUserId { get; set; }

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

        /// <summary>Етап виробництва, який виконується на цій станції</summary>
        public int ProductionStage { get; set; }

        /// <summary>Локація в цеху</summary>
        [MaxLength(100)]
        public string? Location { get; set; }

        /// <summary>Чи активна станція</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Чи потрібна авторизація працівника</summary>
        public bool RequiresWorkerAuth { get; set; } = true;

        /// <summary>Тайм-аут сесії в хвилинах (0 = без тайм-ауту)</summary>
        public int SessionTimeoutMinutes { get; set; } = 60;

        /// <summary>Потужність станції (м³/день). 0 = не враховувати</summary>
        public decimal Capacity { get; set; } = 0;

        /// <summary>IP-адреса або ідентифікатор пристрою</summary>
        [MaxLength(100)]
        public string? DeviceIdentifier { get; set; }

        // === Сканер, прив'язаний до станції ===

        /// <summary>Модель сканера (значення <see cref="Models.Scanning.ScannerModel"/>)</summary>
        public int ScannerModel { get; set; } = 0;

        /// <summary>Тип фізичного підключення (значення <see cref="Models.Scanning.ScannerConnectionType"/>)</summary>
        public int ScannerConnectionType { get; set; } = 0;

        /// <summary>Чи увімкнено зв'язку зі сканером</summary>
        public bool ScannerEnabled { get; set; } = false;

        /// <summary>Назва пристрою / сканера (для зручності)</summary>
        [MaxLength(150)]
        public string? ScannerDeviceName { get; set; }

        /// <summary>Серійний номер сканера</summary>
        [MaxLength(100)]
        public string? ScannerSerialNumber { get; set; }

        /// <summary>USB VID (для USB HID)</summary>
        [MaxLength(10)]
        public string? ScannerUsbVid { get; set; }

        /// <summary>USB PID (для USB HID)</summary>
        [MaxLength(10)]
        public string? ScannerUsbPid { get; set; }

        /// <summary>COM-порт (для Serial)</summary>
        [MaxLength(20)]
        public string? ScannerComPort { get; set; }

        /// <summary>Baud rate для Serial</summary>
        public int? ScannerBaudRate { get; set; }

        /// <summary>MAC-адреса (для Bluetooth)</summary>
        [MaxLength(50)]
        public string? ScannerBluetoothMac { get; set; }

        /// <summary>IP-адреса сканера (TCP / HTTP)</summary>
        [MaxLength(50)]
        public string? ScannerIpAddress { get; set; }

        /// <summary>TCP/UDP порт (для мережевого підключення)</summary>
        public int? ScannerTcpPort { get; set; }

        /// <summary>Webhook URL (для HTTP)</summary>
        [MaxLength(500)]
        public string? ScannerWebhookUrl { get; set; }

        /// <summary>Префікс штрих-коду (опціонально)</summary>
        [MaxLength(20)]
        public string? ScannerPrefix { get; set; }

        /// <summary>Суфікс штрих-коду (зазвичай \r або \n)</summary>
        [MaxLength(20)]
        public string? ScannerSuffix { get; set; }

        /// <summary>Додаткові налаштування у форматі JSON</summary>
        public string? ScannerExtraJson { get; set; }

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

    /// <summary>
    /// Сканер, прив'язаний до станції. Одна станція може мати декілька сканерів
    /// (наприклад, один промисловий + один-два ручних для роботи з браком).
    /// </summary>
    public class WorkstationScannerEntity
    {
        [Key]
        public int Id { get; set; }

        public int WorkstationId { get; set; }

        /// <summary>Назва пристрою (для зручності)</summary>
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Роль сканера на станції: 0 = промисловий (основний), 1 = ручний,
        /// 2 = для браку, 3 = резервний.
        /// </summary>
        public int Role { get; set; } = 0;

        /// <summary>Чи це основний сканер станції</summary>
        public bool IsPrimary { get; set; } = false;

        /// <summary>Чи увімкнено цей сканер</summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>Модель сканера (значення <see cref="Models.Scanning.ScannerModel"/>)</summary>
        public int ScannerModel { get; set; } = 0;

        /// <summary>Тип фізичного підключення (значення <see cref="Models.Scanning.ScannerConnectionType"/>)</summary>
        public int ConnectionType { get; set; } = 0;

        [MaxLength(100)]
        public string? SerialNumber { get; set; }

        [MaxLength(10)]
        public string? UsbVid { get; set; }

        [MaxLength(10)]
        public string? UsbPid { get; set; }

        [MaxLength(20)]
        public string? ComPort { get; set; }

        public int? BaudRate { get; set; }

        [MaxLength(50)]
        public string? BluetoothMac { get; set; }

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        public int? TcpPort { get; set; }

        [MaxLength(500)]
        public string? WebhookUrl { get; set; }

        [MaxLength(20)]
        public string? Prefix { get; set; }

        [MaxLength(20)]
        public string? Suffix { get; set; }

        public string? ExtraJson { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedDate { get; set; }
    }
}
