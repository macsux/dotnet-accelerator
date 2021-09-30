using System.Threading;
using DotnetAccelerator.Configuration;
using DotnetAccelerator.Messaging;
using DotnetAccelerator.Modules;
using DotnetAccelerator.Persistence;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

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
            services.AddMediatR(cfg => cfg.Using<MessageBus>(), typeof(Startup));
            services.AddTransient(svc => (IMessageBus) svc.GetRequiredService<IMediator>());
            services.AddModules("DotnetAccelerator.Modules");
            services.AddDbContext<DotnetAcceleratorContext>(opt =>
            {
                var connectionString = Configuration.GetConnectionString("database");
                var dbDriver = Configuration.GetValue<DbType>("DbType");
                switch(dbDriver)
                {
                    case DbType.SQLite:
                        opt.UseSqlite(connectionString);
                        break;
                    case DbType.PostgreSQL:
                        opt.UseNpgsql(connectionString);
                        break;
                };
            });
            services.AddControllers(cfg => cfg.Filters.Add<DomainExceptionFilter>());
            services.AddSwaggerGen(c => { c.SwaggerDoc("v1", new OpenApiInfo {Title = "DotnetAccelerator", Version = "v1"}); });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, DotnetAcceleratorContext context)
        {
            context.Database.EnsureCreated();
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "DotnetAccelerator v1"));

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}