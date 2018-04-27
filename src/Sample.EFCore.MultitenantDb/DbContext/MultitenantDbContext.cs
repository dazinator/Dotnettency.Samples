using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.EFCore
{

    public abstract class MultitenantDbContext<TDbContext, TTenant, TIdType> : DbContext
        where TDbContext : DbContext
        where TTenant : class
    {
        private readonly Task<TTenant> _tenant;
        private Lazy<TIdType> _tenantId;

        private TIdType TenantId
        {
            get
            {
                return _tenantId.Value;
            }
        }

        private static List<Action<DbContext>> _setTenantIdOnSaveCallbacks = new List<Action<DbContext>>();

        public MultitenantDbContext(DbContextOptions<TDbContext> options, Task<TTenant> tenant) : base(options)
        {
            _tenant = tenant;
            _tenantId = new Lazy<TIdType>(() =>
            {
                var t = _tenant.Result;
                return GetTenantId(t);
            });
        }

        protected virtual TIdType GetTenantId(TTenant tenant)
        {
            return default(TIdType);
        }

        protected void HasTenantIdFilter<T>(ModelBuilder modelBuilder, string tenantIdPropertyName, Func<T, TIdType> getTenantId)
          where T : class
        {
            modelBuilder.Entity<T>().Property<TIdType>(tenantIdPropertyName);
            modelBuilder.Entity<T>().HasQueryFilter(b => getTenantId(b).Equals(TenantId));

            Action<DbContext> action = (db) =>
            {
                SetTenantIdProperty<T>(tenantIdPropertyName, TenantId, db);
            };
            _setTenantIdOnSaveCallbacks.Add(action);

        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            SetTenantIdOnSave();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override int SaveChanges()
        {
            SetTenantIdOnSave();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            SetTenantIdOnSave();
            return await base.SaveChangesAsync(cancellationToken);
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken))
        {
            SetTenantIdOnSave();
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private static void SetTenantIdProperty<TEntity>(string propertyName, TIdType id, DbContext db)
            where TEntity : class
        {
            foreach (var item in db.ChangeTracker.Entries<TEntity>())
            {
                if (item.State == EntityState.Added)
                {
                    item.Property(propertyName).CurrentValue = id;
                }
            }
        }

        private void SetTenantIdOnSave()
        {
            ChangeTracker.DetectChanges();
            foreach (var item in _setTenantIdOnSaveCallbacks)
            {
                item(this);
            }
        }
    }

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
