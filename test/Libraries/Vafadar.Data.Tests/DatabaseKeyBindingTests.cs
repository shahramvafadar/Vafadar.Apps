namespace Vafadar.Data.Tests;

public sealed class DatabaseKeyBindingTests
{
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void Every_binding_identity_is_required(int missing) =>
        Assert.Throws<ArgumentException>(() => new DatabaseKeyBinding(missing == 0 ? Guid.Empty : Guid.NewGuid(),
            missing == 1 ? Guid.Empty : Guid.NewGuid(), missing == 2 ? Guid.Empty : Guid.NewGuid()));

    [Fact]
    public void Context_is_stable_for_the_same_binding_and_versioned()
    {
        var a = new DatabaseKeyBinding(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        Assert.Equal(a.GetAuthenticatedContext(), new DatabaseKeyBinding(a.ProfileId, a.InstallationId, a.GenerationId).GetAuthenticatedContext());
        Assert.StartsWith("Vafadar.DatabaseKey.v1|", System.Text.Encoding.UTF8.GetString(a.GetAuthenticatedContext()));
    }
}
