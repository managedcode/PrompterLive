using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using PrompterOne.Core.Localization;
using PrompterOne.Shared.Localization;

namespace PrompterOne.Shared.Pages;

public partial class SettingsPage
{
    [Inject] private IStringLocalizer<SharedResource> Localizer { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private string SelectedLanguageCulture =>
        string.IsNullOrWhiteSpace(_pagePreferences.LanguageCulture)
            ? AppCultureCatalog.ResolveSupportedCulture(CultureInfo.CurrentUICulture.Name)
            : AppCultureCatalog.ResolveSupportedCulture(_pagePreferences.LanguageCulture);

    private string Text(UiTextKey key) => Localizer[key.ToString()];
}
