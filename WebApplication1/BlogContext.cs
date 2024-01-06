using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1
{
    public class BlogContext : DbContext
    {
        public const string DEFAULT_SCHEMA = "blogs";

        public DbSet<Blog> Blogs => Set<Blog>();
        public DbSet<BlogAudit> BlogAudits => Set<BlogAudit>();

        public BlogContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}