<!-- IMPL-REVIEW-REPORT -->
# Implementation Review: CheckoutController Step Core and Guard Deduplication

- **Plan**: context/changes/refactor-opportunities/plan.md
- **Scope**: Full plan
- **Reviewed phases**: 1, 2, 3, 4, 5, 6
- **Date**: 2026-10-20
- **Verdict**: NEEDS ATTENTION
- **Findings**: 0 critical, 2 warnings, 6 observations

## Summary of evidence

- I found no behaviour drift. Both review passes compared the Phase 5 and Phase 6 step cores line by line against the original action bodies. They match: same GA writes in the same order, the same exception texts, and the same exits.
- The guard helper runs `CheckoutDisabled` → customer/store/cart → empty cart → flow → `IsGuestAsync && !AnonymousCheckoutAllowed`, which is today's order. All 20 actions call it (21 occurrences: the definition plus 20 calls). `OpcConfirmOrder` keeps it inside the captcha branch.
- The test files are unchanged since the Phase 2 commit (4d649f3c01), and Phase 2 changed no production code.
- No attribute or constructor lines changed. `cc-virtual-signatures.sh 4d649f3c01` reports that all 40 virtual declarations are unchanged; there are now 45, the 5 new ones as planned.
- The structural checks hold: `if (_orderSettings.CheckoutDisabled)` matches 3 times (the helper, `SelectBillingAddress` and `SelectShippingAddress`); `GetShippingOptionsAsync` appears once (in the step); `ValidatePaymentFormAsync` appears once (in the step).
- `dotnet build -c Release src`: 0 errors. The checkout suite passes 170/170, and the full `dotnet test src` run passes 1273 with 15 skipped and 0 failed.

## Verdicts

| Dimension | Verdict |
|-----------|---------|
| Plan Adherence | WARNING |
| Scope Discipline | PASS |
| Safety & Quality | WARNING |
| Architecture | WARNING |
| Pattern Consistency | PASS |
| Success Criteria | WARNING |

## Findings

### F1 — The "CheckoutDisabled before the customer is loaded" ordering is not pinned by any test

- **Severity**: ⚠️ WARNING
- **Impact**: 🔎 MEDIUM — real tradeoff; pause to reason through it
- **Dimension**: Success Criteria
- **Location**: src/Tests/Nop.Tests/Nop.Web.Tests/Public/Controllers/Checkout/CheckoutGuardTests.cs (whole fixture); src/Presentation/Nop.Web/Controllers/CheckoutController.cs:351-357
- **Detail**: The plan's "Critical Implementation Details" names this ordering as the one side-effect constraint of the guard: on a first visit, `GetCurrentCustomerAsync` can create and persist a guest. The harness always runs as the authenticated admin (CheckoutControllerTestBase.cs:46), so no guest is ever created. If someone later hoists `GetCurrentCustomerAsync` above the `CheckoutDisabled` check, the whole suite still passes. The code is correct today; only the safety net is missing.
- **Fix A ⭐ Recommended**: Add a test (purely additive, which the plan allows) that wraps `IWorkContext` in a counting spy for the controller under test, sets `CheckoutDisabled = true`, calls a multi-step action and an OPC action, and asserts that `GetCurrentCustomerAsync` was never called.
  - Strength: It pins the behaviour itself, whatever the code looks like, and it will also catch an override or a later refactor of the helper.
  - Tradeoff: The per-controller provider in the test base needs one injection point for a replacement `IWorkContext`. That is a small change to the test base.
  - Confidence: MED — the per-controller provider (CheckoutControllerTestBase.cs:164-223) already resolves services itself, so it can take an override, but I have not tried it.
  - Blind spot: How easy the existing test `IWorkContext` registration is to wrap.
- **Fix B**: Add an ast-grep rule to `checks/` asserting that inside `ValidateCheckoutAccessAsync` the `CheckoutDisabled` `if` comes before the first `GetCurrentCustomerAsync`.
  - Strength: No change to the test harness; it runs in seconds.
  - Tradeoff: It checks the code's shape, not its behaviour. It misses the same problem if it comes back through an override or a renamed helper, and it does not run in CI.
  - Confidence: HIGH — ast-grep is already used in checks/.
  - Blind spot: Writing order-sensitive ast-grep rules is awkward (`follows`/`precedes`).
- **Decision**: FIXED (Fix B) — added `checks/cc-guard-order.yml` + `checks/cc-guard-order.sh`. Passes on current code; mutation-tested: hoisted load, nested load and an extra early load each fail (exit 1); a removed `CheckoutDisabled` check fails the anchor (exit 2).

### F2 — `ThrowIfCheckoutGuardFailedAsync` fails open on an unknown outcome

- **Severity**: ⚠️ WARNING
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Safety & Quality
- **Location**: src/Presentation/Nop.Web/Controllers/CheckoutController.cs:419-429
- **Detail**: The `switch` has no `default`, so any outcome other than the four failures is treated as "passed". Its multi-step sibling `GetCheckoutGuardFailureResult` (:406) throws `ArgumentOutOfRangeException` on an unexpected value. If a subclass overrides `ValidateCheckoutAccessAsync` and returns an outcome outside these cases, the OPC actions continue past the guard, possibly with a null `Cart`/`Customer`. It can't be reached today.
- **Fix**: Add `default: if (!result.Passed) throw new ArgumentOutOfRangeException(nameof(result), result.Outcome, ...); break;` (or keep `Passed` as an explicit no-op case and throw on everything else), so it behaves like `GetCheckoutGuardFailureResult`.
- **Decision**: FIXED — added explicit `case Passed: break;` and `default: throw new ArgumentOutOfRangeException(...)`.

### F3 — The new nested types are `public`, which widens Nop.Web's public API

- **Severity**: 💡 OBSERVATION
- **Impact**: 🔎 MEDIUM — real tradeoff; pause to reason through it
- **Dimension**: Architecture
- **Location**: src/Presentation/Nop.Web/Controllers/CheckoutController.cs:2112-2271
- **Detail**: The plan chose `public` "so overriding subclasses can use them" (Implementation Approach §4). `protected` would also let subclasses use them, because `protected virtual` signatures can expose protected nested types. No controller in the codebase has nested types. The only precedents are `protected partial class PlaceOrderContainer` (OrderProcessingService.cs:3378) and `protected enum EntityEventType` (CacheEventConsumer.cs:127). Nothing outside CC uses the new types. This is a plan-level choice rather than drift.
- **Fix A ⭐ Recommended**: Make the nested types `protected`, following the `PlaceOrderContainer` precedent.
  - Strength: Subclasses can still use the types, and nothing new is added to the public API that would later need deprecating.
  - Tradeoff: It goes against what the plan wrote, so the plan needs a short addendum. Tests that name the types would need `TestableCheckoutController` to expose them.
  - Confidence: MED — I have not checked whether the test files refer to the types by name. If they do, this edits Phase-2-frozen tests, beyond pure additions.
  - Blind spot: Whether any test references `CheckoutController.CheckoutGuardResult` etc.
- **Fix B**: Keep them `public` and record the reason in the plan as an addendum.
  - Strength: No code churn, and it is consistent with the plan.
  - Tradeoff: Nop.Web gains permanent public surface area.
  - Confidence: HIGH.
  - Blind spot: None significant.
- **Decision**: FIXED (Fix A) — all 7 nested types are now `protected`. No test or public member refers to them, so the test files were not touched. The plan's §4 is annotated and the reason is recorded under "Addenda".

### F4 — Two harness deviations are explained in code but not recorded in the plan

- **Severity**: 💡 OBSERVATION
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Plan Adherence
- **Location**: src/Tests/Nop.Tests/Nop.Web.Tests/Public/Controllers/Checkout/CheckoutControllerTestBase.cs:154-223; ShippingMethodStepTests.cs:19-20
- **Detail**: (a) The plan said to build the controller with `ActivatorUtilities.CreateInstance(ServiceProvider)`. The code instead builds a per-controller provider that resolves from the shared one; the remarks say this avoids a stack overflow. (b) The plan asked for a new test-only `IPickupPointProvider` returning one point. The tests use the existing `PickupPoint.TestProvider` (points 1-3), and point "99" triggers the "not allowed" branch. Both choices are sound and documented in code, but the plan, which is the source of truth, does not mention them.
- **Fix**: Add a short "Implementation notes / accepted deviations" addendum to plan.md for (a) and (b).
- **Decision**: FIXED — recorded as F4a/F4b under plan.md "Addenda (post-implementation review)".

### F5 — The storefront manual checks have no recorded evidence

- **Severity**: 💡 OBSERVATION
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Success Criteria
- **Location**: context/changes/refactor-opportunities/plan.md:549, :563, :577, :591 (and 2.5/2.6 at :535-536)
- **Detail**: Manual items 3.5, 4.6, 5.6 and 6.6 (storefront walks) and 2.5/2.6 (mutation spot-checks) are marked `[x]` with the phase commit SHA, but nothing records what was observed. They can't be checked from the diff. The DI harness has no routing, filters or views (Phase 1 known gaps), so these manual walks are the only coverage for antiforgery, captcha and views.
- **Fix**: Confirm that the walks were actually done, and add a one-line observation per item in Progress (e.g. "guest OPC with Check/Money Order → completed page; anon-disallowed → error shown").
- **Decision**: SKIPPED

### F6 — The static TypeConverter registration in the test base leaks across fixtures

- **Severity**: 💡 OBSERVATION
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Safety & Quality
- **Location**: src/Tests/Nop.Tests/Nop.Web.Tests/Public/Controllers/Checkout/CheckoutControllerTestBase.cs:73-78
- **Detail**: The static constructor runs `TypeConverterRegistrationStartUpTask` once per process and never undoes it. Fixtures that run after a checkout fixture get the converters, and fixtures that run before do not, so results depend on run order. It is harmless today, because the registration matches production startup.
- **Fix**: Document this in the class remarks, or move the registration into `BaseNopTest` so every fixture sees the same state.
- **Decision**: FIXED — documented in the static constructor comment (process-wide, never undone, results may depend on run order). The test code is unchanged.

### F7 — The signature check skips the constructor and attributes and reads the working tree

- **Severity**: 💡 OBSERVATION
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Success Criteria
- **Location**: context/changes/refactor-opportunities/checks/cc-virtual-signatures.sh:58; checks/cc-virtual-members.yml
- **Detail**: The rule covers virtual methods and properties only. The "constructor unchanged" constraint and attribute preservation (`[IgnoreAntiforgeryToken]`, `[ValidateCaptcha]`, `[FormValueRequired]`, which the DI harness can't exercise) are not checked; the script strips attributes at :58. It also compares against the working tree rather than `HEAD`. I checked the diff by hand: no constructor or attribute lines changed.
- **Fix**: Add a `constructor_declaration` rule (exact match) and an attribute-preserving comparison for action methods. Alternatively, document in the script header that these are checked separately.
- **Decision**: FIXED — `cc-virtual-signatures.sh` now also compares virtual declarations with their attributes kept and the constructor declaration exactly; the working-tree-vs-HEAD behaviour is documented in the header. Passes against 4d649f3c01; mutation-tested: a dropped `[ValidateCaptcha]` and a renamed constructor parameter each fail (exit 1).

### F8 — The reflection coverage test only checks one direction

- **Severity**: 💡 OBSERVATION
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Success Criteria
- **Location**: src/Tests/Nop.Tests/Nop.Web.Tests/Public/Controllers/Checkout/CheckoutGuardTests.cs:261-297
- **Detail**: The test verifies that each of the 20 expected actions exists and has guard cases. It does not flag a new action that calls `ValidateCheckoutAccessAsync` but is missing from the list. The plan's wording ("against an explicit expected list of 20") allows this reading.
- **Fix**: Optionally add an ast-grep check in `checks/` that the number of `ValidateCheckoutAccessAsync(` call sites in CC equals the expected list (today 20).
- **Decision**: FIXED — added `checks/cc-guard-coverage.sh`, which compares, both ways, the set of CC methods calling `ValidateCheckoutAccessAsync` (via ast-grep) with the `_expectedGuardedActions` list read from the test file. That is sturdier than a fixed count. Passes (20 = 20); mutation-tested both directions (exit 1).

## Triage summary (2026-10-20)

- Fixed: F1 (Fix B), F2, F3 (Fix A), F4, F6, F7, F8
- Skipped: F5
- Verification after fixes: `dotnet build -c Release` of Nop.Tests (includes Nop.Web): 0 errors, 0 warnings. Checkout-filtered tests: 195/195 passed on the fresh build. `cc-guard-order.sh`, `cc-guard-coverage.sh` and `cc-virtual-signatures.sh 4d649f3c01`: all OK.
