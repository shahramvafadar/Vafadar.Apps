using Microsoft.Extensions.DependencyInjection;
using Vafadar.Data;
using Vafadar.Zanance.Data;

namespace Vafadar.Zanance.App.Startup;

/// <summary>Prepares all required storage before any financial page, app-lock database read or foreground writer starts.</summary>
public interface IAppDatabaseStartup
{
    /// <summary>Runs under the covered startup worker; protected hosts must register their complete bootstrap.</summary>
    Task PrepareAsync(CancellationToken cancellationToken = default);
}

/// <summary>Existing plaintext migration host; a configured protected session cannot fall back to it.</summary>
internal sealed class LocalDatabaseStartup(IServiceProvider services) : IAppDatabaseStartup
{
    /// <inheritdoc />
    public Task PrepareAsync(CancellationToken cancellationToken = default)
    {
        if (services.GetService<SqlCipherProfileSession<ZananceDbContext>>() is not null ||
            services.GetRequiredService<LocalDatabaseLocation<ZananceDbContext>>().IsProtected)
            throw new InvalidOperationException("Protected storage requires its complete startup bootstrap.");
        return services.MigrateLocalDatabaseAsync<ZananceDbContext>(cancellationToken);
    }
}
