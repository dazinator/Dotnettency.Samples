using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Dotnettency;
using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Razor;

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
                           
                        })
                        // Extension methods available here for supported containers. We are using structuremap..
                        // We are using an overload that allows us to configure structuremap with familiar IServiceCollection.
                        .WithAutofac((tenant, tenantServices) =>
                        {
                            foreach (var item in services)
                            {
                                tenantServices.Add(item);
                            }
                           // tenantServices.AddSingleton(_environment);
                            // This runs to configure each tenant's container, when tenant is browsed for first time.
                            tenantServices.AddMvc();

                            // Tried to fix razor compilation issues here but to no avail..
                            //tenantServices.Configure((RazorViewEngineOptions razorOptions) =>
                            //{
                            //    var previous = razorOptions.CompilationCallback;
                            //    razorOptions.CompilationCallback = (context) =>
                            //    {
                            //        previous?.Invoke(context);

                            //       // var assembly = typeof(Startup).GetTypeInfo().Assembly;
                            //       // var assemblies = assembly.GetReferencedAssemblies().Select(x => MetadataReference.CreateFromFile(Assembly.Load(x).Location))
                            //       // .ToList();
                            //       // assemblies.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("mscorlib")).Location));
                            //       // assemblies.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("System.Private.Corelib")).Location));
                            //       // assemblies.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("Microsoft.AspNetCore.Razor")).Location));
                            //       // assemblies.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("netstandard")).Location));
                                   
                            //       // context.Compilation = context.Compilation.AddReferences(assemblies);
                            //    };
                            //});
                        })
                        .AddPerRequestContainerMiddlewareServices() // services necessary for middleware that initialises tenant containers, and sets HttpContext.RequestServices.
                        .AddPerTenantMiddlewarePipelineServices(); // services necessary for middleware the initialises, and executes tenants middleware pipeline.
                    })
                    .ConfigureTenantMiddleware((a) =>
                    {
                        a.OnInitialiseTenantPipeline((b, c) =>
                        {
                            var log = c.ApplicationServices.GetRequiredService<ILogger<Startup>>();
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

            var listener = new ApplicationMiddlewareDiagnosticListener();
            diagnosticListener.SubscribeWithAdapter(listener);

          //  app.UseMvc();

            app.UseMultitenancy<Tenant>((options) =>
            {
                options.UsePerTenantContainers(); // Middleware that initialises tenant container, and sets HttpContext.RequestServices appropriately.
                options.UsePerTenantMiddlewarePipeline(); // Middleware that initialises and executes appropriate tenant middleware pipeline for current tenant.
            });
        }
    }
}
