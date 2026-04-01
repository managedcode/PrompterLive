using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Pages;
using PrompterOne.Shared.Storage;
using PrompterOne.Shared.Tests;

namespace PrompterOne.App.Tests;

public sealed class TeleprompterPersistenceTests : BunitContext
{
    private const int PersistedFocalPointPercent = 42;
    private const int PersistedFontSize = 40;
    private const int PersistedTextWidthPixels = 900;
    private const int UpdatedFocalPointPercent = 37;
    private const int UpdatedFontSize = 40;
    private const int UpdatedTextWidthPixels = 980;
    private const double PersistedFontScale = PersistedFontSize / 36d;
    private const double PersistedTextWidthRatio = PersistedTextWidthPixels / 1100d;
    private const double UpdatedTextWidthRatio = UpdatedTextWidthPixels / 1100d;
    private const string DisabledCameraAttribute = "false";

    [Fact]
    public void TeleprompterPage_RestoresPersistedReaderLayoutSettings()
    {
        var harness = TestHarnessFactory.Create(this);
        harness.JsRuntime.SavedValues[BrowserAppSettingsKeys.ReaderSettings] = new ReaderSettings(
            FontScale: PersistedFontScale,
            TextWidth: PersistedTextWidthRatio,
            FocalPointPercent: PersistedFocalPointPercent);

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.TeleprompterDemo);
        var cut = Render<TeleprompterPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(
                PersistedTextWidthPixels.ToString(CultureInfo.InvariantCulture),
                cut.Find($"#{UiDomIds.Teleprompter.WidthValue}").TextContent.Trim());
            Assert.Equal(
                PersistedFontSize.ToString(CultureInfo.InvariantCulture),
                cut.Find($"#{UiDomIds.Teleprompter.FontLabel}").TextContent.Trim());
            Assert.Equal(
                $"top:{PersistedFocalPointPercent}%;",
                cut.FindByTestId(UiTestIds.Teleprompter.FocalGuide).GetAttribute("style"));
        });
    }

    [Fact]
    public void TeleprompterPage_PersistsReaderLayoutAndCameraPreferenceChanges()
    {
        var harness = TestHarnessFactory.Create(this);
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.TeleprompterDemo);
        var cut = Render<TeleprompterPage>();

        cut.WaitForAssertion(() => Assert.Contains(UiTestIds.Teleprompter.WidthSlider, cut.Markup, StringComparison.Ordinal));

        cut.FindByTestId(UiTestIds.Teleprompter.FontUp).Click();
        cut.FindByTestId(UiTestIds.Teleprompter.WidthSlider).Input(UpdatedTextWidthPixels);
        cut.FindByTestId(UiTestIds.Teleprompter.FocalSlider).Input(UpdatedFocalPointPercent);
        cut.FindByTestId(UiTestIds.Teleprompter.CameraToggle).Click();

        cut.WaitForAssertion(() =>
        {
            var savedSettings = harness.JsRuntime.GetSavedValue<ReaderSettings>(BrowserAppSettingsKeys.ReaderSettings);

            Assert.Equal(UpdatedFontSize, int.Parse(cut.Find($"#{UiDomIds.Teleprompter.FontLabel}").TextContent.Trim(), CultureInfo.InvariantCulture));
            Assert.Equal(UpdatedTextWidthPixels, int.Parse(cut.Find($"#{UiDomIds.Teleprompter.WidthValue}").TextContent.Trim(), CultureInfo.InvariantCulture));
            Assert.Equal($"top:{UpdatedFocalPointPercent}%;", cut.FindByTestId(UiTestIds.Teleprompter.FocalGuide).GetAttribute("style"));

            Assert.Equal(PersistedFontScale, savedSettings.FontScale, 2);
            Assert.Equal(UpdatedTextWidthRatio, savedSettings.TextWidth, 4);
            Assert.Equal(UpdatedFocalPointPercent, savedSettings.FocalPointPercent);
            Assert.False(savedSettings.ShowCameraScene);
            Assert.Equal(DisabledCameraAttribute, cut.Find($"#{UiDomIds.Teleprompter.Camera}").GetAttribute("data-camera-autostart"));
            Assert.False(harness.Session.State.ReaderSettings.ShowCameraScene);
        });
    }
}
