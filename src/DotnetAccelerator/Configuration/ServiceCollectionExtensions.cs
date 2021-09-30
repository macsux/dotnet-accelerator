using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetAccelerator.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddModules(this IServiceCollection services, string modulesNamespace)
        {
            var serviceTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes()
                    .Where(x => x.Namespace != null && x.Namespace.StartsWith(modulesNamespace) && x.Name.EndsWith("Service")))
                .ToList();
            foreach (var serviceType in serviceTypes)
            {
                var interfaces = serviceType.GetInterfaces();
                services.AddScoped(serviceType);
                foreach(var interfaceType in interfaces)
                {
                    services.AddScoped(interfaceType, serviceType);
                }
            }

            return services;
        }
    }
}