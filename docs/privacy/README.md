# Privacy

Privacy is a design goal of every Vafadar app, not an afterthought. It also makes store compliance (Google Play
Data safety, Apple App Privacy) straightforward.

## Principles

1. **Local-first.** User data stays on the device by default.
2. **No developer servers** for apps that do not need them. The developer cannot see user data.
3. **User-owned backups.** Backups go to the user's own Google Drive or OneDrive, with the narrowest scopes
   (the app's own folder), optionally encrypted with a password only the user knows.
4. **No ads, no analytics, no tracking SDKs** unless explicitly decided for an app and documented in the matrix.
5. **Minimal permissions.** Every Android/iOS permission is justified in the matrix.
6. **Transparency.** One public privacy policy, plus a per-app matrix that maps exactly what each app does.

## Documents

| Document | Purpose |
|---|---|
| [privacy-policy.md](privacy-policy.md) | The public privacy policy text (source for `vafadar.pro/privacy`) |
| [privacy-matrix.md](privacy-matrix.md) | Per-app data inventory, mapped to Google Play Data safety and Apple privacy labels |

## Process

* Any change that collects, stores, sends or shares a new kind of data (or adds a permission or SDK) updates the
  privacy matrix in the same pull request (the PR template asks for it).
* Before each store release, compare the matrix with the Play Data safety form and the App Store privacy details.
* The policy is published on `vafadar.pro` (e.g. GitHub Pages or the website of the domain) at a stable URL that is
  entered in the store listings and in the Google OAuth consent screen.

> These documents are a technical description and a starting point, not legal advice. Have the final policy
> reviewed if an app processes data beyond what is described here or is offered in regions with specific rules.
