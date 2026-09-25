using System.Xml.Linq;
using Vafadar.Testing;

namespace Vafadar.Finance.Core.Tests;

public sealed class FinanceAppTests
{
    [Fact]
    public void App_id_is_the_released_package_name()
    {
        // Changing the app id creates a different app in the stores and orphans existing backups.
        Assert.Equal("pro.vafadar.finance", FinanceApp.AppId);
    }

    [Fact]
    public void App_project_uses_the_same_application_id()
    {
        var project = XDocument.Load(RepositoryPaths.Combine("src", "Apps", "Finance", "Vafadar.Finance.App", "Vafadar.Finance.App.csproj"));
        var applicationId = project.Descendants("ApplicationId").Single().Value;

        Assert.Equal(FinanceApp.AppId, applicationId);
    }
}
