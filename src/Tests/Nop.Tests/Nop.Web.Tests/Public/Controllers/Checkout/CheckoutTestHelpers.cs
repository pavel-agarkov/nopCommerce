using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core;
using Nop.Core.Configuration;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Security;
using Nop.Core.Domain.Shipping;
using Nop.Data;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Orders;
using NUnit.Framework;

namespace Nop.Tests.Nop.Web.Tests.Public.Controllers.Checkout;

/// <summary>
/// Arrange and restore helpers for checkout controller tests.
/// </summary>
/// <remarks>
/// <see cref="CaptureStateAsync"/> snapshots what checkout tests change in the shared store
/// (the settings changed through <see cref="SetSettingsAsync{TSettings}"/>, the current customer's cart items
/// and the current customer's generic attributes, plus any sample product switched to "no shipping");
/// <see cref="RestoreStateAsync"/> puts them back,
/// so the outcome of a test does not depend on test order.
/// </remarks>
public class CheckoutTestHelpers
{
    #region Fields

    private readonly IServiceProvider _serviceProvider;
    private readonly List<Func<Task>> _settingsRestorers = new();
    private readonly HashSet<Type> _changedSettingsTypes = new();
    private readonly List<Func<Task>> _productRestorers = new();
    private List<ShoppingCartItem> _cartSnapshot = new();
    private List<GenericAttribute> _genericAttributesSnapshot = new();

    #endregion

    #region Ctor

    public CheckoutTestHelpers(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    #endregion

    #region Utilities

    private T GetService<T>()
    {
        return _serviceProvider.GetRequiredService<T>();
    }

    private static ShoppingCartItem Copy(ShoppingCartItem item)
    {
        return new ShoppingCartItem
        {
            Id = item.Id,
            StoreId = item.StoreId,
            ShoppingCartTypeId = item.ShoppingCartTypeId,
            CustomerId = item.CustomerId,
            ProductId = item.ProductId,
            CustomWishlistId = item.CustomWishlistId,
            AttributesXml = item.AttributesXml,
            CustomerEnteredPrice = item.CustomerEnteredPrice,
            Quantity = item.Quantity,
            RentalStartDateUtc = item.RentalStartDateUtc,
            RentalEndDateUtc = item.RentalEndDateUtc,
            CreatedOnUtc = item.CreatedOnUtc,
            UpdatedOnUtc = item.UpdatedOnUtc
        };
    }

    private static GenericAttribute Copy(GenericAttribute attribute)
    {
        return new GenericAttribute
        {
            Id = attribute.Id,
            EntityId = attribute.EntityId,
            KeyGroup = attribute.KeyGroup,
            Key = attribute.Key,
            Value = attribute.Value,
            StoreId = attribute.StoreId,
            CreatedOrUpdatedDateUTC = attribute.CreatedOrUpdatedDateUTC
        };
    }

    private async Task<List<ShoppingCartItem>> LoadCartItemsAsync(int customerId)
    {
        //read the table directly, so the snapshot never comes from a cache
        return await GetService<IRepository<ShoppingCartItem>>().Table
            .Where(sci => sci.CustomerId == customerId)
            .ToListAsync();
    }

    private async Task<List<GenericAttribute>> LoadGenericAttributesAsync(int customerId)
    {
        var keyGroup = nameof(Customer);

        return await GetService<IRepository<GenericAttribute>>().Table
            .Where(ga => ga.EntityId == customerId && ga.KeyGroup == keyGroup)
            .ToListAsync();
    }

    private async Task RestoreCartAsync(Customer customer)
    {
        var shoppingCartService = GetService<IShoppingCartService>();
        var cartRepository = GetService<IRepository<ShoppingCartItem>>();
        var snapshotIds = _cartSnapshot.Select(sci => sci.Id).ToHashSet();

        var current = await LoadCartItemsAsync(customer.Id);

        //remove items the test added
        foreach (var item in current.Where(sci => !snapshotIds.Contains(sci.Id)))
            await shoppingCartService.DeleteShoppingCartItemAsync(item, resetCheckoutData: false);

        //put back items the test removed or changed
        var currentById = current.ToDictionary(sci => sci.Id);
        foreach (var original in _cartSnapshot)
        {
            if (!currentById.TryGetValue(original.Id, out var item))
            {
                var restored = Copy(original);
                restored.Id = 0;
                await cartRepository.InsertAsync(restored);
                continue;
            }

            if (item.Quantity == original.Quantity && item.AttributesXml == original.AttributesXml &&
                item.CustomerEnteredPrice == original.CustomerEnteredPrice)
                continue;

            item.Quantity = original.Quantity;
            item.AttributesXml = original.AttributesXml;
            item.CustomerEnteredPrice = original.CustomerEnteredPrice;
            await cartRepository.UpdateAsync(item);
        }
    }

    private async Task RestoreGenericAttributesAsync(Customer customer)
    {
        var genericAttributeService = GetService<IGenericAttributeService>();
        var snapshotById = _genericAttributesSnapshot.ToDictionary(ga => ga.Id);

        var current = await LoadGenericAttributesAsync(customer.Id);
        var currentIds = current.Select(ga => ga.Id).ToHashSet();

        foreach (var attribute in current)
        {
            if (!snapshotById.TryGetValue(attribute.Id, out var original))
            {
                //added by the test
                await genericAttributeService.DeleteAttributeAsync(attribute);
                continue;
            }

            if (attribute.Value == original.Value)
                continue;

            attribute.Value = original.Value;
            await genericAttributeService.UpdateAttributeAsync(attribute);
        }

        //deleted by the test
        foreach (var original in _genericAttributesSnapshot.Where(ga => !currentIds.Contains(ga.Id)))
        {
            var restored = Copy(original);
            restored.Id = 0;
            await genericAttributeService.InsertAttributeAsync(restored);
        }
    }

    #endregion

    #region State

    /// <summary>
    /// Snapshot the current customer's cart and generic attributes; call in [SetUp]
    /// </summary>
    public async Task CaptureStateAsync()
    {
        var customer = await GetCurrentCustomerAsync();

        _cartSnapshot = (await LoadCartItemsAsync(customer.Id)).Select(Copy).ToList();
        _genericAttributesSnapshot = (await LoadGenericAttributesAsync(customer.Id)).Select(Copy).ToList();
        _settingsRestorers.Clear();
        _changedSettingsTypes.Clear();
        _productRestorers.Clear();
    }

    /// <summary>
    /// Restore changed settings, the current customer's cart and generic attributes; call in [TearDown]
    /// </summary>
    public async Task RestoreStateAsync()
    {
        foreach (var restore in _settingsRestorers)
            await restore();

        _settingsRestorers.Clear();
        _changedSettingsTypes.Clear();

        foreach (var restore in _productRestorers)
            await restore();

        _productRestorers.Clear();

        var customer = await GetCurrentCustomerAsync();

        //the cart goes first: deleting cart items may reset checkout generic attributes
        await RestoreCartAsync(customer);
        await RestoreGenericAttributesAsync(customer);
    }

    #endregion

    #region Context

    public async Task<Customer> GetCurrentCustomerAsync()
    {
        return await GetService<IWorkContext>().GetCurrentCustomerAsync();
    }

    public async Task<int> GetCurrentStoreIdAsync()
    {
        return (await GetService<IStoreContext>().GetCurrentStoreAsync()).Id;
    }

    #endregion

    #region Settings

    /// <summary>
    /// Change settings of the given type and save them; the original values are restored in [TearDown]
    /// </summary>
    public async Task SetSettingsAsync<TSettings>(Action<TSettings> change) where TSettings : ISettings, new()
    {
        var settingService = GetService<ISettingService>();

        if (_changedSettingsTypes.Add(typeof(TSettings)))
        {
            var original = await settingService.LoadSettingAsync<TSettings>();
            _settingsRestorers.Add(() => settingService.SaveSettingAsync(original));
        }

        var settings = await settingService.LoadSettingAsync<TSettings>();
        change(settings);
        await settingService.SaveSettingAsync(settings);
    }

    public Task SetOrderSettingsAsync(Action<OrderSettings> change) => SetSettingsAsync(change);

    public Task SetShippingSettingsAsync(Action<ShippingSettings> change) => SetSettingsAsync(change);

    public Task SetPaymentSettingsAsync(Action<PaymentSettings> change) => SetSettingsAsync(change);

    public Task SetCaptchaSettingsAsync(Action<CaptchaSettings> change) => SetSettingsAsync(change);

    #endregion

    #region Cart

    /// <summary>
    /// Remove every shopping cart item of the current customer in the current store; restored in [TearDown]
    /// </summary>
    public async Task ClearCartAsync()
    {
        var shoppingCartService = GetService<IShoppingCartService>();
        var customer = await GetCurrentCustomerAsync();
        var storeId = await GetCurrentStoreIdAsync();

        var cart = await shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, storeId);
        foreach (var item in cart.ToList())
            await shoppingCartService.DeleteShoppingCartItemAsync(item, resetCheckoutData: false);
    }

    /// <summary>
    /// Add one sample product (the first simple, published, ship-enabled product without attributes that is not
    /// a gift card, download, rental or recurring product and can be added without warnings)
    /// to the current customer's cart; removed in [TearDown]
    /// </summary>
    /// <param name="requiresShipping">
    /// Whether the product requires shipping. The sample data has no plain product that is not ship enabled,
    /// so for false the chosen product's IsShipEnabled is switched off; it is switched back in [TearDown]
    /// </param>
    /// <returns>The product added to the cart</returns>
    public async Task<Product> AddSampleProductToCartAsync(bool requiresShipping = true)
    {
        var productRepository = GetService<IRepository<Product>>();
        var mappingRepository = GetService<IRepository<ProductAttributeMapping>>();
        var shoppingCartService = GetService<IShoppingCartService>();
        var customer = await GetCurrentCustomerAsync();
        var storeId = await GetCurrentStoreIdAsync();

        var productsWithAttributes = mappingRepository.Table.Select(pam => pam.ProductId);

        var candidates = await productRepository.Table
            .Where(p => p.Published && !p.Deleted &&
                        p.ProductTypeId == (int)ProductType.SimpleProduct &&
                        p.IsShipEnabled &&
                        !p.IsGiftCard && !p.IsDownload && !p.IsRental && !p.IsRecurring &&
                        !p.CustomerEntersPrice &&
                        !productsWithAttributes.Contains(p.Id))
            .OrderBy(p => p.Id)
            .ToListAsync();

        foreach (var product in candidates)
        {
            var warnings = await shoppingCartService.AddToCartAsync(customer, product, ShoppingCartType.ShoppingCart,
                storeId, quantity: Math.Max(product.OrderMinimumQuantity, 1));

            if (warnings.Any())
                continue;

            if (!requiresShipping)
            {
                product.IsShipEnabled = false;
                await productRepository.UpdateAsync(product);

                var productId = product.Id;
                _productRestorers.Add(async () =>
                {
                    var changed = await productRepository.GetByIdAsync(productId);
                    changed.IsShipEnabled = true;
                    await productRepository.UpdateAsync(changed);
                });
            }

            return product;
        }

        Assert.Fail("No sample product can be added to the cart without warnings");
        return null;
    }

    #endregion

    #region Checkout generic attributes

    public async Task SetSelectedShippingOptionAsync(ShippingOption shippingOption)
    {
        await GetService<IGenericAttributeService>().SaveAttributeAsync(await GetCurrentCustomerAsync(),
            NopCustomerDefaults.SelectedShippingOptionAttribute, shippingOption, await GetCurrentStoreIdAsync());
    }

    public async Task SetOfferedShippingOptionsAsync(List<ShippingOption> shippingOptions)
    {
        await GetService<IGenericAttributeService>().SaveAttributeAsync(await GetCurrentCustomerAsync(),
            NopCustomerDefaults.OfferedShippingOptionsAttribute, shippingOptions, await GetCurrentStoreIdAsync());
    }

    public async Task SetSelectedPaymentMethodAsync(string paymentMethodSystemName)
    {
        await GetService<IGenericAttributeService>().SaveAttributeAsync(await GetCurrentCustomerAsync(),
            NopCustomerDefaults.SelectedPaymentMethodAttribute, paymentMethodSystemName, await GetCurrentStoreIdAsync());
    }

    /// <summary>
    /// Clear the selected/offered shipping options, the selected pickup point and the selected payment method
    /// </summary>
    public async Task ClearCheckoutAttributesAsync()
    {
        var genericAttributeService = GetService<IGenericAttributeService>();
        var customer = await GetCurrentCustomerAsync();
        var storeId = await GetCurrentStoreIdAsync();

        await genericAttributeService.SaveAttributeAsync<ShippingOption>(customer, NopCustomerDefaults.SelectedShippingOptionAttribute, null, storeId);
        await genericAttributeService.SaveAttributeAsync<List<ShippingOption>>(customer, NopCustomerDefaults.OfferedShippingOptionsAttribute, null, storeId);
        await genericAttributeService.SaveAttributeAsync<PickupPoint>(customer, NopCustomerDefaults.SelectedPickupPointAttribute, null, storeId);
        await genericAttributeService.SaveAttributeAsync<string>(customer, NopCustomerDefaults.SelectedPaymentMethodAttribute, null, storeId);
    }

    #endregion
}

/// <summary>
/// Readers for checkout action results
/// </summary>
public static class CheckoutResults
{
    /// <summary>
    /// Assert the result is a RedirectToRouteResult and return its route name
    /// </summary>
    public static string RouteName(IActionResult result)
    {
        return result.Should().BeOfType<RedirectToRouteResult>().Subject.RouteName;
    }

    /// <summary>
    /// Assert the result is a ChallengeResult
    /// </summary>
    public static ChallengeResult Challenge(IActionResult result)
    {
        return result.Should().BeOfType<ChallengeResult>().Subject;
    }

    /// <summary>
    /// Assert the result is a ViewResult and return its view name (null for the default view) and model type
    /// </summary>
    public static (string ViewName, Type ModelType) View(IActionResult result)
    {
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;

        return (viewResult.ViewName, viewResult.Model?.GetType());
    }

    /// <summary>
    /// Assert the result is a JsonResult and return the keys and values of its (anonymous) value object
    /// </summary>
    public static IDictionary<string, object> Json(IActionResult result)
    {
        var value = result.Should().BeOfType<JsonResult>().Subject.Value;
        value.Should().NotBeNull();

        return value.GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(value));
    }
}
