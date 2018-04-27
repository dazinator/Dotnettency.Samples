using Microsoft.EntityFrameworkCore;

namespace Sample.EFCore
{
    public class IsolatedDbContext
    {
        public DbSet<Blog> Blogs { get; set; }
    }
}
