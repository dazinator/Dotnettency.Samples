using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Dotnettency;
using Microsoft.Extensions.Configuration;
using System;
using Microsoft.AspNetCore.Mvc.Infrastructure;

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
            services.AddRouting();

            _loggerFactory.AddConsole();
            var logger = _loggerFactory.CreateLogger<Startup>();

            var serviceProvider = services.AddAspNetCoreMultiTenancy<Tenant>((options) =>
            {
                options                  
                    .InitialiseTenant<TenantShellFactory>() // factory class to load tenant when it needs to be initialised for the first time. Can use overload to provide a delegate instead.                    
                    .ConfigureTenantContainers((containerBuilder) =>
                    {
                        containerBuilder.Events((events) =>
                        {
                            // callback invoked after tenant container is created.
                            events.OnTenantContainerCreated(async (tenantResolver, tenantServiceProvider) =>
                            {
                                var tenant = await tenantResolver;

                            })
                            // callback invoked after a nested container is created for a tenant. i.e typically during a request.
                            .OnNestedTenantContainerCreated(async (tenantResolver, tenantServiceProvider) =>
                            {
                                var tenant = await tenantResolver;

                            });
                        })
                        // Extension methods available here for supported containers. We are using structuremap..
                        // We are using an overload that allows us to configure structuremap with familiar IServiceCollection.
                        .WithStructureMap((tenant, tenantServices) =>
                        {
                            tenantServices.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
                            tenantServices.AddMvc();

                        })
                        .AddPerRequestContainerMiddlewareServices()
                        .AddPerTenantMiddlewarePipelineServices(); // allows tenants to have there own middleware pipeline accessor stored in their tenant containers.
                                                                   // .WithModuleContainers(); // Creates a child container per IModule.
                    })
                    .ConfigureTenantMiddleware((a) =>
                    {
                        
                        a.OnInitialiseTenantPipeline((b, c) =>
                        {
                            var log = c.ApplicationServices.GetRequiredService<ILogger<Startup>>();
                            logger.LogDebug("Configuring tenant middleware pipeline for tenant: " + b.Tenant?.Name ?? "");


                            //if (env.IsDevelopment())
                            //{
                            //    appBuilder.UseBrowserLink();
                            //    appBuilder.UseDeveloperExceptionPage();
                            //}
                            //else
                            //{
                            //    appBuilder.UseExceptionHandler("/Error");
                            //}

                            c.UseStaticFiles();

                            // appBuilder.UseStaticFiles(); // This demonstrates static files middleware, but below I am also using per tenant hosting environment which means each tenant can see its own static files in addition to the main application level static files.

                            //  appBuilder.UseModules<Tenant, ModuleBase>();

                            // welcome page only enabled for tenant FOO.
                            if (b.Tenant?.Name == "Foo")
                            {
                                c.UseWelcomePage("/welcome");
                            }

                            c.UseMvc();
                            // display info.

                        });
                    });
                // configure per tenant hosting environment.
                //.ConfigurePerTenantHostingEnvironment(_environment, (tenantHostingEnvironmentOptions) =>
                //{
                //    tenantHostingEnvironmentOptions.OnInitialiseTenantContentRoot((contentRootOptions) =>
                //    {
                //        // WE use a tenant's guid id to partition one tenants files from another on disk.
                //        // NOTE: We use an empty guid for NULL tenants, so that all NULL tenants share the same location.
                //        var tenantGuid = (contentRootOptions.Tenant?.TenantGuid).GetValueOrDefault();
                //        contentRootOptions.TenantPartitionId(tenantGuid)
                //                           .AllowAccessTo(_environment.ContentRootFileProvider); // We allow the tenant content root file provider to access to the environments content root.
                //    });

                //    tenantHostingEnvironmentOptions.OnInitialiseTenantWebRoot((webRootOptions) =>
                //    {
                //        // WE use the tenant's guid id to partition one tenants files from another on disk.
                //        var tenantGuid = (webRootOptions.Tenant?.TenantGuid).GetValueOrDefault();
                //        webRootOptions.TenantPartitionId(tenantGuid)
                //                           .AllowAccessTo(_environment.WebRootFileProvider); // We allow the tenant web root file provider to access the environments web root files.
                //    });
                //});
            });

            // When using tenant containers, must return IServiceProvider.
            return serviceProvider;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {

            app.UseMultitenancy<Tenant>((options) =>
            {
                options.UsePerTenantContainers();
                options.UsePerTenantMiddlewarePipeline();
            });


            //app.UseRouter(((routeBuilder) =>
            //{
            //    // Makes sure that should any child route match, then the tenant container is restored prior to that route handling the request.
            //    routeBuilder.EnsureTenantContainer<Tenant>((childRouteBuilder) =>
            //    {
            //        // Adds a route that will handle the request via the current tenants middleware pipleine. 
            //        childRouteBuilder.MapTenantMiddlewarePipeline<Tenant>((context, appBuilder) =>
            //        {

            //            var logger = appBuilder.ApplicationServices.GetRequiredService<ILogger<Startup>>();
            //            logger.LogDebug("Configuring tenant middleware pipeline for tenant: " + context.Tenant?.Name ?? "");


            //            if (env.IsDevelopment())
            //            {
            //                appBuilder.UseBrowserLink();
            //                appBuilder.UseDeveloperExceptionPage();
            //            }
            //            else
            //            {
            //                appBuilder.UseExceptionHandler("/Error");
            //            }

            //            appBuilder.UseStaticFiles();

            //            // appBuilder.UseStaticFiles(); // This demonstrates static files middleware, but below I am also using per tenant hosting environment which means each tenant can see its own static files in addition to the main application level static files.

            //            //  appBuilder.UseModules<Tenant, ModuleBase>();

            //            // welcome page only enabled for tenant FOO.
            //            if (context.Tenant?.Name == "Foo")
            //            {
            //                appBuilder.UseWelcomePage("/welcome");
            //            }

            //            appBuilder.UseMvc();
            //            // display info.
            //            // appBuilder.Run(DisplayInfo);

            //        }); // handled by the tenant's middleware pipeline - if there is one.                  
            //    });
            //}));


        }
    }
}
