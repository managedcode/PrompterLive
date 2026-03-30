using System.Globalization;
using Microsoft.JSInterop;
using PrompterLive.Core.Localization;

namespace PrompterLive.App.Services;

internal static class BrowserCultureRuntime
{
    private const string ApplyDocumentCultureMethodName = "PrompterLive.localization.applyDocumentCulture";
    private const string GetBrowserCulturesMethodName = "PrompterLive.localization.getBrowserCultures";
    private const string GetStoredCultureMethodName = "PrompterLive.localization.getStoredCulture";

    public static async Task ApplyPreferredCultureAsync(IJSRuntime jsRuntime)
    {
        var storedCulture = await jsRuntime.InvokeAsync<string?>(GetStoredCultureMethodName);
        var browserCultures = await jsRuntime.InvokeAsync<string[]?>(GetBrowserCulturesMethodName) ?? [];
        var requestedCultures = new[] { storedCulture }.Concat(browserCultures);
        var culture = CultureInfo.GetCultureInfo(AppCultureCatalog.ResolvePreferredCulture(requestedCultures));
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        await jsRuntime.InvokeVoidAsync(ApplyDocumentCultureMethodName, culture.Name);
    }
}
