using Dotnettency;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;

namespace Sample.RazorPages
{

    public class Startup
    {
        private readonly IHostingEnvironment _environment;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IConfiguration _configuration;

        public Startup(IHostingEnvironment environment, ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            _environment = environment;
            _loggerFactory = loggerFactory;
            _configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {

            // services.AddRouting();
            services.AddMiddlewareAnalysis();
            //  services.AddMvc();
            services.AddWebEncoders(); // Not sure why this is necessary. See https://github.com/aspnet/Mvc/issues/8340 may not be necessary in 2.1.0

            _loggerFactory.AddConsole();
            ILogger<Startup> logger = _loggerFactory.CreateLogger<Startup>();
           

            IServiceProvider serviceProvider = services.AddAspNetCoreMultiTenancy<Tenant>((options) =>
            {
                options
                    .InitialiseTenant<TenantShellFactory>() // factory class to load tenant when it needs to be initialised for the first time. Can use overload to provide a delegate instead.                    
                    .ConfigureTenantContainers((containerBuilder) =>
                    {
                        containerBuilder.Events((events) =>
                        {

                        })
                        // Extension methods available here for supported containers. We are using structuremap..
                        // We are using an overload that allows us to configure structuremap with familiar IServiceCollection.
                        .WithAutofac((tenant, tenantServices) =>
                        {
                            //foreach (var item in services)
                            //{
                            //    tenantServices.Add(item);
                            //}
                            tenantServices.AddSingleton(_environment); // See https://github.com/aspnet/Mvc/issues/8340
                            tenantServices.AddWebEncoders();
                            // This runs to configure each tenant's container, when tenant is browsed for first time.
                            tenantServices.AddMvc();
                          
                        })
                        .AddPerRequestContainerMiddlewareServices() // services necessary for middleware that initialises tenant containers, and sets HttpContext.RequestServices.
                        .AddPerTenantMiddlewarePipelineServices(); // services necessary for middleware the initialises, and executes tenants middleware pipeline.
                    })
                    .ConfigureTenantMiddleware((a) =>
                    {
                        a.OnInitialiseTenantPipeline((b, c) =>
                        {
                            ILogger<Startup> log = c.ApplicationServices.GetRequiredService<ILogger<Startup>>();
                            //  logger.LogDebug("Configuring tenant middleware pipeline for tenant: " + b.Tenant?.Name ?? "");                                                   
                            c.UseWelcomePage("/welcome");
                            c.UseMvc();

                        });
                    });
            });

            // When using tenant containers, must return IServiceProvider.
            return serviceProvider;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, DiagnosticListener diagnosticListener, IHostingEnvironment env)
        {

            ApplicationMiddlewareDiagnosticListener listener = new ApplicationMiddlewareDiagnosticListener();
            diagnosticListener.SubscribeWithAdapter(listener);          

            app.UseMultitenancy<Tenant>((options) =>
            {
                options.UsePerTenantContainers(); // Middleware that initialises tenant container, and sets HttpContext.RequestServices appropriately.
                options.UsePerTenantMiddlewarePipeline(); // Middleware that initialises and executes appropriate tenant middleware pipeline for current tenant.
            });
        }
    }
}
