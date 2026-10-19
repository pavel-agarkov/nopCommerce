<!-- PLAN-REVIEW-REPORT -->
# Plan Review: CheckoutController Step Core and Guard Deduplication

- **Plan**: context/changes/refactor-opportunities/plan.md
- **Mode**: Deep
- **Date**: 2026-10-19
- **Verdict**: REVISE
- **Findings**: 1 critical, 3 warnings, 1 observation

## Verdicts

| Dimension | Verdict |
|-----------|---------|
| End-State Alignment | WARNING |
| Lean Execution | PASS |
| Architectural Fitness | PASS |
| Blind Spots | FAIL |
| Plan Completeness | WARNING |

## Grounding
7/7 paths ✓, symbols ✓ (22 `CheckoutDisabled` sites, 2 `GetShippingOptionsAsync`, 2 `ValidatePaymentFormAsync`), brief↔plan ✓, Progress↔Phase ✓. Blast radius: no subclasses of CC, and none of the new names collide with anything.

## Findings

### F1 — The WAF-on-SQLite boot sequence can't work as specified

- **Severity**: ❌ CRITICAL
- **Impact**: 🔬 HIGH — architectural stakes; think carefully before deciding
- **Dimension**: Blind Spots
- **Location**: Phase 1 §2 (factory contract b/c), Desired End State
- **Detail**: `Program.Main` does all its setup before `Build()`, so the factory's overrides arrive too late:
  - `NopDbStartup.cs:65-71` calls `services.BuildServiceProvider()` and runs `ApplyUpSchemaMigrations` while services are still being configured. That happens before WAF's `ConfigureServices` can swap in `TestDataProviderManager` (override c).
  - `AddNopDbEngines` (`FluentMigratorExtensions.cs:68-79`) throws `NotImplementedException` for any provider other than MySql/SqlServer/PostgreSQL once the DB counts as installed. `DataProviderType` has no SQLite value, so SQLite is only reachable as `Unknown`, which throws.
  - Plugins load only from `App_Data/plugins.json` at boot (`PluginsInfo.cs:259-266`). Installing CheckMoneyOrder and FixedByWeightByTotal after boot never loads their controllers or views.
  - `PublishAppStartedEventAsync` runs against the DB before any client exists (permissions, themes, plugin install/update, scheduler), so "install the store after boot" is impossible.

  The pre-agreed gate would catch this, but Phase 1 as written describes something that cannot be built without changing production code in Nop.Data. That production change is outside "CC-only, additive".
- **Fix A ⭐ Recommended**: Make the DI fallback the primary harness and move WAF to its own later change.
  - Strength: No production change outside CC. `BaseNopTest` already wires all 28 constructor dependencies and SQLite. Matches the plan's own "don't keep tuning" gate.
  - Tradeoff: No routing, model binding or filters, and no view rendering. Characterization is weaker (see F2).
  - Confidence: HIGH — the DI path is proven by the existing Nop.Tests suite.
  - Blind spot: How many OPC branches can be pinned without a view engine.
- **Fix B**: Add a "Phase 0" production seam in Nop.Data (SQLite/`Unknown` in `AddNopDbEngines`, a pre-boot DataConfig through env vars, a pre-installed SQLite file DB and pre-seeded `plugins.json`), as its own revertible change.
  - Strength: Keeps the full-pipeline oracle (filters, binding, real JSON).
  - Tradeoff: Touches the production data layer and widens scope beyond #2. Needs its own plan and review. Also brings the HTTPS (`store.SslEnabled`) and guest-permission traps into play.
  - Confidence: MED — several boot hurdles stacked; each fixable, none proven.
  - Blind spot: Whether the SQLite schema migrations run cleanly with production startup.
- **Decision**: FIXED (Fix A)

### F2 — The DI harness leaves the rendering branches that Phases 5–6 restructure unpinned

- **Severity**: ⚠️ WARNING
- **Impact**: 🔎 MEDIUM — real tradeoff; pause to reason through it
- **Dimension**: Blind Spots
- **Location**: Phase 1 gate/fallback, Phase 2 §2–§3
- **Detail**: The fallback says branches that call `RenderPartialViewToStringAsync` are "asserted only up to the call, or skipped". Those are exactly the OPC success paths: `OpcLoadStepAfterShippingMethod`, the confirm-order JSON and the payment-info JSON. The multi-step `View()` / `ShippingMethod()` re-render branches are affected too. If F1 pushes the plan onto the fallback, Phases 5–6 would restructure branches the suite doesn't actually pin.
- **Fix**: In the fallback, register a stub `IRazorViewEngine` or override rendering with a test subclass that returns the view name. Assert on the result type, the view or partial name, the JSON keys and the GA state. List explicitly which branches are pinned in DI mode.
  - Strength: Keeps the oracle meaningful for every branch Phases 5–6 touch.
  - Tradeoff: A test subclass of CC overrides virtual helpers, which is slightly less faithful.
  - Confidence: MED — the rendering helpers are on the base controller; I haven't checked that they're virtual.
  - Blind spot: Whether `RenderPartialViewToStringAsync` can be overridden. (Resolved in triage: both rendering helpers are `protected virtual`, BaseController.cs:46, :73.)
- **Decision**: FIXED

### F3 — The Phase 4 "1 match" check will fail: two partial guards remain

- **Severity**: ⚠️ WARNING
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Plan Completeness
- **Location**: Phase 4 Automated Verification (4.3); Current State Analysis
- **Detail**: CC has 22 `if (_orderSettings.CheckoutDisabled)` sites, not 20. `SelectBillingAddress` (CC:647) and `SelectShippingAddress` (CC:804) have a disabled-only guard, and the plan leaves both actions untouched. After Phase 4 there will be 3 matches, not 1.
- **Fix**: Change 4.3 to "3 matches: `ValidateCheckoutAccessAsync`, `SelectBillingAddress`, `SelectShippingAddress`". Add one line to Current State Analysis noting the two partial guards are deliberately out of scope.
- **Decision**: FIXED

### F4 — The shipping characterization can't reach the pickup branches and misses one throw branch

- **Severity**: ⚠️ WARNING
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Blind Spots
- **Location**: Phase 1 §2 (plugin list), Phase 2 §2
- **Detail**:
  - The pickup branch runs only when `AllowPickupInStore` and `DisplayPickupInStoreOnShippingMethodPage` are both on. The second one defaults to false (`InstallRequiredData.cs:1874`).
  - The branch also needs a pickup provider. `Pickup.PickupInStore` isn't in the harness's plugin list, and the branch needs at least one configured pickup point.
  - `ParsePickupOptionAsync` throws "Pickup point is not allowed" (CC:187). Multi-step lets it escape as an unhandled exception; OPC catches it and returns JSON. That divergence goes straight through the extracted step method and is not pinned.
  - Multi-step failures fall back to `ShippingMethod()`, which can auto-save through `BypassShippingMethodSelectionIfOnlyOne` (CC:949-958). Pin the GA state for that case too.
- **Fix**: Add `Pickup.PickupInStore` plus one pickup point and both settings to the harness arrangements. Add the branches "pickup point not allowed" (multi-step throws, OPC returns `{error:1}`) and "invalid option with bypass-if-only-one on".
- **Decision**: FIXED (pickup provider is a test-only `IPickupPointProvider` in Nop.Tests, following F1)

### F5 — The "V1 superset check" compares names, not signatures, and has no runnable command

- **Severity**: OBSERVATION
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Plan Completeness
- **Location**: Criteria 3.3 / 4.4 / 5.4 / 6.4
- **Detail**: The research's V1 rule only matches members that carry a `virtual` modifier, and it isn't saved anywhere in the repo. A changed parameter list on an existing member would still pass a superset check by name.
- **Fix**: Save the rule as a file under the change folder. Diff the full declaration text (name, parameters, return type) of every virtual CC member between `<phase-2-sha>` and HEAD.
- **Decision**: FIXED
