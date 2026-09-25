using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Vafadar.Authentication;

namespace Vafadar.Backup.GoogleDrive;

/// <summary>
/// Stores backups in the <c>appDataFolder</c> of the signed-in user's Google Drive.
/// </summary>
/// <remarks>
/// <para>
/// The app data folder is hidden from the Drive UI and only accessible to the OAuth client (Google Cloud project) that
/// created it. Users see the used storage under Drive settings → "Manage apps" and can delete it there.
/// Files are uploaded with the resumable protocol, so there is no practical size limit.
/// </para>
/// <para>Access tokens come from the <see cref="IAccessTokenProvider"/> registered for <see cref="ExternalIdentityProvider.Google"/>.</para>
/// </remarks>
public sealed class GoogleDriveBackupStorage : IBackupStorage
{
    /// <summary>The <see cref="IBackupStorage.Id"/> of this storage.</summary>
    public const string StorageId = "google-drive";

    /// <summary>The name of the <see cref="HttpClient"/> registered for this storage.</summary>
    public const string HttpClientName = "Vafadar.Backup.GoogleDrive";

    private const string FilesEndpoint = "https://www.googleapis.com/drive/v3/files";
    private const string UploadEndpoint = "https://www.googleapis.com/upload/drive/v3/files";
    private const string AppDataFolder = "appDataFolder";
    private const string FileFields = "id,name,size,createdTime";

    private readonly HttpClient _http;
    private readonly IAccessTokenProvider _tokens;

    /// <summary>Creates the storage.</summary>
    public GoogleDriveBackupStorage(HttpClient http, IAccessTokenProvider tokens)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(tokens);
        _http = http;
        _tokens = tokens;
    }

    /// <inheritdoc />
    public string Id => StorageId;

    /// <inheritdoc />
    public async Task<IReadOnlyList<BackupFileInfo>> ListAsync(CancellationToken cancellationToken = default)
    {
        var result = new List<BackupFileInfo>();
        string? pageToken = null;

        do
        {
            var url = $"{FilesEndpoint}?spaces={AppDataFolder}&pageSize=100"
                + $"&q={Uri.EscapeDataString("trashed = false")}"
                + $"&fields={Uri.EscapeDataString($"nextPageToken,files({FileFields})")}"
                + (pageToken is null ? string.Empty : $"&pageToken={Uri.EscapeDataString(pageToken)}");

            using var response = await SendAsync(new HttpRequestMessage(HttpMethod.Get, url), cancellationToken);
            var page = await response.Content.ReadFromJsonAsync(GoogleDriveJsonContext.Default.DriveFileList, cancellationToken);

            result.AddRange((page?.Files ?? []).Select(ToInfo));
            pageToken = page?.NextPageToken;
        }
        while (!string.IsNullOrEmpty(pageToken));

        return result;
    }

    /// <inheritdoc />
    public async Task<BackupFileInfo> UploadAsync(string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(content);

        // 1. Start a resumable upload session with the file metadata.
        var start = new HttpRequestMessage(HttpMethod.Post, $"{UploadEndpoint}?uploadType=resumable&fields={Uri.EscapeDataString(FileFields)}")
        {
            Content = JsonContent.Create(new DriveFileMetadata(fileName, [AppDataFolder]), GoogleDriveJsonContext.Default.DriveFileMetadata),
        };
        start.Headers.Add("X-Upload-Content-Type", "application/octet-stream");

        Uri sessionUri;
        using (var response = await SendAsync(start, cancellationToken))
        {
            sessionUri = response.Headers.Location
                ?? throw new BackupStorageException(StorageId, "Google Drive did not return an upload session.");
        }

        // 2. Upload the content in a single request.
        var upload = new HttpRequestMessage(HttpMethod.Put, sessionUri) { Content = await CreateContentAsync(content, cancellationToken) };

        using var uploaded = await SendAsync(upload, cancellationToken);
        var file = await uploaded.Content.ReadFromJsonAsync(GoogleDriveJsonContext.Default.DriveFile, cancellationToken)
            ?? throw new BackupStorageException(StorageId, "Google Drive returned no file after the upload.");

        return ToInfo(file);
    }

    /// <inheritdoc />
    public async Task<Stream> OpenReadAsync(string fileId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        using var response = await SendAsync(
            new HttpRequestMessage(HttpMethod.Get, $"{FilesEndpoint}/{Uri.EscapeDataString(fileId)}?alt=media"),
            cancellationToken);

        var buffer = new MemoryStream();
        await response.Content.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;
        return buffer;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string fileId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        using var response = await SendAsync(
            new HttpRequestMessage(HttpMethod.Delete, $"{FilesEndpoint}/{Uri.EscapeDataString(fileId)}"),
            cancellationToken,
            allowNotFound: true);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken, bool allowNotFound = false)
    {
        using (request)
        {
            var token = await _tokens.GetAccessTokenAsync(GoogleDriveScopes.All, cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.IsSuccessStatusCode || (allowNotFound && response.StatusCode == HttpStatusCode.NotFound))
            {
                return response;
            }

            using (response)
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new AuthenticationRequiredException(ExternalIdentityProvider.Google);
                }

                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new BackupStorageException(
                    StorageId,
                    $"Google Drive request failed with {(int)response.StatusCode} {response.ReasonPhrase}: {Truncate(body)}",
                    (int)response.StatusCode);
            }
        }
    }

    // The request owns its content and disposes it; never hand it the caller's stream.
    private static async Task<HttpContent> CreateContentAsync(Stream content, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);

        var httpContent = new ByteArrayContent(buffer.ToArray());
        httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        return httpContent;
    }

    private static BackupFileInfo ToInfo(DriveFile file) => new(file.Id, file.Name, file.Size, file.CreatedTime);

    private static string Truncate(string value) => value.Length <= 500 ? value : value[..500];
}
