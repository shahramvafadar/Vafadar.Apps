# Monetization

> **Zanance (2026-10-07, D-61):** Free → Plus → Pro with Plus Lifetime replaces the "Free + Pro one-time" model for
> Zanance; design in `src/Apps/Zanance/docs/enhancements/2026-10-commercial-release/`. The store-policy rules below
> still apply; the planned library will follow the entitlement architecture described there.

Apps are primarily for personal use. The generic models below describe possible choices for other apps. Zanance
follows the approved D-61 plan model and the canonical ZCR backlog; the older generic Pro-unlock example is not its
product contract. Store-policy review remains required when implementing or releasing purchases.

## Models

| Model | Description |
|---|---|
| Free | Everything free. Optionally a tip jar. |
| Free + Pro | Core features free; a one-time in-app purchase unlocks Pro features. |
| Tip jar | Consumable in-app products ("Small coffee", "Large coffee") that unlock nothing. |
| Zanance (D-61) | Free; Plus monthly/yearly or local-only Plus Lifetime; Pro monthly/yearly for implemented online services. Store products, quotas and sale activation remain future work. |

No advertising is planned. Ads would add third-party SDKs that collect data, which conflicts with the privacy goals.

## Store policies (verify before every release)

* **Google Play** (Payments policy) and **Apple** (App Review Guideline 3.1.1) require their own billing systems for
  digital goods and features sold inside the app. Tips/donations to the developer inside the app are treated as
  in-app purchases as well.
* Therefore: **inside the app**, Pro and tips use Google Play Billing / StoreKit. **External** donation links
  (GitHub Sponsors, Buy Me a Coffee, PayPal) appear only on the website `vafadar.pro` and in README files, not in
  the app.
* Policies change and differ by country; check the current Play and App Store rules when implementing and before each
  release that touches purchases.

## Planned store integration (not implemented)

For Zanance, the approved design is
[src/Apps/Zanance/docs/enhancements/2026-10-commercial-release/03-architecture.md](../../src/Apps/Zanance/docs/enhancements/2026-10-commercial-release/03-architecture.md).
It separates plan, purchase kind, verified lifecycle state, quota policy and offline entitlement. A product-id Boolean
alone is not the entitlement contract. No store adapter or shared monetization project exists yet; extraction must
follow the repository sharing rules.

* Android: Google Play Billing Library (via a .NET binding or a maintained plugin); iOS: StoreKit.
* Product ids/catalog are chosen per app and store during implementation. Zanance has Plus monthly/yearly/Lifetime
  and Pro monthly/yearly in its design; there is no Pro Lifetime. Tip products are a separate other-app option.
* Zanance's planned verified entitlement cache must handle expiry, revocation, store/server failures and clock changes
  per ZCR-BIL-02/03; a local flag does not prove a purchase.
* Windows: free, or Microsoft Store add-ons if the app is ever published there.
* Before billing/backend implementation, update the privacy matrix with actual store and server data flows; do not
  assume purchase history remains solely with the store.

## UX rules

* Never block access to the user's own data behind a purchase (export and backup always work).
* A tip is clearly labelled as a thank-you that unlocks nothing.
* Show store prices as returned by the store (already localized and in the user's currency).
