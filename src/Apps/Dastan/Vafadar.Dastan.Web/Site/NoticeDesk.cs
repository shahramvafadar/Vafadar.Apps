using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Vafadar.Dastan.Data;
using Vafadar.Web;

namespace Vafadar.Dastan.Web.Site;

/// <summary>
/// Where somebody tells who runs the site that something on it is against the law or against its terms: the
/// mechanism Article 16 of Regulation (EU) 2022/2065 asks of whoever keeps what others wrote. Anybody can give
/// a notice, with or without an account. The site keeps it in a folder (<see cref="DastanOptions.Notices"/>),
/// tells its reference at once, and does nothing else by itself: a person reads it and decides. A site that was
/// told no folder names the mail address of its operator instead.
/// </summary>
public sealed class NoticeDesk
{
    /// <summary>How many notices one sender may give in an hour.</summary>
    public const int PerHour = 5;

    /// <summary>The address of the page with the form, without a language.</summary>
    public const string Page = "/notice";

    private readonly NoticeBox? _box;
    private readonly TimeProvider _time;

    // Who gave how many notices in the last hour, in the memory of the process: an address is never written anywhere.
    private readonly RequestWindow _sent;

    /// <summary>Initializes a new instance of the <see cref="NoticeDesk"/> class.</summary>
    public NoticeDesk(IOptions<DastanOptions> options, IHostEnvironment environment, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(environment);
        _time = time;
        _sent = new RequestWindow(time);
        if (options.Value.Notices)
        {
            _box = new NoticeBox(options.Value.DeliveryDatabase("Dastan:Notices"));
        }
    }

    /// <summary>Gets a value indicating whether the site takes notices with its form.</summary>
    public bool TakesNotices => _box is not null;

    /// <summary>Keeps a notice and returns its reference; <c>later</c> for a sender who gave many, <c>full</c> for a box that takes no more.</summary>
    public string Take(string sender, string language, string address, string kind, string explanation, string? name, string? mail)
    {
        var now = _time.GetUtcNow();
        if (!_sent.Allows(sender, PerHour))
        {
            return "later";
        }

        return _box!.Take(language, address, kind, explanation, name, mail, goodFaith: true, now.UtcDateTime) ?? "full";
    }

    /// <summary>Maps the form of a notice.</summary>
    public static IEndpointRouteBuilder MapContentNotices(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapPost("/preferences/report-content", (
            HttpContext context,
            NoticeDesk desk,
            [FromForm] string? language,
            [FromForm] string? address,
            [FromForm] string? kind,
            [FromForm] string? explanation,
            [FromForm] string? name,
            [FromForm] string? mail,
            [FromForm] string? website,
            [FromForm(Name = "good-faith")] string? goodFaith) =>
        {
            var tag = language is "fa" ? "fa" : "en";
            var page = $"/{tag}{Page}";
            if (!desk.TakesNotices)
            {
                return Results.NotFound();
            }

            // A program that fills in every field has filled in the one a person does not see.
            if (!string.IsNullOrEmpty(website))
            {
                return Results.Redirect($"{page}?problem=later#form");
            }

            var problems = NoticeBox.Problems(address, kind, explanation, name, mail, goodFaith == "on");
            if (problems.Count > 0)
            {
                return Results.Redirect($"{page}?problem={string.Join(',', problems)}#form");
            }

            var taken = desk.Take(Sender.Of(context), tag, address!, kind!, explanation!, name, mail);
            return taken is "later" or "full"
                ? Results.Redirect($"{page}?problem={taken}#form")
                : Results.Redirect($"{page}?received={taken}#received");
        });
        return endpoints;
    }
}
