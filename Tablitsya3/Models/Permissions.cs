using System.Collections.Generic;

namespace Tablitsya3.Models
{
    /// <summary>
    /// Дія в межах сторінки/розділу
    /// </summary>
    public enum PermissionAction
    {
        View = 0,
        Edit = 1,
        Delete = 2,
    }

    /// <summary>
    /// Опис розділу/сторінки, для якої налаштовуються права.
    /// Ключ дозволу має формат "<page>.<action>", наприклад "orders.view".
    /// </summary>
    public sealed class PermissionPage
    {
        public string Key { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string Group { get; init; } = string.Empty;
        public bool SupportsEdit { get; init; } = true;
        public bool SupportsDelete { get; init; } = true;

        public string ViewKey => $"{Key}.view";
        public string EditKey => $"{Key}.edit";
        public string DeleteKey => $"{Key}.delete";

        public IEnumerable<string> AllKeys()
        {
            yield return ViewKey;
            if (SupportsEdit) yield return EditKey;
            if (SupportsDelete) yield return DeleteKey;
        }
    }

    /// <summary>
    /// Реєстр усіх відомих сторінок/розділів і відповідних permission-ключів.
    /// </summary>
    public static class Permissions
    {
        public const string ClaimType = "perm";
        public const string PolicyPrefix = "perm:";

        // === Робота з виробництвом ===
        public static readonly PermissionPage Home = new() { Key = "home", DisplayName = "Головна", Group = "Загальне", SupportsEdit = false, SupportsDelete = false };
        public static readonly PermissionPage Planning = new() { Key = "planning", DisplayName = "Діаграми / Планування", Group = "Виробництво", SupportsEdit = true, SupportsDelete = false };
        public static readonly PermissionPage Gantt = new() { Key = "gantt", DisplayName = "Об'єднана діаграма", Group = "Виробництво", SupportsEdit = false, SupportsDelete = false };
        public static readonly PermissionPage Orders = new() { Key = "orders", DisplayName = "Замовлення", Group = "Виробництво" };
        public static readonly PermissionPage BulkOrders = new() { Key = "bulk_orders", DisplayName = "Нові замовлення (масово)", Group = "Виробництво", SupportsDelete = false };

        // === Сканування ===
        public static readonly PermissionPage Scanning = new() { Key = "scanning", DisplayName = "Сканування деталей", Group = "Сканування" };
        public static readonly PermissionPage ScanningImport = new() { Key = "scanning_import", DisplayName = "Імпорт проектів", Group = "Сканування" };

        // === Працівники / Станції ===
        public static readonly PermissionPage Workers = new() { Key = "workers", DisplayName = "Працівники", Group = "Персонал" };
        public static readonly PermissionPage WorkerStats = new() { Key = "worker_stats", DisplayName = "Статистика працівників", Group = "Персонал", SupportsEdit = false, SupportsDelete = false };
        public static readonly PermissionPage Workstations = new() { Key = "workstations", DisplayName = "Станції", Group = "Персонал" };
        public static readonly PermissionPage WorkerBadge = new() { Key = "worker_badge", DisplayName = "Бейджі працівників", Group = "Персонал", SupportsDelete = false };

        // === Якість ===
        public static readonly PermissionPage Defects = new() { Key = "defects", DisplayName = "Брак", Group = "Якість" };

        // === Аналітика / Адмін ===
        public static readonly PermissionPage Ai = new() { Key = "ai", DisplayName = "AI Аналітика", Group = "Аналітика", SupportsDelete = false };
        public static readonly PermissionPage WorkshopSettings = new() { Key = "workshop_settings", DisplayName = "Налаштування цехів", Group = "Адміністрування" };
        public static readonly PermissionPage Backup = new() { Key = "backup", DisplayName = "Бекапи", Group = "Адміністрування" };
        public static readonly PermissionPage Logs = new() { Key = "logs", DisplayName = "Журнал", Group = "Адміністрування", SupportsEdit = false, SupportsDelete = true };
        public static readonly PermissionPage Users = new() { Key = "users", DisplayName = "Користувачі", Group = "Адміністрування" };
        public static readonly PermissionPage Roles = new() { Key = "roles", DisplayName = "Ролі та права", Group = "Адміністрування" };

        public static readonly IReadOnlyList<PermissionPage> AllPages = new[]
        {
            Home, Planning, Gantt, Orders, BulkOrders,
            Scanning, ScanningImport,
            Workers, WorkerStats, Workstations, WorkerBadge,
            Defects,
            Ai, WorkshopSettings, Backup, Logs, Users, Roles
        };

        public static IEnumerable<string> AllKeys()
        {
            foreach (var p in AllPages)
                foreach (var k in p.AllKeys())
                    yield return k;
        }

        public static bool IsKnownKey(string key) => _knownKeys.Value.Contains(key);

        private static readonly System.Lazy<HashSet<string>> _knownKeys =
            new(() => new HashSet<string>(AllKeys(), System.StringComparer.OrdinalIgnoreCase));

        /// <summary>
        /// Дефолтні права для системних ролей під час першого засіювання БД.
        /// </summary>
        public static IReadOnlyDictionary<string, HashSet<string>> DefaultRolePermissions => _defaultRolePermissions.Value;

        private static readonly System.Lazy<IReadOnlyDictionary<string, HashSet<string>>> _defaultRolePermissions =
            new(BuildDefaultRolePermissions);

        private static IReadOnlyDictionary<string, HashSet<string>> BuildDefaultRolePermissions()
        {
            // Admin — все, що зареєстровано в Permissions
            var admin = new HashSet<string>(AllKeys(), System.StringComparer.OrdinalIgnoreCase);

            // Manager — все, крім адмінських розділів
            var manager = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var p in AllPages)
            {
                if (p == Backup || p == Logs || p == Users || p == Roles || p == WorkshopSettings)
                    continue;
                foreach (var k in p.AllKeys()) manager.Add(k);
            }

            // Operator — основна робота на лінії
            var operatorSet = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
            {
                Home.ViewKey,
                Planning.ViewKey,
                Gantt.ViewKey,
                Orders.ViewKey,
                Scanning.ViewKey, Scanning.EditKey,
                ScanningImport.ViewKey,
                WorkerStats.ViewKey,
                Defects.ViewKey, Defects.EditKey,
            };

            // Viewer — лише перегляд
            var viewer = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var p in AllPages)
            {
                if (p == Users || p == Roles || p == Backup || p == Logs || p == WorkshopSettings) continue;
                viewer.Add(p.ViewKey);
            }

            return new Dictionary<string, HashSet<string>>(System.StringComparer.OrdinalIgnoreCase)
            {
                ["Admin"] = admin,
                ["Manager"] = manager,
                ["Operator"] = operatorSet,
                ["Viewer"] = viewer,
            };
        }
    }
}
