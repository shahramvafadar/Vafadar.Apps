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

    /// <summary>
    /// Gets or sets whether the tool has accounts (step 11.7). Without them it answers the machine it runs on
    /// only and takes the name an editor types; with them it answers other machines, shows nothing to somebody
    /// who has not signed in, and keeps every act under the name of the account.
    /// </summary>
    public bool Accounts { get; set; }

    /// <summary>Gets or sets the folder of the keys of the sign-in, so that a sign-in outlives the process.</summary>
    public string Keys { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the folder the site keeps the reports of its visitors in (step 14.8). Empty, the tool shows
    /// no reports.
    /// </summary>
    public string Reports { get; set; } = string.Empty;

    /// <summary>Gets the proxies in front of the tool that are believed about how an editor came.</summary>
    public IList<string> TrustedProxies { get; } = [];
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
    public StudioStore(IOptions<StudioOptions> options, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.Value.Store))
        {
            throw new InvalidOperationException("The tool of the editors needs the file of its store: set Studio:Store (the environment variable Studio__Store) to the file.");
        }

        _file = Path.GetFullPath(options.Value.Store);
        _files = string.IsNullOrWhiteSpace(options.Value.Files) ? null : Path.GetFullPath(options.Value.Files);
        ArgumentNullException.ThrowIfNull(time);
        _time = time;
        using var store = EditorialStore.Open(_file, _time, _files);
    }

    /// <summary>Opens the store for one request.</summary>
    public EditorialStore Open() => EditorialStore.Open(_file, _time, _files, migrate: false);
}

/// <summary>Who looks at a page of the tool: the language and the appearance they chose.</summary>
public sealed class Editor
{
    /// <summary>Reads the language and the appearance of the request.</summary>
    public Editor(IHttpContextAccessor accessor, TimeProvider time, IOptions<StudioOptions> options, EditorialStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(time);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(accessor);
        var context = accessor.HttpContext ?? throw new InvalidOperationException("An editor exists during a request only.");
        Language = context.GetSiteLanguage();
        Theme = ThemePreference.Read(context);
        Accounts = options.Value.Accounts;
        Account = Accounts ? StudioAccess.Session(context.User)?.Name : null;
        Name = Account ?? EditorName.Read(context);

        // A tool without accounts runs on the machine of its one editor, who may do everything.
        Roles = !Accounts ? EditorRoles.All : Account is null ? EditorRoles.None : store.RolesOf(Account);
        Today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
    }

    /// <summary>Gets whether the tool has accounts.</summary>
    public bool Accounts { get; }

    /// <summary>Gets the name of the account the editor signed in with; nothing in a tool without accounts.</summary>
    public string? Account { get; }

    /// <summary>Gets what the editor may do (step 13.3).</summary>
    public EditorRoles Roles { get; }

    /// <summary>Returns whether the editor may do what that role does.</summary>
    public bool May(EditorRoles role) => (Roles & role) == role;

    /// <summary>Gets the name a form starts with: the account, or the name the editor gave with their last act.</summary>
    public string? Name { get; }

    /// <summary>
    /// Returns the name an act is kept with: the account of the editor – whatever a form says – or, in a tool
    /// without accounts, the name the form was given.
    /// </summary>
    public string? By(string? given) => Account ?? ActInput.Text(given);

    /// <summary>Gets the day on the machine of the tool, to start a form with.</summary>
    public DateOnly Today { get; }

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
