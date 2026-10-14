using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Vafadar.Zanance.Api;

/// <summary>Pure design-time model construction; never opens a service or financial database.</summary>
public sealed class ServiceDesignFactory : IDesignTimeDbContextFactory<ServiceDbContext>
{
    /// <inheritdoc />
    public ServiceDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<ServiceDbContext>()
        .UseNpgsql("Host=127.0.0.1;Database=zanance_design_only;Username=design_only").Options);
}
