using Microsoft.AspNetCore.Authorization;

namespace DotnetAccelerator.Security
{
    public static class AuthorizationPolicyBuilderExtensions
    {
        public static AuthorizationPolicyBuilder RequireScope(this AuthorizationPolicyBuilder builder, string scope) => builder.RequireClaim("scope", scope);
    }
}