using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Layout;
using PrompterOne.Shared.Pages;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Settings.Models;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

public sealed class BrowserSettingsCrossTabTests : BunitContext
{
    private const string ActiveStateValue = "active";

    [Test]
    public async Task BrowserSettingsStore_SaveAsync_PublishesSettingsChangedMessage()
    {
        var jsRuntime = new TestJsRuntime();
        var messageBus = new CrossTabMessageBus(jsRuntime);
        var settingsStore = new BrowserSettingsStore(jsRuntime, messageBus);
        var preferences = SettingsPagePreferences.Default with
        {
            ColorScheme = SettingsAppearanceValues.LightColorScheme
        };

        await settingsStore.SaveAsync(SettingsPagePreferences.StorageKey, preferences);

        var publishRecord = Assert.Single(
            jsRuntime.InvocationRecords,
            record => string.Equals(record.Identifier, CrossTabInteropMethodNames.Publish, StringComparison.Ordinal));

        Assert.Equal(CrossTabMessagingDefaults.ChannelName, publishRecord.Arguments[0]?.ToString());

        var envelope = Assert.IsType<CrossTabMessageEnvelope>(publishRecord.Arguments[1]);
        Assert.Equal(CrossTabMessageTypes.SettingsChanged, envelope.MessageType);
        Assert.Equal(messageBus.InstanceId, envelope.SourceInstanceId);

        var payload = envelope.DeserializePayload<BrowserSettingChangePayload>();
        Assert.NotNull(payload);
        Assert.Equal(SettingsPagePreferences.StorageKey, payload.Key);
        Assert.Equal(BrowserSettingChangeKinds.Saved, payload.ChangeKind);
    }

    [Test]
    public async Task MainLayout_AppliesRemoteThemeChange_WhenAnotherTabUpdatesPreferences()
    {
        var harness = TestHarnessFactory.Create(this);
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(AppRoutes.Library);

        var cut = Render<MainLayout>(parameters => parameters
            .Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<div>Body</div>"))));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(
                AppTestData.Theme.ApplySettingsInvocation,
                harness.JsRuntime.Invocations,
                StringComparer.Ordinal);
        });

        harness.JsRuntime.SavedValues[SettingsPagePreferences.StorageKey] = SettingsPagePreferences.Default with
        {
            ColorScheme = SettingsAppearanceValues.LightColorScheme
        };

        await harness.CrossTabMessageBus.ReceiveAsync(
            CrossTabMessageEnvelope.Create(
                CrossTabMessageTypes.SettingsChanged,
                sourceInstanceId: "remote-tab",
                new BrowserSettingChangePayload(
                    SettingsPagePreferences.StorageKey,
                    BrowserSettingChangeKinds.Saved)));

        cut.WaitForAssertion(() =>
        {
            var latestInvocation = harness.JsRuntime.InvocationRecords
                .Where(record => string.Equals(record.Identifier, AppTestData.Theme.ApplySettingsInvocation, StringComparison.Ordinal))
                .Last();

            Assert.Equal(SettingsAppearanceValues.LightColorScheme, latestInvocation.Arguments[0]?.ToString());
        });
    }

    [Test]
    public async Task SettingsPage_ReloadsAppearanceState_WhenAnotherTabUpdatesPreferences()
    {
        var harness = TestHarnessFactory.Create(this);
        var cut = Render<SettingsPage>();

        cut.WaitForAssertion(() => Assert.Contains(UiTestIds.Settings.NavAppearance, cut.Markup, StringComparison.Ordinal));
        cut.FindByTestId(UiTestIds.Settings.NavAppearance).Click();

        harness.JsRuntime.SavedValues[SettingsPagePreferences.StorageKey] = SettingsPagePreferences.Default with
        {
            ColorScheme = SettingsAppearanceValues.LightColorScheme
        };

        await harness.CrossTabMessageBus.ReceiveAsync(
            CrossTabMessageEnvelope.Create(
                CrossTabMessageTypes.SettingsChanged,
                sourceInstanceId: "remote-tab",
                new BrowserSettingChangePayload(
                    SettingsPagePreferences.StorageKey,
                    BrowserSettingChangeKinds.Saved)));

        cut.WaitForAssertion(() =>
        {
            var themeOption = cut.FindByTestId(UiTestIds.Settings.ThemeOption(SettingsAppearanceValues.LightColorScheme));
            Assert.Equal(ActiveStateValue, themeOption.GetAttribute("data-active"));
        });
    }
}
