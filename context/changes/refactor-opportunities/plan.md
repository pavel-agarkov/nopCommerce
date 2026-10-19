# CheckoutController Step Core and Guard Deduplication — Implementation Plan

## Overview

This plan carries out ranked opportunity **#2** from the research ([research.md](research.md), "Refactor opportunities" §2): deduplicate the one-page (OPC) and multi-step checkout flows in `CheckoutController` (CC), and replace the guard block inlined in 20 actions with one `protected virtual` helper. It is a **pure, behaviour-preserving refactor**. Characterization tests on a new `WebApplicationFactory` harness (in-memory SQLite, run by the existing `dotnet test` CI step) pin today's behaviour before the first structural change. Every phase is one revertible commit.

Opportunities #1 (order-placement failure handling) and #3 (explicit placed-order id) are deliberately **not** in this change; see "What We're NOT Doing".

Paths below are relative to `src/` unless they start with `context/` or `.github/`.

## Current State Analysis

- **Guard block, 20 copies (research V15, confirmed in planning).** The check order is the same in 18 actions: `CheckoutDisabled` → load customer, store, cart → `!cart.Any()` → flow-mode check → guest check. The 18 are:
  - **12 multi-step actions:** `BillingAddress` (CC:604), `NewBillingAddress` (:681), `ShippingAddress` (:772), `NewShippingAddress` (:828), `ShippingMethod` (:914), `SelectShippingMethod` (:968), `PaymentMethod` (:1052), `SelectPaymentMethod` (:1107), `PaymentInfo` (:1156), `EnterPaymentInfo` (:1205), `Confirm` (:1252), `ConfirmOrder` (:1278).
  - **6 of the 7 OPC actions:** `OnePageCheckout` GET (:1495), `OpcSaveBilling` (:1519), `OpcSaveShipping` (:1673), `OpcSaveShippingMethod` (:1792), `OpcSavePaymentMethod` (:1880), `OpcSavePaymentInfo` (:1953).
  
  Two actions vary:
  - **`OpcConfirmOrder`** (:2019), the seventh OPC action: the standard guard is nested inside the captcha branch, after the customer was already loaded for the captcha decision (:2023-2041).
  - **`Index`** (:342): no flow-mode check, and its guest condition also includes `downloadableProductsRequireRegistration` (:355-359).
- **Guard exits differ by flow.**
  - Multi-step actions return `RedirectToRoute(CART)` (disabled or empty cart), `RedirectToRoute(CHECKOUT_ONE_PAGE)` (OPC enabled), or `Challenge()` (guest not allowed).
  - `OnePageCheckout` GET is the same, except that a disabled OPC redirects to `CHECKOUT` (:1507-1508).
  - The 6 OPC POSTs throw `Exception`s with fixed texts, the same in all 6:
    - localized `Checkout.Disabled`;
    - `"Your cart is empty"`;
    - `"One page checkout is disabled"`;
    - `"Anonymous checkout is not allowed"`.
    
    Their own `catch` logs these with `_logger.WarningAsync(exc.Message, exc, customer)` and returns `Json(new { error = 1, message })` (e.g. :1872-1876).
- **Shipping-method pair.** `SelectShippingMethod` (:968-1050) and `OpcSaveShippingMethod` (:1792-1877) share their pickup, parse, lookup, delivery-date and save logic (V34: 0.85 similarity). Branches that differ:
  - **Shipping not required.** Multi-step saves a `null` `SelectedShippingOptionAttribute` and redirects to `CHECKOUT_PAYMENT_METHOD` (:987-992). OPC throws `"Shipping is not required"` **without saving** (:1813-1814).
  - **Empty or unparsable option.** Multi-step re-renders `ShippingMethod()` (:1011-1015). OPC throws `"Selected shipping method can't be parsed"`.
  - **Option not found.** Multi-step re-renders `ShippingMethod()` (:1038-1039). OPC throws `"Selected shipping method can't be loaded"`.
  - **Pickup chosen, or a normal save.** Multi-step redirects to `CHECKOUT_PAYMENT_METHOD`. OPC returns `OpcLoadStepAfterShippingMethod(cart)`.
- **Payment-info pair.** `EnterPaymentInfo` (:1205-1250) and `OpcSavePaymentInfo` (:1953-2015) share the load-method, validate-form and save-request logic (V34: 0.87). Branches that differ:
  - **Payment workflow not required.** Only multi-step checks this; it redirects to `CHECKOUT_CONFIRM` (:1225-1227).
  - **Method not found.** Multi-step redirects to `CHECKOUT_PAYMENT_METHOD`. OPC throws `"Payment method is not selected"`.
  - **Valid form.** Multi-step redirects to `CHECKOUT_CONFIRM`. OPC renders the `OpcConfirmOrder` partial in the `confirm-order` section with `goto_section = "confirm_order"`.
  - **Invalid form.** Multi-step returns `View(model)`. OPC renders the `OpcPaymentInfo` partial in the `payment-info` section.
  
  `OpcSavePaymentInfo` has `[IgnoreAntiforgeryToken]` (:1952).
- **Precedent for the shape.** `protected virtual EditAddressAsync(…, Func<…, Task<JsonResult>> getResult)` (CC:268) and `DeleteAddressAsync` (:311) already share logic between OPC and multi-step actions (V33).
- **Extensibility contract.** CC is `public partial`, derives from `BasePublicController`, and has 40 `public/protected virtual` members (V1) and 28 constructor parameters (V3). "All service methods virtual" is a stated convention (#7840). There are no in-repo subclasses (V23); third-party subclasses are **[U]**.
- **Safety net today.** No controller tests exist.
  - CI (`.github/workflows/dotnet.yml`) runs `dotnet restore src` → `dotnet build --configuration Release src` → `dotnet test --no-build --configuration Release src` on `windows-latest`.
  - Nop.Tests (`BaseNopTest`) uses in-memory SQLite through `TestDataProviderManager` → `SqLiteNopDataProvider`, since `DataProviderType.Unknown` maps to SQLite (TestDataProviderManager.cs:21-29). It installs the store in `BaseNopTest.Init` (BaseNopTest.cs:110-140).

## Desired End State

- A new test project, `Tests/Nop.Web.Integration.Tests`, boots Nop.Web through `WebApplicationFactory<Program>` on in-memory SQLite.
  - `dotnet test src` runs it in CI with no workflow change.
  - It leaves no file under `Presentation/Nop.Web` created or modified, including git-ignored files such as `App_Data/appsettings.json` and `App_Data/plugins.json`.
  - If the Phase 1 gate fails, the DI fallback harness in Nop.Tests takes its place, as agreed.
- Characterization tests pin:
  - every guard outcome of all 20 guarded actions;
  - every branch of the shipping-method and payment-info pairs.
  
  They pass both before and after each structural phase, without being edited.
- CC has:
  - one `protected virtual` guard helper, used by all 20 actions;
  - one `protected virtual` shipping-method step method and one payment-info step method, each mapped by both flows.
  
  Action names, routes, HTTP attributes, JSON keys and values, exception texts, warning logging, redirect targets and every existing `public/protected virtual` signature and the constructor are unchanged. New types and members are additive.

Verification: the characterization suite is green at the Phase 2 commit, and green and unedited at every later phase commit. A `git diff` of the test files between the Phase 2 commit and HEAD is empty, except for additions.

### Key Discoveries:

- `BaseNopTest` already registers all 28 CC constructor dependencies, including every `ISettings` through reflection (BaseNopTest.cs:438-442). It also registers a `DefaultHttpContext`, `NopTestUrlHelper` (returns `""`), TempData and session (BaseNopTest.cs:235-257). The DI fallback is therefore viable.
- `Program` is `public partial class Program` with a top-level `Main` that uses `WebApplication.CreateBuilder` (Nop.Web/Program.cs:8-12), which `WebApplicationFactory<Program>` supports.
- On first boot, `BindApplicationSettings` writes `App_Data/appsettings.json` under the content root through `AppSettingsHelper.SaveAppSettings` (Nop.Web.Framework/Infrastructure/Extensions/ServiceCollectionExtensions.cs:81-86). That file, `plugins.json` and `installedPlugins.json` are git-ignored (`.gitignore`), so plain `git status` would not reveal pollution.
- `DataSettingsManager.IsDatabaseInstalled()` is true when `DataConfig.ConnectionString` is non-empty (DataSettingsManager.cs:144-146). `DataConfig` comes from `AppSettings` (:124). Production `DataProviderManager` (NopDbStartup.cs:58) knows SqlServer, MySql and PostgreSQL only.
- Startup sets process-wide statics (`Singleton<AppSettings>`, `Singleton<DataConfig>`, `Singleton<ITypeFinder>`, `CommonHelper.DefaultFileProvider`, `EngineContext`), and so does `BaseNopTest`. The two harnesses cannot share a test process. `dotnet test` runs each test assembly in its own testhost.
- Plugins are built into `Presentation/Nop.Web/Plugins/<Group.Name>` (e.g. Nop.Plugin.Payments.CheckMoneyOrder.csproj:12-13). Razor runtime compilation is enabled (ServiceCollectionExtensions.cs:310).
- CC has `[AutoValidateAntiforgeryToken]` at class level (CC:32). `[ValidateCaptcha]` is on `ConfirmOrder` and `OpcConfirmOrder` only (CC:1276, :2017).

## What We're NOT Doing

- **Opportunity #1** (order-placement failure handling in OPS, including the `TransactionScope` spike): excluded. Its behaviour-changing steps go to separate changes.
- **Opportunity #3** (explicit placed-order id in OPC): excluded; it is a behaviour change.
- **Any behaviour change.** No message, log level, redirect, JSON shape, route or status code changes. Behaviour changes found during the work become separate changes (CLAUDE.md hard rule).
- **The payment-method pair** (`SelectPaymentMethod`/`OpcSavePaymentMethod`, V34 0.64) and **the confirm pair** (`ConfirmOrder`/`OpcConfirmOrder`, the money path through `PlaceOrderAsync`): no shared step core. These actions receive only the guard helper.
- **Address pairs** (`NewBillingAddress`/`OpcSaveBilling`, `NewShippingAddress`/`OpcSaveShipping`): guard helper only.
- **The non-guard actions** (`Completed`, `GetAddressById`, the address edit/delete actions, `SelectBillingAddress`, `SelectShippingAddress`, `OpcCompleteRedirectionPayment`): untouched.
- **A guard `TypeFilterAttribute`.** It was rejected because it is inherited by overriding actions, which would change the contract for third-party subclasses, and because it runs at a different pipeline point.
- **Constructor or existing-signature changes** on CC, and moving step logic into a new service.
- **A real antiforgery token flow in tests.** A test-only bypass was chosen.
- **Fixes noticed along the way:** the stale "X seconds" comment (N11), `[IgnoreAntiforgeryToken]` on `OpcSavePaymentInfo` (N3), and typing the OPC JSON contract (C8).
- **CI workflow changes.** The new project runs under the existing `dotnet test src` step.

## Implementation Approach

1. **Guard first.** Build the harness and gate on it (Phase 1). Then write characterization tests against the unchanged CC (Phase 2).
2. **Structural change in small steps, one revertible commit each, Mikado-ordered:**
   - Introduce the guard helper and adopt it in the multi-step actions (Phase 3).
   - Adopt it in the OPC actions (Phase 4).
   - Extract the shipping-method step core (Phase 5), then the payment-info step core (Phase 6).
3. **How the flows share code.** Each new piece of shared logic returns an **outcome value**. Each action maps that outcome to its existing exit (redirect, `Challenge`, view, JSON, or `throw` inside the existing OPC `try`). This is the `EditAddressAsync` precedent turned into a result object instead of a callback. Because the OPC actions still throw the same `Exception` inside their existing `try`, warning logging and the `{ error = 1, message }` JSON are preserved without duplicating the catch.
4. **Additive, contract-safe types.** New members are `protected virtual` (nopCommerce convention). New outcome types are `public`, so overriding subclasses can use them. Nothing existing is removed or re-signed.

## Critical Implementation Details

**Side-effect ordering in the guard.** `CheckoutDisabled` must be evaluated **before** `_workContext.GetCurrentCustomerAsync()`. On a first visit that call can create and persist a guest customer, and today a disabled checkout returns before it. Within the guest check, `IsGuestAsync(customer)` must still be evaluated before `AnonymousCheckoutAllowed` (today's `&&` order). In `OpcConfirmOrder` the helper runs where the inline guard runs today, inside the captcha branch. Re-reading the customer there returns the request-cached instance.

**Harness isolation (Phase 1).** The default content root of `WebApplicationFactory` is the Nop.Web project directory, so a naive host writes git-ignored files into the developer's working tree. A stray `App_Data/appsettings.json` with a SQLite connection string would make a later local `Nop.Web` run think it is installed. The isolation check therefore uses `git status --porcelain --ignored`, not plain `git status`.

**Static-state isolation.** The new project must not reference `BaseNopTest` or run any Nop.Tests fixture. Link the two SQLite source files (`SqLiteNopDataProvider.cs`, `TestDataProviderManager.cs`) into the project as compile items rather than adding a `ProjectReference` to Nop.Tests. This keeps Nop.Tests types (the `TestPaymentMethod` plugin and test settings) out of `WebAppTypeFinder`'s assembly scan.

## Phase 1: WebApplicationFactory harness (gate)

### Overview

Add a new test project that boots the real Nop.Web pipeline on in-memory SQLite, installs a store, and lets tests send checkout requests as a guest customer. The phase ends at a **gate**: either the harness boots cleanly, or the plan switches to the agreed DI fallback.

### Changes Required:

#### 1. New test project

**File**: `Tests/Nop.Web.Integration.Tests/Nop.Web.Integration.Tests.csproj`, plus the `src/NopCommerce.sln` entry under the existing `Tests` solution folder

**Intent**: Host the WAF characterization tests in their own assembly, so the process-wide statics set by Nop.Web startup never meet `BaseNopTest`'s.

**Contract**:
- `net10.0` (inherited from `Directory.Build.props`).
- NUnit, NUnit3TestAdapter, Microsoft.NET.Test.Sdk and AwesomeAssertions, at the versions Nop.Tests.csproj uses.
- `Microsoft.AspNetCore.Mvc.Testing`, matching the repo's ASP.NET Core 10.0.x patch.
- `ProjectReference` to `Presentation/Nop.Web`.
- `SqLiteNopDataProvider.cs` and `TestDataProviderManager.cs` linked from `Tests/Nop.Tests/` as `<Compile Include=… Link=…>`, with no reference to Nop.Tests.

#### 2. Web application factory

**File**: `Tests/Nop.Web.Integration.Tests/NopWebApplicationFactory.cs`

**Intent**: Boot Nop.Web against a fresh in-memory SQLite database, install the store with the same `InstallationSettings` as `BaseNopTest.Init`, and install the plugins the checkout needs (`Payments.CheckMoneyOrder`, `Shipping.FixedByWeightByTotal`). Neither the boot nor the tests may touch the source tree.

**Contract**:
- `WebApplicationFactory<Nop.Web.Program>` with an overridden host configuration that:
  - (a) points the content root at a per-run temp copy, or otherwise redirects every `App_Data` and `Plugins` write away from `Presentation/Nop.Web`. The implementer picks the mechanism; the invariant is the isolation success criterion below.
  - (b) supplies a `DataConfig` whose non-empty connection string targets a shared-cache in-memory SQLite database kept open for the factory's lifetime.
  - (c) replaces `IDataProviderManager` with the linked `TestDataProviderManager`.
  - (d) registers a test-only antiforgery bypass, for example by replacing `IAntiforgery` with a validating no-op, so POSTs succeed without tokens.
- One factory per test run (NUnit `[SetUpFixture]`).
- The client keeps cookies, so successive requests act as the same guest customer.

#### 3. Test helpers

**File**: `Tests/Nop.Web.Integration.Tests/CheckoutTestHelpers.cs` (name at the implementer's discretion)

**Intent**: Give tests a small vocabulary for arranging and asserting:
- set `OrderSettings`, `ShippingSettings`, `PaymentSettings` and `CaptchaSettings` values through `ISettingService` in a host scope, and restore them after each test;
- add a sample product to the current guest's cart, or clear the cart;
- set or clear the customer's checkout GenericAttributes (selected/offered shipping options, selected payment method);
- read a response as either a redirect (status + `Location`) or JSON (parsed keys/values).

**Contract**: Test-only; no production code.

#### 4. Gate smoke test

**File**: `Tests/Nop.Web.Integration.Tests/HarnessSmokeTests.cs`

**Intent**: Prove that the harness works end to end before Phase 2 depends on it.

**Contract**: Two tests:
- a guest with an empty cart doing `GET /checkout` is redirected to the cart route;
- a guest with one cart item, OPC enabled and shipping not required, sending `POST` to `OpcSaveShippingMethod` gets JSON `{ error: 1, message: "Shipping is not required" }`.

**Gate / fallback (agreed):** if the harness can't meet every automated criterion below, do not keep tuning it. Switch to the **DI fallback**: `Tests/Nop.Tests/Nop.Web.Tests/Public/Controllers/CheckoutControllerTests.cs` deriving from the existing test base. It builds CC with `ActivatorUtilities.CreateInstance` on `ServiceProvider`, with a `ControllerContext` on the shared `HttpContext`. It asserts `RedirectToRouteResult.RouteName`, `ChallengeResult`, `ViewResult` and `JsonResult.Value`. Record the gate outcome and the reason in the Phase 1 block of this plan before continuing. Known gap of the fallback: no routing, no model binding, no filters. OPC branches that call `RenderPartialViewToStringAsync` need a view engine that `BaseNopTest` doesn't register, so those branches are asserted only up to the call, or skipped with a note.

### Success Criteria:

#### Automated Verification:

- Solution builds: `dotnet build --configuration Release src`
- New project's tests pass: `dotnet test --no-build --configuration Release src/Tests/Nop.Web.Integration.Tests`
- Existing tests still pass: `dotnet test --no-build --configuration Release src/Tests/Nop.Tests`
- Full CI-equivalent run passes: `dotnet test --no-build --configuration Release src`
- Source tree stays clean: the output of `git status --porcelain --ignored src/Presentation/Nop.Web` is identical before and after a test run

#### Manual Verification:

- Gate outcome (WAF or DI fallback) and reason recorded in this phase block
- Local `Nop.Web` still starts in its previous state (installed or install wizard) after running the new tests

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the manual testing was successful before proceeding to the next phase.

---

## Phase 2: Characterization tests

### Overview

Pin the current behaviour of every guard outcome in all 20 guarded actions, and every branch of the two pairs that Phases 5-6 restructure. This phase changes no production code. The tests are written against the unchanged CC and must not be edited in later phases.

### Changes Required:

#### 1. Guard characterization

**File**: `Tests/Nop.Web.Integration.Tests/Checkout/CheckoutGuardTests.cs`

**Intent**: For each of the 20 actions, assert the exact exit for each guard condition it checks, in today's order:
- checkout disabled;
- empty cart;
- wrong flow (OPC enabled for a multi-step action, OPC disabled for an OPC action);
- guest with anonymous checkout disallowed.

**Contract**:
- **Multi-step actions** (12): the redirect route (`CART` / `CHECKOUT_ONE_PAGE`) or a `Challenge` (302 to the login path).
- **`OnePageCheckout` GET**: `CART` / `CHECKOUT` / `Challenge`.
- **The 6 OPC POSTs**: JSON `{ error: 1, message: <exact text> }`, with the localized `Checkout.Disabled` text read from `ILocalizationService` in the test, plus a warning log entry for the current customer.
- **`OpcConfirmOrder`**: the same JSON with the captcha disabled.
- **`Index`**: `CART` for disabled and for an empty cart; `Challenge` for a guest when anonymous checkout is disallowed, and also when downloadable products require registration.
- **Precedence**: a case with two conditions true at once (e.g. checkout disabled and an empty cart) asserts the first-checked exit.
- Parameterize with NUnit `[TestCase]` per action/condition. Test the multi-step POST actions with the `nextstep` form field so that `[FormValueRequired("nextstep")]` selects them.

#### 2. Shipping-method pair characterization

**File**: `Tests/Nop.Web.Integration.Tests/Checkout/ShippingMethodStepTests.cs`

**Intent**: Pin every branch of `SelectShippingMethod` and `OpcSaveShippingMethod` listed under Current State Analysis, including the stored state.

**Contract**:
- **Per branch:** the exit (redirect route, re-rendered `ShippingMethod` view 200, JSON keys `update_section.name`, `goto_section`, `error`, `message`) and the resulting `SelectedShippingOptionAttribute`, `SelectedPickupPointAttribute` and `DesiredDeliveryDate` values. Read them back through `IGenericAttributeService` in a host scope.
- **Branches:**
  - shipping not required (multi-step saves `null`; OPC saves nothing);
  - pickup in store chosen;
  - pickup offered but not chosen;
  - empty option string;
  - malformed option (no separator);
  - option not found;
  - success with offered options cached in GA;
  - success with no cached options (loaded from the shipping service).

#### 3. Payment-info pair characterization

**File**: `Tests/Nop.Web.Integration.Tests/Checkout/PaymentInfoStepTests.cs`

**Intent**: Pin every branch of `EnterPaymentInfo` and `OpcSavePaymentInfo` listed under Current State Analysis.

**Contract**:
- **Branches:**
  - payment workflow not required (multi-step only, redirect to `CHECKOUT_CONFIRM`);
  - no selected method (redirect to `CHECKOUT_PAYMENT_METHOD` vs JSON `"Payment method is not selected"`);
  - valid form (redirect to `CHECKOUT_CONFIRM` vs JSON `update_section.name = "confirm-order"`, `goto_section = "confirm_order"`, non-empty `html`);
  - invalid form (multi-step 200 view vs JSON `update_section.name = "payment-info"`).
- Check that the process payment request was stored on the valid paths (`IOrderProcessingService.GetProcessPaymentRequestAsync` returns non-null), and not stored on the invalid paths.
- **Inducing an invalid form.** CheckMoneyOrder accepts any form, so this branch needs a payment method whose `ValidatePaymentFormAsync` can return a warning. Use the stock plugin that validates its form, or a test-only `IPaymentMethod` plugin registered in the test host. The implementer picks one and notes the choice in the test file. If neither can be induced in the WAF host, assert that branch in the DI harness and record why.

### Success Criteria:

#### Automated Verification:

- All characterization tests pass against unchanged production code: `dotnet test --no-build --configuration Release src/Tests/Nop.Web.Integration.Tests`
- Production code unchanged in this phase: `git diff --stat HEAD~1 -- src/Presentation src/Libraries` is empty for the phase commit
- Every one of the 20 guarded actions appears in at least one guard test case (checked by a test that reflects over CC's `public virtual` action methods against an explicit expected list of 20)
- Full suite passes: `dotnet test --no-build --configuration Release src`

#### Manual Verification:

- Mutation spot-check: temporarily swapping one guard exit in CC (e.g. `CART` → `CHECKOUT`) makes at least one test fail; revert afterwards
- Mutation spot-check: temporarily removing the `null` save in the multi-step "shipping not required" branch makes a shipping-method test fail; revert afterwards

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the manual testing was successful before proceeding to the next phase.

---

## Phase 3: Guard helper and multi-step adoption

### Overview

Introduce the shared guard helper and its outcome type, then replace the inlined guard in the 12 multi-step actions and `Index`.

### Changes Required:

#### 1. Guard outcome types

**File**: `Presentation/Nop.Web/Controllers/CheckoutController.cs` (nested public types, keeping CC a single file)

**Intent**: Describe the guard's verdict and hand back the customer, store and cart it already loaded, so actions don't load them twice.

**Contract**: Additive public types:
- `CheckoutFlow`: `None` (no flow-mode or guest check; used by `Index`), `MultiStep`, `OnePage`.
- `CheckoutGuardResult`: `Outcome` ∈ {`Passed`, `CheckoutDisabled`, `CartEmpty`, `WrongFlow`, `GuestNotAllowed`}; `Customer`, `Store`, `Cart` (null when the guard stopped before loading them).

#### 2. Guard helper and multi-step mapper

**File**: `Presentation/Nop.Web/Controllers/CheckoutController.cs`

**Intent**: Run the checks once, in today's order, and map a failed result to today's multi-step and `OnePageCheckout` exits.

**Contract**:
- `protected virtual Task<CheckoutGuardResult> ValidateCheckoutAccessAsync(CheckoutFlow flow)`. It checks `CheckoutDisabled`, then loads customer, store and cart, then empty cart, then the flow mode (`MultiStep`: fail if OPC is enabled; `OnePage`: fail if OPC is disabled; `None`: skip), then guest (`IsGuestAsync(customer) && !AnonymousCheckoutAllowed`; skipped for `None`).
- `protected virtual IActionResult GetCheckoutGuardFailureResult(CheckoutGuardResult result, CheckoutFlow flow)`:
  - `CheckoutDisabled` / `CartEmpty` → `RedirectToRoute(CART)`;
  - `WrongFlow` → `RedirectToRoute(CHECKOUT_ONE_PAGE)` for `MultiStep`, `RedirectToRoute(CHECKOUT)` for `OnePage`;
  - `GuestNotAllowed` → `Challenge()`.

#### 3. Adopt in multi-step actions and Index

**File**: `Presentation/Nop.Web/Controllers/CheckoutController.cs`

**Intent**: Replace the inlined guard in `BillingAddress`, `NewBillingAddress`, `ShippingAddress`, `NewShippingAddress`, `ShippingMethod`, `SelectShippingMethod`, `PaymentMethod`, `SelectPaymentMethod`, `PaymentInfo`, `EnterPaymentInfo`, `Confirm` and `ConfirmOrder` with the helper plus mapper. In `Index`, use `CheckoutFlow.None` and keep its own guest/downloadable condition inline after the helper.

**Contract**:
- Action signatures and attributes are unchanged.
- Locals `customer`/`store`/`cart` come from the result.
- Code after the guard is untouched. This includes the second `IsGuestAsync` check in `NewBillingAddress` and `ConfirmOrder`, which belongs to their bodies, not the guard.

### Success Criteria:

#### Automated Verification:

- Characterization suite passes unedited: `dotnet test --no-build --configuration Release src/Tests/Nop.Web.Integration.Tests`
- Test files unchanged since Phase 2: `git diff <phase-2-sha> HEAD -- src/Tests/Nop.Web.Integration.Tests` is empty
- No existing CC member signature changed: the `public/protected virtual` member list of CC at HEAD is a superset of the list at `<phase-2-sha>` (ast-grep V1 rule)
- Full suite passes: `dotnet test --no-build --configuration Release src`

#### Manual Verification:

- Storefront smoke test with OPC disabled: walk the multi-step checkout as a guest to the confirm page; with anonymous checkout disallowed, the login challenge appears

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the manual testing was successful before proceeding to the next phase.

---

## Phase 4: Guard adoption in OPC actions

### Overview

Use the helper in `OnePageCheckout` and the six `Opc*` POST actions, keeping the exact exception texts and the existing catch, which logs a warning and returns `{ error = 1, message }`.

### Changes Required:

#### 1. OPC guard thrower

**File**: `Presentation/Nop.Web/Controllers/CheckoutController.cs`

**Intent**: Turn a failed guard result into the same `Exception` the OPC actions throw today, so their existing `catch` produces identical logs and JSON.

**Contract**: `protected virtual Task ThrowIfCheckoutGuardFailedAsync(CheckoutGuardResult result)` throws `new Exception(text)`:
- `CheckoutDisabled` → `await _localizationService.GetResourceAsync("Checkout.Disabled")`;
- `CartEmpty` → `"Your cart is empty"`;
- `WrongFlow` → `"One page checkout is disabled"`;
- `GuestNotAllowed` → `"Anonymous checkout is not allowed"`.

For `Passed` it does nothing. The exception type stays `System.Exception`.

#### 2. Adopt in OPC actions

**File**: `Presentation/Nop.Web/Controllers/CheckoutController.cs`

**Intent**:
- `OnePageCheckout` GET: use `ValidateCheckoutAccessAsync(CheckoutFlow.OnePage)` plus `GetCheckoutGuardFailureResult(…, OnePage)`.
- `OpcSaveBilling`, `OpcSaveShipping`, `OpcSaveShippingMethod`, `OpcSavePaymentMethod`, `OpcSavePaymentInfo`: call the helper, then `ThrowIfCheckoutGuardFailedAsync` **inside** the existing `try`.
- `OpcConfirmOrder`: do the same at the current position, inside the captcha branch.

**Contract**:
- Action signatures and attributes are unchanged, including `[IgnoreAntiforgeryToken]` and `[ValidateCaptcha]`.
- Each `try`/`catch` stays where it is.
- Body checks after the guard (e.g. `"Shipping is not required"`, the second guest check in `OpcSaveBilling`/`OpcConfirmOrder`) are untouched.

### Success Criteria:

#### Automated Verification:

- Characterization suite passes unedited: `dotnet test --no-build --configuration Release src/Tests/Nop.Web.Integration.Tests`
- Test files unchanged since Phase 2: `git diff <phase-2-sha> HEAD -- src/Tests/Nop.Web.Integration.Tests` is empty
- No inlined guard copies remain: ast-grep `if (_orderSettings.CheckoutDisabled) $B` in CC matches only inside `ValidateCheckoutAccessAsync` (1 match)
- No existing CC member signature changed (V1 superset check as in Phase 3)
- Full suite passes: `dotnet test --no-build --configuration Release src`

#### Manual Verification:

- Storefront smoke test with OPC enabled (the default): complete a one-page checkout as a guest with Check/Money Order; with anonymous checkout disallowed, the OPC shows the "Anonymous checkout is not allowed" error

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the manual testing was successful before proceeding to the next phase.

---

## Phase 5: Shipping-method shared step core

### Overview

Move the shared logic of `SelectShippingMethod` and `OpcSaveShippingMethod` into one `protected virtual` step method returning a step result. Each action maps the result to its current exit.

### Changes Required:

#### 1. Step result type and step method

**File**: `Presentation/Nop.Web/Controllers/CheckoutController.cs`

**Intent**: One implementation of the pickup, parse, lookup, delivery-date and save sequence, with outcomes detailed enough to reproduce both flows' differing branches exactly.

**Contract**:
- Additive public type `ShippingMethodStepResult` with an outcome:
  - `PickupSaved`: the pickup option was saved;
  - `OptionSaved`: the shipping option (and delivery date, if any) was saved;
  - `OptionNotParsed`: empty or malformed option;
  - `OptionNotFound`.
- `protected virtual Task<ShippingMethodStepResult> SaveShippingMethodStepAsync(string shippingoption, IFormCollection form, Customer customer, Store store, IList<ShoppingCartItem> cart)`.
- The step starts **after** the "shipping required" check. That check stays in each action, because the two flows handle it differently: multi-step saves `null` and redirects, OPC throws without saving.
- The step performs the pickup branch, including saving a `null` `SelectedPickupPointAttribute` when pickup is offered but not chosen, then parse, lookup (GA cache first, then the shipping service), delivery date and save, with the same GA writes in the same order as today.

#### 2. Map in both actions

**File**: `Presentation/Nop.Web/Controllers/CheckoutController.cs`

**Intent**: Each action becomes guard → shipping-required check → step → map.

**Contract**:
- `SelectShippingMethod`: `PickupSaved` / `OptionSaved` → `RedirectToRoute(CHECKOUT_PAYMENT_METHOD)`; `OptionNotParsed` / `OptionNotFound` → `await ShippingMethod()`.
- `OpcSaveShippingMethod`: `PickupSaved` / `OptionSaved` → `await OpcLoadStepAfterShippingMethod(cart)`; `OptionNotParsed` → throw `"Selected shipping method can't be parsed"`; `OptionNotFound` → throw `"Selected shipping method can't be loaded"`, inside the existing `try`.

### Success Criteria:

#### Automated Verification:

- Characterization suite passes unedited: `dotnet test --no-build --configuration Release src/Tests/Nop.Web.Integration.Tests`
- Test files unchanged since Phase 2: `git diff <phase-2-sha> HEAD -- src/Tests/Nop.Web.Integration.Tests` is empty
- Shared logic exists once: ast-grep `$S.GetShippingOptionsAsync($$$)` in CC no longer appears in either `SelectShippingMethod` or `OpcSaveShippingMethod`, only in the step method and in calls outside the pair
- No existing CC member signature changed (V1 superset check)
- Full suite passes: `dotnet test --no-build --configuration Release src`

#### Manual Verification:

- Storefront: choose a shipping method in both flows (OPC on, then off); with "pickup in store" enabled, choose pickup in both flows; the next step loads as before

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the manual testing was successful before proceeding to the next phase.

---

## Phase 6: Payment-info shared step core

### Overview

Move the shared load-method, validate-form and save-request logic of `EnterPaymentInfo` and `OpcSavePaymentInfo` into one `protected virtual` step method. Each action maps the result to its current exit.

### Changes Required:

#### 1. Step result type and step method

**File**: `Presentation/Nop.Web/Controllers/CheckoutController.cs`

**Intent**: One implementation of "load the selected payment method, validate the posted form into `ModelState`, store the process payment request when valid".

**Contract**:
- Additive public type `PaymentInfoStepResult` with an outcome (`MethodNotSelected`, `Invalid`, `Saved`) and the loaded `IPaymentMethod` (null for `MethodNotSelected`).
- `protected virtual Task<PaymentInfoStepResult> SavePaymentInfoStepAsync(IFormCollection form, Customer customer, Store store)`. It loads the plugin by `SelectedPaymentMethodAttribute`, adds each `ValidatePaymentFormAsync` warning to `ModelState` with key `""`, and on `ModelState.IsValid` calls `SetProcessPaymentRequestAsync(await paymentMethod.GetPaymentInfoAsync(form))`.
- The multi-step-only `IsPaymentWorkflowRequiredAsync` check stays in `EnterPaymentInfo`, before the step.

#### 2. Map in both actions

**File**: `Presentation/Nop.Web/Controllers/CheckoutController.cs`

**Intent**: Each action becomes guard → (multi-step: workflow-required check) → step → map.

**Contract**:
- `EnterPaymentInfo`: `MethodNotSelected` → `RedirectToRoute(CHECKOUT_PAYMENT_METHOD)`; `Saved` → `RedirectToRoute(CHECKOUT_CONFIRM)`; `Invalid` → `View(await _checkoutModelFactory.PreparePaymentInfoModelAsync(paymentMethod))`.
- `OpcSavePaymentInfo`: `MethodNotSelected` → throw `"Payment method is not selected"` inside the existing `try`; `Saved` → the existing `confirm-order` JSON; `Invalid` → the existing `payment-info` JSON.

### Success Criteria:

#### Automated Verification:

- Characterization suite passes unedited: `dotnet test --no-build --configuration Release src/Tests/Nop.Web.Integration.Tests`
- Test files unchanged since Phase 2: `git diff <phase-2-sha> HEAD -- src/Tests/Nop.Web.Integration.Tests` is empty
- Shared logic exists once: ast-grep `$P.ValidatePaymentFormAsync($$$)` in CC matches only inside `SavePaymentInfoStepAsync`
- No existing CC member signature changed (V1 superset check)
- Full suite passes: `dotnet test --no-build --configuration Release src`

#### Manual Verification:

- Storefront: complete payment info with Check/Money Order in both flows (OPC on, then off); the confirm step loads as before

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation from the human that the manual testing was successful before proceeding to the next phase.

---

## Testing Strategy

### Unit Tests:

- None new. The service-level suite in Nop.Tests is unaffected and must stay green (`dotnet test src/Tests/Nop.Tests`).

### Integration Tests:

- **WAF characterization suite** (Phase 2): guard outcomes of all 20 actions, both pairs' branches, the GA state those branches write, and warning-log entries on OPC guard failures.
- **Precedence cases**: two guard conditions true at once.
- **Fixed after Phase 2.** The suite is written once and never edited in Phases 3-6; any edit is evidence of a behaviour change and stops the phase.
- **DI fallback** (only if the Phase 1 gate fails): the same cases at the action level, with the known gaps recorded in Phase 1.

### Manual Testing Steps:

1. With default settings (OPC on), complete a guest checkout with Check/Money Order and fixed-rate shipping; confirm the completed page appears.
2. Turn OPC off in admin (Settings → Order settings); repeat the checkout through the multi-step pages.
3. With anonymous checkout disallowed, start checkout as a guest in both modes; confirm the login challenge (multi-step) and the OPC error.
4. Turn checkout off (`CheckoutDisabled`); confirm the checkout URL redirects to the cart.

## Performance Considerations

None expected. The guard helper performs the same calls in the same order, and `GetShoppingCartAsync` / `IsGuestAsync` are already request-cached (research C7). The new test project adds one store installation per test run to CI time.

## Migration Notes

None. There is no schema, setting, resource, route or view change. Third-party CC subclasses keep compiling and behaving identically, because no existing member is removed or re-signed; new members are additive.

## References

- Research: `context/changes/refactor-opportunities/research.md` (opportunity #2, C6; verification V1-V3, V15, V23, V33, V34)
- Source analysis: `context/changes/checkout-flow-analysis/research.md`
- Callback precedent: `Presentation/Nop.Web/Controllers/CheckoutController.cs:268`, `:311`
- Test harness bootstrap: `Tests/Nop.Tests/BaseNopTest.cs:110-140`, `:235-257`, `:438-442`; `Tests/Nop.Tests/TestDataProviderManager.cs:21-29`
- App settings write on first boot: `Presentation/Nop.Web.Framework/Infrastructure/Extensions/ServiceCollectionExtensions.cs:81-86`
- CI: `.github/workflows/dotnet.yml`

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles. See `references/progress-format.md`.

### Phase 1: WebApplicationFactory harness (gate)

#### Automated

- [ ] 1.1 Solution builds: `dotnet build --configuration Release src`
- [ ] 1.2 New project's tests pass: `dotnet test --no-build --configuration Release src/Tests/Nop.Web.Integration.Tests`
- [ ] 1.3 Existing tests still pass: `dotnet test --no-build --configuration Release src/Tests/Nop.Tests`
- [ ] 1.4 Full CI-equivalent run passes: `dotnet test --no-build --configuration Release src`
- [ ] 1.5 Source tree stays clean: the output of `git status --porcelain --ignored src/Presentation/Nop.Web` is identical before and after a test run

#### Manual

- [ ] 1.6 Gate outcome (WAF or DI fallback) and reason recorded in this phase block
- [ ] 1.7 Local `Nop.Web` still starts in its previous state (installed or install wizard) after running the new tests

### Phase 2: Characterization tests

#### Automated

- [ ] 2.1 All characterization tests pass against unchanged production code
- [ ] 2.2 Production code unchanged in this phase
- [ ] 2.3 Every one of the 20 guarded actions appears in at least one guard test case
- [ ] 2.4 Full suite passes: `dotnet test --no-build --configuration Release src`

#### Manual

- [ ] 2.5 Mutation spot-check: swapping one guard exit makes a test fail
- [ ] 2.6 Mutation spot-check: removing the multi-step null save makes a shipping-method test fail

### Phase 3: Guard helper and multi-step adoption

#### Automated

- [ ] 3.1 Characterization suite passes unedited
- [ ] 3.2 Test files unchanged since Phase 2
- [ ] 3.3 No existing CC member signature changed
- [ ] 3.4 Full suite passes: `dotnet test --no-build --configuration Release src`

#### Manual

- [ ] 3.5 Storefront smoke test with OPC disabled (multi-step walk + login challenge)

### Phase 4: Guard adoption in OPC actions

#### Automated

- [ ] 4.1 Characterization suite passes unedited
- [ ] 4.2 Test files unchanged since Phase 2
- [ ] 4.3 No inlined guard copies remain
- [ ] 4.4 No existing CC member signature changed
- [ ] 4.5 Full suite passes: `dotnet test --no-build --configuration Release src`

#### Manual

- [ ] 4.6 Storefront smoke test with OPC enabled (guest checkout + anonymous-disallowed error)

### Phase 5: Shipping-method shared step core

#### Automated

- [ ] 5.1 Characterization suite passes unedited
- [ ] 5.2 Test files unchanged since Phase 2
- [ ] 5.3 Shared shipping logic exists once
- [ ] 5.4 No existing CC member signature changed
- [ ] 5.5 Full suite passes: `dotnet test --no-build --configuration Release src`

#### Manual

- [ ] 5.6 Storefront: shipping method and pickup in both flows

### Phase 6: Payment-info shared step core

#### Automated

- [ ] 6.1 Characterization suite passes unedited
- [ ] 6.2 Test files unchanged since Phase 2
- [ ] 6.3 Shared payment-info logic exists once
- [ ] 6.4 No existing CC member signature changed
- [ ] 6.5 Full suite passes: `dotnet test --no-build --configuration Release src`

#### Manual

- [ ] 6.6 Storefront: payment info with Check/Money Order in both flows
