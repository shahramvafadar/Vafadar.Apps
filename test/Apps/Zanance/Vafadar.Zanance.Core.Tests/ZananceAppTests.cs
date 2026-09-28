using System.Xml.Linq;
using Vafadar.Testing;

namespace Vafadar.Zanance.Core.Tests;

public sealed class ZananceAppTests
{
    [Fact]
    public void App_id_is_the_released_package_name()
    {
        // Changing the app id creates a different app in the stores and orphans existing backups.
        Assert.Equal("pro.vafadar.zanance", ZananceApp.AppId);
    }

    [Fact]
    public void App_project_uses_the_same_application_id()
    {
        var project = XDocument.Load(RepositoryPaths.Combine("src", "Apps", "Zanance", "Vafadar.Zanance.App", "Vafadar.Zanance.App.csproj"));
        var applicationId = project.Descendants("ApplicationId").Single().Value;

        Assert.Equal(ZananceApp.AppId, applicationId);
    }
}
