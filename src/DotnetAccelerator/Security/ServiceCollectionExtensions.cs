using System.Security.Claims;
using System.Threading.Tasks;
using idunno.Authentication.Basic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.SpringBootAdminClient;

namespace DotnetAccelerator.Security
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSpringBootAdmin(this IServiceCollection services)
        {
            services.AddSpringBootAdminClient();
            services.AddSingleton(provider =>
            {
                var actuatorSecurityOptions = provider.GetRequiredService<IOptions<ActuatorSecurityOptions>>().Value;
                var springBootAdminOptions = ActivatorUtilities.CreateInstance<SpringBootAdminClientOptions>(provider);
                springBootAdminOptions.Metadata ??= new();
                springBootAdminOptions.Metadata.TryAdd("user.name", actuatorSecurityOptions.UserName);
                springBootAdminOptions.Metadata.TryAdd("user.password", actuatorSecurityOptions.Password);
                return springBootAdminOptions;
            });
            return services;
        }
        public static IServiceCollection AddActuatorSecurity(this IServiceCollection services)
        {
            services.AddOptions<ActuatorSecurityOptions>()
                .Configure<IConfiguration>((options, config) =>
                {
                    var section = config.GetSection("Spring:Boot:Admin:Client:Metadata");
                    var username = section.GetValue<string>("user.name");
                    var password = section.GetValue<string>("user.name");
                    if (username != null)
                    {
                        options.UserName = username;
                    }

                    if (password != null)
                    {
                        options.Password = password;
                    }
                });
            services.AddAuthentication().AddBasic(BasicAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.ForwardDefaultSelector = httpContext =>
                    (httpContext.Request.Path.StartsWithSegments("/actuator") ? BasicAuthenticationDefaults.AuthenticationScheme : null)!;
                options.ForwardChallenge = BasicAuthenticationDefaults.AuthenticationScheme;

                options.Events = new BasicAuthenticationEvents
                {
                    OnValidateCredentials = context =>
                    {
                        var options = context.HttpContext.RequestServices.GetRequiredService<IOptionsSnapshot<ActuatorSecurityOptions>>().Value;
                        if (context.Username == options.UserName && context.Password == options.Password) 
                        {
                            context.Principal = new ClaimsPrincipal(new ClaimsIdentity(new[] {new Claim("scope", KnownScopes.Actuators)}));
                            context.Success();
                        }

                        return Task.CompletedTask;
                    }
                };
            });
            return services;
        }
    }
}