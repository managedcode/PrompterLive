using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using PrompterOne.Core.Localization;
using PrompterOne.Shared.Components.Diagnostics;
using PrompterOne.Shared.Components.GoLive;
using PrompterOne.Shared.Components.Library;
using PrompterOne.Shared.Components.Settings;
using PrompterOne.Shared.Localization;
using PrompterOne.Shared.Settings.Models;
using PrompterOne.Shared.Tests;

namespace PrompterOne.App.Tests;

public sealed class LocalizationRenderingTests : BunitContext
{
    public LocalizationRenderingTests()
    {
        TestHarnessFactory.Create(this);
    }

    [Fact]
    public void LibrarySidebar_RendersUkrainianLabels_WhenCurrentCultureIsUkrainian()
    {
        using var _ = new CultureScope(AppCultureCatalog.UkrainianCultureName);

        var cut = Render<LibrarySidebar>(parameters => parameters
            .Add(component => component.Folders, [])
            .Add(component => component.AllScriptCount, 5)
            .Add(component => component.IsAllSelected, true)
            .Add(component => component.OnSelectFolder, EventCallback.Factory.Create<string>(this, _ => Task.CompletedTask))
            .Add(component => component.OnStartCreateFolder, EventCallback.Factory.Create(this, () => Task.CompletedTask)));

        Assert.Contains(Text(UiTextKey.LibraryAllScripts), cut.Markup);
        Assert.Contains(Text(UiTextKey.LibraryFavorites), cut.Markup);
        Assert.Contains(Text(UiTextKey.LibrarySettings), cut.Markup);
    }

    [Fact]
    public void LibraryFolderCreateModal_RendersFrenchLabels_WhenCurrentCultureIsFrench()
    {
        using var _ = new CultureScope(AppCultureCatalog.FrenchCultureName);

        var cut = Render<LibraryFolderCreateModal>(parameters => parameters
            .Add(component => component.FolderOptions, [])
            .Add(component => component.FolderDraftName, string.Empty)
            .Add(component => component.FolderDraftParentId, LibrarySelectionKeys.Root)
            .Add(component => component.OnCancelCreateFolder, EventCallback.Factory.Create(this, () => Task.CompletedTask))
            .Add(component => component.OnSubmitCreateFolder, EventCallback.Factory.Create(this, () => Task.CompletedTask))
            .Add(component => component.OnFolderDraftNameChanged, EventCallback.Factory.Create<string>(this, _ => Task.CompletedTask))
            .Add(component => component.OnFolderDraftParentChanged, EventCallback.Factory.Create<string>(this, _ => Task.CompletedTask)));

        Assert.Contains(Text(UiTextKey.LibraryCreateFolderTitle), cut.Markup);
        Assert.Contains(Text(UiTextKey.CommonCreate), cut.Markup);
        Assert.Contains(Text(UiTextKey.CommonCancel), cut.Markup);
    }

    [Fact]
    public void DiagnosticsBanner_RendersItalianDismissLabel_WhenCurrentCultureIsItalian()
    {
        using var _ = new CultureScope(AppCultureCatalog.ItalianCultureName);
        var diagnostics = Services.GetRequiredService<PrompterOne.Shared.Services.Diagnostics.UiDiagnosticsService>();
        diagnostics.ReportRecoverable("diagnostics", "Localized diagnostics", "detail");

        var cut = Render<DiagnosticsBanner>();

        Assert.Contains(Text(UiTextKey.DiagnosticsDismiss), cut.Markup);
    }

    [Fact]
    public void LoggingErrorBoundary_RendersLocalizedFatalActions_WhenCurrentCultureIsFrench()
    {
        using var _ = new CultureScope(AppCultureCatalog.FrenchCultureName);

        var cut = Render<LoggingErrorBoundary>(parameters => parameters
            .AddChildContent<ThrowingLocalizationDiagnosticsComponent>());

        Assert.Contains(Text(UiTextKey.DiagnosticsRetry), cut.Markup);
        Assert.Contains(Text(UiTextKey.DiagnosticsLibrary), cut.Markup);
        Assert.Contains(Text(UiTextKey.DiagnosticsFatalTitle), cut.Markup);
    }

    [Fact]
    public void GoLiveHero_RendersLocalizedDefaults_WhenCurrentCultureIsUkrainian()
    {
        using var _ = new CultureScope(AppCultureCatalog.UkrainianCultureName);

        var cut = Render<GoLiveHero>(parameters => parameters
            .Add(component => component.HasScriptContext, true));

        Assert.Contains(Text(UiTextKey.GoLiveHeroEyebrow), cut.Markup);
        Assert.Contains(Text(UiTextKey.GoLiveHeroDescription), cut.Markup);
        Assert.Contains(Text(UiTextKey.HeaderLearn), cut.Markup);
        Assert.Contains(Text(UiTextKey.HeaderRead), cut.Markup);
    }

    [Fact]
    public void SettingsAppearanceSection_RendersGermanLabels_WhenCurrentCultureIsGerman()
    {
        using var _ = new CultureScope(AppCultureCatalog.GermanCultureName);

        var cut = Render<SettingsAppearanceSection>(parameters => parameters
            .Add(component => component.DisplayStyle, string.Empty)
            .Add(component => component.IsCardOpen, static _ => true)
            .Add(component => component.ColorScheme, SettingsAppearanceValues.DarkColorScheme)
            .Add(component => component.AccentColor, SettingsAppearanceValues.DefaultAccentColor)
            .Add(component => component.SelectedLanguageCulture, AppCultureCatalog.GermanCultureName)
            .Add(component => component.TeleprompterFont, "Inter (Default)")
            .Add(component => component.TeleprompterFontSize, 48)
            .Add(component => component.TeleprompterTextColor, "#FFFFFF")
            .Add(component => component.UiDensity, SettingsAppearanceValues.DefaultDensity)
            .Add(component => component.ToggleCard, EventCallback.Factory.Create<string>(this, _ => Task.CompletedTask))
            .Add(component => component.UpdateColorScheme, EventCallback.Factory.Create<string>(this, _ => Task.CompletedTask))
            .Add(component => component.UpdateAccentColor, EventCallback.Factory.Create<string>(this, _ => Task.CompletedTask))
            .Add(component => component.UpdateLanguageCulture, EventCallback.Factory.Create<string>(this, _ => Task.CompletedTask))
            .Add(component => component.UpdateTeleprompterFont, EventCallback.Factory.Create<string>(this, _ => Task.CompletedTask))
            .Add(component => component.UpdateTeleprompterFontSize, EventCallback.Factory.Create<ChangeEventArgs>(this, _ => Task.CompletedTask))
            .Add(component => component.UpdateTeleprompterTextColor, EventCallback.Factory.Create<string>(this, _ => Task.CompletedTask))
            .Add(component => component.ToggleMirrorTeleprompterText, EventCallback.Factory.Create(this, () => Task.CompletedTask))
            .Add(component => component.ToggleShowWordHighlight, EventCallback.Factory.Create(this, () => Task.CompletedTask))
            .Add(component => component.UpdateUiDensity, EventCallback.Factory.Create<string>(this, _ => Task.CompletedTask))
            .Add(component => component.ToggleReduceMotion, EventCallback.Factory.Create(this, () => Task.CompletedTask))
            .Add(component => component.ToggleShowShortcutOverlay, EventCallback.Factory.Create(this, () => Task.CompletedTask)));

        Assert.Contains(Text(UiTextKey.SettingsAppearanceSectionTitle), cut.Markup);
        Assert.Contains(Text(UiTextKey.SettingsAppearanceLanguageLabel), cut.Markup);
        Assert.Contains(Text(UiTextKey.SettingsAppearanceThemeDark), cut.Markup);
        Assert.Contains(Text(UiTextKey.SettingsAppearanceUiDensityLabel), cut.Markup);
    }

    private string Text(UiTextKey key) =>
        Services.GetRequiredService<IStringLocalizer<SharedResource>>()[key.ToString()];

    private sealed class ThrowingLocalizationDiagnosticsComponent : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            throw new InvalidOperationException("Forced localized boundary failure.");
        }
    }
}
