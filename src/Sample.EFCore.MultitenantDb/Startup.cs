using Dotnettency;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Sample.EFCore
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAspNetCoreMultiTenancy<Tenant>((multiTenancyOptions) =>
            {
                multiTenancyOptions
                    .InitialiseTenant<TenantShellFactory>();
            });

            services.AddDbContext<SampleMultitenantDbContext>((options) =>
            {
                var path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
                var dbFilePath = System.IO.Path.Combine(path, "DotnettencyMultitenantDb.db");
                options.UseSqlite($"Filename={dbFilePath};");
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
               // options.UsePerTenantContainers();
            });

            app.Use(async (context, next) =>
            {
                // Do your usual DB creation / migreation if necessary to ensure DB exists and is up to date.
                // Ensure db is created..
                using (var scope = context.RequestServices.CreateScope())
                {
                    var scopedDbContext = scope.ServiceProvider.GetRequiredService<SampleMultitenantDbContext>();
                    await scopedDbContext.Database.EnsureCreatedAsync();
                }

                await next?.Invoke();

                // Do logging or other work that doesn't write to the Response.
            });

            app.Map("/AddBlog", (b) =>
            {
                b.Run(async (context) =>
                {                             

                    var tenantAwaredDbContext = context.RequestServices.GetRequiredService<SampleMultitenantDbContext>();

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
                    var tenantAwaredDbContext = context.RequestServices.GetRequiredService<SampleMultitenantDbContext>();
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
