using System.Net;
using System.Text.Json;
using Vafadar.Authentication;
using Vafadar.Testing;

namespace Vafadar.Backup.GoogleDrive.Tests;

public sealed class GoogleDriveBackupStorageTests
{
    private readonly FakeHttpMessageHandler _http = new();
    private readonly StaticAccessTokenProvider _tokens = new("drive-token");
    private readonly GoogleDriveBackupStorage _storage;

    public GoogleDriveBackupStorageTests()
    {
        _storage = new GoogleDriveBackupStorage(new HttpClient(_http), _tokens);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task List_reads_all_pages_of_the_app_data_folder()
    {
        _http
            .RespondJson("""{ "nextPageToken": "p2", "files": [ { "id": "1", "name": "a.vbak", "size": "123", "createdTime": "2026-09-25T10:00:00Z" } ] }""")
            .RespondJson("""{ "files": [ { "id": "2", "name": "b.vbak", "size": "7" } ] }""");

        var files = await _storage.ListAsync(Ct);

        Assert.Equal(
            [new BackupFileInfo("1", "a.vbak", 123, new DateTimeOffset(2026, 9, 25, 10, 0, 0, TimeSpan.Zero)), new BackupFileInfo("2", "b.vbak", 7, null)],
            files);
        Assert.All(_http.Requests, r => Assert.Equal("Bearer drive-token", r.Authorization));
        Assert.Contains("spaces=appDataFolder", _http.Requests[0].Uri.Query, StringComparison.Ordinal);
        Assert.Contains("pageToken=p2", _http.Requests[1].Uri.Query, StringComparison.Ordinal);
        Assert.All(_tokens.RequestedScopes, scopes => Assert.Equal([GoogleDriveScopes.AppData], scopes));
    }

    [Fact]
    public async Task Upload_uses_a_resumable_session_in_the_app_data_folder()
    {
        _http
            .Respond(_ => new HttpResponseMessage(HttpStatusCode.OK) { Headers = { Location = new Uri("https://upload.example/session/1") } })
            .RespondJson("""{ "id": "new-id", "name": "x.vbak", "size": "3" }""");
        using var content = new MemoryStream([1, 2, 3]);

        var file = await _storage.UploadAsync("x.vbak", content, Ct);

        Assert.Equal(new BackupFileInfo("new-id", "x.vbak", 3, null), file);

        var start = _http.Requests[0];
        Assert.Equal(HttpMethod.Post, start.Method);
        Assert.Contains("uploadType=resumable", start.Uri.Query, StringComparison.Ordinal);
        using var metadata = JsonDocument.Parse(start.BodyText);
        Assert.Equal("x.vbak", metadata.RootElement.GetProperty("name").GetString());
        Assert.Equal("appDataFolder", metadata.RootElement.GetProperty("parents")[0].GetString());

        var upload = _http.Requests[1];
        Assert.Equal(HttpMethod.Put, upload.Method);
        Assert.Equal("https://upload.example/session/1", upload.Uri.ToString());
        Assert.Equal([1, 2, 3], upload.Body);

        Assert.True(content.CanRead, "The caller's stream must not be disposed.");
    }

    [Fact]
    public async Task OpenRead_downloads_the_file_content()
    {
        _http.Respond(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([9, 8, 7]) });

        await using var stream = await _storage.OpenReadAsync("file/1", Ct);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, Ct);

        Assert.Equal([9, 8, 7], buffer.ToArray());
        Assert.EndsWith("/files/file%2F1", _http.Requests[0].Uri.AbsolutePath, StringComparison.Ordinal);
        Assert.Equal("?alt=media", _http.Requests[0].Uri.Query);
    }

    [Fact]
    public async Task Deleting_a_missing_file_is_not_an_error()
    {
        _http.RespondStatus(HttpStatusCode.NotFound);

        await _storage.DeleteAsync("gone", Ct);

        Assert.Equal(HttpMethod.Delete, _http.Requests[0].Method);
    }

    [Fact]
    public async Task Unauthorized_requires_signing_in_again()
    {
        _http.RespondStatus(HttpStatusCode.Unauthorized);

        var error = await Assert.ThrowsAsync<AuthenticationRequiredException>(() => _storage.ListAsync(Ct));

        Assert.Equal(ExternalIdentityProvider.Google, error.Provider);
    }

    [Fact]
    public async Task Failures_are_reported_with_the_status_code()
    {
        _http.RespondJson("""{ "error": { "message": "quota exceeded" } }""", HttpStatusCode.Forbidden);

        var error = await Assert.ThrowsAsync<BackupStorageException>(() => _storage.ListAsync(Ct));

        Assert.Equal(403, error.StatusCode);
        Assert.Equal(GoogleDriveBackupStorage.StorageId, error.StorageId);
        Assert.Contains("quota exceeded", error.Message, StringComparison.Ordinal);
    }
}
