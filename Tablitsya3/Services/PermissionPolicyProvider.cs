using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Tablitsya3.Models;

namespace Tablitsya3.Services
{
    /// <summary>
    /// Вимагає, щоб у токена/cookie був claim `perm` зі значенням <see cref="RequiredKey"/>,
    /// або щоб роль користувача була "Admin" (powerful fallback).
    /// </summary>
    public sealed class PermissionRequirement : IAuthorizationRequirement
    {
        public string RequiredKey { get; }
        public PermissionRequirement(string requiredKey) { RequiredKey = requiredKey; }
    }

    public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            var user = context.User;
            if (user?.Identity?.IsAuthenticated != true) return Task.CompletedTask;

            // Adminу — все, навіть якщо матриця ще не зашита
            if (user.IsInRole("Admin"))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            var hasPerm = user.Claims.Any(c =>
                string.Equals(c.Type, Permissions.ClaimType, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(c.Value, requirement.RequiredKey, StringComparison.OrdinalIgnoreCase));

            if (hasPerm) context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Динамічно створює AuthorizationPolicy з ім'ям "perm:<key>".
    /// </summary>
    public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
    {
        private readonly DefaultAuthorizationPolicyProvider _fallback;

        public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        {
            _fallback = new DefaultAuthorizationPolicyProvider(options);
        }

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();
        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            if (!string.IsNullOrEmpty(policyName) &&
                policyName.StartsWith(Permissions.PolicyPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var key = policyName.Substring(Permissions.PolicyPrefix.Length);
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddRequirements(new PermissionRequirement(key))
                    .Build();
                return Task.FromResult<AuthorizationPolicy?>(policy);
            }

            return _fallback.GetPolicyAsync(policyName);
        }
    }
}
