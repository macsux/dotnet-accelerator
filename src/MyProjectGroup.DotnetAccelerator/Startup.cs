using System;
using System.Data;
using System.Linq;
using idunno.Authentication.Basic;
using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using MyProjectGroup.Common;
using MyProjectGroup.Common.Messaging;
using MyProjectGroup.Common.Modules;
using MyProjectGroup.Common.Persistence;
using MyProjectGroup.Common.Security;
using MyProjectGroup.DotnetAccelerator.Persistence;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connector;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Tracing;
using DbType = MyProjectGroup.Common.Persistence.DbType;
#if enableSecurity
using Microsoft.AspNetCore.Authentication.JwtBearer;
#endif
#if configserver
using Steeltoe.Extensions.Configuration.ConfigServer;
#endif

namespace MyProjectGroup.DotnetAccelerator
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
            BootstrapLoggerFactory.Update(configuration);
            Logger = BootstrapLoggerFactory.Instance.CreateLogger<Startup>();
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Environment { get; }
        public ILogger Logger { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            if (Environment.IsDevelopment())
            {
                // remove zipkin trace ids from logs when running in local development
                services.AddSingleton<IDynamicMessageProcessor, NullLogProcessor>();
            }
            services.AddDistributedTracingAspNetCore();
            // add all steeltoe actuators, but make them only respond on a management port
            services.AddAllActuators();
            var managementPort = Configuration.GetValue<uint>("Management:Port");
            services.AddSingleton<IStartupFilter>(new AllActuatorsStartupFilter(c => c.RequireHost($"*:{managementPort}")));
            // register with Spring Boot Admin if integration is enabled. Spring boot admin will scrape this apps actuators and display in GUI
            // spring boot admin can be used instead of TAP LiveView when running locally
            if (Configuration.GetValue<string>("Spring:Boot:Admin:Client:Url") != null)
            {
                services.AddSpringBootAdmin();
            }
            
#if configserver
            services.AddConfigServerHealthContributor();
#endif

#if enableSecurity
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(cfg =>
                {
                    cfg.ForwardDefaultSelector = httpContext => (httpContext.Request.Path.StartsWithSegments("/actuator")? BasicAuthenticationDefaults.AuthenticationScheme : null)!;
                    Configuration.GetSection($"Authentication:{JwtBearerDefaults.AuthenticationScheme}").Bind(cfg);
                });
            services.AddAuthorization(authz =>
            {
                authz.AddPolicy(KnownAuthorizationPolicy.AirportRead, policy => policy.RequireScope(KnownScope.Read));
                authz.AddPolicy(KnownAuthorizationPolicy.WeatherRead, policy => policy.RequireScope(KnownScope.Read));
                authz.AddPolicy(KnownAuthorizationPolicy.WeatherWrite, policy => policy.RequireScope(KnownScope.Write));
            });
#endif
            services.AddMediatR(cfg => cfg.Using<MessageBus>(), typeof(Startup));
            services.AddTransient(svc => (IMessageBus) svc.GetRequiredService<IMediator>());
            services.AddModules();
            services.AddDbContext<DotnetAcceleratorContext>(opt =>
            {
                var connectionString = Configuration.GetConnectionString("database");
                var dbDriver = Configuration.GetValue<DbType>("DbType");
                switch (dbDriver)
                {
                    case DbType.SQLite:
                        if (connectionString.Contains(":memory") || connectionString.Contains("mode=memory"))
                        {
                            // in memory database needs to have its connection permanently open or it will get auto-deleted
                            var keepAliveConnection = new SqliteConnection(connectionString);
                            keepAliveConnection.Open();
                            opt.UseSqlite(keepAliveConnection);
                        }
                        else
                        {
                            opt.UseSqlite(connectionString);
                        }

                        break;
#if postgresql
                    case DbType.PostgreSQL:
                        opt.UseNpgsql(connectionString);
                        break;
#endif
#if mysql
                    case DbType.MySQL:
                        opt.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
                        break;
#endif
                }
            });
            services.AddScoped<IDbConnection>(ctx => ctx.GetRequiredService<DotnetAcceleratorContext>().Database.GetDbConnection());
            services.AddScoped<IHealthContributor, RelationalDbHealthContributor>(); // allow db connection health to show up in actuator health endpoint
            services.AddControllers(cfg => cfg.Filters.Add<DomainExceptionFilter>()); // respond with HTTP400 if domain exception is thrown
            services.AddSwaggerGen(c =>
            {
                c.CustomOperationIds(api =>
                {
                    var actionDescriptor = (ControllerActionDescriptor) api.ActionDescriptor;
                    HttpMethodAttribute? methodAttribute = api.HttpMethod switch
                    {
                        "GET" => actionDescriptor.EndpointMetadata.OfType<HttpGetAttribute>().FirstOrDefault(),
                        "POST" =>  actionDescriptor.EndpointMetadata.OfType<HttpPostAttribute>().FirstOrDefault(),
                        "PUT" =>  actionDescriptor.EndpointMetadata.OfType<HttpPutAttribute>().FirstOrDefault(),
                        "DELETE" =>  actionDescriptor.EndpointMetadata.OfType<HttpDeleteAttribute>().FirstOrDefault(),
                        "PATCH" =>  actionDescriptor.EndpointMetadata.OfType<HttpPatchAttribute>().FirstOrDefault(),
                        "OPTIONS" =>  actionDescriptor.EndpointMetadata.OfType<HttpOptionsAttribute>().FirstOrDefault(),
                        "HEAD" =>  actionDescriptor.EndpointMetadata.OfType<HttpHeadAttribute>().FirstOrDefault(),
                        _ => null!
                    };
                    if (methodAttribute?.Name is not null)
                    {
                        return methodAttribute.Name;
                    }

                    return $"{actionDescriptor.ControllerName}_{actionDescriptor.ActionName}"; 

                    // return $"{((ControllerActionDescriptor) api.ActionDescriptor).ControllerName}_{api.HttpMethod}_{string.Join("_", api.ParameterDescriptions.Select(x => x.Name))}".ToLower();
                });
                c.SwaggerDoc("v1", new OpenApiInfo {Title = "MyProjectGroup.DotnetAccelerator", Version = "v1"});
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            BootstrapLoggerFactory.Update(loggerFactory);
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.MigrateDatabase<DotnetAcceleratorContext>();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "MyProjectGroup.DotnetAccelerator v1");
            });
            app.UseRouting();
            // app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}