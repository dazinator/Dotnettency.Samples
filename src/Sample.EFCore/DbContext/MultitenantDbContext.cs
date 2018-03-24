using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.EFCore
{

    public class MultitenantDbContext : DbContext
    {

        private readonly Task<Tenant> _tenant;
        private const string TenantIdColumnName = "TenantID";

        public MultitenantDbContext(DbContextOptions<MultitenantDbContext> options, Task<Tenant> tenant) : base(options)
        {
            _tenant = tenant;
        }

        public DbSet<Blog> Blogs { get; set; }

        private Guid _tenantId
        {
            get
            {
                return _tenant.Result.TenantGuid;
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            ConfigureTenantFilter<Blog>(modelBuilder);
        }

        private void ConfigureTenantFilter<T>(ModelBuilder modelBuilder)
            where T : class
        {
            modelBuilder.Entity<T>().Property<Guid>(TenantIdColumnName);
            modelBuilder.Entity<T>().HasQueryFilter(b => EF.Property<Guid>(b, TenantIdColumnName) == _tenantId);
        }

        #region SaveChanges - SetTenantID on new entities

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            SetTenantId().Wait();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override int SaveChanges()
        {
            SetTenantId().Wait();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            await SetTenantId();
            return await base.SaveChangesAsync(cancellationToken);
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken))
        {
            await SetTenantId();
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private async Task SetTenantId()
        {
            ChangeTracker.DetectChanges();
            var tenant = await _tenant;
            foreach (var item in ChangeTracker.Entries())
            {
                if (item.State == EntityState.Added)
                {
                    item.Property(TenantIdColumnName).CurrentValue = tenant.TenantGuid;
                }
            }
        }

        #endregion

    }
}
