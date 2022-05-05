using System.Data;
using idunno.Authentication.Basic;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
#if enableSecurity
using Microsoft.AspNetCore.Authentication.JwtBearer;
#endif
#if configserver
using Steeltoe.Extensions.Configuration.ConfigServer;
#endif
using MyProjectGroup.Common;
using MyProjectGroup.Common.Configuration;
using MyProjectGroup.Common.Messaging;
using MyProjectGroup.Common.Modules;
using MyProjectGroup.Common.Persistence;
using MyProjectGroup.Common.Security;
using MyProjectGroup.Common.Swagger;
using MyProjectGroup.DotnetAccelerator;
using MyProjectGroup.DotnetAccelerator.Persistence;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connector;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Tracing;
using DbType = MyProjectGroup.Common.Persistence.DbType;

var builder = WebApplication.CreateBuilder(args);
builder.UseYamlWithProfilesAppConfiguration<Program>(args);
BootstrapLoggerFactory.Update(builder.Configuration);
builder.Logging.AddDynamicConsole();
var configuration = builder.Configuration;
var services = builder.Services;

// add all steeltoe actuators, but make them only respond on a management port
var managementPort = builder.Configuration.GetValue<uint>("Management:Port");
builder.WebHost.AddAllActuators(c =>
{
    if (managementPort > 0)
    {
        c.RequireHost($"*:{managementPort}");
    }
});
if (builder.Environment.IsDevelopment())
{
    // remove zipkin trace ids from logs when running in local development
    services.AddSingleton<IDynamicMessageProcessor, NullLogProcessor>();
}
services.AddDistributedTracingAspNetCore();
// services.AddAllActuators();
// services.AddSingleton<IStartupFilter>(new AllActuatorsStartupFilter(c => c.RequireHost($"*:{managementPort}")));
// register with Spring Boot Admin if integration is enabled. Spring boot admin will scrape this apps actuators and display in GUI
// spring boot admin can be used instead of TAP LiveView when running locally
if (configuration.GetValue<string>("Spring:Boot:Admin:Client:Url") != null)
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
        configuration.GetSection($"Authentication:{JwtBearerDefaults.AuthenticationScheme}").Bind(cfg);
    });
services.AddAuthorization(authz =>
{
    authz.AddPolicy(KnownAuthorizationPolicy.AirportRead, policy => policy.RequireScope(KnownScope.Read));
    authz.AddPolicy(KnownAuthorizationPolicy.WeatherRead, policy => policy.RequireScope(KnownScope.Read));
    authz.AddPolicy(KnownAuthorizationPolicy.WeatherWrite, policy => policy.RequireScope(KnownScope.Write));
});
#endif
services.AddMediatR(cfg => cfg.Using<MessageBus>(), typeof(Program));
services.AddTransient(svc => (IMessageBus) svc.GetRequiredService<IMediator>());
services.AddModules();
services.AddDbContext<DotnetAcceleratorContext>(opt =>
{
    var connectionString = configuration.GetConnectionString("database");
    var dbDriver = configuration.GetValue<DbType>("DbType");
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
    c.UseOperationIdsConventions();
    c.SwaggerDoc("v1", new OpenApiInfo {Title = "MyProjectGroup.DotnetAccelerator", Version = "v1"});
});

var app = builder.Build();
BootstrapLoggerFactory.Update(app.Services.GetRequiredService<ILoggerFactory>());
if (app.Environment.IsDevelopment())
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
app.Run();