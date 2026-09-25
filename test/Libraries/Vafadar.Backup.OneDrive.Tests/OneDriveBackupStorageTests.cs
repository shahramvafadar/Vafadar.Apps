using System.Net;
using Vafadar.Authentication;
using Vafadar.Testing;

namespace Vafadar.Backup.OneDrive.Tests;

public sealed class OneDriveBackupStorageTests
{
    private readonly FakeHttpMessageHandler _http = new();
    private readonly StaticAccessTokenProvider _tokens = new("graph-token");
    private readonly OneDriveBackupStorage _storage;

    public OneDriveBackupStorageTests()
    {
        _storage = new OneDriveBackupStorage(new HttpClient(_http), _tokens);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task List_returns_files_of_the_app_folder_and_follows_next_links()
    {
        _http
            .RespondJson("""
                {
                  "@odata.nextLink": "https://graph.microsoft.com/v1.0/next-page",
                  "value": [
                    { "id": "1", "name": "a.vbak", "size": 10, "createdDateTime": "2026-09-25T10:00:00Z", "file": { "mimeType": "application/octet-stream" } },
                    { "id": "f", "name": "some folder", "folder": { "childCount": 0 } }
                  ]
                }
                """)
            .RespondJson("""{ "value": [ { "id": "2", "name": "b.vbak", "size": 20, "file": {} } ] }""");

        var files = await _storage.ListAsync(Ct);

        Assert.Equal(["a.vbak", "b.vbak"], files.Select(f => f.FileName));
        Assert.Equal("/v1.0/me/drive/special/approot/children", _http.Requests[0].Uri.AbsolutePath);
        Assert.Equal("https://graph.microsoft.com/v1.0/next-page", _http.Requests[1].Uri.ToString());
        Assert.All(_http.Requests, r => Assert.Equal("Bearer graph-token", r.Authorization));
        Assert.All(_tokens.RequestedScopes, scopes => Assert.Equal([OneDriveScopes.AppFolder], scopes));
    }

    [Fact]
    public async Task Upload_puts_the_file_into_the_app_folder()
    {
        _http.RespondJson("""{ "id": "new", "name": "x y.vbak", "size": 3, "file": {} }""", HttpStatusCode.Created);
        using var content = new MemoryStream([1, 2, 3]);

        var file = await _storage.UploadAsync("x y.vbak", content, Ct);

        Assert.Equal("new", file.Id);
        Assert.Equal(HttpMethod.Put, _http.Requests[0].Method);
        Assert.Equal("/v1.0/me/drive/special/approot:/x%20y.vbak:/content", _http.Requests[0].Uri.AbsolutePath);
        Assert.Equal([1, 2, 3], _http.Requests[0].Body);
        Assert.True(content.CanRead, "The caller's stream must not be disposed.");
    }

    [Fact]
    public async Task OpenRead_downloads_from_the_pre_authenticated_url_without_the_access_token()
    {
        _http
            .RespondJson("""{ "id": "1", "name": "a.vbak", "file": {}, "@microsoft.graph.downloadUrl": "https://download.example/a" }""")
            .Respond(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([4, 5]) });

        await using var stream = await _storage.OpenReadAsync("1", Ct);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, Ct);

        Assert.Equal([4, 5], buffer.ToArray());
        Assert.Equal("https://download.example/a", _http.Requests[1].Uri.ToString());
        Assert.Null(_http.Requests[1].Authorization);
    }

    [Fact]
    public async Task Deleting_a_missing_file_is_not_an_error()
    {
        _http.RespondStatus(HttpStatusCode.NotFound);

        await _storage.DeleteAsync("gone", Ct);

        Assert.Equal("/v1.0/me/drive/items/gone", _http.Requests[0].Uri.AbsolutePath);
    }

    [Fact]
    public async Task Unauthorized_requires_signing_in_again()
    {
        _http.RespondStatus(HttpStatusCode.Unauthorized);

        var error = await Assert.ThrowsAsync<AuthenticationRequiredException>(() => _storage.ListAsync(Ct));

        Assert.Equal(ExternalIdentityProvider.Microsoft, error.Provider);
    }
}
