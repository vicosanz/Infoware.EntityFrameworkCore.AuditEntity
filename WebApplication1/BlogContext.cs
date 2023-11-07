using Infoware.EntityFrameworkCore.AuditEntity;
using Infoware.EntityFrameworkCore.AuditEntity.Extensions;
using Infoware.SensitiveDataLogger.JsonSerializer;
using Infoware.SRI.DocumentosElectronicos.Context.Models;
using Microsoft.EntityFrameworkCore;
using Test.Models;

namespace Test
{
    public class BlogContext : DbContext
    {
        public const string DEFAULT_SCHEMA = "blogs";
        private readonly ILogJsonSerializer _logJsonSerializer;

        public DbSet<Blog> Blogs => Set<Blog>();
        public DbSet<BlogAudit> BlogAudits => Set<BlogAudit>();

        public BlogContext(DbContextOptions options, ILogJsonSerializer logJsonSerializer) : base(options)
        {
            _logJsonSerializer = logJsonSerializer;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        public override int SaveChanges()
        {
            throw new NotImplementedException();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await this.SaveWithAuditsAsync(_logJsonSerializer,
                (Type source) => {
                    var result = (IExtendedBaseAudit?) Activator.CreateInstance(source);
                    result!.UserId = "User1";
                    result!.UserName = "Username";
                    return Task.FromResult((IExtendedBaseAudit?)result);
                },  
                (cancellationToken) => base.SaveChangesAsync(cancellationToken),
                cancellationToken);
        }

    }
}