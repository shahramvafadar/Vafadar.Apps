using Vafadar.Data;
using Vafadar.Finance.Data;

namespace Vafadar.Finance.App;

/// <summary>
/// Creates or upgrades the on-device database while the app starts, before any page reads from it.
/// </summary>
internal sealed class DatabaseInitializer : IMauiInitializeService
{
    public void Initialize(IServiceProvider services) => services.MigrateLocalDatabase<FinanceDbContext>();
}
