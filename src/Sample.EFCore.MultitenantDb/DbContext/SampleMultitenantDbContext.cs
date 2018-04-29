using Dotnettency.EFCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Sample.EFCore
{
    public class SampleMultitenantDbContext : MultitenantDbContext<SampleMultitenantDbContext, Tenant, Guid>
    {

        private const string TenantIdPropertyName = "TenantId";

        public SampleMultitenantDbContext(DbContextOptions<SampleMultitenantDbContext> options, Task<Tenant> tenant) : base(options, tenant)
        {
        }

        public DbSet<Blog> Blogs { get; set; }

        protected override Guid GetTenantId(Tenant tenant)
        {
            return tenant.TenantGuid;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            HasTenantIdFilter<Blog>(modelBuilder, TenantIdPropertyName, (b) => EF.Property<Guid>(b, TenantIdPropertyName));
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
