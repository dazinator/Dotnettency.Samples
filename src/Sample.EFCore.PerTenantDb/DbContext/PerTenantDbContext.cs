using Microsoft.EntityFrameworkCore;

namespace Sample.EFCore
{
    public class PerTenantDbContext : DbContext
    {

        public PerTenantDbContext(DbContextOptions<PerTenantDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<Blog> Blogs { get; set; }
    }


}
