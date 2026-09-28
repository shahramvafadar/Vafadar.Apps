using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Vafadar.Zanance.Data;

/// <summary>
/// Lets the EF Core tools (<c>dotnet ef</c>) create the context without starting the MAUI app.
/// </summary>
internal sealed class ZananceDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ZananceDbContext>
{
    public ZananceDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<ZananceDbContext>().UseSqlite("Data Source=zanance.design.db").Options);
}
