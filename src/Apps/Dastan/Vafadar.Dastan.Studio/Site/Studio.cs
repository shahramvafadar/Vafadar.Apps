using System.Globalization;
using System.Net;
using Microsoft.Extensions.Options;
using Vafadar.Core.Text;
using Vafadar.Dastan.Store;
using Vafadar.Web;

namespace Vafadar.Dastan.Studio.Site;

/// <summary>Where the store of the editors lies.</summary>
public sealed class StudioOptions
{
    /// <summary>Gets or sets the file of the store. The tool does not start without it.</summary>
    public string Store { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the folder in which the files of the collections lie (<c>content/samples</c>), from which the
    /// store looks for the files a media item names. Empty, the store does not look for files.
    /// </summary>
    public string Files { get; set; } = string.Empty;
}

/// <summary>
/// The store of the tool: the file it was told, brought to the form this program knows when the tool starts, and
/// opened anew for every request.
/// </summary>
public sealed class StudioStore
{
    private readonly string _file;
    private readonly string? _files;
    private readonly TimeProvider _time;

    /// <summary>Opens the store once, so that a store that cannot be opened stops the tool at its start.</summary>
    public StudioStore(IOptions<StudioOptions> options, TimeProvider? time = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.Value.Store))
        {
            throw new InvalidOperationException("The tool of the editors needs the file of its store: set Studio:Store (the environment variable Studio__Store) to the file.");
        }

        _file = Path.GetFullPath(options.Value.Store);
        _files = string.IsNullOrWhiteSpace(options.Value.Files) ? null : Path.GetFullPath(options.Value.Files);
        _time = time ?? TimeProvider.System;
        using var store = EditorialStore.Open(_file, _time, _files);
    }

    /// <summary>Opens the store for one request.</summary>
    public EditorialStore Open() => EditorialStore.Open(_file, _time, _files, migrate: false);
}

/// <summary>Who looks at a page of the tool: the language and the appearance they chose.</summary>
public sealed class Editor
{
    /// <summary>Reads the language and the appearance of the request.</summary>
    public Editor(IHttpContextAccessor accessor)
    {
        ArgumentNullException.ThrowIfNull(accessor);
        var context = accessor.HttpContext ?? throw new InvalidOperationException("An editor exists during a request only.");
        Language = context.GetSiteLanguage();
        Theme = ThemePreference.Read(context);
    }

    /// <summary>Gets the language of the page.</summary>
    public SiteLanguage Language { get; }

    /// <summary>Gets the appearance the editor chose.</summary>
    public ThemeChoice Theme { get; }

    /// <summary>Writes a number with the digits of the language of the page.</summary>
    public string Number(int value)
    {
        var digits = value.ToString(CultureInfo.InvariantCulture);
        return Language.Tag == "fa" ? Digits.ToPersian(digits) : digits;
    }

    /// <summary>Writes a moment of the store – kept in universal time – as a day and a time.</summary>
    public static string Moment(DateTime at) => at.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + " UTC";
}

/// <summary>Keeps the tool to the machine it runs on.</summary>
public static class OnlyThisMachine
{
    /// <summary>
    /// Answers a request that comes from this machine and refuses every other one. The tool shows what is not
    /// published and has no accounts (ADR 0011): until it knows who an editor is, where it runs is its only guard.
    /// </summary>
    public static IApplicationBuilder UseOnlyThisMachine(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.Use(async (context, next) =>
        {
            // A request without an address comes from inside the process – the tests of the tool.
            if (context.Connection.RemoteIpAddress is { } address && !IPAddress.IsLoopback(address))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("The tool of the editors answers the machine it runs on only.").ConfigureAwait(false);
                return;
            }

            await next(context).ConfigureAwait(false);
        });
    }
}
