using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Test;

namespace WebApplication1
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCustomDbContext(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<BlogContext>(
                options =>
                {
                    options.UseSqlServer(configuration["ConnectionStrings:DocumentosElectronicosSRI"], sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(typeof(Program).GetTypeInfo().Assembly.GetName().Name);
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), null);
                    });
                },
                ServiceLifetime.Singleton  //Showing explicitly that the DbContext is shared across the HTTP request scope (graph of objects started in the HTTP request)
            );
            return services;
        }
    }
}
