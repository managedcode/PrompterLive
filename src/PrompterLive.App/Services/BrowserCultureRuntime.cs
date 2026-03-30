using System.Globalization;
using System.Text.Json;
using Microsoft.JSInterop;
using PrompterLive.Core.Localization;
using PrompterLive.Shared.Services;

namespace PrompterLive.App.Services;

internal static class BrowserCultureRuntime
{
    private const string EvaluateMethodName = "eval";
    private const string GetBrowserCulturesExpression = "Array.isArray(window.navigator.languages) && window.navigator.languages.length > 0 ? window.navigator.languages : [window.navigator.language || 'en']";
    private const string GetStoredCultureMethodName = "localStorage.getItem";
    private const string SetDocumentCultureExpressionFormat = "document.documentElement.lang = {0}";

    public static async Task ApplyPreferredCultureAsync(IJSRuntime jsRuntime)
    {
        var storedCulture = NormalizeStoredCulture(
            await jsRuntime.InvokeAsync<string?>(GetStoredCultureMethodName, BrowserStorageKeys.CultureSetting));
        var browserCultures = await jsRuntime.InvokeAsync<string[]?>(EvaluateMethodName, GetBrowserCulturesExpression) ?? [];
        var requestedCultures = new[] { storedCulture }.Concat(browserCultures);
        var culture = CultureInfo.GetCultureInfo(AppCultureCatalog.ResolvePreferredCulture(requestedCultures));
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        await jsRuntime.InvokeVoidAsync(
            EvaluateMethodName,
            string.Format(
                CultureInfo.InvariantCulture,
                SetDocumentCultureExpressionFormat,
                JsonSerializer.Serialize(culture.Name)));
    }

    private static string? NormalizeStoredCulture(string? storedCulture)
    {
        if (string.IsNullOrWhiteSpace(storedCulture))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<string>(storedCulture) ?? storedCulture;
        }
        catch (JsonException)
        {
            return storedCulture;
        }
    }
}
