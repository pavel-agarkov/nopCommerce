---
date: 2026-10-18T22:47:01+02:00
researcher: Claude (Opus 5.5) for pavel-agarkov
git_commit: 210b5dcf29206cd9ecc4346345dcabf2b9da1794
branch: claude/checkout-refactor-opportunities-2d2245
repository: nopSolutions/nopCommerce (fork pavel-agarkov/nopCommerce)
topic: "Checkout refactor opportunities: which documented issues are worth fixing, target shape, order"
tags: [research, refactoring, checkout, orders, OrderProcessingService, CheckoutController, technical-debt]
status: complete
last_updated: 2026-10-18
last_updated_by: Claude (Opus 5.5)
---

# Research: Checkout refactor opportunities

**Date**: 2026-10-18T22:47:01+02:00
**Researcher**: Claude (Opus 5.5) for pavel-agarkov
**Git Commit**: 210b5dcf29 (`src/` is unchanged since 13cdb1593b, the commit the prior analysis was written against: `git diff --stat 13cdb1593b HEAD -- src` is empty)
**Branch**: claude/checkout-refactor-opportunities-2d2245
**Repository**: nopSolutions/nopCommerce

## Research Question

The prior analysis, [checkout-flow-analysis/research.md](../checkout-flow-analysis/research.md) (referred to below as **CFA**), documents the debt and risks of the checkout and order-placement module without recommending anything. This document answers the question it left open: **which of those issues are worth fixing, what target shape each should have, and in what order to address them.** It is exploration only. Nothing was changed and nothing is decided here; the ranking at the end is a proposal for a separate planning session.

The CFA findings are treated as verified evidence and are not re-derived. Priors also include [repo-map.md](../../map/repo-map.md) and its evidence files. Every candidate was investigated by three read-only sub-agents:
- **Shape**: how the code looks today.
- **History**: why it looks that way.
- **Feasibility**: what an incremental, reversible fix would take.

The parent re-checked the decisive claims (listed at the end of this section).

Conventions:
- Paths are relative to `src/`.
- **CC** = `Presentation/Nop.Web/Controllers/CheckoutController.cs`, **OPS** = `Libraries/Nop.Services/Orders/OrderProcessingService.cs`, **SCS** = `Libraries/Nop.Services/Orders/ShoppingCartService.cs`, **GA** = GenericAttribute, **OPC** = one-page checkout.
- Tags: **[E]** evidence (read in code, git or the issue tracker), **[I]** inference, **[U]** unknown.

The parent re-checked these claims at HEAD:
- OPS:1601-1602, where `PreparePlaceOrderDetailsAsync` is called before the local `placeOrder` and its `try`.
- OPS:1686-1691, the mutex comment.
- CC:136, which still says "X seconds".
- OnePageCheckout.cshtml:206 and public.onepagecheckout.js:808-810: OPC success goes to `CHECKOUT_COMPLETED` without an order id.
- BaseDataProvider.cs:503-533: Serializable isolation with `TransactionScopeAsyncFlowOption.Enabled`.
- `PlaceOrderWithLock = false` and `OnePageCheckoutEnabled = true` in InstallRequiredData.cs:1893 and :1870.
- PayPalCommerceServiceManager.cs:2170-2172: when the result is not `Success`, it throws `NopException`.
- `git show` of faf8ec0002, 42ae7dfdec, 17dfa667d0 (the removed commented-out `TransactionScope` at old lines 960/1688), a0ca5250e9 and 48e78a7b08.

## 0. Issue inventory and classification (audit list)

Every issue CFA identifies, whatever it was labelled, is listed here. **CANDIDATE** means a fix would change code structure. A non-candidate (**—**) is kept as input to feasibility and cost.

| # | Issue (CFA section) | Label in CFA | Class | Grouped into |
|---|---|---|---|---|
| I1 | No transaction around order placement (§4.1) | debt [E] | CANDIDATE | C1 |
| I2 | Payment runs before any DB write (§4.1) | debt [E] | CANDIDATE | C1 |
| I3 | Partial order left after a mid-flow failure (§4.1) | debt [I] | CANDIDATE | C1 |
| I4 | Exceptions flattened to strings, 10 CC + 7 OPS sites (§4.1, A14) | debt [E] | CANDIDATE | C2 |
| I5 | `PreparePlaceOrderDetailsAsync` outside the inner `try` (OPS:1602) (§4.1) | debt [E] | CANDIDATE | C2 |
| I6 | Duplicate-order lock: process-local named `Mutex`, sync-over-async (§4.1, A2) | debt [E]/[I] | CANDIDATE | C3 |
| I7 | Heuristic "latest order" lookups CC:142/430/2143 (§4.1, A19) | debt [E] | CANDIDATE | C4 |
| I8 | God classes: OPS 50, CC 28, ShoppingCartModelFactory 50, SCS 31, CheckoutModelFactory 31, OTCS 22 ctor params (§4.2, A7) | debt [E] | CANDIDATE | C5 |
| I9 | OPC vs multi-step near-copies (§4.2) | debt [E] | CANDIDATE | C6 |
| I10 | Guard block repeated in 20 actions (§1.1, A6) | debt [E] | CANDIDATE | C6 |
| I11 | Repeated validation: cart warnings at 7 sites, `IsPaymentWorkflowRequiredAsync` at 10, guest check at 21 + 1 (§4.2, A4/A5) | debt [E] | CANDIDATE | C7 |
| I12 | Untyped OPC JS ↔ controller contract, 6 hard-coded URLs, no named routes (§1.3, §4.3, A15) | debt [E] | CANDIDATE | C8 |
| I13 | `ProcessPaymentRequest` stored as a JSON blob in GA (§4.3) | debt [E] | CANDIDATE | C9 |
| I14 | Checkout state spread over 12 GA keys / 51 writes (§1.7, A17) | insight [E] | CANDIDATE | C9 |
| I15 | `IPaymentMethod`: 21 members, no default implementation, no payment base class (§4.3, A20) | debt [E] | CANDIDATE | C10 |
| I16 | PayPalCommerce reaches deep into core: 12 OPS methods, a third `PlaceOrderAsync` entry point (§4.3, A3, A8) | debt [E] | CANDIDATE | C11 |
| I17 | Feature-add lockstep: entity + migrations + resources + install data + mapper (§3.2-3.3, §4.3) | debt [E] | CANDIDATE | C12 |
| I18 | Two unrelated interfaces both named `IShoppingCartModelFactory` (§3.1 note) | finding [E] | CANDIDATE | C13 |
| N1 | Runtime coupling via event consumers and `ModelPreparedEvent` (§1.8, §4.3, A11) | debt [E] | — (the extension mechanism itself; blast-radius input) | |
| N2 | `ExportManagerTests` hard-codes Order property names (§3.2, §4.3) | debt [E] | — (test debt) | |
| N3 | `[IgnoreAntiforgeryToken]` on `OpcSavePaymentInfo` (§4.4, A16) | security [E]/[U] | — (one-attribute question for a security review, not structure) | |
| N4 | Zero direct tests for placement, cart warnings, payment processing, controllers; no E2E; the test payment method always returns Paid (§2, §4.5, A12-A13) | test debt [E] | — (cost and prerequisite input) | |
| N5 | Untested money-path branches (§2.3) | test debt [E] | — | |
| N6 | Cart cleared with `publishEvent: false`; `ClearShoppingCartEvent`/`ResetCheckoutDataEvent` have 0 consumers (§1.8, A21) | finding [E] | — (behaviour, not structure) | |
| N7 | Wide interface fan-out: 1 implementation each, 7–49 referencing files (§3.1, A9) | blast radius [E] | — (blast-radius input) | |
| N8 | Reverts and friction: #7709, #8083, #8239 (§3.5) | history [E] | — (debt-cost signal) | |
| N9 | Same named-`Mutex` idiom outside checkout: PictureService ×3, UploadedImagesMigration, BaseNopTest (§4.1) | debt [E] | — (outside the module) | |
| N10 | Untested `PlaceOrderWithLock` branch and opaque defaults of order settings (§2.3, Open Questions) | unknown | — (the default is now known: `false`, InstallRequiredData.cs:1893 [E]) | |
| N11 | Stale comment CC:136 "X seconds" (the unit has been minutes since #7394); copied into PayPalCommerce and Amazon Pay (found in this research) | doc drift [E] | — (comment fix) | |

That gives 18 issues grouped into 13 candidates (C1–C13) and 11 non-candidates.

## Summary

- **[E] Most of the shape is intentional.** By the History verdicts:
  - Intentional constraint: C3, C9, C11, C13.
  - Intentional at origin: C1 (inherited from 2010 and never activated), C4 (2011–2013 workarounds), C6 (two flows kept on purpose since 2011), C10 (deliberate growth).
  - Accidental: C2 (a side effect of #7325 in 2024), C5 (growth on an intentional convention) and most of C7.
  - C12 is an undocumented convention. C8 has an unknown origin, but its contract is de-facto public (it caused the #7744 revert).
- **[E] The real compatibility contract is wider than the interfaces.** It includes:
  - every `public/protected virtual` member: 70 in OPS, 40 in CC (Feasibility count; Shape counted 70 for OPS);
  - the constructors (subclasses must chain them);
  - the OPC view and JS, which third-party themes override (#7744).
  
  No in-repo class subclasses OPS, CC, SCS or the factories. Third-party use is **[U]**.
- **[E] Safeguards are thin.** CI (`.github/workflows/dotnet.yml`) only runs restore → build Release → `dotnet test` on push/PR to `develop`. There is no analyzer, API-compat, coverage, JS or E2E step. The test harness (BaseNopTest, real services on in-memory SQLite) **can** host service-level characterization tests of `PlaceOrderAsync`, but it registers no controllers.
- **Proposed ranking** (details in "Refactor opportunities"):
  1. Order-placement failure handling in OPS (C2 + the transactional part of C1).
  2. Deduplicate the OPC and multi-step controller flows, and move the guard block into a filter (C6).
  3. Pass the placed order id explicitly instead of the "latest order" guess (C4).
  
  The pay-before-persist ordering in C1 is a **business lifecycle redesign** (a pending-order state, payment compensation), so it is flagged and not designed here.

## Detailed findings per candidate

### C1. Placement consistency (I1, I2, I3)

**Current shape**
- **[E]** `PlaceOrderAsync` (OPS:1594) runs these steps in order:
  1. payment, `GetProcessPaymentResultAsync` (OPS:1610);
  2. order save (:1616), cart → items (:1621), discount/gift-card history (:1624/:1627), recurring (:1631);
  3. notifications (:1634) — which insert `QueuedEmail` and notes and can write a PDF to disk (OPS:824-860);
  4. GA reset (:1637-1639), activity log (:1641), `OrderPlacedEvent` (:1646), status/paid handling (:1649-1652).
  
  All of it sits in one `try` that converts exceptions to strings (:1663-1667), with no transaction.
- **[E]** An async-capable transaction abstraction already exists: `INopDataProvider.CreateTransactionScope()` (INopDataProvider.cs:331; BaseDataProvider.cs:503-533). It is `Required`, Serializable, `TransactionScopeAsyncFlowOption.Enabled`, and is used today only by bulk operations (EntityRepository.cs:364/470/504; SyncCodeHelper.cs:103/119). There is no unit of work (grep `UnitOfWork` in `Libraries/`: 0).
- **[E]** `EventPublisher.PublishAsync` runs consumers inline and catches and logs their exceptions (EventPublisher.cs:25-51). At least 5 plugins consume `OrderPlacedEvent` (CFA §1.8).
- **[I]** A scope around the post-payment writes would contain plugin consumers that do external I/O, and the PDF file write.

**Intentionality: intentional at origin (2010), never revisited; the reason is unknown.**
- **[E]** The 2010 SVN import (03c6afceb8, `OrderManager.cs:3384-3385`) already had the transaction scope commented out, next to an "uncomment this line to support transactions" note. It moved to OrderService (357433a641) and then OPS (a4615a5b2f, 2011). 17dfa667d0 (2015-04-09, "#11792 … Simplified PlaceOrder") deleted the commented-out lines. A transaction was **never active** in placement.
- **[E]** Payment before insert is also original (OrderManager.cs:3346 vs :3388).
- **[E]** Every later transaction commit is about bulk ops: fff869d1e5 (#5820, 2021), 107a835a1b (#7721), 3a1db40a68 (#8212, 2026, Serializable deadlocks). The comment at BaseDataProvider.cs:512-524 cites deadlocks #6482 and #6681.
- **[E]** The only upstream duplicate-order report is #5131 (2020, two tabs, two Paid orders via PayPal Express); it was moved to the plugin repo. No issue about partial orders was found **[U: `gh search` recall is limited]**.

**Feasibility**
- **[I]** Wrapping the post-payment DB writes (OPS:1616-1640) in the existing scope is internal to OPS. `IOrderProcessingService` would not change, and no schema change is needed.
- Risks:
  - **[E]** Serializable isolation over a long flow (deadlocks: #6482, #6681, #8212).
  - **[I]** Events and e-mails fire inside the scope.
  - **[U]** Whether LinqToDB's per-call connections enlist in an ambient scope for non-bulk calls.
  - **[U]** Whether in-memory SQLite in the tests honours it.
- **[E]** A transaction cannot undo the payment. Real atomicity needs either:
  - compensation via the existing `IPaymentMethod.VoidAsync`/`RefundAsync`, which not every plugin supports (CheckMoneyOrder stubs them, CFA §3.1); or
  - persisting a pending order before paying.
  
  **Both change the order lifecycle that plugins, PayPalCommerce's own entry (PayPalCommerceServiceManager.cs:2170) and `OrderPlacedEvent` consumers observe. That is a business-concept redesign: stop here.**
- **[E]** The harness can host the prerequisite. BaseNopTest registers the real OPS. CheckoutModelFactoryTests.cs:43-80 already builds a cart with the test payment and FixedRate shipping. `TestPaymentMethod` has static `TestSupport*` toggles (TestPaymentMethod.cs:221-231, set in OrderProcessingServiceTests.cs:30-33), but none for a payment failure: `ProcessPaymentAsync` always returns Paid (:103-111).
  - **[I]** A failure toggle following the same pattern would touch only test code.
  - **[I]** A post-insert failure can be induced with a throwing event consumer or a test subclass overriding a `protected virtual` step.

### C2. Error handling (I4, I5)

**Current shape**
- **[E]** Typed pieces exist:
  - `NopException` (Nop.Core/NopException.cs:6), already thrown in OPS (:1612, :2834, :2945) and in PayPalCommerce;
  - the `BaseNopResult` family (Nop.Services/BaseNopResult.cs:6): `PlaceOrderResult`, `ProcessPaymentResult`, `Capture/Refund/VoidPaymentResult` and others;
  - `BaseController.ErrorJson` (BaseController.cs:121, :134).
- **[E]** There is no MVC exception filter, only the `UseNopExceptionHandler` middleware (ApplicationBuilderExtensions.cs:72-113).
- **[E]** CC throws plain `Exception` with localized text to steer control flow (CC:1310-1311; OPC guards CC:1797-1812). Those land in `catch (Exception exc)` blocks that return `exc.Message` (CC:1356-1360, :2108-2112).
- **[E]** `PreparePlaceOrderDetailsAsync` runs at OPS:1602, before the local `placeOrder` and its `try`. Validation failures therefore escape as exceptions instead of `PlaceOrderResult.Errors`, and the error logging at OPS:1672-1676 does not run for them.

**Intentionality: split verdict.**
- **[E]** The catch-to-string pattern is long-standing (intentional "result object" style). Blame puts all 7 OPS sites at a4615a5b2f (2011-03-22). The CC sites date from 5578ca1101, 6b6b4b3a3b and a0ca5250e9 (2011).
- **[E] The hoist of `PreparePlaceOrderDetailsAsync` out of the `try` is accidental.** Before 2024 it was inside `try` (line 1034 at 17dfa667d0). faf8ec0002 (2024-09-30, "#7325 Added possibility to enable lock for order placement") hoisted it so the lock key could use `details.Customer.Id` **[I]**. Neither the commit nor #7325 mentions the change in error semantics.

**Feasibility**
- **[I]** Bringing preparation failures back inside the result path changes only OPS.
  - Callers CC:1355 and :2122 catch already, so they keep working.
  - PayPalCommerce converts `!Success` into `NopException` (PayPalCommerceServiceManager.cs:2170-2172 [E]) inside its own `HandleFunctionAsync` wrapper (:2102 [E]), so it sees an equivalent failure.
  - The customer-visible error text and logging change.
- **[I]** Replacing the 17 catch sites with typed handling is a larger, behaviour-visible change (messages shown to customers). The value of doing it is **[U]**: no issue reports a lost stack trace.
- **[I]** First step: a characterization test of `PlaceOrderAsync` with an invalid cart (for example, an empty cart reaches OPS:502-526), recording today's "throws" behaviour. The existing harness can host it.

### C3. Duplicate-order lock (I6)

**Current shape**
- **[E]** OPS:1681-1718 uses a named `Mutex` keyed by customer, a static-cache flag, `.Result` ×3 and `.Wait()` ×1. The comment says a semaphore was rejected because of a UNIX `PlatformNotSupportedException` (OPS:1689-1690).
- **[E]** `ILocker.PerformActionWithLockAsync` exists (Nop.Core/Caching/ILocker.cs:12). It has `MemoryCacheLocker` and `DistributedCacheLocker` implementations, chosen by config (Nop.Web.Framework/Infrastructure/NopStartup.cs:123/127). It is used by ScheduleTaskRunner.cs:141 and SitemapModelFactory.cs:690 and is covered by LockerTests.
- **[E]** Its semantics differ: it is try-lock (it returns `false` when the lock is held, where the Mutex waits), and `DistributedCacheLocker` does a non-atomic get-then-set (:33-56).

**Intentionality: intentional constraint (security-driven opt-in); the sync-over-async is a known, commented trade-off.**
- **[E]** Introduced in faf8ec0002 (#7325) against a "single packet attack" that redeemed a gift card twice. The issue says the lock is off by default because "in some cases it could lead to deadlocks". The default `PlaceOrderWithLock = false` is at InstallRequiredData.cs:1893.
- **[E]** The Mutex idiom comes from PictureService. There, b4a113e82a (#5273, 2021) went back from a named semaphore to a mutex because of Linux `PlatformNotSupportedException`.
- **[U]** No discussion of `SemaphoreSlim`/AsyncLock or multi-instance deployments was found.

**Feasibility**
- **[I]** `ILocker` is not a drop-in replacement: try-lock and non-atomic semantics would turn a wait into a "MinOrderPlacementInterval" error. Injecting it adds a constructor parameter, which breaks third-party subclasses unless resolved lazily.
- **[E]** The branch is off by default and untested (CFA §2.3).

### C4. Heuristic latest-order lookups (I7)

**Current shape**
- **[E]** The three lookups:
  - CC:142, `IsMinimumOrderPlacementIntervalValidAsync`: a pre-placement check, so no order id exists yet.
  - CC:430, `Completed(int? orderId)`: falls back to the latest order only when `orderId` is missing. Multi-step passes the id (CC:1349).
  - CC:2143, `OpcCompleteRedirectionPayment()`: takes no parameter. `OpcConfirmOrder` has `placeOrderResult.PlacedOrder` at CC:2072 but returns a literal URL without the id (CC:2096).
- **[E] The OPC success path always uses the fallback.** `ConfirmOrder.init(..., '@Url.RouteUrl(CHECKOUT_COMPLETED)', ...)` passes no id (OnePageCheckout.cshtml:206), and the JS navigates to `ConfirmOrder.successUrl` (public.onepagecheckout.js:808-810).
- **[E]** `OnePageCheckoutEnabled = true` in installed data (InstallRequiredData.cs:1870). **[I]** The heuristic is therefore the default storefront path, not an edge case.
- **[E]** The route already accepts `{orderId:int?}` (RouteProvider.cs:321). There is no `TempData` in CC.
- **[E]** PayPalCommerce re-implements the CC:142 check (PayPalCommerceServiceManager.cs:2152-2157).

**Intentionality: intentional at origin (2011–2013 workarounds); the gap is accidental drift.**
- **[E]** The 3-minute window and `OpcCompleteRedirectionPayment` come from a0ca5250e9 (2011-09-29, "One page checkout. Redirection payment methods work now"). Its comment says redirection doesn't work inside the AJAX request.
- **[E]** 7aa1addc34 (2012-12-03) added `orderId` to `Completed` because, with several unpaid orders, it "always shows the last payment completed". The latest-order lookup stayed as a fallback, but the OPC success URL was never given the id.
- **[E]** The interval check (CC:136) comes from 45c30db6c5 (2012). Its "X seconds" comment has been stale since #7394 switched the unit to minutes.

**Feasibility**
- **[I]** Additive and reversible. Add the id to the OPC success JSON and the redirection URL, prefer it in JS, and keep the latest-order fallback for overridden themes (#7744 shows themes override exactly these files [E]).
- Files: CC, public.onepagecheckout.js, OnePageCheckout.cshtml. No route or schema change.
- **[E]** No controller or JS tests exist, and BaseNopTest registers no controllers. **[I]** A testable seam (extracting the lookup into a factory or service method) or a new controller harness is a prerequisite for automated protection.

### C5. God classes (I8)

**Current shape**
- **[E]** OPS is `public partial class` (OPS:40) with roughly 18 responsibility clusters, each mapped one-to-one to `IOrderProcessingService` members. Ranges from the Shape agent, approximate:
  - placement: prep/validation 288-595, save/notify 723-865, steps 1233-1486, entry 1581-1721
  - recurring: 596-722, 1891-2235, 3261-3320
  - status transitions: 1012-1232, 1487-1556
  - capture / refund / void: 2476-3120
  - shipment: 2236-2374
  - reorder: 3121-3191
  - min-total / payment-workflow checks: 3192-3260
  - payment-request GA storage: 3321-3372
- **[E]** All the large classes are `partial` with `virtual` members; CC derives from `BasePublicController` (CC:33).

**Intentionality: accidental growth on top of an intentional extensibility convention.**
- **[E]** Injected `I*` fields / lines by sampled date:

  | Class | 2012 | now |
  |---|---|---|
  | OPS | 25/2536 | 42/3564 |
  | CC | 17/1392 | 20/2186 |
  | SCS | 12/916 | 27/2097 |

  CC flattened after the model factories were extracted (#233, 2016).
- **[E]** Splits have happened before: a4615a5b2f (2011, CRUD vs workflow), 17dfa667d0 (2015), dc8084baf8 (#3590), #7851 (2025).
- **[E]** "All service methods virtual" is a stated convention (#7840, 12d9660e39, 2025; #7253). No issue refusing a split was found **[U]**.

**Feasibility**
- **[I]** The biggest compatibility hazard: constructors (50/28/50/31/31 parameters), 50 `protected readonly` fields in OPS and `IShoppingCartService` (49 files, 10 plugins) are all visible to subclasses or plugins. Extracting collaborators while keeping the old `protected virtual` members as delegating shims is possible, but every extraction also touches NopStartup.cs and the hand-mirrored BaseNopTest.cs:374-381 **[E]**. Cost is high, and there is no direct customer-facing payoff.

### C6. OPC vs multi-step duplication and guard block (I9, I10)

**Current shape**
- **[E]** `SelectShippingMethod` (CC:968-1050) and `OpcSaveShippingMethod` (CC:1792-1877) share their parsing and saving logic line for line. They differ only in how they exit: a redirect, or `throw new Exception` followed by `OpcLoadStepAfterShippingMethod`. The same holds for the payment-method, payment-info and confirm pairs (CFA §4.2).
- **[E]** The guard sequence is inlined (e.g. CC:971-985, 1281-1294, 1797-1809), 20 times in total (A6).
- **[E]** Precedents already in CC and the framework:
  - `protected virtual EditAddressAsync(..., Func<…, Task<JsonResult>> getResult)` (CC:268) and `DeleteAddressAsync` (CC:311) take a result callback and are reused by 4 actions (CC:488, 519, 545, 572), some with a `bool opc` flag (CC:486).
  - The filter pattern: `sealed XAttribute : TypeFilterAttribute` with an inner `IAsyncAuthorizationFilter` and an `ignore` flag (CheckAccessPublicStoreAttribute.cs:11-102). There are 26 such filters in Nop.Web.Framework/Mvc/Filters, and 6 are applied by `BasePublicController` (BasePublicController.cs:7-12).

**Intentionality: the two flows are intentional (since 2011); the copy-paste between them is accidental.**
- **[E]** OPC existed in the 2010 WebForms code (2694be8d7a). The MVC version is 6b6b4b3a3b (2011-09-28).
- **[E]** The only deduplication pass, 6e06a89787 (2013, "Less duplicated code"), extracted only OPC-internal helpers. #7203 (2024) refactored address preparation. No attempt to merge the step logic and no "keep separate" rationale were found **[E/U]**.

**Feasibility**
- **[I]** Internal to CC if action names, routes, JSON keys and the 11 existing `protected virtual` helpers stay. The shared step logic returns a result object that each action maps to a redirect or JSON — the `EditAddressAsync` precedent. The guard becomes a filter or shared helper following the `TypeFilterAttribute` pattern.
- **[E]** CC co-changed in 32 kept commits since 2022; #7709 was reverted across exactly these files (CFA §3.4-3.5).
- **[E]** No controller tests and no controller registration in BaseNopTest. **[I]** The main cost is a new controller-level harness (for example `WebApplicationFactory`, none in `src/`, CFA §2.4), or moving step logic into a testable service first.

### C7. Repeated validation (I11)

**Current shape**
- **[E]** A per-request cache exists: `IShortTermCacheManager` / `PerRequestCacheManager`, scoped (Nop.Web.Framework/Infrastructure/NopStartup.cs:96). `GetShoppingCartAsync` (SCS:779) and the customer-role lookups behind `IsGuestAsync` (CustomerService.cs:1277-1278, :1370) use it.
- **[E]** `IsPaymentWorkflowRequiredAsync` (OPS:3241-3254) recomputes `GetShoppingCartTotalAsync` on every call. OTCS has no cache references, and cart warnings are not cached.

**Intentionality: mostly accretion; one site is bug-driven.**
- **[E]** The cart-warning call sites date from 2011 (b36b86b578, a4615a5b2f). Later edits are mechanical, plus #6359 (2022, performance).
- **[E]** 7a2ff4d416 (2012) added the "skip payment pages if not required" checks after a bug where reward points exceeded the cart total — deliberate defence in depth.

**Feasibility**
- **[I]** Memoization through `IShortTermCacheManager` would be internal. OPS:502 must stay as the last line of defence.
- **[U]** The debt cost is unmeasured: no profiling and no performance issue tied to these calls was found.

### C8. Untyped OPC JS ↔ controller contract (I12)

**Current shape**
- **[E]** One typed piece exists: `UpdateSectionJsonModel` (Nop.Web/Models/Checkout/UpdateSectionJsonModel.cs:3, used 17×). Everything else is anonymous `Json(new …)`: 32 in CC, 54 in ShoppingCartController.
- **[E]** OnePageCheckout.cshtml builds `'@(storeLocation)checkout/...'` literal URLs (:65, 66, 74, 96, 110, 133, 162, 185, 206) and CC builds one too (CC:2096). Elsewhere in Nop.Web views, `Url.RouteUrl` appears 157 times.

**Intentionality: unknown origin; the contract is load-bearing for third-party themes.**
- **[E]** 48e78a7b08 reverted #7709 under #7744, "4.80.7 change to OnePageCheckout.cshtml breaks OPC", reported by a Pacific theme user with overridden views/JS. A maintainer deferred the change to 4.90 rather than a 4.80.x patch.

**Feasibility**
- **[I]** A typed DTO serialized with the same key names and named routes that keep the same URLs is non-breaking for the stock theme. **[U]** Theme copies keep their literals regardless. **[E]** No JS lint, JS tests or E2E in CI.

### C9. Checkout state in GenericAttribute (I13, I14)

**Current shape**
- **[E]** Keys are static properties in `NopCustomerDefaults` (lines 51-151).
- **[E]** `ResetCheckoutDataAsync` (IShoppingCartService.cs:339; SCS:1935-~1970) clears the step keys but not `ProcessPaymentRequestAttribute`, which is cleared separately (CC:1335).
- **[E]** No `CheckoutState`/`CheckoutContext` type exists. `ProcessPaymentRequest` is the only aggregate-like DTO, encapsulated behind `IOrderProcessingService.Get/SetProcessPaymentRequestAsync` (IOrderProcessingService.cs:349, :357).

**Intentionality: intentional constraint, recent.**
- **[E]** 42ae7dfdec (2025-02-26, #7379 "Avoid using sessions for storing ProcessPaymentRequest"): there is no session for AJAX/Web API requests. A commenter objected to GA table performance; it was merged anyway.
- **[E]** Reusing the order GUID across attempts is deliberate brute-force protection (85d55663df, #3622, 2019).

**Feasibility**
- **[I]** The storage can be swapped behind the existing interface (OPS only). A new state model needs a table, an `UpgradeTo*/SchemaMigration` and an in-flight data migration. Changing the shape of `ProcessPaymentRequest` reaches 17 files (CFA §3.1).

### C10. `IPaymentMethod` shape (I15)

**Current shape**
- **[E]** `public partial interface IPaymentMethod : IPlugin` (IPaymentMethod.cs:10). The shipping, tax and widget plugin interfaces have the same shape.
- **[E]** `BasePlugin` (BasePlugin.cs:6-63) is the only plugin base class.
- **[E]** A default-interface-member precedent exists in `IConfig` (Nop.Core/Configuration/IConfig.cs:14, :20). The target framework is `net10.0` with no `LangVersion` (Directory.Build.props:3), so default interface members are available.

**Intentionality: intentional growth.**
- **[E]** 30 commits since 2011; members were added in 2011, 2012, 2013, 2014, 2016 and 2017, and it went async in 2020.
- **[E]** #6080/#6085 (2022), asking for more extensible payment plugins, were declined ("decided not to implement this").
- **[U]** Default interface members or a payment base class were never discussed.

**Feasibility**
- **[I]** Adding default implementations for "not supported" members is additive and binary-compatible. **[E]** No API-compat check guards accidental breaks.
- **[I]** The debt cost is low: the evidence shows stub boilerplate in 2 in-repo plugins, not defects.

### C11. PayPalCommerce coupling (I16)

**Current shape**
- **[E]** Its `PlaceOrderAsync` (PayPalCommerceServiceManager.cs:2100-2181) mirrors CC `ConfirmOrder`: empty-cart check (2108-2111), GA payment request (2113), interval check (2152-2157), setting StoreId/CustomerId/system name (2163-2165), `PlaceOrderAsync` (2170) and clearing the request (2178). It adds PayPal-specific order status, liability and total checks (2122-2149).
- **[E]** It uses `ICheckoutModelFactory` (PayPalCommerceModelFactory.cs:111, 247).
- **[I]** It never calls `PostProcessPaymentAsync`.

**Intentionality: intentional.**
- **[E]** 0d47801ef2 (2024-06-11, "PayPalCommerce plugin. New implementation"). The settings `DisplayButtonsOnShoppingCart`, `DisplayButtonsOnProductDetails` and `SkipOrderConfirmPage`, and the plugin's own confirm controller and view, place orders outside CheckoutController by design.

**Feasibility**
- **[I]** Not a standalone target. It is a **constraint** on C1, C2 and C4: any change to placement semantics must be checked against :2170 and its duplicated guards.

### C12. Feature-add lockstep (I17)

- **[E] Shape.** Per-version migration folders (Nop.Data/Migrations/UpgradeTo4xx–500; Nop.Web.Framework/Migrations/UpgradeTo490/{Setting,Localization,…}Migration), `defaultResources.nopres.xml`, `InstallRequiredData.cs` and `AdminMapperConfiguration.cs:75`. Discovery is already reflection-based: migration attributes (MigrationManager.cs:51-103), mapper profiles (NopEngine.cs:61-65), settings (SettingService.cs:334, 373). There are no source generators. **[I]** What remains manual is content.
- **[E] Intentionality: a platform convention, undocumented in the repo** (CONTRIBUTING.md links to external docs only).
- **[I] Feasibility.** A structural change would be platform-wide, beyond the checkout module. A completeness test or a checklist is the proportionate response, and that is not structural.

### C13. Two `IShoppingCartModelFactory` interfaces (I18)

- **[E] Shape.** `Nop.Web.Factories.IShoppingCartModelFactory` and `Nop.Web.Areas.Admin.Factories.IShoppingCartModelFactory`. No file imports both. Namespace qualification resolves the collision (Nop.Web/Infrastructure/NopStartup.cs:77 vs :108; BaseNopTest.cs:535 vs :562).
- **[E] Intentionality: intentional mirrored convention.** 14 interface names collide between `Areas/Admin/Factories` and `Factories`, a result of #1897 ("Use model factories in Nop.Admin projects (similar to Nop.Web)", 2018).
- **[I] Feasibility.** A rename would be a public break for 17 referencing files, with no defect evidence.

## Refactor opportunities

Ranking criterion: debt cost (money-path risk, observed incidents and friction, intentionality) against change cost (contracts touched, safeguards available, prerequisite work). This is a proposal for the planning session, not a decision.

### 1. Order-placement failure handling in `OrderProcessingService` (C2 + the transactional part of C1)

- **Current → target shape.**
  - Current: preparation failures escape as exceptions (OPS:1602, accidental since #7325); post-payment writes commit one by one (OPS:1616-1640); a mid-flow failure leaves an order row and possibly a captured payment, while the caller reports failure (CFA §4.1 [I]).
  - Target: one failure contract — every placement failure, including preparation, comes back through `PlaceOrderResult` with logging. The post-payment DB writes run inside the existing `INopDataProvider.CreateTransactionScope()`, with notifications and events moved after commit if the planning session confirms that ordering.
  - **Out of scope:** what happens to an already-captured payment (compensation via Void/Refund, or a pending-order-first lifecycle). That is a business lifecycle redesign for a separate analysis.
- **Why rank 1.**
  - Debt cost is the highest in the module: it is the money path. The partial-order failure mode follows from the code shape, there is an upstream duplicate-paid-order report (#5131), and the error-semantics regression is accidental and recent (faf8ec0002, 2024).
  - Change cost is the lowest of the structural options for the first stages. They are internal to one class with no interface, route or schema change, and they reuse existing abstractions (`PlaceOrderResult`, `NopException`, `CreateTransactionScope`).
  - The existing harness can host the safety net (real OPS on SQLite, cart setup in CheckoutModelFactoryTests.cs:43-80, the toggle pattern in TestPaymentMethod.cs:221-231).
- **Blast radius.**
  - **[E]** Code: OPS only.
  - **[I]** Behaviour observed by:
    - CC:1355/:2122 (error text);
    - PayPalCommerceServiceManager.cs:2170-2172, which already treats `!Success` as `NopException`;
    - `OrderPlacedEvent` consumers in 5 plugins plus core consumers, if event timing changes;
    - third-party subclasses overriding the `protected virtual` placement steps (OPS:212-1557) **[U]**.
  - Operational risks: Serializable isolation over a longer unit (deadlock history: #6482, #6681, #8212) **[E]**, and LinqToDB enlistment of non-bulk calls in an ambient scope **[U]**.
- **Incremental path sketch.**
  1. Add characterization tests that pin today's behaviour: success, invalid cart (throws today), payment failure (needs a test-only failure toggle), and a post-insert failure (a test consumer or subclass throws).
  2. Bring preparation failures back into the result path; the tests flip from "throws" to "returns errors".
  3. Verify that a scope enlists non-bulk LinqToDB writes, on SQLite and on SQL Server.
  4. Wrap the post-payment DB writes in the scope behind a setting, off by default. This mirrors how `PlaceOrderWithLock` shipped (#7325).
  5. Hand the paid-but-not-persisted case to a separate business analysis.
  
  Each step can be reverted independently.
- **First prerequisite step.** A characterization test suite for `OrderProcessingService.PlaceOrderAsync` in `Tests/Nop.Tests/Nop.Services.Tests/Orders/`, with a test-only payment-failure toggle on `TestPaymentMethod`. Today the method has no direct test (CFA A12).

### 2. One step core for OPC and multi-step checkout, guards as a filter (C6)

- **Current → target shape.** Today 4 action pairs are near-copies that differ only in exit style (CC:968-1050 vs 1792-1877, CC:1310-1350 vs 2052-2103 …), and a 4-condition guard is inlined in 20 actions. The target: each step's logic in one `protected virtual` method returning a step result, which the multi-step action maps to a redirect and the OPC action to the same JSON keys. This follows the existing `EditAddressAsync(…, getResult)` callback precedent (CC:268, :311). The guard becomes a `TypeFilterAttribute` filter with an `ignore` flag, following CheckAccessPublicStoreAttribute.cs:11-102.
- **Why rank 2.**
  - Debt cost: the duplication is accidental. CC is the module's most co-changed storefront file (32 kept commits since 2022; 7 with the OPC JS and view), and the #7709 revert hit exactly these files. Every fix has to be made twice.
  - Change cost: moderate. The code change is internal to CC plus one filter, with no route, JSON or view change. But there is **no controller-level safety net** (BaseNopTest registers no controllers; there is no `WebApplicationFactory`), so the prerequisite is new test infrastructure.
  - It ranks below #1 because its risk is maintenance cost, not money integrity, and it needs a new harness where #1 reuses an existing one.
- **Blast radius.** **[E]** CC (29 public actions, 11 `protected virtual` helpers) plus a new filter class. **[I]** It stays non-breaking only if action names, routes, the JSON keys (`update_section`, `goto_section`, `redirect`, `success`, `wrong_billing_address`) and existing `protected virtual` signatures are preserved. Theme-overridden views and JS are unaffected as long as the contract holds (#7744 is the cautionary case). Third-party CC subclasses **[U]**.
- **Incremental path sketch.**
  1. Build the controller harness and record characterization tests for one pair (shipping method): multi-step redirect targets and OPC JSON payloads.
  2. Extract the shared step core for that pair only.
  3. Repeat pair by pair (payment method, payment info, confirm). Each pair is one reversible commit.
  4. Introduce the guard filter, initially on new or extracted paths only, then remove the inlined copies action by action.
- **First prerequisite step.** A controller-level test harness able to invoke `CheckoutController` actions against the existing BaseNopTest service container, with a characterization test of `SelectShippingMethod` and `OpcSaveShippingMethod`.

### 3. Explicit placed-order id instead of the "latest order" heuristic (C4)

- **Current → target shape.** The OPC success URL carries no id (OnePageCheckout.cshtml:206; js:808-810), so on the default storefront configuration (`OnePageCheckoutEnabled = true`, InstallRequiredData.cs:1870) `Completed` falls back to the customer's latest order (CC:430). `OpcCompleteRedirectionPayment` picks the latest order if it is no more than 3 minutes old (CC:2143, :2156). The target: the id comes from `PlaceOrderResult.PlacedOrder` (already available at CC:2072). It goes into the OPC success JSON and the redirection URL, and the route already has `{orderId:int?}` (RouteProvider.cs:321). The latest-order fallback stays for overridden themes.
- **Why rank 3.**
  - Debt cost: moderate. 7aa1addc34 (2012) is precedent that the latest-order fallback shows the wrong order when a customer has several unpaid orders. The fix was applied to multi-step only, so OPC — the default — still depends on the guess.
  - Change cost: the lowest of any candidate. It is additive and needs no new abstraction.
  - It ranks below #1 and #2 because its impact is on the confirmation and redirection-payment pages, not on order data integrity, and because its automated protection depends on the controller harness from #2.
- **Blast radius.** **[E]** CC, public.onepagecheckout.js and OnePageCheckout.cshtml (3 files) — the same files as the #7709 revert. **[I]** With the fallback kept, themes that override the view or JS keep working unchanged. PayPalCommerce does not use these endpoints (it never calls `PostProcessPaymentAsync`). The CC:142 interval check is out of scope (no order exists yet) apart from its stale "X seconds" comment (N11).
- **Incremental path sketch.**
  1. Make the server accept and prefer an explicit id where one is passed (redirection action parameter, success URL).
  2. Emit the id in the OPC JSON.
  3. Update the stock view and JS to use it.
  4. Keep the fallback; whether to remove it later is a planning decision.
- **First prerequisite step.** A testable seam for "resolve the order to show", either a protected method or a factory method reachable from BaseNopTest, or the controller harness from #2, with a test showing the wrong-order case of two recent orders.

### Considered but rejected

| Candidate | Rejected because (evidence) |
|---|---|
| C1 pay-before-persist ordering | A business lifecycle redesign (pending orders, compensation through Void/Refund that not every plugin supports — CheckMoneyOrder stubs them). Out of scope for structural refactoring; only its transactional part is in #1. |
| C3 duplicate-order lock | An intentional security opt-in (#7325, off by default, InstallRequiredData.cs:1893). The sync-over-async is a commented, platform-driven trade-off (#5273). The existing `ILocker` is try-lock and non-atomic (DistributedCacheLocker.cs:33-56), so it is not a drop-in replacement. |
| C5 god classes (standalone split) | Growth sits on an intentional extensibility contract: 70 virtual members, 50-parameter constructors and protected fields visible to subclasses, `IShoppingCartService` in 10 plugins. Highest change cost, no direct defect payoff. #1 and #2 reduce size locally as a side effect. |
| C7 repeated validation | One site is deliberate defence in depth (7a2ff4d416); the rest is accretion with no measured cost (**[U]** no profiling or performance issue). OPS:502 must stay. Cheap memoization is possible later via `IShortTermCacheManager`. |
| C8 untyped OPC contract | Load-bearing for third-party themes: #7744 reverted a change to exactly these files, and the maintainers deferred it to a major release. A typed rewrite gives little while theme copies keep their literals. #2 and #3 preserve the contract instead. |
| C9 checkout state in GA | Intentional and recent (#7379, 2025-02: sessions unavailable for AJAX/Web API). It is already encapsulated behind `Get/SetProcessPaymentRequestAsync`. A new state model needs a schema plus data migration. |
| C10 `IPaymentMethod` defaults | Low debt cost: stub boilerplate in 2 in-repo plugins, no defects. Maintainers declined broader payment-plugin rework (#6080/#6085). Additive default members stay available as a cheap, non-structural option. |
| C11 PayPalCommerce coupling | Intentional (0d47801ef2, its own confirm flow by design). It is a constraint to check in #1 and #3, not a target. |
| C12 feature-add lockstep | A platform-wide convention outside the module, already reflection-discovered. The proportionate response is a completeness test or checklist, not structure. |
| C13 duplicate interface name | An intentional mirrored convention (14 collisions, #1897). A rename breaks public references in 17 files for no defect. |

## Code References

- `Libraries/Nop.Services/Orders/OrderProcessingService.cs`:
  - `:1594-1721` PlaceOrderAsync; `:1601-1602` preparation outside the try; `:1610` payment; `:1616-1652` post-payment steps; `:1663-1676` catch and logging
  - `:1681-1718` lock; `:3241-3254` IsPaymentWorkflowRequiredAsync; `:3321-3372` payment request in GA
- `Libraries/Nop.Data/DataProviders/BaseDataProvider.cs:503-533` — CreateTransactionScope (Serializable, async flow); `INopDataProvider.cs:331`
- `Libraries/Nop.Core/Caching/ILocker.cs:12`; `DistributedCacheLocker.cs:33-56`
- `Libraries/Nop.Services/BaseNopResult.cs:6`; `Orders/PlaceOrderResult.cs:8`; `Nop.Core/NopException.cs:6`
- `Presentation/Nop.Web/Controllers/CheckoutController.cs`:
  - `:136` stale comment; `:142`, `:430`, `:2143`, `:2156` latest-order lookups; `:2072`, `:2096` OPC redirection
  - `:268`, `:311` callback helpers; `:968-1050` vs `:1792-1877` duplicated pair
- `Presentation/Nop.Web/Views/Checkout/OnePageCheckout.cshtml:206`; `wwwroot/js/public.onepagecheckout.js:808-810`
- `Presentation/Nop.Web/Infrastructure/RouteProvider.cs:321` — `{orderId:int?}`
- `Presentation/Nop.Web.Framework/Mvc/Filters/CheckAccessPublicStoreAttribute.cs:11-102` — filter pattern
- `Plugins/Nop.Plugin.Payments.PayPalCommerce/Services/PayPalCommerceServiceManager.cs:2100-2181`
- `Libraries/Nop.Services/Installation/InstallRequiredData.cs:1870`, `:1893`
- `Tests/Nop.Tests/Nop.Services.Tests/Payments/TestPaymentMethod.cs:103-111`, `:221-231`; `Tests/Nop.Tests/Nop.Web.Tests/Public/Factories/CheckoutModelFactoryTests.cs:43-80`
- `.github/workflows/dotnet.yml` — CI: restore, build, test only; push/PR to `develop`

## Architecture Insights

- The extensibility contract is the main constraint on any refactor. nopCommerce promises override-ability through `partial` classes, `virtual` members (an explicit convention, #7840) and DI replacement. In practice, "internal" changes must keep `protected virtual` signatures and constructors stable, or change them additively.
- The storefront view and JS act as a public API for themes (#7744). Server-side changes in checkout should be additive on the JSON and URL contract.
- The test harness is service-level only. Refactors inside services (#1) can be protected today; refactors inside controllers (#2, #3) first need new harness infrastructure.

## Historical Context (from prior changes)

- [checkout-flow-analysis/research.md](../checkout-flow-analysis/research.md) — the source of the issue list. Two of its claims are updated here:
  - Open question "default of `PlaceOrderWithLock` / `OnePageCheckoutEnabled`" is **resolved**: `false` / `true` (InstallRequiredData.cs:1893, :1870).
  - "Partial orders after a mid-flow failure" remains **[I]**, not reproduced.
- [repo-map.md](../../map/repo-map.md) — the claim that `#8239` was reverted without a re-land is now explained by the maintainer: "there was no problem originally", so it was rolled back completely (from `gh`). The #7709 revert reason is now known (#7744, theme breakage).

## Related Research

- `context/changes/checkout-flow-analysis/research.md`
- `context/map/repo-map.md`, `context/map/evidence/1-history.md`, `2-discussion.md`

## Open Questions

- **[U]** Whether LinqToDB non-bulk repository calls enlist in an ambient `TransactionScope`, on SQL Server and on in-memory SQLite. This blocks step 3 of opportunity #1.
- **[U]** Third-party subclassing or overriding of OPS, CC and SCS, and theme overrides of the OPC view and JS. Not visible from the repo.
- **[U]** Why the 2010 transaction scope was left commented out. No surviving rationale; CodePlex-era issue numbers (#11792) cannot be looked up.
- **[U]** The real runtime cost of repeated validation (C7). Unmeasured.
- **[U]** Rationale for `[IgnoreAntiforgeryToken]` on `OpcSavePaymentInfo` (N3). Not traced; it belongs to a security review.
- **[U]** Release tags are absent locally, so the growth trend in C5 is sampled by date and counts injected `I*` fields, not constructor parameters.
- Search coverage: `gh search` recall is limited, so every "no issue found" statement is **[U]**, not proof of absence.
