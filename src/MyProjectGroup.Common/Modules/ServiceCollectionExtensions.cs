using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace MyProjectGroup.Common.Modules
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds services found inside selected namespace to the service container.
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="modulesNamespace">Namespace to scan</param>
        /// <remarks>Services are located as any class ending with word "Service". It is registered into container for every interface it implements</remarks>
        public static IServiceCollection AddModules(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            var serviceTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes().Where(x => x.IsClass && !x.IsAbstract && x.IsAssignableTo(typeof(IService))))
                .ToList();
            foreach (var serviceType in serviceTypes)
            {
                var interfaces = serviceType.GetInterfaces();
                services.AddScoped(serviceType);
                foreach(var interfaceType in interfaces)
                {
                    services.Add(new ServiceDescriptor(interfaceType, serviceType, lifetime));
                }
            }

            return services;
        }
    }
}