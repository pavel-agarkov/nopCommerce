# Evidence: history

Source: `.work/git-log.txt` (git, `--since=2022-01-01 --no-merges --name-status -M`), attributed with `.work/attribute.mjs` + `capabilities.tsv`. Scripts: `.work/hist.mjs`, `h2`..`h6.mjs`.

- Window 2022-01-01..HEAD 5b31db5b27 (effective 2021-12-29..2026-10-16), buckets = calendar quarters by author date (4 commits dated 2021 appear in 21Q1/Q2/Q4).
- Commits in log 1970; **counted 1955** (15 mass commits from excluded-commits.txt dropped). **Without the 18 sweeping commits: 1937.** Noise filter (lockfiles, media, `*.min.*`, bin/obj) applied: 3 commits end up with no files.
- File-changes counted: **19,889** with sweeping commits, **10,705** without. A change = one file in one commit (new path for renames); a commit counts once per capability it touches.
- Unmapped: **421 changes (2.1%)**, 60 commits; all are files of deleted plugins: What3words 111, Payments.Manual 97, AccessiBe 94, PayPalStandard 41, ShipStation 40, EasyPost 28, Majako.* 8, other 2. (evidence) They describe removed code, not live risk.
- Agent-authored: 0. Two commits carry a `Claude Opus 4.8` co-author trailer (human commits); a third trailer match is a human named Claude. Bot commits: 0.
- Labels: **evidence** = computed this session; **inference** = interpretation; **unknown** = not shown by git history.

## Activity per capability

Rank shown both ways. Because a commit counts once per touched capability, commit shares sum to more than 100%.

| capability | changes | % changes | commits | % commits | changes w/o sweeping | commits w/o sweeping | rank (all / w/o sweep) |
|---|---|---|---|---|---|---|---|
| foundations | 2105 | 10.6 | 658 | 33.7 | 1283 | 642 | 1 / 1 |
| catalog | 1993 | 10.0 | 336 | 17.2 | 1035 | 322 | 2 / 3 |
| ops | 1891 | 9.5 | 437 | 22.4 | 961 | 423 | 3 / 4 |
| platform | 1550 | 7.8 | 556 | 28.4 | 1211 | 541 | 4 / 2 |
| identity | 1390 | 7.0 | 251 | 12.8 | 612 | 238 | 5 / 8 |
| messaging | 1340 | 6.7 | 213 | 10.9 | 799 | 200 | 6 / 5 |
| frontend-ui | 1338 | 6.7 | 238 | 12.2 | 570 | 221 | 7 / 10 |
| orders | 1312 | 6.6 | 264 | 13.5 | 654 | 250 | 8 / 6 |
| content | 1272 | 6.4 | 169 | 8.6 | 575 | 156 | 9 / 9 |
| admin-ui | 1034 | 5.2 | 304 | 15.5 | 642 | 288 | 10 / 7 |
| payments | 923 | 4.6 | 101 | 5.2 | 379 | 88 | 11 / 12 |
| integrations | 688 | 3.5 | 85 | 4.3 | 489 | 74 | 12 / 11 |
| shipping | 588 | 3.0 | 76 | 3.9 | 237 | 64 | 13 / 14 |
| tax | 539 | 2.7 | 85 | 4.3 | 222 | 72 | 14 / 15 |
| localization | 439 | 2.2 | 82 | 4.2 | 123 | 69 | 15 / 18 |
| unmapped | 421 | 2.1 | 60 | 3.1 | 308 | 51 | - |
| discounts | 291 | 1.5 | 60 | 3.1 | 148 | 48 | 17 / 17 |
| media | 283 | 1.4 | 79 | 4.0 | 186 | 68 | 18 / 16 |
| vendors | 271 | 1.4 | 48 | 2.5 | 85 | 36 | 19 / 20 |
| search | 129 | 0.6 | 41 | 2.1 | 94 | 29 | 20 / 19 |
| ai | 72 | 0.4 | 11 | 0.6 | 72 | 11 | 21 / 21 |
| docs | 20 | 0.1 | 19 | 1.0 | 20 | 19 | 22 / 22 |

- (evidence) The sweeping commits are 46% of all file-changes (9,184 of 19,889). Removing them moves identity from rank 5 to 8, frontend-ui from 7 to 10, localization from 15 to 18 and platform from 4 to 2. Top-4 membership (foundations, catalog, ops, platform) is stable either way.
- (inference) The four catch-all buckets (foundations, platform, ops, admin-ui, frontend-ui) hold the most commits by construction (shared Core/Framework/Data/Services); they are not business capabilities. Among domain capabilities the order is **catalog > identity/messaging/orders/content > payments > integrations**.
- (evidence) Mechanical file types inside the non-sweeping set (10,705 changes): localization catalog/migration files 705 (6.6%), `plugin.json` 495 (4.6%), `.csproj` 423 (4.0%), DefaultClean `styles(.rtl).css` 161. `plugin.json` + `.csproj` make up 116/799 of messaging, 97/489 of integrations, 78/379 of payments, 66/237 of shipping, 57/222 of tax, 122/1283 of foundations: version-bump churn inflates the plugin-heavy capabilities (false-signals rules 3 and 7).

## Top files in top capabilities

Non-sweeping set, files per commit count.

| capability | top files (commits touching) |
|---|---|
| foundations | `Nop.Web.Framework/Migrations/UpgradeTo460/LocalizationMigration.cs` 61; `Nop.Tests/BaseNopTest.cs` 54; `UpgradeTo470/LocalizationMigration.cs` 51; `UpgradeTo490/LocalizationMigration.cs` 49; `Nop.Core.csproj` 45 |
| catalog | `Admin/Controllers/ProductController.cs` 48; `Nop.Services/Catalog/ProductService.cs` 44; `Factories/ProductModelFactory.cs` 40; `Factories/CatalogModelFactory.cs` 32; `Nop.Core/Domain/Catalog/CatalogSettings.cs` 22 |
| ops | `Services/Installation/InstallationService.cs` 77; `Nop.Services.csproj` 73; `Installation/InstallRequiredData.cs` 63; `ExportImport/ImportManager.cs` 47; `ExportManager.cs` 35 |
| platform | `App_Data/Localization/defaultResources.nopres.xml` 221; `Nop.Data.csproj` 51; `NopCommerce.sln` 33; `Nop.Web/package.json` 28; `Nop.Data/DataProviders/BaseDataProvider.cs` 26 |
| identity | `Controllers/CustomerController.cs` 32; `Services/Customers/CustomerService.cs` 24; `Factories/CustomerModelFactory.cs` 21; plugin.json of ExternalAuth.Facebook 21 and GoogleAuthenticator 18 |
| messaging | `Services/Messages/MessageTokenProvider.cs` 40; `WorkflowMessageService.cs` 24; plugin.json of Brevo 22, FacebookPixel 20, Omnisend 18 |
| frontend-ui | `Themes/DefaultClean/Content/css/styles.rtl.css` 57; `styles.css` 54; `wwwroot/css/admin/styles.css` 19; `Factories/CommonModelFactory.cs` 19 |
| orders | `Services/Orders/OrderProcessingService.cs` 38; `ShoppingCartService.cs` 31; `Controllers/CheckoutController.cs` 29; `ShoppingCartController.cs` 23; `ShoppingCartModelFactory.cs` 20 |
| content | `Framework/Menu/AdminMenu.cs` 14; `Views/Boards/Topic.cshtml` 12; plugin.json Swiper 11; `Views/Blog/BlogPost.cshtml` 9 |
| admin-ui | `Admin/Infrastructure/Mapper/AdminMapperConfiguration.cs` 92; `Admin/Controllers/SettingController.cs` 73; `Admin/Factories/SettingModelFactory.cs` 59 |
| payments | PayPalCommerce `plugin.json` 44, `Services/PayPalCommerceServiceManager.cs` 19, `PayPalCommercePaymentMethod.cs` 9 |
| integrations | RFQ `plugin.json` 24, Zettle plugin.json 19, WebApi.Frontend plugin.json 12, `RfqService.cs` 9 |

- (inference) Real logic hot spots: `OrderProcessingService`, `ShoppingCartService`, `ProductService`, `ProductController`, `CustomerService`, `MessageTokenProvider`, `InstallationService`/`InstallRequiredData`, `ImportManager`/`ExportManager`, `SettingController`+`SettingModelFactory`, `AdminMapperConfiguration`, PayPalCommerce service manager.
- (evidence) `defaultResources.nopres.xml` (221 commits) and `LocalizationMigration.cs` per release (235 commits touch any) are lockstep translation corridors, see Hubs.

## Trend

Commits per quarter per capability (counted set, 1955; cross-cutting commits counted for each capability). Full table: `.work/h3.mjs` output; yearly view (non-sweeping commits) below.

| capability | 2022 | 2023 | 2024 | 2025 | 2026 (to Oct) | label |
|---|---|---|---|---|---|---|
| ALL | 411 | 365 | 409 | 451 | 297 | seasonal, flat |
| foundations | 148 | 130 | 120 | 142 | 101 | constant |
| platform | 116 | 112 | 120 | 112 | 80 | constant |
| ops | 83 | 82 | 84 | 96 | 78 | constant |
| catalog | 64 | 61 | 63 | 92 | 40 | constant, 2025 spike |
| admin-ui | 67 | 50 | 46 | 75 | 48 | seasonal |
| orders | 53 | 62 | 44 | 55 | 33 | constant |
| identity | 40 | 52 | 56 | 55 | 35 | constant |
| messaging | 30 | 35 | 50 | 52 | 32 | rising |
| content | 28 | 26 | 24 | 48 | 29 | rising in 2025 |
| frontend-ui | 40 | 40 | 41 | 75 | 24 | spike 2025, fading 2026 |
| payments | 9 | 14 | 31 | 28 | 6 | spike 2024-25, fading |
| integrations | 4 | 6 | 17 | 32 | 15 | **rising** |
| shipping | 2 | 12 | 25 | 14 | 10 | rising then settling |
| tax | 10 | 19 | 18 | 16 | 9 | constant |
| localization | 10 | 13 | 20 | 21 | 5 | fading in 2026 |
| media | 19 | 13 | 4 | 20 | 12 | seasonal |
| discounts | 5 | 10 | 18 | 9 | 6 | seasonal |
| search | 1 | 6 | 8 | 11 | 3 | low, slight rise |
| vendors | 8 | 5 | 7 | 8 | 8 | constant (low) |
| ai | 0 | 0 | 0 | 7 | 4 | **new in 2025** |
| docs | 9 | 4 | 3 | 1 | 2 | fading |

- (evidence) Whole repo peaks in Q4 and Q1 and dips in Q2-Q3 in every year (e.g. 22Q4 136, 23Q1 143, 23Q3 42, 24Q4 160, 25Q1 150), matching release cycles (4.60, 4.70, 4.80, 4.90, 5.00 in subjects). 2026 is partial (26Q4 only 25 commits at 16 Oct); 26Q3 has 124.
- Campaigns behind spikes (subjects): payments 24Q4 = Amazon Pay and PayPal Commerce fixes plus the 4.90 release; integrations 25Q4 (22 commits) = .NET 10 update (#7859, touches all plugin.json) and RFQ/quotes rules (#7909, #7982, #7988); catalog 25Q1 (38) = search model, category discount, product count fixes (#7515, #7636, #7582, #7642); content+frontend-ui+messaging 25Q3 = Reminders (#7743), NopUrl rename (#7818), "partial modifier" follow-ups (#5720); ai 25Q3-Q4 = AI SEO optimisation (#7732, #7964), product description generation (#7730), AI logging and tests (#7898, #405); platform 24Q4 = 4.90 release, driver.js product tours (#7398).
- (evidence) Plugin removals/replacements in the window (from the contract): Sendinblue to Brevo, PayPalStandard to PayPalCommerce, AmazonPay, CyberSource, EasyPost, ShipStation and four widgets deleted; Forums/News/Polls moved from core to plugins (mass commits, excluded).

## Fix pressure

Heuristic (contract): fix commit = subject matches `\bfix(es|ed)?\b|\bbug\b|regression|incorrect|wrong|not working|doesn't|does not|issue with`; revert = subject contains `revert`. Commit counted for each capability touched; "no-cross" excludes commits touching more than 3 capabilities (277 of 1955, 14%). Overall fix share is 466/1955 = **23.8%** (baseline). 1536 commits (79%) cite a `#NNNN` issue, but the log has no labels, so bug-labelled references are **unknown**.

| capability | commits | fix | fix % | fix % (no cross-cutting) | reverts (cross-cutting) |
|---|---|---|---|---|---|
| catalog | 336 | 80 | 24 | 38 | 3 (0) |
| orders | 264 | 61 | 23 | 35 | 4 (1) |
| frontend-ui | 238 | 46 | 19 | 32 | 2 (0) |
| media | 79 | 14 | 18 | 31 | 2 (1) |
| payments | 101 | 17 | 17 | 35 | 0 |
| ops | 437 | 73 | 17 | 27 | 1 (1) |
| messaging | 213 | 35 | 16 | 26 | 1 |
| admin-ui | 304 | 48 | 16 | 29 | 2 (1) |
| foundations | 658 | 99 | 15 | 21 | 4 (1) |
| tax | 85 | 11 | 13 | 28 | 0 |
| content | 169 | 20 | 12 | 24 | 2 |
| discounts | 60 | 7 | 12 | 38 | 0 |
| identity | 251 | 29 | 12 | 18 | 2 |
| platform | 556 | 62 | 11 | 16 | 4 (1) |
| shipping | 76 | 8 | 11 | 23 | 0 |
| vendors | 48 | 4 | 8 | 29 | 0 |
| localization | 82 | 6 | 7 | 15 | 0 |
| integrations | 85 | 6 | 7 | 10 | 1 |
| search | 41 | 2 | 5 | 10 | 0 |
| ai | 11 | 0 | 0 | 0 | 0 |

- (evidence) 14 reverts in total. The largest, `db6c197d83` (2023-08, 22 files, 6 capabilities), reverted a `#region Utils` rename: pure spillover into platform/ops/media/orders/foundations/admin-ui. Business reverts: `3f19686119` (2026-09, currency parsing #8239; catalog, integrations, orders), `2d2955927e` (2026-01, "Continue Shopping" redirect #8083; catalog, orders), `48e78a7b08` (2025-06, one-page checkout address pre-fill #7709; orders, frontend-ui), `2f5ec12057` (data-action on submit button #6690). Reverts are concentrated in **orders/checkout** (4 of 14) and catalog (3); the rest are dependency pins (Azure.Storage.Blobs, LigerShark, SqlClient) and refactoring rollbacks.
- (evidence) Rule 12: only 15 fix commits are cross-cutting (e.g. modal dialog fix #7881 over 9 capabilities, typo fixes), so spillover does not drive the ranking. Rule 5: sampled fix subjects in platform/ops/foundations are mostly typos, test fixes, locale strings; frontend-ui/admin-ui fixes are CSS/styling ("Fixed admin css", RTL slider) and typos. Those capabilities' fix share is demoted to **mechanical/iteration**.
- (inference) Real fix pressure sits in **catalog (price calculation, search model, discount display), orders/checkout (cart, one-page checkout, reverts)** and payments (PayPalCommerce/AmazonPay). Payments and discounts have small denominators (101 and 60 commits): treat the percentages as indicative only.
- (inference) The fix regex is broad (`wrong`, `incorrect`, `issue with`) and also matches feature subjects; shares are upper bounds.

## Co-change between capabilities

Rule: per commit, set of capabilities touched; dropped = 15 mass + 18 sweeping + 17 commits touching more than half of the 22 capabilities (more than 11), leaving **1917 commits, 663 multi-capability**. `unmapped` and `docs` excluded from pairs. Confidence = shared commits ÷ commits of the less active side.

Top pairs by support:

| pair | support | confidence |
|---|---|---|
| foundations + platform | 263 | 0.50 |
| foundations + ops | 201 | 0.49 |
| admin-ui + foundations | 153 | 0.55 |
| ops + platform | 153 | 0.37 |
| admin-ui + platform | 131 | 0.47 |
| admin-ui + ops | 119 | 0.42 |
| catalog + foundations | 95 | 0.31 |
| foundations + identity | 92 | 0.42 |
| catalog + ops | 91 | 0.29 |
| foundations + frontend-ui | 82 | 0.38 |
| foundations + orders | 71 | 0.30 |
| catalog + orders | 70 | 0.29 |
| foundations + messaging | 60 | 0.33 |
| catalog + content | 57 | 0.40 |
| identity + orders | 56 | 0.25 |
| catalog + identity | 56 | 0.25 |

Domain-only pairs (neither side a catch-all bucket), support at least 8, by confidence: catalog+vendors 20 (0.71, but vendors has 28 commits), identity+vendors 18 (0.64), content+vendors 14 (0.50), orders+vendors 13 (0.46), catalog+search 10 (0.45), discounts+identity 15 (0.44), **catalog+content 57 (0.40)**, identity+tax 22 (0.39), messaging+tax 21 (0.37), identity+localization 20 (0.36), shipping+tax 17 (0.35), catalog+discounts 12 (0.35), **content+identity 49 (0.35)**.

- (inference) Catch-all pairs mostly reflect the layering (a service change plus Framework/Data/migration/installer). The domain corridors worth noting: catalog with content (admin menu, SEO, widgets) and identity; vendors with catalog/identity (the `*Vendor*` glob drags vendor fields in product and customer models); tax with identity and messaging (customer tax exemption, message tokens). Support for most domain pairs is under 30 commits, so these are weak.

## Hub files and freshness

Files co-changing with the most distinct partner capabilities (partners seen in at least 2 commits; multi-capability commits only). All 25 listed files exist at HEAD (`git cat-file -e HEAD:<path>`): **0 deleted or moved**.

| file | multi-cap commits | partners (>=2 commits) | what it is |
|---|---|---|---|
| `Nop.Web/App_Data/Localization/defaultResources.nopres.xml` | 209 | 19 | English resource catalog, one entry per feature string (translation corridor) |
| `Admin/Controllers/SettingController.cs` | 68 | 18 | single admin controller for all settings pages |
| `Admin/Controllers/ProductController.cs` | 34 | 18 | catalog admin, touches pricing, vendors, SEO, media |
| `Admin/Infrastructure/Mapper/AdminMapperConfiguration.cs` | 81 | 16 | AutoMapper registry of all admin models (config as code) |
| `Nop.Services/Installation/InstallRequiredData.cs` | 56 | 15 | seed data for a fresh install (settings, templates, tasks) |
| `Admin/Factories/SettingModelFactory.cs` | 57 | 14 | pair of SettingController |
| `Framework/Migrations/UpgradeTo490/500/LocalizationMigration.cs`, `SettingMigration.cs` | 47 / 39 / 25 / 22 | 13-14 | per-release upgrade migrations for new locale strings/settings |
| `Nop.Tests/BaseNopTest.cs` | 39 | 14 | shared test setup, registers services |
| `Themes/DefaultClean/Content/css/styles.css` | 42 | 14 | one storefront stylesheet |
| `Nop.Services/Customers/CustomerService.cs`, `Orders/OrderProcessingService.cs`, `ImportManager.cs`, `ExportManager.cs` | 15-32 | 13-16 | domain services reaching many features |
| Plugin `plugin.json` (PayPalCommerce 17, Avalara 18, RFQ 11, FacebookPixel, GoogleAuthenticator, FixedByWeightByTotal) | 8-18 | 13-17 | plugin version/compat manifest bumped by release and .NET upgrades |

- Rule 3 (lockstep guarded by build/check): `defaultResources.nopres.xml` plus `LocalizationMigration.cs` is a translation key plus its upgrade migration, and `AdminMapperConfiguration`/`BaseNopTest` register new models/services; each is a corridor of the "add a feature" workflow, not hidden coupling. Whether a build step fails when a resource key is missing is **unknown** from history.
- Rule 4/7: `plugin.json` "hubs" share one cause: release/.NET version bumps (226 commits touch a plugin.json; 12 of them touch 10+ at once, e.g. "Update to .NET 10", "Changed nopCommerce version to 5.00", 4.90). Counted once as release machinery; **mechanical**.
- Rule 2: `AdminMapperConfiguration` and `InstallRequiredData` are fan-in by design; a missing mapping fails at startup or in tests (inference, not verified).
- Genuine hubs: `SettingController`/`SettingModelFactory` and `ProductController` (everything admin routes through them) and `OrderProcessingService`.

## Mechanical signals

Demoted from risk ranking and why:
- **Sweeping commits** (18, 9,184 changes = 46% of file-changes): kept in counts, but the "with / without" table above shows rank shifts; mass commits (15) excluded.
- **Plugin version churn** (`plugin.json` 495, `.csproj` 423 changes): inflates messaging, integrations, payments, shipping, tax, identity and foundations (rules 3, 7).
- **Localization corridor** (resource XML 221 commits, LocalizationMigration 235, together 705 changes): platform/foundations rank partly driven by it (rule 3).
- **Fix ratio in platform/ops/foundations/frontend-ui/admin-ui**: typos, tests, CSS tweaks (rule 5).
- **Catch-all buckets** (foundations, platform, ops, admin-ui, frontend-ui) are a layering artefact of the attribution, not capabilities (contract known weakness).
- **Deleted plugins** (421 unmapped changes) and the `docs` count (20 changes; includes only the toolkit commit of this branch, `5b31db5b27 add 10x skills`, which is counted in the 1970) are history, not activity (rules 8, 11).
- Seasonality: Q4/Q1 peaks follow releases; do not read a Q4 spike as an incident.

## Unknowns

- (unknown) Bug-labelled issue references: the log holds only `#NNNN` numbers (1536 commits); labels live on the forge. Fix share uses the subject heuristic only.
- (unknown) Test-to-code pairing: `src/Tests/*` only matches by name globs; whether a capability (e.g. orders, tax) has tests that change with it cannot be told from capability-level co-change.
- (unknown) Pairs that should change together but do not: admin model factory vs storefront factory for the same entity, core domain vs the plugin's hand-kept copies (e.g. PayPalCommerce vs core Payments), `installation.*.xml` and other-language packs vs `defaultResources.nopres.xml` (only English is in the repo's catalog), `upgradescripts/*.sql` vs `Migrations/*` (SQL scripts for pre-4.x upgrades), and external consumers of Web API (WebApi.Frontend, NopMobileApp clients live outside this repo; rule 10).
- (unknown) Whether a failing build/test guards the resource-key and mapper-registration corridors (rule 3 guard check not performed: no source reading).
- (unknown) Per-author concentration and review activity: out of scope for this agent (people/discussion agents).
- (unknown) Attribution of `plugin.json` bumps and `Views` shared by several features: filename-keyword globs (`*Order*`, `*Customer*`) can mis-attribute admin controllers and tests (contract weakness); 2.1% unmapped, but misattribution rate is not measured.

## Commands run

- `node hist.mjs` helpers over `.work/git-log.txt` (parse, noise filter, `cap()` attribution): `h1.mjs` (totals, unmapped), `h2.mjs` (activity, top files), `h3.mjs` (mechanical shares, trend), `h4.mjs` (fix/revert), `h5.mjs` (co-change, hubs, `git cat-file -e HEAD:<path>` freshness), `h6.mjs` (spike subjects, plugin.json).
- `awk` over `git-log.txt` header lines to check co-author trailers.
