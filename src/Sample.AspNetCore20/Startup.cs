using Dotnettency;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Sample.AspNetCore20
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
           return services.AddMultiTenancy<Tenant>((multiTenancyOptions) =>
            {
                multiTenancyOptions
                    .InitialiseTenant<TenantShellFactory>()
                    .ConfigureTenantContainers((containerBuilder) =>
                    {
                        containerBuilder.WithStructureMap((tenant, tenantServices) =>
                        {
                            if (tenant.Name == "Moogle")
                            {
                                tenantServices.AddSingleton<GreetingService>(new GreetingService(true));
                            }
                            else
                            {
                                tenantServices.AddSingleton<GreetingService>(new GreetingService(false));
                            }
                        });
                    });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {        

            app.UseMultitenancy<Tenant>((options) =>
            {
                options.UsePerTenantContainers();
            });

            app.Run(async (context) =>
            {
                var greetingService = context.RequestServices.GetRequiredService<GreetingService>();
                await context.Response.WriteAsync(greetingService.Greet());
            });

        }
    }
}
