using System;
using System.Data;
using idunno.Authentication.Basic;
using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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
                services.AddSingleton<IDynamicMessageProcessor, NullLogProcessor>();
            }
            services.AddDistributedTracingAspNetCore();
            
            services.AddSecureActuators();
#if configserver
            services.AddConfigServerHealthContributor();
#endif
            if (Configuration.GetValue<string>("Spring:Boot:Admin:Client:Url") != null)
            {
                services.AddSpringBootAdmin();
            }
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
                        opt.UseSqlite(connectionString);
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
            services.AddSwaggerGen(c => { c.SwaggerDoc("v1", new OpenApiInfo {Title = "MyProjectGroup.DotnetAccelerator", Version = "v1"}); });
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
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapAllActuators().RequireAuthorization(KnownAuthorizationPolicy.Actuators);
            });
        }
    }
}