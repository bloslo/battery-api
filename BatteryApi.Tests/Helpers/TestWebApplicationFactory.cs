using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;

namespace BatteryApi.Tests.Helpers;

public class TestWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>, IAsyncLifetime where TProgram : class
{
    private readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder()
        .WithDatabase("battery_system")
        .WithUsername("postgres")
        .WithPassword("postsgres")
        .WithImage("postgres:16-alpine")
        .WithCleanUp(true)
        .Build();

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<BatteryDb>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<BatteryDb>(options =>
            {
                options.UseNpgsql(_postgreSqlContainer.GetConnectionString());
                options.EnableSensitiveDataLogging();
            });

            var ServiceProvider = services.BuildServiceProvider();

            using (var scope = ServiceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<BatteryDb>();
                context.Database.EnsureCreated();
            }
        });

        return base.CreateHost(builder);
    }

    public Task InitializeAsync()
    {
        return _postgreSqlContainer.StartAsync();
    }

    Task IAsyncLifetime.DisposeAsync()
    {
        return _postgreSqlContainer.DisposeAsync().AsTask();
    }
}
