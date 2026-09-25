# Monetization

Apps are primarily for personal use. Some are published for free, some as "free + Pro", and some offer a way to
thank the developer ("buy me a coffee"). This document records how that is done without violating store policies.

## Models

| Model | Description |
|---|---|
| Free | Everything free. Optionally a tip jar. |
| Free + Pro | Core features free; a one-time in-app purchase unlocks Pro features. |
| Tip jar | Consumable in-app products ("Small coffee", "Large coffee") that unlock nothing. |

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

## Planned library: Vafadar.Monetization

```text
IStoreBilling
├── GetProductsAsync(productIds)     localized price and title from the store
├── PurchaseAsync(productId)         starts the store purchase flow
├── RestorePurchasesAsync()          required by Apple, useful on new devices
└── IsEntitled(productId)            cached entitlement (works offline)
```

* Android: Google Play Billing Library (via a .NET binding or a maintained plugin); iOS: StoreKit.
* Product ids per app, e.g. `pro_unlock` (non-consumable), `tip_small` / `tip_large` (consumable).
* Entitlements are cached locally so Pro features work offline; purchases are re-validated with the store when online.
* Windows: free, or Microsoft Store add-ons if the app is ever published there.
* Purchase data is handled by the stores; the privacy matrix records "purchase history – handled by Google/Apple".

## UX rules

* Never block access to the user's own data behind a purchase (export and backup always work).
* A tip is clearly labelled as a thank-you that unlocks nothing.
* Show store prices as returned by the store (already localized and in the user's currency).
