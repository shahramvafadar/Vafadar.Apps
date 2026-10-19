using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Linq;
using Vafadar.Zanance.Core.Rates;

namespace Vafadar.Zanance.Api.Rates;

/// <summary>One bounded public daily feed across all pairs and identities; metadata and cached history never dispatch HTTP.</summary>
internal sealed class EcbMarketProvider(HttpClient client, TimeProvider time) : IMarketProvider, IDisposable
{
    private sealed record Feed(DateOnly Date, IReadOnlyDictionary<string, decimal> Rates, EcbDailyEvidence Evidence);
    private readonly object gate = new();
    private Task<Feed>? pending;
    private Feed? cached;
    /// <summary>Finite actual uncompressed public XML bound, independent of upstream Content-Length.</summary>
    internal const int MaximumBytes = 262144;
    /// <summary>Disposes only this adapter's explicitly owned fixed-source transport at service shutdown.</summary>
    public void Dispose() => client.Dispose();
    /// <inheritdoc />
    public MarketCatalog Catalog => EcbReference.Catalog;

    /// <summary>Creates the fixed-source OS-trusted transport without cookies, redirects, credentials or hidden retries.</summary>
    internal static HttpClient CreateClient() => new(new SocketsHttpHandler
    {
        AllowAutoRedirect = false, UseCookies = false, Credentials = null,
        AutomaticDecompression = DecompressionMethods.None, ConnectTimeout = TimeSpan.FromSeconds(5),
        MaxResponseHeadersLength = 16,
    }) { Timeout = Timeout.InfiniteTimeSpan };

    /// <inheritdoc />
    public async Task<MarketObservation> FetchAsync(MarketRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var symbol = MarketReference.Validate(Catalog, request, time.GetUtcNow());
        Task<Feed> original;
        lock (gate)
        {
            if (cached is { } current && time.GetUtcNow() < current.Evidence.FetchedAt.AddSeconds(300))
                original = Task.FromResult(current);
            else
            {
                // Retain even an uncooperative original transport until terminal. A caller deadline cannot create a second feed request.
                if (pending is not null) original = pending;
                else
                {
                    original = Task.Run(ReadAsync); pending = original;
                    // Capture before observing terminal work: a synchronous completion may retire reentrantly.
                    _ = RetireAsync(original);
                }
            }
        }
        var feed = await original.WaitAsync(ct);
        var evidence = feed.Evidence with
        {
            BasePerEuro = feed.Rates[symbol.Base], QuotePerEuro = feed.Rates[symbol.Currency],
            Calculation = symbol.Base == "EUR" ? EcbReference.Direct : EcbReference.Division,
        };
        return new(EcbReference.QuoteId(feed.Date, symbol, evidence.SourceSha256), feed.Date, null,
            EcbReference.Calculate(evidence.BasePerEuro, evidence.QuotePerEuro)) { Daily = evidence };
    }

    private async Task<Feed> ReadAsync()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(12), time);
        using var request = new HttpRequestMessage(HttpMethod.Get, EcbReference.FeedUrl);
        request.Headers.Accept.ParseAdd("application/xml"); request.Headers.Accept.ParseAdd("text/xml");
        request.Headers.AcceptEncoding.ParseAdd("identity"); request.Headers.UserAgent.ParseAdd("Zanance/1.0");
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
        if (response.StatusCode != HttpStatusCode.OK || response.RequestMessage?.RequestUri?.AbsoluteUri != EcbReference.FeedUrl ||
            response.Content.Headers.ContentEncoding.Count != 0 || response.Content.Headers.ContentLength > MaximumBytes ||
            response.Content.Headers.ContentType?.MediaType is not ("text/xml" or "application/xml"))
            throw new MarketRefusal(502, "MarketSourceResponseRefused");
        await using var input = await response.Content.ReadAsStreamAsync(deadline.Token);
        using var body = new MemoryStream(); var buffer = new byte[4096]; int count;
        while ((count = await input.ReadAsync(buffer, deadline.Token)) != 0)
        {
            if (body.Length + count > MaximumBytes) throw new MarketRefusal(502, "MarketSourceResponseRefused");
            body.Write(buffer, 0, count);
        }
        var fetched = time.GetUtcNow();
        var result = Parse(body.ToArray(), fetched, response.Content.Headers.LastModified?.ToUniversalTime());
        lock (gate)
        {
            // A regressed publication or altered same-date original is not a silently replaceable source set.
            if (cached is { } prior && (result.Date < prior.Date || result.Date == prior.Date && !result.Rates.OrderBy(p => p.Key).SequenceEqual(prior.Rates.OrderBy(p => p.Key))))
                throw new MarketRefusal(502, "MarketSourceResponseRefused");
            cached = result;
        }
        return result;
    }

    private async Task RetireAsync(Task<Feed> original)
    {
        try { await original; } catch (Exception error) when (error is not OutOfMemoryException) { }
        lock (gate) if (ReferenceEquals(pending, original)) pending = null;
    }

    private static Feed Parse(byte[] bytes, DateTimeOffset fetched, DateTimeOffset? modified)
    {
        try
        {
            if (bytes.Length is 0 or > MaximumBytes) throw new XmlException();
            using var stream = new MemoryStream(bytes, writable: false);
            using var reader = XmlReader.Create(stream, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = MaximumBytes,
                MaxCharactersFromEntities = 0, IgnoreComments = true, IgnoreProcessingInstructions = true,
            });
            // Bound depth before constructing a tree, rather than testing after unbounded recursive allocation.
            while (reader.Read()) if (reader.Depth > 4 || reader.AttributeCount > 3 || reader.Value.Length > 256) throw new XmlException();
            stream.Position = 0;
            using var treeReader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = MaximumBytes });
            var document = XDocument.Load(treeReader);
            XNamespace gesmes = "http://www.gesmes.org/xml/2002-08-01";
            XNamespace fx = "http://www.ecb.int/vocabulary/2002-08-01/eurofxref";
            var root = document.Root;
            if (root?.Name != gesmes + "Envelope" || root.Elements().Count() != 3 ||
                root.Element(gesmes + "subject")?.Value != "Reference rates" ||
                root.Element(gesmes + "Sender")?.Element(gesmes + "name")?.Value != "European Central Bank") throw new XmlException();
            var container = root.Element(fx + "Cube") ?? throw new XmlException();
            var dated = container.Elements().Single();
            if (container.Attributes().Any() || dated.Name != fx + "Cube" || dated.Attributes().Count() != 1 ||
                !DateOnly.TryParseExact((string?)dated.Attribute("time"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ||
                date > DateOnly.FromDateTime(fetched.UtcDateTime) || date < DateOnly.FromDateTime(fetched.UtcDateTime).AddDays(-7) ||
                modified is { } changed && changed > fetched.AddMinutes(5)) throw new XmlException();
            var rows = dated.Elements().ToArray(); if (rows.Length is < 5 or > 64) throw new XmlException();
            Dictionary<string, decimal> rates = new(StringComparer.Ordinal) { ["EUR"] = 1m };
            foreach (var row in rows)
            {
                var code = (string?)row.Attribute("currency"); var raw = (string?)row.Attribute("rate");
                if (row.Name != fx + "Cube" || row.HasElements || row.Value.Length != 0 || row.Attributes().Count() != 2 ||
                    code is not { Length: 3 } || !code.All(char.IsAsciiLetterUpper) || raw is not { Length: > 0 and <= 24 } ||
                    raw.Any(c => !char.IsAsciiDigit(c) && c != '.') ||
                    !decimal.TryParse(raw, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value) ||
                    value is <= 0 or > 1_000_000_000m || !rates.TryAdd(code, value)) throw new XmlException();
            }
            foreach (var symbol in EcbReference.Catalog.Symbols) if (!rates.ContainsKey(symbol.Base) || !rates.ContainsKey(symbol.Currency)) throw new XmlException();
            return new(date, rates, new(1, Convert.ToHexStringLower(SHA256.HashData(bytes)), fetched, modified, 1, 1, EcbReference.Direct));
        }
        catch (Exception error) when (error is XmlException or InvalidOperationException or ArgumentException or OverflowException)
        { throw new MarketRefusal(502, "MarketSourceResponseRefused"); }
    }
}
