using System;

namespace Sample.AspNetCore20
{
    public class Tenant
    {
        public Tenant(Guid tenantGuid, string name)
        {
            TenantGuid = tenantGuid;
            Name = name;
        }

        public Guid TenantGuid { get; set; }
        public string Name { get; set; }
    }
}
