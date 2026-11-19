using Microsoft.Extensions.Options;
using Vafadar.Dastan.Data;

namespace Vafadar.Dastan.Web.Site;

/// <summary>
/// Sends the mail of the site that waits (ADR 0016): every few seconds what is due, as long as the site runs.
/// It is there only where the site was told a server for mail and has a database; several sites on one
/// database may each have one – a message is taken by one of them. A failure of the server or of the database
/// is tried again at the next round and never stops the site.
/// </summary>
public sealed partial class MailPump : BackgroundService
{
    /// <summary>How long the pump rests between two looks at what is due.</summary>
    public static readonly TimeSpan Rest = TimeSpan.FromSeconds(5);

    private readonly MailOptions _mail;
    private readonly string _database;
    private readonly TimeProvider _time;
    private readonly ILogger<MailPump> _log;

    /// <summary>Initializes a new instance of the <see cref="MailPump"/> class.</summary>
    public MailPump(IOptions<DastanOptions> options, TimeProvider time, ILogger<MailPump> log)
    {
        ArgumentNullException.ThrowIfNull(options);
        _mail = options.Value.Mail;
        _database = options.Value.Database ?? string.Empty;
        _time = time;
        _log = log;
    }

    /// <summary>Sends what is due now, once. Returns how many messages were sent.</summary>
    public int Round() => new MailOutbox(_database).Deliver(new SmtpMailSender(_mail), _time.GetUtcNow().UtcDateTime, _mail.Domain);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                Round();
            }
#pragma warning disable CA1031 // Whatever a round fails with – the database away for a moment – the next round tries again.
            catch (Exception failed)
#pragma warning restore CA1031
            {
                // What kind of failure, never its text: it can repeat an address or a connection.
                RoundFailed(_log, failed.GetType().Name);
            }

            try
            {
                await Task.Delay(Rest, _time, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "A round of sending mail failed ({Kind}); it is tried again.")]
    private static partial void RoundFailed(ILogger log, string kind);
}
