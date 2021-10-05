using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetAccelerator.Modules
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds services found inside selected namespace to the service container.
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="modulesNamespace">Namespace to scan</param>
        /// <remarks>Services are located as any class ending with word "Service". It is registered into container for every interface it implements</remarks>
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