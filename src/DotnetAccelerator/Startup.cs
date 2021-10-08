using DotnetAccelerator.Messaging;
using DotnetAccelerator.Modules;
using DotnetAccelerator.Persistence;
using DotnetAccelerator.Security;
#if enableSecurity
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using idunno.Authentication.Basic;
#endif
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
#if configserver
using Steeltoe.Extensions.Configuration.ConfigServer;
#endif
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Tracing;

namespace DotnetAccelerator
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Environment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
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
            services.AddModules("DotnetAccelerator.Modules");
            services.AddDbContext<DotnetAcceleratorContext>(opt =>
            {
                var connectionString = Configuration.GetConnectionString("database");
                var dbDriver = Configuration.GetValue<DbType>("DbType");
                _ = dbDriver switch
                {
                    DbType.SQLite => opt.UseSqlite(connectionString),
#if postgresql
                    DbType.PostgreSQL => opt.UseNpgsql(connectionString),
#endif
#if mysql
                    DbType.MySQL => opt.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)),
#endif
                    _ => opt
                };
            });
            services.AddControllers(cfg => cfg.Filters.Add<DomainExceptionFilter>());
            services.AddSwaggerGen(c => { c.SwaggerDoc("v1", new OpenApiInfo {Title = "DotnetAccelerator", Version = "v1"}); });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.MigrateDatabase<DotnetAcceleratorContext>();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "DotnetAccelerator v1");
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