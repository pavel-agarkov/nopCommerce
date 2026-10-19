# CheckoutController Step Core and Guard Deduplication — Plan Brief

> Full plan: `context/changes/refactor-opportunities/plan.md`
> Research: `context/changes/refactor-opportunities/research.md`

## What & Why

The research ranked three checkout refactor opportunities. This plan implements **#2**: remove the accidental copy-paste between the one-page (OPC) and multi-step checkout in `CheckoutController`, and the guard block inlined in 20 actions. CC is the most co-changed storefront file (32 kept commits since 2022), and the #7709 revert hit exactly these files. Every fix there currently has to be made twice. The change is a **pure refactor**: observable behaviour stays identical, per the CLAUDE.md hard rule.

## Starting Point

- **The guard block** (checkout disabled → empty cart → wrong flow → guest) is copied into 20 actions. The exits differ by flow: multi-step uses redirect or `Challenge`; OPC POSTs throw fixed texts that their `catch` logs and returns as `{ error = 1, message }`.
- **The shipping-method and payment-info action pairs** are near-copies (V34: 0.85 / 0.87). They differ only in exits and a few flow-specific branches.
- **There are no controller tests.** `BaseNopTest` is service-level only, on in-memory SQLite.

## Desired End State

- A DI controller harness in Nop.Tests builds CC on in-memory SQLite and calls its actions directly. It runs under the existing CI `dotnet test src` step.
- Characterization tests pin every guard outcome of the 20 actions and every branch of both pairs, and stay unedited through all later phases.
- CC has one `protected virtual` guard helper used by all 20 actions, and one shared `protected virtual` step method per extracted pair.
- All routes, JSON, messages, logs and existing signatures are unchanged.

## Key Decisions Made

| Decision | Choice | Why (1 sentence) | Source |
|---|---|---|---|
| Opportunity to implement | #2 only (OPC/multi-step dedup + guards) | It is the only ranked option that is a pure structural refactor | Plan |
| Behaviour-changing steps | Split into separate changes, not done here | CLAUDE.md: a phase that changes observable behaviour is a separate change | Plan |
| Transactional part of #1 | Excluded entirely | Not part of #2; LinqToDB enlistment stays an open question for a later change | Plan |
| Extensibility contract | Additive only: no ctor or existing-signature change | Third-party subclasses are unknown [U]; 40 virtual members and a 28-param ctor are public contract | Plan (Research: V1, V3) |
| Test harness | DI harness in Nop.Tests (`ActivatorUtilities` on `BaseNopTest`), test subclass stubbing only the two render helpers | WAF on SQLite needs production seams in Nop.Data, outside "CC-only, additive" | Plan review F1, F2 |
| WAF harness | Deferred to its own change | Full-pipeline coverage is worth having, but not at the cost of a data-layer change inside this refactor | Plan review F1 |
| Guard mechanism | `protected virtual` helper returning an outcome, mapped per action | Preserves per-flow exits and override-ability; a filter would be inherited by overrides | Plan (Research: CC:268 precedent) |
| Guard scope | All 20 guarded actions | Actually removes the lockstep-edit debt | Plan |
| Pairs with shared step core | Shipping method + payment info | Closest pairs (0.85/0.87); payment-method diverges (0.64), confirm is the money path | Plan (Research: V34) |

## Scope

**In scope:**
- DI controller harness in `Tests/Nop.Tests/Nop.Web.Tests/Public/Controllers/Checkout/`
- Guard characterization (20 actions), shipping-method and payment-info characterization
- Guard helper adopted in 13 multi-step-side actions (12 + `Index`) and 7 OPC actions
- Shared step cores for the shipping-method and payment-info pairs

**Out of scope:** opportunities #1 and #3; the payment-method, confirm and address pairs' bodies; a guard filter; constructor changes; any message, log, route or JSON change; the stale comment (N11); antiforgery on `OpcSavePaymentInfo` (N3); typing the OPC JSON contract (C8); CI workflow edits; a `WebApplicationFactory` harness (deferred, plan review F1).

## Architecture / Approach

Guard first, then small structural steps (Mikado order). Shared logic returns an **outcome value**:
- the guard returns `CheckoutGuardResult { Outcome, Customer, Store, Cart }`;
- the step methods return `ShippingMethodStepResult` / `PaymentInfoStepResult`.

Each action maps the outcome to its current exit. Multi-step actions return a redirect, `Challenge` or view. OPC actions `throw` the same `Exception` inside their existing `try`, so warning logs and the `{ error = 1, message }` JSON stay identical. Flow-specific branches stay in the actions:
- "shipping not required": multi-step saves `null`, OPC throws without saving;
- "payment workflow required": checked only in multi-step.

## Phases at a Glance

| Phase | What it delivers | Key risk |
|---|---|---|
| 1. DI controller harness | Test base + `TestableCheckoutController` in Nop.Tests, smoke tests | Shared `BaseNopTest` state leaking between tests (restore in `[TearDown]`) |
| 2. Characterization tests | Pinned guard + pair behaviour (incl. pickup and bypass branches), no prod change | Arranging test-only payment and pickup providers |
| 3. Guard helper + multi-step | Helper, outcome types, 12 actions + `Index` adopted | Guard side-effect ordering (guest creation before the disabled check) |
| 4. Guard in OPC | 7 OPC actions adopted, exact texts and logging kept | `OpcConfirmOrder` nested captcha position |
| 5. Shipping step core | One shared shipping-method step | Preserving the multi-step-only `null` save |
| 6. Payment-info step core | One shared payment-info step | `ModelState` side effects must stay identical |

**Prerequisites:** .NET SDK 10.0.x; the solution builds, including the plugins into `Presentation/Nop.Web/Plugins`; a local storefront for the manual smoke tests.
**Estimated effort:** ~4-6 sessions. Phases 1-2 are the bulk (harness and ~100 test cases); Phases 3-6 are small, one commit each.

## Open Risks & Assumptions

- The DI harness doesn't exercise routing, model binding, filters or Razor output. The storefront manual checks in Phases 3-6 cover those until the deferred WAF change lands.
- Third-party CC subclasses and theme overrides are **[U]**. The additive-only contract is the mitigation.
- Phase 2 tests become the behaviour oracle. Any edit to them in Phases 3-6 stops the phase as a suspected behaviour change.

## Success Criteria (Summary)

- The storefront checkout behaves exactly as before in both OPC and multi-step modes (manual smoke tests plus an unedited characterization suite).
- A guard or shared-step fix in CC is made in one place instead of 20 or 2.
- CI gains a controller-level safety net for checkout with no workflow change.
