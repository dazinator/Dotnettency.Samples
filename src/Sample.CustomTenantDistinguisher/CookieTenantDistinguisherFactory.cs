using Dotnettency;
using System;
using Microsoft.AspNetCore.Http;
using Dotnettency.AspNetCore;

namespace Sample.CustomTenantDistinguisher
{
    public class CookieTenantDistinguisherFactory : HttpContextTenantDistinguisherFactory<Tenant>
    {
        public CookieTenantDistinguisherFactory(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
        }

        protected override TenantDistinguisher GetTenantDistinguisher(HttpContext context)
        {

            var uri = context.Request.GetUri();

            var cookie = context.Request.Cookies["tenant"];
            if (!string.IsNullOrWhiteSpace(cookie))
            {
                switch (cookie)
                {
                    case "Moogle":
                        return new TenantDistinguisher(new Uri("tenant://Moogle"));

                    case "Gicrosoft":
                        return new TenantDistinguisher(new Uri("tenant://Gicrosoft"));
                }
            }

            return uri;
        }
    }
}
