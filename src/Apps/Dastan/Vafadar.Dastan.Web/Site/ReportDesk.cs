using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Vafadar.Dastan.Core;
using Vafadar.Dastan.Data;
using Vafadar.Web;

namespace Vafadar.Dastan.Web.Site;

/// <summary>
/// Where a visitor reports an error (step 14.8, R-GOV-03). The site takes reports only when it was told to
/// (<c>Dastan:Reports</c>) and keeps them in its database; the editors read them with their tool. A report is the statement,
/// the words and the day: the site keeps nothing about who wrote it.
/// </summary>
public sealed class ReportDesk
{
    /// <summary>The address the form of a report is sent to. It has no language.</summary>
    public const string Address = "/report";

    /// <summary>How many reports one sender (<see cref="Sender"/>) may send in an hour.</summary>
    public const int PerHour = 5;

    private readonly ReportBox? _box;
    private readonly TimeProvider _time;

    // Who sent how many reports in the present hour. It lives in the memory of the process and is thrown away
    // with the hour: an address is never written anywhere.
    private readonly ConcurrentDictionary<string, int> _sent = new(StringComparer.Ordinal);
    private long _hour;

    /// <summary>Initializes a new instance of the <see cref="ReportDesk"/> class.</summary>
    public ReportDesk(IOptions<DastanOptions> options, IHostEnvironment environment, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(environment);
        _time = time;
        if (options.Value.Reports)
        {
            _box = new ReportBox(options.Value.DeliveryDatabase("Dastan:Reports"));
        }
    }

    /// <summary>Gets a value indicating whether the site takes reports.</summary>
    public bool IsOpen => _box is not null;

    /// <summary>Takes a report. Returns what the page says afterwards: <c>thanks</c>, <c>later</c> or <c>full</c>.</summary>
    public string Take(string sender, string collection, int? revision, string language, string statement, string text)
    {
        var now = _time.GetUtcNow();
        var hour = now.ToUnixTimeSeconds() / 3600;
        if (Interlocked.Exchange(ref _hour, hour) != hour)
        {
            _sent.Clear();
        }

        if (_sent.AddOrUpdate(sender, 1, (_, count) => count + 1) > PerHour)
        {
            return "later";
        }

        return _box!.Take(collection, revision, language, statement, text, DateOnly.FromDateTime(now.UtcDateTime)) ? "thanks" : "full";
    }

    /// <summary>Returns what the visitor may see of a statement, or <see langword="null"/>: a report is about a statement its writer could read.</summary>
    public static EvidenceView? Find(Projection projection, string statement)
    {
        ArgumentNullException.ThrowIfNull(projection);
        if (!ReportBox.IsStatement(statement))
        {
            return null;
        }

        var parts = statement.Split('/');
        return (parts[1], parts.Length) switch
        {
            ("entity", 3) => projection.Evidence(new EntityId(parts[2])),
            ("relation", 3) => projection.Evidence(new RelationId(parts[2])),
            ("media", 3) => projection.Evidence(new MediaId(parts[2])),
            ("event", 3) => projection.Evidence(new Core.EventId(parts[2])),
            ("event", 5) => projection.Evidence(new Core.EventId(parts[2]), new EntityId(parts[4])),
            _ => null,
        };
    }
}

/// <summary>The address the form of a report is sent to.</summary>
public static class ReportEndpoint
{
    /// <summary>
    /// Maps the endpoint of the form: the fields <c>collection</c>, <c>statement</c>, <c>text</c> and
    /// <c>returnUrl</c>, and <c>website</c>, which a person does not see and leaves empty.
    /// </summary>
    public static IEndpointConventionBuilder MapVisitorReports(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost(ReportDesk.Address, (
            HttpContext context,
            DastanLibrary library,
            ReportDesk desk,
            Visitor visitor,
            [FromForm] string? collection,
            [FromForm] string? statement,
            [FromForm] string? text,
            [FromForm] string? website,
            [FromForm] string? returnUrl) =>
        {
            if (!desk.IsOpen)
            {
                return Results.NotFound();
            }

            // A report is about a statement of this site that its writer could read, in words of a length a form allows.
            var words = (text ?? string.Empty).Trim();
            if (library.Find(collection) is not { } site
                || words.Length is 0 or > ReportBox.LongestText
                || visitor.Of(site) is not { } projection
                || ReportDesk.Find(projection, statement ?? string.Empty) is null)
            {
                return Results.BadRequest();
            }

            // A program that fills in every field has filled in the one a person does not see: it is thanked and
            // nothing is kept.
            var outcome = string.IsNullOrEmpty(website)
                ? desk.Take(Sender.Of(context), site.Collection, site.Revision?.Number, visitor.Language.Tag, statement!, words)
                : "thanks";
            var back = ReturnAddress.Safe(returnUrl);
            var end = back.IndexOf('#', StringComparison.Ordinal);
            back = end < 0 ? back : back[..end];
            return Results.Redirect($"{back}{(back.Contains('?', StringComparison.Ordinal) ? '&' : '?')}reported={outcome}#report");
        });
}
