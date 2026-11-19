# 0016. Accounts on ASP.NET Core Identity, with a confirmed mail address

* Status: Accepted (D-077); takes the place of the accounts of [0014](0014-visitor-accounts.md) and of the editors' accounts of [0011](0011-editors-store.md)
* Date: 2026-11-19
* Step: 19.4 of the [delivery plan](../delivery-plan.md)

## Context

An account of a visitor was a name and a password with recovery codes, and nothing else: no mail address,
because no service to send mail had been chosen. That was a mistake of reasoning – which service sends mail
is a setting, whether an account has an address is part of what an account is. Without an address nobody can
be told anything, a lost password is a lost account, and an invitation is a link somebody has to carry. The
visitors and the editors also each had their own code for passwords, stamps and lock-outs.

## Decision

* **ASP.NET Core Identity** keeps passwords, tokens, lock-outs and sign-ins for both kinds of account, in the
  schema `identity`. Visitors and editors are **two kinds of user in two sets of tables with two cookies**; an
  editor is made by the operator and is never made, merged or given a role because a name or an address is
  the same as a visitor's.
* **A mail address is required, unique and confirmed.** An account that has not confirmed can sign in only to
  confirm or to have the mail sent again; nothing else of the site treats it as an account. The address is
  never shown to anybody else.
* **The name stays** what others see: chosen, unique, in any script. The password has at least 12 characters;
  five wrong ones pause sign-in for 15 minutes; another password signs out everywhere.
* **The journeys**: register, confirm, send again, sign in, forgotten password, another address (confirmed at
  the new one, told to the old one), recovery codes as a second way, close and think it over for 30 days,
  closed by the operator with the reasons – on the page after proof of who asks, and by mail.
* **Google and Microsoft** are asked for who somebody is and for the address they confirmed, nothing else.
  A first sign-in makes an account with that address. An address that an account already has does not sign
  anybody in: the visitor signs in to that account and links the provider there. An account that has only a
  provider proves itself through it – also to read why the operator closed it.
* **Mail leaves through an outbox**: a row in `delivery` written in the same transaction as what it tells of,
  sent by a service of the site with retries that wait longer each time, never twice (a key per message),
  with what failed kept for the operator. The sender is `IMailSender`; the one of the product speaks SMTP
  (MailKit) to whatever service the settings name – choosing a service is the owner's and changes no code.
  On a developer's machine and in the tests a mail catcher of the Compose project receives everything.
* **Texts of mail** are written in both languages, as plain text and as simple HTML with the direction of the
  language, and say only what the receiver needs; a link in one is used once and for a short time.
* **Invitations** go by mail and still as a link. What the trial asked for – a site by invitation only – stays
  a setting.
* **Nothing is carried over** (D-080). The legal texts and the records of processing are written again for
  accounts with an address (step 19.10).

## What is built

**The mail (2026-11-19, first part of step 19.4).** The outbox is the table `delivery."Mail"`
(`MailOutbox` in `Vafadar.Dastan.Data`): a message has a key and is kept once; it can be written inside the
transaction of what it tells of; what is due is taken with a lock of its row, so several senders never take
the same message; a failure waits 1, 5, 30 minutes, 2, 6, 12, 24 hours and is given up at the eighth try; the
text of a sent message is emptied and its record forgotten after 30 days. `SmtpMailSender` speaks SMTP with
MailKit 4.18.2 (MIT) – plain text and HTML side by side, the language in the head, the same Message-ID on
every try. The site sends every five seconds (`MailPump`) where `Dastan:Mail` names a server, and does not
start with half of the settings. The operator lists, retries and forgets with the command `mail`. Mailpit
1.33.0 (MIT), pinned by its digest, catches the mail of a development machine and of the tests, in Compose and
in CI. "Never twice" holds for every hand-over and for every sender at the same moment; a site that stops
between a server taking a message and that being written down sends it once more under the same identifier –
said in the operations guide.

**Not built yet:** everything that writes mail – the accounts on Identity, the journeys, the texts of the
messages in both languages, invitations and statements of reasons by mail, the editors' accounts. The
delivery plan lists the parts of step 19.4.

## Consequences

The site sends mail, so it needs a sender with a domain that receivers trust (SPF, DKIM, DMARC) before it is
public – the owner's part. The store of the visitors loses its own code for passwords. The rules of Dastan –
invitations, claims, consent, circles, publication – stay Dastan's and are not Identity's.
