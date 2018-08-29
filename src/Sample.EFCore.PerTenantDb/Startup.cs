using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dotnettency;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sample.EFCore.PerTenantDb
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {           

            return services.AddAspNetCoreMultiTenancy<Tenant>((multiTenancyOptions) =>
            {
                multiTenancyOptions
                   .InitialiseTenant<TenantShellFactory>()
                   .ConfigureTenantContainers((containerBuilder) =>
                   {
                       containerBuilder.Events((events) =>
                       {
                           // callback invoked after tenant container is created.
                           events.OnTenantContainerCreated(async (tenantResolver, tenantServiceProvider) =>
                           {
                               // This is where we ensure the database is created / migrated on startup of each tenant.



                               var tenant = await tenantResolver;

                               using (var scope = tenantServiceProvider.CreateScope())
                               {
                                   var scopedDbContext = scope.ServiceProvider.GetRequiredService<PerTenantDbContext>();
                                   await scopedDbContext.Database.EnsureCreatedAsync();
                               }
                           });
                       });

                       // This is where we configure each tenants actual container. We have to use a supported container
                       // to do this (like structuremap) because the built in IServiceProvider doesn't really support configuring 
                       // nested / child containers for each tenant.
                       containerBuilder.WithStructureMap((tenant, tenantServices) =>
                       {                        
                          tenantServices.AddDbContext<PerTenantDbContext>((options) =>
                          {
                              var path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
                              var dbFilePath = System.IO.Path.Combine(path, tenant.Name + ".db"); // note we use the tenants name..
                               options.UseSqlite($"Filename={dbFilePath};");
                          });
                       })
                       .AddPerRequestContainerMiddlewareServices(); // Required.. Allows the dotnettency middleware to work. Think harder about this.

                   });
            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMultitenancy<Tenant>((options) =>
            {
                options.UsePerTenantContainers();
                // options.UsePerTenantMiddlewarePipeline();
            });



            app.Map("/AddBlog", (b) =>
            {
                b.Run(async (context) =>
                {

                    var tenantAwaredDbContext = context.RequestServices.GetRequiredService<PerTenantDbContext>();

                    // We will create a blog post entity.
                    // It will be automatically get a TenantID for the current tenant.
                    // We don't have to set TenantID, its automatic.

                    var blog = new Blog()
                    {
                        Rating = 1,
                        Url = $"http://" + Guid.NewGuid()
                    };

                    tenantAwaredDbContext.Add(blog);
                    tenantAwaredDbContext.SaveChanges();
                });
            });

            app.Map("/Blogs", (b) =>
            {
                b.Run(async (context) =>
                {
                    var tenantAwaredDbContext = context.RequestServices.GetRequiredService<PerTenantDbContext>();
                    var blogs = await tenantAwaredDbContext.Blogs.ToArrayAsync();
                    foreach (var item in blogs)
                    {
                        await context.Response.WriteAsync($"Blog Id: {item.BlogId}, Url: {item.Url}");
                    }
                });
            });


            app.Run(async (context) =>
            {

                var tenantTask = context.RequestServices.GetRequiredService<Task<Tenant>>();
                var tenant = await tenantTask;



                await context.Response.WriteAsync("Hello: " + tenant.Name);
            });
        }
    }
}
