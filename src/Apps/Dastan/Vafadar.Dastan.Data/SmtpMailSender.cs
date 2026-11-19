using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Vafadar.Dastan.Data;

/// <summary>How the mail of the site leaves it: the server that takes it and who the site says it is.</summary>
public sealed class MailOptions
{
    /// <summary>Gets or sets the name of the SMTP server. Empty, the site sends no mail and what needs mail is refused or said to be unavailable.</summary>
    public string? Host { get; set; }

    /// <summary>Gets or sets the port of the server: 587 with STARTTLS as a rule, 465 with TLS from the start, 1025 for a catcher on a development machine.</summary>
    public int Port { get; set; } = 587;

    /// <summary>
    /// Gets or sets how the connection is protected: <c>StartTls</c> (the default; refuses a server that does
    /// not offer it), <c>Tls</c> from the start, or <c>None</c> – for a catcher on the same machine only.
    /// </summary>
    public string Security { get; set; } = "StartTls";

    /// <summary>Gets or sets the name the site signs in to the server with, when the server asks for one.</summary>
    public string? User { get; set; }

    /// <summary>Gets or sets the password for that name. A secret: it is told to the running site and never written into a file of the repository.</summary>
    public string? Password { get; set; }

    /// <summary>Gets or sets the address the mail of the site comes from.</summary>
    public string? From { get; set; }

    /// <summary>Gets or sets the name shown beside that address.</summary>
    public string FromName { get; set; } = "Dastan";

    /// <summary>Gets a value indicating whether the site can send mail: a server and an address to send from are named.</summary>
    public bool CanSend => !string.IsNullOrWhiteSpace(Host) && MailOutbox.IsAddress(From);

    /// <summary>Gets the name after the <c>@</c> of the address the mail comes from, which also names its messages.</summary>
    public string Domain => From is { } from && from.IndexOf('@', StringComparison.Ordinal) is > 0 and var at ? from[(at + 1)..].Trim() : "localhost";
}

/// <summary>
/// Sends a message by SMTP (ADR 0016) to whatever server the settings name. Which service that is, is a
/// setting of who runs the site and changes no code. A message goes to one address, is written as plain text
/// and – when it has one – as HTML beside it, and says the language it is in.
/// </summary>
public sealed class SmtpMailSender : IMailSender
{
    private readonly MailOptions _options;

    /// <summary>Initializes a new instance of the <see cref="SmtpMailSender"/> class.</summary>
    public SmtpMailSender(MailOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!options.CanSend)
        {
            throw new ArgumentException("Mail needs a server (Host) and an address to send from (From).", nameof(options));
        }

        _options = options;
    }

    /// <inheritdoc />
    public void Send(OutgoingMail mail, string messageId)
    {
        ArgumentNullException.ThrowIfNull(mail);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        using var message = Message(mail, messageId, _options);
        using var client = new SmtpClient { Timeout = 20_000 };
        var security = _options.Security.ToUpperInvariant() switch
        {
            "NONE" => SecureSocketOptions.None,
            "TLS" => SecureSocketOptions.SslOnConnect,
            _ => SecureSocketOptions.StartTls,
        };
        client.Connect(_options.Host!, _options.Port, security);
        if (!string.IsNullOrEmpty(_options.User))
        {
            client.Authenticate(_options.User, _options.Password ?? string.Empty);
        }

        client.Send(message);
        client.Disconnect(quit: true);
    }

    /// <summary>Returns a message as it is sent: its head, its plain text and, beside it, its HTML.</summary>
    public static MimeMessage Message(OutgoingMail mail, string messageId, MailOptions options)
    {
        ArgumentNullException.ThrowIfNull(mail);
        ArgumentNullException.ThrowIfNull(options);
        var message = new MimeMessage { Subject = mail.Subject, MessageId = messageId.Trim('<', '>') };
        message.From.Add(new MailboxAddress(options.FromName, options.From!));
        message.To.Add(MailboxAddress.Parse(mail.To));
        message.Headers.Add(HeaderId.ContentLanguage, mail.Language);

        // Nobody answers this mail automatically, and a server that does knows not to answer it.
        message.Headers.Add("Auto-Submitted", "auto-generated");
        var body = new BodyBuilder { TextBody = mail.Text, HtmlBody = mail.Html };
        message.Body = body.ToMessageBody();
        return message;
    }
}
