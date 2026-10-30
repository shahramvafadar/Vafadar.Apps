using Vafadar.Zanance.App.Security;
namespace Vafadar.Zanance.App.Tests;

/// <summary>Exact original modal authority refuses another path or injected query/fragment.</summary>
public sealed class NativeSensitivePresentationRouteTests
{
    [Theory]
    [InlineData("D_FAULT_LockPage42", true)]
    [InlineData("D_FAULT_LockPage420", false)]
    [InlineData("other/D_FAULT_LockPage42", false)]
    [InlineData("D_FAULT_LockPage42/other", false)]
    [InlineData("D_FAULT_LockPage42?other=1", false)]
    [InlineData("D_FAULT_LockPage42%2fother", false)]
    [InlineData("D_FAULT_LockPage42%3fother=1", false)]
    [InlineData("D_FAULT_LockPage42#other", false)]
    public void Only_the_exact_original_single_modal_route_is_admitted(string proposed, bool accepted) =>
        Assert.Equal(accepted, NativeSensitivePresentation.IsExactTarget(proposed, "D_FAULT_LockPage42"));
}
