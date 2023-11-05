# Infoware.EntityFrameworkCore.AuditEntity
 Create an individual AuditEntity for selected tables to register changes

### Get it!
[![NuGet Badge](https://buildstats.info/nuget/Infoware.EntityFrameworkCore.AuditEntity)](https://www.nuget.org/packages/Infoware.EntityFrameworkCore.AuditEntity/)

### How to use it
- Create a new entity inherits BaseAudit<BaseTable, TPrimaryKeyType>, you cannot use this package if your tables have composite primary key

```csharp
	public class BlogAudit : BaseAudit<Blog, long>
    {
    }

```

- Or if you need inject additional fields to Audit table, create a new BaseAudit

```csharp
    public class ExtendedBaseAudit<TEntity, TPrimaryKeyType> : BaseAudit<TEntity, TPrimaryKeyType> where TEntity : IAuditable
    {
        [Required]
        public string UserId { get; set; } = null!;

        [Required]
        public string UserName { get; set; } = null!;
    }

    public class ExtendedBaseAudit : BaseAudit
    {
        public string UserId { get; set; } = null!;

        public string UserName { get; set; } = null!;
    }


	public class BlogAudit : ExtendedBaseAudit<Blog, long>
    {
    }

```

- Insert Audit table to the DBContext
```csharp
    public class MyContext : DbContext
    {
        public DbSet<Blog> Blogs => Set<Blog>();
        public DbSet<BlogAudit> BlogAudits => Set<BlogAudit>();

```

- Now you must override SaveChanges method or create new one to save changes without forget save audits. If you create a new BaseAudit you must inject additional fields in this process

```csharp
    public class MyContext : DbContext
    {
        private readonly ISessionService _sessionService;
        private readonly ILogJsonSerializer _logJsonSerializer;

        public MyContext(DbContextOptions options, ILogJsonSerializer logJsonSerialize, ISessionService sessionService) : base(options)
        {
            _sessionService = sessionService;
            _logJsonSerializer = logJsonSerializer;
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var data = await _sessionService.GetSessionDataAsync();
            return await this.SaveWithAuditsAsync(_logJsonSerializer,
                FactoryBase, 
                (cancellationToken) => base.SaveChangesAsync(cancellationToken),
                cancellationToken);
        }

        internal static Task<IBaseAudit?> FactoryBase(Type source)
        {
            var result = (IBaseAudit?)Activator.CreateInstance(source);
            return Task.FromResult(result);
        }


```

- If you extended a base audit with additional fields, you must filled manually

```csharp
        internal static async Task<IBaseAudit?> FactoryBase(Type source)
        {
            var data = await _sessionService.GetSessionDataAsync();

            var result = (ExtendedBaseAudit?)Activator.CreateInstance(source);
            result!.UserId = data.UserId;
            result!.UserName = data.UserName;
            return result;
        }
```

- Finally you must inject dependencies

```csharp
        services.AddAnotherService();
        ....
        // inject your session service or another method to get additional information for audit tables
        services.AddTransient<ISessionService, YourSessionService>();

        services.AddSingleton<ILogJsonSerializer, LogJsonSerializer>();

```
## Buy me a coofee
If you want, buy me a coofee :coffee: https://www.paypal.com/paypalme/vicosanzdev?locale.x=es_XC

