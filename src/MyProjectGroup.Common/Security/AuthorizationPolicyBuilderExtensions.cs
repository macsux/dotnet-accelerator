using Microsoft.AspNetCore.Authorization;

namespace MyProjectGroup.Common.Security;

public static class AuthorizationPolicyBuilderExtensions
{
    public static AuthorizationPolicyBuilder RequireScope(this AuthorizationPolicyBuilder policyBuilder, string scope) => policyBuilder.RequireClaim("scope", scope);
}