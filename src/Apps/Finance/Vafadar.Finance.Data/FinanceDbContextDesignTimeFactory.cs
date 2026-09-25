using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Vafadar.Finance.Data;

/// <summary>
/// Lets the EF Core tools (<c>dotnet ef</c>) create the context without starting the MAUI app.
/// </summary>
internal sealed class FinanceDbContextDesignTimeFactory : IDesignTimeDbContextFactory<FinanceDbContext>
{
    public FinanceDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<FinanceDbContext>().UseSqlite("Data Source=finance.design.db").Options);
}
