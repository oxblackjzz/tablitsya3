namespace Tablitsya3.Models
{
    /// <summary>
    /// Рівень доступу користувача до системи
    /// </summary>
    public enum UserRole
    {
        /// <summary>
        /// Тільки перегляд (головна, діаграми, замовлення)
        /// </summary>
        Viewer = 0,

        /// <summary>
        /// Сканування та перегляд оперативних даних
        /// </summary>
        Operator = 1,

        /// <summary>
        /// Планування виробництва, замовлення, працівники, станції
        /// </summary>
        Manager = 2,

        /// <summary>
        /// Повний доступ + керування користувачами/налаштуваннями
        /// </summary>
        Admin = 3,
    }

    public static class UserRoleExtensions
    {
        public static string GetDisplayName(this UserRole role) => role switch
        {
            UserRole.Admin => "Адміністратор",
            UserRole.Manager => "Менеджер",
            UserRole.Operator => "Оператор",
            UserRole.Viewer => "Спостерігач",
            _ => role.ToString(),
        };

        public static string GetBadgeColor(this UserRole role) => role switch
        {
            UserRole.Admin => "#dc3545",
            UserRole.Manager => "#0d6efd",
            UserRole.Operator => "#198754",
            UserRole.Viewer => "#6c757d",
            _ => "#6c757d",
        };
    }

    /// <summary>
    /// Назви політик авторизації
    /// </summary>
    public static class AuthPolicies
    {
        public const string AdminOnly = "AdminOnly";
        public const string ManagerOrAbove = "ManagerOrAbove";
        public const string OperatorOrAbove = "OperatorOrAbove";
        public const string AnyAuthenticated = "AnyAuthenticated";
    }
}
