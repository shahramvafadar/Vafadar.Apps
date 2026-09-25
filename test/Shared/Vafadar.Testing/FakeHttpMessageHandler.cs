using System.Net;
using System.Text;

namespace Vafadar.Testing;

/// <summary>
/// An <see cref="HttpMessageHandler"/> that answers requests from a queue of canned responses and records every
/// request (with its body captured before the request is disposed).
/// </summary>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responses = new();

    /// <summary>Gets the requests received so far.</summary>
    public List<RecordedRequest> Requests { get; } = [];

    /// <summary>Queues a response.</summary>
    public FakeHttpMessageHandler Respond(Func<HttpRequestMessage, HttpResponseMessage> response)
    {
        _responses.Enqueue(response);
        return this;
    }

    /// <summary>Queues a response with a JSON body.</summary>
    public FakeHttpMessageHandler RespondJson(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        Respond(_ => new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") });

    /// <summary>Queues a response without a body.</summary>
    public FakeHttpMessageHandler RespondStatus(HttpStatusCode status) =>
        Respond(_ => new HttpResponseMessage(status));

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsByteArrayAsync(cancellationToken);
        Requests.Add(new RecordedRequest(
            request.Method,
            request.RequestUri!,
            request.Headers.Authorization?.ToString(),
            request.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value), StringComparer.OrdinalIgnoreCase),
            request.Content?.Headers.ContentType?.MediaType,
            body));

        if (_responses.Count == 0)
        {
            throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}");
        }

        var response = _responses.Dequeue()(request);
        response.RequestMessage = request;
        return response;
    }
}

/// <summary>A request received by <see cref="FakeHttpMessageHandler"/>.</summary>
public sealed record RecordedRequest(
    HttpMethod Method,
    Uri Uri,
    string? Authorization,
    IReadOnlyDictionary<string, string> Headers,
    string? ContentType,
    byte[]? Body)
{
    /// <summary>Gets the body as UTF-8 text.</summary>
    public string BodyText => Body is null ? string.Empty : Encoding.UTF8.GetString(Body);
}
