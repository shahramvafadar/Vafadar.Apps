using Android.App;
using Vafadar.Testing;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Serializes tests for the process-wide original Android capture slot.</summary>
[CollectionDefinition("Original Android camera ownership", DisableParallelization = true)]
public sealed class CameraNativeOwnershipCollection { }

/// <summary>Original native result ownership survives concurrent callers, cancellation, acceptance and launch failure.</summary>
[Collection("Original Android camera ownership")]
public sealed class CameraNativeOwnershipTests
{
    /// <summary>A second request cannot replace the first callback or create another native output.</summary>
    [Fact]
    public async Task Concurrent_capture_refuses_before_launch_and_keeps_first_native_owner()
    {
        using var directory = new TemporaryDirectory();
        var activity = new CameraBoundaryActivity(directory.Path); Platform.CurrentActivity = activity;
        var first = CameraCapture.TakePhotoAsync(); var second = CameraCapture.TakePhotoAsync();
        try
        {
            Assert.True(second.IsFaulted);
            await Assert.ThrowsAsync<InvalidOperationException>(() => second);
            Assert.Equal(1, activity.Launches); Assert.False(first.IsCompleted);
            Assert.Single(Directory.GetDirectories(directory.Path));
            Assert.Empty(Directory.GetFiles(directory.Path, "*", SearchOption.AllDirectories));
        }
        finally
        {
            CameraCapture.OnActivityResult(activity.LastRequest, Result.Canceled);
            await first.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
            try { await second.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken); } catch (InvalidOperationException) { }
            Platform.CurrentActivity = null;
        }
    }

    /// <summary>A native launch failure releases ownership so the next real operation is not permanently blocked.</summary>
    [Fact]
    public async Task Launch_failure_releases_ownership_for_next_original_capture()
    {
        using var directory = new TemporaryDirectory();
        var activity = new CameraBoundaryActivity(directory.Path) { ThrowOnLaunch = true }; Platform.CurrentActivity = activity;
        try
        {
            await Assert.ThrowsAsync<Android.Content.ActivityNotFoundException>(() => CameraCapture.TakePhotoAsync());
            activity.ThrowOnLaunch = false; var next = CameraCapture.TakePhotoAsync();
            Assert.False(next.IsCompleted); Assert.Equal(2, activity.Launches);
            Assert.True(CameraCapture.OnActivityResult(activity.LastRequest, Result.Canceled));
            Assert.Null(await next.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken));
        }
        finally { Platform.CurrentActivity = null; }
    }

    /// <summary>Native acceptance retains every real output byte and deletes only that owned temporary camera file.</summary>
    [Fact]
    public async Task Accepted_capture_reads_original_bytes_then_deletes_owned_output()
    {
        using var directory = new TemporaryDirectory();
        var activity = new CameraBoundaryActivity(directory.Path); Platform.CurrentActivity = activity;
        try
        {
            var capture = CameraCapture.TakePhotoAsync(); var output = activity.LastOutput!.AbsolutePath;
            byte[] expected = [0xFF, 0xD8, 0x45, 0x58, 0x01, 0xFF, 0xD9];
            await File.WriteAllBytesAsync(output, expected, TestContext.Current.CancellationToken);
            Assert.True(CameraCapture.OnActivityResult(activity.LastRequest, Result.Ok));
            Assert.Equal(expected, await capture.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken));
            Assert.False(File.Exists(output));
        }
        finally { Platform.CurrentActivity = null; }
    }

    /// <summary>Unknown results cannot consume the owner; cancellation removes a partial output and frees the next request.</summary>
    [Fact]
    public async Task Unknown_result_is_ignored_and_cancelled_output_does_not_strand_owner()
    {
        using var directory = new TemporaryDirectory();
        var activity = new CameraBoundaryActivity(directory.Path); Platform.CurrentActivity = activity;
        try
        {
            var capture = CameraCapture.TakePhotoAsync(); var output = activity.LastOutput!.AbsolutePath;
            await File.WriteAllBytesAsync(output, new byte[] { 1, 2, 3 }, TestContext.Current.CancellationToken);
            Assert.False(CameraCapture.OnActivityResult(activity.LastRequest + 1, Result.Ok)); Assert.False(capture.IsCompleted);
            Assert.True(CameraCapture.OnActivityResult(activity.LastRequest, Result.Canceled));
            Assert.Null(await capture.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken)); Assert.False(File.Exists(output));
            var next = CameraCapture.TakePhotoAsync(); Assert.False(next.IsCompleted); Assert.Equal(2, activity.Launches);
            CameraCapture.OnActivityResult(activity.LastRequest, Result.Canceled);
            Assert.Null(await next.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken));
        }
        finally { Platform.CurrentActivity = null; }
    }

    /// <summary>Encoded-size refusal removes its owned output and releases the result slot without another accepted image.</summary>
    [Fact]
    public async Task Oversized_native_output_releases_owner_and_keeps_original_encoded_limit()
    {
        using var directory = new TemporaryDirectory();
        var activity = new CameraBoundaryActivity(directory.Path); Platform.CurrentActivity = activity;
        try
        {
            var capture = CameraCapture.TakePhotoAsync(); var output = activity.LastOutput!.AbsolutePath;
            await using (var file = File.Create(output)) file.SetLength(Presentation.AttachmentFiles.MaxSourceBytes + 1L);
            CameraCapture.OnActivityResult(activity.LastRequest, Result.Ok);
            await Assert.ThrowsAsync<InvalidDataException>(() => capture);
            Assert.False(File.Exists(output));
            var next = CameraCapture.TakePhotoAsync(); Assert.False(next.IsCompleted); Assert.Equal(2, activity.Launches);
            CameraCapture.OnActivityResult(activity.LastRequest, Result.Canceled);
            Assert.Null(await next.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken));
        }
        finally { Platform.CurrentActivity = null; }
    }
}
