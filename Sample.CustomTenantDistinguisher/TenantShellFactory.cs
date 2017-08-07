using System.Threading.Tasks;
using Dotnettency;
using System;

namespace Sample.CustomTenantDistinguisher
{
    public class TenantShellFactory : ITenantShellFactory<Tenant>
    {
        public Task<TenantShell<Tenant>> Get(TenantDistinguisher distinguisher)
        {
            // If cookie was present, then scheme will be our own custom one.         
            if (distinguisher.Uri.Scheme == "tenant")
            {
                var tenant = distinguisher.Uri.Host.ToLowerInvariant();
                switch (tenant)
                {
                    case "moogle":
                        return CreateMoogleTenant();
                    case "gicrosoft":
                        return CreateGicrosoftTenant();
                }

            }

            // Otherwise, just pick tenant based on port.
            if (distinguisher.Uri.Port == 5000 || distinguisher.Uri.Port == 5001)
            {
                return CreateMoogleTenant();
            }

            if (distinguisher.Uri.Port == 5002)
            {
                return CreateGicrosoftTenant();
            }


            throw new NotImplementedException("Please make request on ports 5000 - 5003 to see various behaviour.");

        }

        private Task<TenantShell<Tenant>> CreateGicrosoftTenant()
        {
            Guid tenantId = Guid.Parse("b17fcd22-0db1-47c0-9fef-1aa1cb09605e");
            var tenant = new Tenant(tenantId, "Gicrosoft");
            var result = new TenantShell<Tenant>(tenant);
            return Task.FromResult(result);
        }

        private Task<TenantShell<Tenant>> CreateMoogleTenant()
        {
            Guid tenantId = Guid.Parse("049c8cc4-3660-41c7-92f0-85430452be22");
            var tenant = new Tenant(tenantId, "Moogle");
            // Also adding any additional Uri's that should be mapped to this same tenant.
            var result = new TenantShell<Tenant>(tenant, new Uri("http://localhost:5000"),
                                                         new Uri("http://localhost:5001"));
            return Task.FromResult(result);
        }
    }
}
