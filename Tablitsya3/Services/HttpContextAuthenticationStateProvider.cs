using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Tablitsya3.Services
{
    /// <summary>
    /// Постачальник стану автентифікації для Blazor Server,
    /// який зчитує користувача з HttpContext (cookie auth).
    /// </summary>
    public class HttpContextAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HttpContextAuthenticationStateProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var user = _httpContextAccessor.HttpContext?.User
                       ?? new ClaimsPrincipal(new ClaimsIdentity());
            return Task.FromResult(new AuthenticationState(user));
        }
    }
}
