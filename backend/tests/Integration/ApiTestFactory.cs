using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Infrastructure.Data;

namespace Integration;

public class ApiTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "integration-secret-key-at-least-32-bytes!!",
                ["Jwt:Issuer"] = "stroy",
                ["Jwt:Audience"] = "stroy",
                ["Jwt:ExpiresHours"] = "8",
                ["ADMIN_USERNAME"] = "admin",
                ["ADMIN_PASSWORD"] = "secret123",
            });
        });
        builder.ConfigureTestServices(services =>
        {
            // Remove the SQLite DbContext registration added by Program.cs
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            // Also remove any IDbContextOptionsConfiguration<AppDbContext> to avoid
            // "two providers registered" conflict
            var configDescriptors = services
                .Where(d => d.ServiceType == typeof(IDbContextOptionsConfiguration<AppDbContext>))
                .ToList();
            foreach (var d in configDescriptors) services.Remove(d);

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("IntegrationTestDb"));
        });
    }
}
