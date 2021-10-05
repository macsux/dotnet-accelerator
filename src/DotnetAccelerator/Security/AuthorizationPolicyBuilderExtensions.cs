using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace DotnetAccelerator.Security
{
    public static class AuthorizationPolicyBuilderExtensions
    {
        public static AuthorizationPolicyBuilder RequireScope(this AuthorizationPolicyBuilder builder, string scope) => builder.RequireClaim("scope", scope);
        public static bool HasScope(this ClaimsPrincipal principal, string scope) => principal.HasClaim("scope", scope);
    }
}