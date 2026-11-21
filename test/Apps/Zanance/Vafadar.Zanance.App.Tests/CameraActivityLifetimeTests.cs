using Android.App;
using Vafadar.Testing;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Capture results and destruction belong to the exact original Activity across native recreation.</summary>
[Collection("Original Android camera ownership")]
public sealed class CameraActivityLifetimeTests
{
    [Fact]
    public async Task Destroying_original_activity_after_camera_accepts_retires_output_read()
    {
        using var directory = new TemporaryDirectory();
        var owner = new CameraBoundaryActivity(directory.Path);
        var ct = TestContext.Current.CancellationToken;
        Platform.CurrentActivity = owner;
        var capture = CameraCapture.TakePhotoAsync(ct);
        var output = owner.LastOutput!;
        try
        {
            await File.WriteAllBytesAsync(output.AbsolutePath, new byte[] { 1, 2, 3 }, ct);
            var destroyed = false;
            output.BeforeLength = () =>
            {
                destroyed = true;
                CameraCapture.OnDestroyed(owner);
            };
            Assert.True(CameraCapture.OnActivityResult(owner, owner.LastRequest, Result.Ok));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => capture.WaitAsync(TimeSpan.FromSeconds(3), ct));
            Assert.True(destroyed);
            Assert.False(File.Exists(output.AbsolutePath));
        }
        finally
        {
            CameraCapture.OnDestroyed(owner);
            Platform.CurrentActivity = null;
        }
    }

    /// <summary>A foreign Activity cannot settle a pending original capture using its valid request code.</summary>
    [Fact]
    public async Task Foreign_activity_result_cannot_complete_original_capture()
    {
        using var directory = new TemporaryDirectory();
        var owner = new CameraBoundaryActivity(directory.Path);
        var other = new CameraBoundaryActivity(directory.Path);
        Platform.CurrentActivity = owner;
        var capture = CameraCapture.TakePhotoAsync(TestContext.Current.CancellationToken);
        try
        {
            Platform.CurrentActivity = other;
            Assert.True(CameraCapture.OnActivityResult(other, owner.LastRequest, Result.Ok));
            Assert.False(capture.IsCompleted);
            Assert.Equal(1, owner.Launches);
            Assert.Equal(0, other.Launches);
        }
        finally
        {
            CameraCapture.OnActivityResult(owner, owner.LastRequest, Result.Canceled);
            await capture.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
            Platform.CurrentActivity = null;
        }
    }

    /// <summary>Destruction clears the original wait/output; its delayed callback cannot complete a recreated owner's request.</summary>
    [Fact]
    public async Task Destroyed_activity_cancels_wait_and_old_result_cannot_settle_recreated_capture()
    {
        using var directory = new TemporaryDirectory();
        var owner = new CameraBoundaryActivity(directory.Path);
        Platform.CurrentActivity = owner;
        var first = CameraCapture.TakePhotoAsync(TestContext.Current.CancellationToken);
        var oldCode = owner.LastRequest;
        var output = owner.LastOutput!.AbsolutePath;
        await File.WriteAllBytesAsync(output, new byte[] { 1, 2, 3 }, TestContext.Current.CancellationToken);
        CameraCapture.OnDestroyed(owner);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken));
        Assert.False(File.Exists(output));
        var replacement = new CameraBoundaryActivity(directory.Path);
        Platform.CurrentActivity = replacement;
        var second = CameraCapture.TakePhotoAsync(TestContext.Current.CancellationToken);
        try
        {
            Assert.NotEqual(oldCode, replacement.LastRequest);
            Assert.True(CameraCapture.OnActivityResult(replacement, oldCode, Result.Ok));
            Assert.True(CameraCapture.OnActivityResult(owner, oldCode, Result.Ok));
            Assert.False(second.IsCompleted);
            Assert.Equal(1, replacement.Launches);
        }
        finally
        {
            CameraCapture.OnActivityResult(replacement, replacement.LastRequest, Result.Canceled);
            await second.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
            Platform.CurrentActivity = null;
        }
    }

    /// <summary>Retiring a different Activity leaves the current original operation and output ownership intact.</summary>
    [Fact]
    public async Task Destroying_foreign_activity_does_not_cancel_current_capture()
    {
        using var directory = new TemporaryDirectory();
        var owner = new CameraBoundaryActivity(directory.Path);
        Platform.CurrentActivity = owner;
        var capture = CameraCapture.TakePhotoAsync(TestContext.Current.CancellationToken);
        try
        {
            CameraCapture.OnDestroyed(new CameraBoundaryActivity(directory.Path));
            Assert.False(capture.IsCompleted);
            Assert.Equal(1, owner.Launches);
        }
        finally
        {
            CameraCapture.OnActivityResult(owner, owner.LastRequest, Result.Canceled);
            await capture.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
            Platform.CurrentActivity = null;
        }
    }
}
