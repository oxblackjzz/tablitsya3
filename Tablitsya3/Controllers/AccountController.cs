using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Tablitsya3.Models;
using Tablitsya3.Services;

namespace Tablitsya3.Controllers
{
    [Route("account")]
    public class AccountController : Controller
    {
        private readonly AuthService _auth;
        private readonly ILogger<AccountController> _logger;

        public AccountController(AuthService auth, ILogger<AccountController> logger)
        {
            _auth = auth;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromForm] string username,
            [FromForm] string password,
            [FromForm] string? returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            {
                return Redirect(BuildLoginUrl(returnUrl, "Введіть логін і пароль"));
            }

            var user = await _auth.AuthenticateAsync(username.Trim().ToLowerInvariant(), password);
            if (user == null)
            {
                _logger.LogWarning("Невдала спроба входу для {Username}", username);
                return Redirect(BuildLoginUrl(returnUrl, "Невірний логін або пароль"));
            }

            var role = (UserRole)user.Role;
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
                new("DisplayName", user.DisplayName ?? user.Username),
                new(ClaimTypes.Role, role.ToString()),
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14),
                });

            _logger.LogInformation("Вхід успішний: {Username} ({Role})", user.Username, role);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return Redirect("/");
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect("/login");
        }

        [HttpGet("logout")]
        public async Task<IActionResult> LogoutGet()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect("/login");
        }

        private static string BuildLoginUrl(string? returnUrl, string error)
        {
            var url = "/login?error=" + Uri.EscapeDataString(error);
            if (!string.IsNullOrEmpty(returnUrl))
                url += "&returnUrl=" + Uri.EscapeDataString(returnUrl);
            return url;
        }
    }
}
