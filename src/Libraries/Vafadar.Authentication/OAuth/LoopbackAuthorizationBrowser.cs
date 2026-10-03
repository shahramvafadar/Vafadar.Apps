using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Vafadar.Authentication.OAuth;

/// <summary>
/// The desktop authorization flow of RFC 8252 §7.3: a one-time HTTP listener on the loopback address
/// <c>http://127.0.0.1:{port}/</c> receives the redirect of the system browser. The port is chosen by the operating
/// system for each sign-in; the listener accepts connections from this computer only and stops when the session is
/// disposed.
/// </summary>
/// <remarks>
/// A plain <see cref="TcpListener"/> is used instead of <c>HttpListener</c>: it needs no URL reservation and no
/// administrator rights. Browsers may open idle "preconnect" sockets, so every connection is handled on its own with a
/// short read timeout, and only a request to the redirect path that carries <c>code</c> or <c>error</c> completes the
/// sign-in.
/// </remarks>
/// <param name="openBrowser">Opens a URI in the user's default browser (for example <c>Launcher.OpenAsync</c>).</param>
public sealed class LoopbackAuthorizationBrowser(Func<Uri, CancellationToken, Task> openBrowser) : IAuthorizationBrowser
{
    private const int MaxRequestBytes = 16 * 1024;

    private readonly Func<Uri, CancellationToken, Task> _openBrowser = openBrowser ?? throw new ArgumentNullException(nameof(openBrowser));

    /// <summary>Gets how long the browser may take before the sign-in counts as abandoned (default 5 minutes).</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>Gets how long one connection may take to send its request (default 10 seconds).</summary>
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>Gets the text of the page the browser shows after the redirect.</summary>
    public string CompletionMessage { get; init; } = "Sign-in is complete. You can close this tab and return to the app.";

    /// <inheritdoc />
    public ValueTask<IAuthorizationBrowserSession> StartAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ValueTask.FromResult<IAuthorizationBrowserSession>(new Session(this, listener));
    }

    /// <summary>
    /// Parses the request target of a redirect (<c>/?code=…&amp;state=…</c>) when its path is
    /// <paramref name="expectedPath"/>; returns <see langword="null"/> for any other path.
    /// </summary>
    internal static IReadOnlyDictionary<string, string>? ParseRedirectTarget(string target, string expectedPath)
    {
        var queryStart = target.IndexOf('?', StringComparison.Ordinal);
        var path = queryStart < 0 ? target : target[..queryStart];
        if (!string.Equals(path, expectedPath, StringComparison.Ordinal))
        {
            return null;
        }

        var parameters = new Dictionary<string, string>(StringComparer.Ordinal);
        if (queryStart >= 0)
        {
            foreach (var pair in target[(queryStart + 1)..].Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var separator = pair.IndexOf('=', StringComparison.Ordinal);
                var name = Decode(separator < 0 ? pair : pair[..separator]);
                var value = separator < 0 ? string.Empty : Decode(pair[(separator + 1)..]);
                parameters.TryAdd(name, value);
            }
        }

        return parameters;

        static string Decode(string value) => Uri.UnescapeDataString(value.Replace('+', ' '));
    }

    private sealed class Session : IAuthorizationBrowserSession
    {
        private readonly LoopbackAuthorizationBrowser _owner;
        private readonly TcpListener _listener;

        public Session(LoopbackAuthorizationBrowser owner, TcpListener listener)
        {
            _owner = owner;
            _listener = listener;
            RedirectUri = new Uri($"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/");
        }

        public Uri RedirectUri { get; }

        public async Task<IReadOnlyDictionary<string, string>> AuthorizeAsync(Uri authorizationUri, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(authorizationUri);
            using var wait = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            wait.CancelAfter(_owner.Timeout);
            var done = new TaskCompletionSource<IReadOnlyDictionary<string, string>>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var registration = wait.Token.Register(() => done.TrySetCanceled(wait.Token));

            // Listen before the browser opens, so a fast redirect is never missed.
            var accepting = AcceptAsync(done, wait.Token);
            try
            {
                await _owner._openBrowser(authorizationUri, wait.Token);
                return await done.Task;
            }
            finally
            {
                await wait.CancelAsync();
                await accepting;
            }
        }

        public ValueTask DisposeAsync()
        {
            _listener.Dispose();
            return ValueTask.CompletedTask;
        }

        private async Task AcceptAsync(TaskCompletionSource<IReadOnlyDictionary<string, string>> done, CancellationToken cancellationToken)
        {
            var handlers = new List<Task>();
            try
            {
                while (!done.Task.IsCompleted)
                {
                    var client = await _listener.AcceptTcpClientAsync(cancellationToken);
                    handlers.Add(HandleAsync(client, done, cancellationToken));
                }
            }
            catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException or SocketException)
            {
                // The sign-in finished, was cancelled or timed out.
            }

            await Task.WhenAll(handlers);
        }

        private async Task HandleAsync(TcpClient client, TaskCompletionSource<IReadOnlyDictionary<string, string>> done, CancellationToken cancellationToken)
        {
            using (client)
            {
                try
                {
                    using var read = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    read.CancelAfter(_owner.RequestTimeout);
                    var stream = client.GetStream();
                    var target = await ReadRequestTargetAsync(stream, read.Token);
                    var parameters = target is null ? null : ParseRedirectTarget(target, RedirectUri.AbsolutePath);
                    if (parameters is null || !(parameters.ContainsKey("code") || parameters.ContainsKey("error")))
                    {
                        // A favicon request or anything else that is not the redirect.
                        await WriteResponseAsync(stream, "404 Not Found", string.Empty, read.Token);
                        return;
                    }

                    await WriteResponseAsync(stream, "200 OK", CompletionPage(_owner.CompletionMessage), read.Token);
                    done.TrySetResult(parameters);
                }
                catch (Exception ex) when (ex is IOException or OperationCanceledException or SocketException or ObjectDisposedException)
                {
                    // An idle or broken connection; the browser's real request arrives on another one.
                }
            }
        }

        // Reads the request head and returns the target of a GET request ("/?code=…"), or null for anything else.
        private static async Task<string?> ReadRequestTargetAsync(NetworkStream stream, CancellationToken cancellationToken)
        {
            var buffer = new byte[MaxRequestBytes];
            var length = 0;
            while (length < buffer.Length)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(length), cancellationToken);
                if (read == 0)
                {
                    break;
                }

                length += read;
                if (buffer.AsSpan(0, length).IndexOf("\r\n\r\n"u8) >= 0)
                {
                    break;
                }
            }

            var head = Encoding.ASCII.GetString(buffer, 0, length);
            var lineEnd = head.IndexOf("\r\n", StringComparison.Ordinal);
            var parts = (lineEnd < 0 ? head : head[..lineEnd]).Split(' ');
            return parts.Length == 3 && parts[0] == "GET" && parts[1].StartsWith('/') ? parts[1] : null;
        }

        private static async Task WriteResponseAsync(NetworkStream stream, string status, string html, CancellationToken cancellationToken)
        {
            var body = Encoding.UTF8.GetBytes(html);
            var head = Encoding.ASCII.GetBytes(
                $"HTTP/1.1 {status}\r\nContent-Type: text/html; charset=utf-8\r\nContent-Length: {body.Length}\r\n" +
                "Cache-Control: no-store\r\nConnection: close\r\n\r\n");
            await stream.WriteAsync(head, cancellationToken);
            await stream.WriteAsync(body, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }

        private static string CompletionPage(string message)
        {
            var text = WebUtility.HtmlEncode(message);
            return $"<!doctype html><html><head><meta charset=\"utf-8\"><title>{text}</title></head>" +
                $"<body style=\"font-family:system-ui,sans-serif;margin:3em;line-height:1.5\"><p>{text}</p></body></html>";
        }
    }
}
