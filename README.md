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

- Create an interceptor class inherit from BaseAuditInterceptor, if you extended a base audit with additional fields, you must filled manually

```csharp
    public class AuditInterceptor : BaseAuditInterceptor
    {
        public AuditInterceptor(ILogJsonSerializer logJsonSerializer): base(logJsonSerializer) { }

        /// Forget that if you do not extend base audit
        public override async Task<IBaseAudit?> InitAuditObject(Type entityAuditType)
        {
            var result = (IExtendedBaseAudit?)Activator.CreateInstance(entityAuditType);
            ///Mockup a delay simulating extract additional data from service
            await Task.Delay(1);
            result!.UserId = "User1";
            result!.UserName = "Username";
            return result;
        }
    }
```

- Inject interceptor into Services and attach it to context

```csharp
    builder.Services.AddAnotherService();
    ....
    // inject your session service or another method to get additional information for audit tables
    builder.Services.AddTransient<ISessionService, YourSessionService>();

    builder.Services.AddSingleton<ILogJsonSerializer, LogJsonSerializer>();

    builder.Services.AddSingleton<AuditInterceptor>();
    builder.Services.AddDbContext<BlogContext>(
        (serviceProvider, options) => 
        {
            options.UseSqlServer(configuration["ConnectionStrings:MyconnectionString"], sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(Program).GetTypeInfo().Assembly.GetName().Name);
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), null);
            });

            options.AddInterceptors(
                serviceProvider.GetRequiredService<AuditInterceptor>()
            );
        },
        ServiceLifetime.Singleton  //Showing explicitly that the DbContext is shared across the HTTP request scope (graph of objects started in the HTTP request)
    );
```

## Buy me a coofee
If you want, buy me a coofee :coffee: https://www.paypal.com/paypalme/vicosanzdev?locale.x=es_XC

