using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Vafadar.Authentication;

namespace Vafadar.Backup.OneDrive;

/// <summary>
/// Stores backups in the app folder of the signed-in user's OneDrive (<c>Apps/&lt;app name&gt;</c>).
/// </summary>
/// <remarks>
/// <para>
/// Unlike Google Drive's hidden app data folder, this folder is visible to the user, who can see, download and delete
/// backups in OneDrive. Uploads use the simple upload API (up to 250 MB per file).
/// </para>
/// <para>Access tokens come from the <see cref="IAccessTokenProvider"/> registered for <see cref="ExternalIdentityProvider.Microsoft"/>.</para>
/// </remarks>
public sealed class OneDriveBackupStorage : IBackupStorage
{
    /// <summary>The <see cref="IBackupStorage.Id"/> of this storage.</summary>
    public const string StorageId = "onedrive";

    /// <summary>The name of the <see cref="HttpClient"/> registered for this storage.</summary>
    public const string HttpClientName = "Vafadar.Backup.OneDrive";

    private const string DriveEndpoint = "https://graph.microsoft.com/v1.0/me/drive";
    private const string AppFolderEndpoint = DriveEndpoint + "/special/approot";

    private readonly HttpClient _http;
    private readonly IAccessTokenProvider _tokens;

    /// <summary>Creates the storage.</summary>
    public OneDriveBackupStorage(HttpClient http, IAccessTokenProvider tokens)
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
        string? url = $"{AppFolderEndpoint}/children?$select=id,name,size,createdDateTime,file&$top=200";

        while (url is not null)
        {
            using var response = await SendAsync(new HttpRequestMessage(HttpMethod.Get, url), cancellationToken);
            var page = await response.Content.ReadFromJsonAsync(OneDriveJsonContext.Default.DriveItemCollection, cancellationToken);

            result.AddRange((page?.Value ?? []).Where(item => item.File is not null).Select(ToInfo));
            url = page?.NextLink;
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<BackupFileInfo> UploadAsync(string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(content);

        var request = new HttpRequestMessage(HttpMethod.Put, $"{AppFolderEndpoint}:/{Uri.EscapeDataString(fileName)}:/content")
        {
            Content = await CreateContentAsync(content, cancellationToken),
        };

        using var response = await SendAsync(request, cancellationToken);
        var item = await response.Content.ReadFromJsonAsync(OneDriveJsonContext.Default.DriveItem, cancellationToken)
            ?? throw new BackupStorageException(StorageId, "OneDrive returned no item after the upload.");

        return ToInfo(item);
    }

    /// <inheritdoc />
    public async Task<Stream> OpenReadAsync(string fileId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        // The item's pre-authenticated download URL must be fetched WITHOUT the Graph access token.
        string downloadUrl;
        using (var response = await SendAsync(
            new HttpRequestMessage(HttpMethod.Get, $"{DriveEndpoint}/items/{Uri.EscapeDataString(fileId)}"),
            cancellationToken))
        {
            var item = await response.Content.ReadFromJsonAsync(OneDriveJsonContext.Default.DriveItem, cancellationToken);
            downloadUrl = item?.DownloadUrl
                ?? throw new BackupStorageException(StorageId, $"OneDrive item '{fileId}' has no download URL.");
        }

        using var download = await _http.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!download.IsSuccessStatusCode)
        {
            throw new BackupStorageException(StorageId, $"Downloading from OneDrive failed with {(int)download.StatusCode}.", (int)download.StatusCode);
        }

        var buffer = new MemoryStream();
        await download.Content.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;
        return buffer;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string fileId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        using var response = await SendAsync(
            new HttpRequestMessage(HttpMethod.Delete, $"{DriveEndpoint}/items/{Uri.EscapeDataString(fileId)}"),
            cancellationToken,
            allowNotFound: true);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken, bool allowNotFound = false)
    {
        using (request)
        {
            var token = await _tokens.GetAccessTokenAsync(OneDriveScopes.All, cancellationToken);
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
                    throw new AuthenticationRequiredException(ExternalIdentityProvider.Microsoft);
                }

                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new BackupStorageException(
                    StorageId,
                    $"OneDrive request failed with {(int)response.StatusCode} {response.ReasonPhrase}: {Truncate(body)}",
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

    private static BackupFileInfo ToInfo(DriveItem item) => new(item.Id, item.Name, item.Size, item.CreatedDateTime);

    private static string Truncate(string value) => value.Length <= 500 ? value : value[..500];
}
