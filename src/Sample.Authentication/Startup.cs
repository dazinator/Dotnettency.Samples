using Dotnettency;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace Sample.Authentication
{
    public class Startup
    {
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            IServiceProvider serviceProvider = services.AddAspNetCoreMultiTenancy<Tenant>((options) =>
            {
                options
                    .InitialiseTenant<TenantShellFactory>() // factory class to load tenant when it needs to be initialised for the first time. Can use overload to provide a delegate instead.                    
                    .ConfigureTenantContainers((containerBuilder) =>
                    {
                        containerBuilder.WithAutofac((tenant, tenantServices) =>
                        {
                            if (tenant.Name == "Moogle")
                            {
                                tenantServices.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                                .AddCookie((c) =>
                                {
                                    c.Cookie.Name = tenant.Name;
                                });
                            }
                        })
                        .AddPerRequestContainerMiddlewareServices() // services needed for per tenant container middleware.
                        .AddPerTenantMiddlewarePipelineServices(); // services needed for per tenant middleware pipeline.
                    })
                    .ConfigureTenantMiddleware((a) =>
                    {
                        a.OnInitialiseTenantPipeline((b, c) =>
                        {
                            c.UseDeveloperExceptionPage();
                            c.UseStaticFiles();

                            //  var log = c.ApplicationServices.GetRequiredService<ILogger<Startup>>();
                            if (b.Tenant.Name == "Moogle")
                            {
                                c.UseAuthentication();

                                // Browse to /Protected endpoint, will issue a challenge if not authenticated.
                                // This challenge automatically redirects to the default login path = /Account/Login
                                c.Map("/Protected", (d) =>
                                {
                                    d.Run(async (h) =>
                                    {
                                        if (!h.User.Identity?.IsAuthenticated ?? false)
                                        {
                                            await h.ChallengeAsync();
                                        }
                                        else
                                        {
                                            await h.Response.WriteAsync("Authenticated as: " + h.User.FindFirstValue(ClaimTypes.Name));
                                        }
                                    });
                                });

                                // Browse to /Account/Login will automatically create a sign in cookie then redirect to /Protected
                                c.Map("/Account/Login", (d) =>
                                {
                                    d.Run(async (h) =>
                                    {
                                        List<Claim> claims = new List<Claim>{
                                            new Claim(ClaimTypes.Name, "testuser"),
                                            new Claim("FullName", "test user"),
                                            new Claim(ClaimTypes.Role, "Administrator"),
                                        };

                                        ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                                        AuthenticationProperties authProperties = new AuthenticationProperties
                                        {
                                            RedirectUri = "/Protected"
                                        };

                                        await h.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                                            new ClaimsPrincipal(claimsIdentity), authProperties);
                                    });
                                });

                            }

                            // All tenants have welcome page middleware enabled.
                            c.UseWelcomePage();                          

                        });
                    });

            });

            // When using tenant containers, must return IServiceProvider.
            return serviceProvider;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app = app.UseMultitenancy<Tenant>((options) =>
            {
                options.UsePerTenantContainers();
                options.UsePerTenantMiddlewarePipeline();
            });
        }
    }
}
