using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using PrompterOne.Core.Localization;
using PrompterOne.Core.Models.Media;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Localization;
using PrompterOne.Shared.Pages;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Settings.Models;
using PrompterOne.Shared.Storage;
using PrompterOne.Shared.Storage.Cloud;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

public sealed class SettingsInteractionTests : BunitContext
{
    private const string FakeEngineerName = "Anna Petrenko";
    private const string FakeFounderName = "Mykola Kovalenko";
    private const string FakeInfrastructureName = "Dmytro Shevchenko";
    private const string ReaderSettingsKey = "prompterone.reader";
    private const string SceneSettingsKey = "prompterone.scene";
    private const string DropboxLabel = "Managed Dropbox";
    private const string DropboxValidationMessage = "Dropbox requires an access token or a refresh token with app key.";
    private const string NotConnectedLabel = "Not connected";
    private const string ClaudeConfiguredModel = "claude-opus-4-6";
    private const string OllamaConfiguredAuthority = "ollama.local:11434";
    private const string OllamaConfiguredModel = "llama3.2";
    private const string OpenAiConfiguredModel = "o3-mini";

    private readonly AppHarness _harness;

    public SettingsInteractionTests()
    {
        _harness = TestHarnessFactory.Create(this);
    }

    [Test]
    public void ReaderCameraToggle_UpdatesSessionState_AndPersistsSetting()
    {
        var cut = Render<SettingsPage>();

        cut.WaitForAssertion(() => Assert.Contains(UiTestIds.Settings.ReaderCameraToggle, cut.Markup, StringComparison.Ordinal));

        var initialValue = _harness.Session.State.ReaderSettings.ShowCameraScene;

        cut.FindByTestId(UiTestIds.Settings.ReaderCameraToggle).Click();

        Assert.Equal(!initialValue, _harness.Session.State.ReaderSettings.ShowCameraScene);
        var readerSettings = _harness.JsRuntime.GetSavedValue<ReaderSettings>(ReaderSettingsKey);
        Assert.Equal(!initialValue, readerSettings.ShowCameraScene);
    }

    [Test]
    public void CloudSection_SaveAndTest_PersistsDropboxPreferences_AndShowsValidationMessage()
    {
        var cut = Render<SettingsPage>();

        cut.WaitForAssertion(() => Assert.Contains(UiTestIds.Settings.CloudPanel, cut.Markup, StringComparison.Ordinal));

        cut.FindByTestId(UiTestIds.Settings.CloudProviderField(CloudStorageProviderIds.Dropbox, CloudStorageFieldIds.AccountLabel))
            .Change(DropboxLabel);
        cut.SelectSettingsOption(UiTestIds.Settings.CloudDefaultProvider, CloudStorageProviderIds.Dropbox);
        cut.FindByTestId(UiTestIds.Settings.CloudProviderConnect(CloudStorageProviderIds.Dropbox)).Click();

        var preferences = _harness.JsRuntime.GetSavedValue<CloudStoragePreferences>(CloudStorageStoreKeys.Preferences);
        var credentials = _harness.JsRuntime.GetSavedValue<DropboxCloudStorageCredentials>(CloudStorageStoreKeys.DropboxCredentials);

        Assert.Equal(CloudStorageProviderIds.Dropbox, preferences.PrimaryProviderId);
        Assert.False(preferences.AutoSyncOnSave);
        Assert.Equal(DropboxLabel, preferences.Dropbox.Connection.AccountLabel);
        Assert.False(preferences.Dropbox.Connection.IsConnected);
        Assert.Equal(string.Empty, credentials.AccessToken);
        Assert.Equal(
            DropboxValidationMessage,
            cut.FindByTestId(UiTestIds.Settings.CloudProviderMessage(CloudStorageProviderIds.Dropbox)).TextContent.Trim());
    }

    [Test]
    public void CloudSection_Disconnect_ClearsStoredCredentials_AndResetsSubtitle()
    {
        var preferences = CloudStoragePreferences.CreateDefault();
        preferences.PrimaryProviderId = CloudStorageProviderIds.Dropbox;
        preferences.Dropbox = new DropboxCloudStorageProfile
        {
            Connection = new CloudStorageConnectionState
            {
                AccountLabel = DropboxLabel,
                IsConnected = true,
                RootPath = "/apps/prompterone"
            }
        };
        _harness.JsRuntime.SavedValues[CloudStorageStoreKeys.Preferences] = preferences;
        _harness.JsRuntime.SavedValues[CloudStorageStoreKeys.DropboxCredentials] = new DropboxCloudStorageCredentials
        {
            AccessToken = "secret-token"
        };

        var cut = Render<SettingsPage>();

        cut.WaitForAssertion(() =>
            Assert.Equal(
                DropboxLabel,
                cut.FindByTestId(UiTestIds.Settings.CloudProviderSubtitle(CloudStorageProviderIds.Dropbox)).TextContent.Trim()));

        cut.FindByTestId(UiTestIds.Settings.CloudProviderDisconnect(CloudStorageProviderIds.Dropbox)).Click();

        Assert.Throws<KeyNotFoundException>(() => _harness.JsRuntime.GetSavedValue<DropboxCloudStorageCredentials>(CloudStorageStoreKeys.DropboxCredentials));

        var savedPreferences = _harness.JsRuntime.GetSavedValue<CloudStoragePreferences>(CloudStorageStoreKeys.Preferences);
        Assert.False(savedPreferences.Dropbox.Connection.IsConnected);
        Assert.Equal(string.Empty, savedPreferences.Dropbox.Connection.AccountLabel);
        Assert.Equal(
            NotConnectedLabel,
            cut.FindByTestId(UiTestIds.Settings.CloudProviderSubtitle(CloudStorageProviderIds.Dropbox)).TextContent.Trim());
    }

    [Test]
    public void CloudSection_LoadsPrimaryProviderCard_AsOpen()
    {
        var preferences = CloudStoragePreferences.CreateDefault();
        preferences.PrimaryProviderId = CloudStorageProviderIds.Dropbox;
        _harness.JsRuntime.SavedValues[CloudStorageStoreKeys.Preferences] = preferences;

        var cut = Render<SettingsPage>();

        cut.WaitForAssertion(() =>
        {
            var dropboxCard = cut.FindByTestId(UiTestIds.Settings.CloudProviderCard(CloudStorageProviderIds.Dropbox));
            Assert.Contains("open", dropboxCard.GetAttribute("class") ?? string.Empty, StringComparison.Ordinal);
        });
    }

    [Test]
    public void SettingsPage_DoesNotStartMicrophoneMonitor_WhenMicSectionIsInactive()
    {
        var cut = Render<SettingsPage>();

        cut.WaitForAssertion(() => Assert.Contains(UiTestIds.Settings.NavCloud, cut.Markup, StringComparison.Ordinal));

        Assert.DoesNotContain(
            AppTestData.Microphone.StartLevelMonitorInvocation,
            _harness.JsRuntime.Invocations,
            StringComparer.Ordinal);
    }

    [Test]
    public void MicrophoneDelaySlider_UpdatesAudioBusState()
    {
        var cut = Render<SettingsPage>();

        cut.WaitForAssertion(() => Assert.Contains(UiTestIds.Settings.MicDelay("mic-1"), cut.Markup, StringComparison.Ordinal));

        cut.FindByTestId(UiTestIds.Settings.MicDelay("mic-1")).Input(320);

        var audioInput = _harness.SceneService.State.AudioBus.Inputs
            .Single(input => input.DeviceId == "mic-1");

        Assert.Equal(320, audioInput.DelayMs);
        Assert.Equal(AudioRouteTarget.Both, audioInput.RouteTarget);
        var savedScene = _harness.JsRuntime.GetSavedValue<MediaSceneState>(SceneSettingsKey);
        Assert.Contains(savedScene.AudioBus.Inputs, input => input.DeviceId == "mic-1" && input.DelayMs == 320);
    }

    [Test]
    public void RecordingSection_PersistsRecordingPreferences()
    {
        var cut = Render<SettingsPage>();

        cut.WaitForAssertion(() => Assert.Contains(UiTestIds.Settings.NavRecording, cut.Markup, StringComparison.Ordinal));

        cut.FindByTestId(UiTestIds.Settings.NavRecording).Click();
        cut.FindByTestId(UiTestIds.Settings.RecordingAutoRecord).Click();
        cut.FindByTestId(UiTestIds.Settings.RecordingVideoBitrate).Input(6400);
        cut.FindByTestId(UiTestIds.Settings.RecordingAudioBitrate).Input(256);

        var savedPreferences = _harness.JsRuntime.GetSavedValue<SettingsPagePreferences>(SettingsPagePreferences.StorageKey);
        Assert.False(savedPreferences.AutoRecordWhenStreaming);
        Assert.Equal(6400, savedPreferences.RecordingVideoBitrateKbps);
        Assert.Equal(256, savedPreferences.RecordingAudioBitrateKbps);
    }

    [Test]
    public void ExactStudioControls_PersistCameraMicAndStreamingPreferences()
    {
        var cut = Render<SettingsPage>();

        cut.WaitForAssertion(() => Assert.Contains(UiTestIds.Settings.CameraResolution, cut.Markup, StringComparison.Ordinal));

        var initialMirrorState = _harness.JsRuntime.GetSavedValue<StudioSettings>(StudioSettingsStore.StorageKey).Camera.MirrorCamera;
        cut.SelectSettingsOption(UiTestIds.Settings.CameraResolution, CameraResolutionPreset.Hd720.ToString());
        cut.FindByTestId(UiTestIds.Settings.CameraMirrorToggle).Click();
        cut.FindByTestId(UiTestIds.Settings.MicLevel).Input(82);
        cut.FindByTestId(UiTestIds.Settings.NoiseSuppression).Click();

        var settings = _harness.JsRuntime.GetSavedValue<StudioSettings>(StudioSettingsStore.StorageKey);
        Assert.Equal(CameraResolutionPreset.Hd720, settings.Camera.Resolution);
        Assert.Equal(!initialMirrorState, settings.Camera.MirrorCamera);
        Assert.Equal(82, settings.Microphone.InputLevelPercent);
        Assert.False(settings.Microphone.NoiseSuppression);
    }

    [Test]
    public void FileStorageSection_RendersBrowserLocalStorageLabels_InsteadOfDesktopPaths()
    {
        var cut = Render<SettingsPage>();

        cut.WaitForAssertion(() => Assert.Contains(UiTestIds.Settings.FilesPanel, cut.Markup, StringComparison.Ordinal));

        var markup = cut.Markup;
        Assert.DoesNotContain("/Users/you/", markup, StringComparison.Ordinal);
        Assert.Contains(BrowserStorageKeys.DocumentLibrary, markup, StringComparison.Ordinal);
        Assert.Contains(PrompterStorageDefaults.BrowserContainerDisplayPrefix, markup, StringComparison.Ordinal);
        Assert.Contains(Text(UiTextKey.SettingsFilesScriptsAutoSave), markup, StringComparison.Ordinal);
        Assert.Contains(Text(UiTextKey.SettingsFilesScriptsHistory), markup, StringComparison.Ordinal);
    }

    [Test]
    public void AboutSection_RestartTourButton_NavigatesToLibraryWithOnboardingRequest()
    {
        var cut = Render<SettingsPage>();
        var navigation = Services.GetRequiredService<NavigationManager>();

        cut.WaitForAssertion(() => Assert.Contains(UiTestIds.Settings.AboutOnboardingRestart, cut.Markup, StringComparison.Ordinal));

        cut.FindByTestId(UiTestIds.Settings.AboutOnboardingRestart).Click();

        cut.WaitForAssertion(() =>
            Assert.EndsWith(AppRoutes.LibraryWithOnboarding(), navigation.Uri, StringComparison.Ordinal));
    }

    [Test]
    public void AiSection_SaveOpenAiDraft_PersistsLocalConfiguration_AndShowsLocalOnlyMessage()
    {
        var cut = Render<SettingsPage>();

        cut.WaitForAssertion(() => Assert.Contains(UiTestIds.Settings.AiPanel, cut.Markup, StringComparison.Ordinal));

        cut.FindByTestId(UiTestIds.Settings.NavAi).Click();
        cut.FindByTestId(UiTestIds.Settings.AiProvider(SettingsAiProviderIds.OpenAi)).Click();
        cut.FindAll("input")
            .First(input => string.Equals(input.GetAttribute("placeholder"), "sk-...", StringComparison.Ordinal))
            .Change("sk-live-openai");
        cut.FindByTestId(UiTestIds.Settings.AiProviderSave(SettingsAiProviderIds.OpenAi)).Click();

        var savedSettings = _harness.JsRuntime.GetSavedValue<AiProviderSettings>(AiProviderSettings.StorageKey);
        Assert.Equal("sk-live-openai", savedSettings.OpenAi.ApiKey);
        Assert.Equal(
            Text(UiTextKey.SettingsAiSavedLocallyDetail),
            cut.FindByTestId(UiTestIds.Settings.AiProviderMessage(SettingsAiProviderIds.OpenAi)).TextContent.Trim());
    }

    [Test]
    public void AiSection_RendersProviderSubtitlesFromCurrentSettings()
    {
        _harness.JsRuntime.SavedValues[AiProviderSettings.StorageKey] = new AiProviderSettings
        {
            ClaudeApi = new AnthropicAiProviderSettings
            {
                Model = ClaudeConfiguredModel
            },
            OpenAi = new OpenAiProviderSettings
            {
                Model = OpenAiConfiguredModel
            },
            Ollama = new OllamaAiProviderSettings
            {
                Endpoint = $"http://{OllamaConfiguredAuthority}",
                Model = OllamaConfiguredModel
            }
        };

        var cut = Render<SettingsPage>();

        cut.FindByTestId(UiTestIds.Settings.NavAi).Click();
        cut.WaitForAssertion(() =>
        {
            Assert.Equal(
                $"Anthropic · {ClaudeConfiguredModel}",
                cut.FindByTestId(UiTestIds.Settings.AiProviderSubtitle(SettingsAiProviderIds.ClaudeApi)).TextContent.Trim());
            Assert.Equal(
                $"OpenAI · {OpenAiConfiguredModel}",
                cut.FindByTestId(UiTestIds.Settings.AiProviderSubtitle(SettingsAiProviderIds.OpenAi)).TextContent.Trim());
            Assert.Equal(
                $"Self-hosted · {OllamaConfiguredAuthority} · {OllamaConfiguredModel}",
                cut.FindByTestId(UiTestIds.Settings.AiProviderSubtitle(SettingsAiProviderIds.Ollama)).TextContent.Trim());
        });
    }

    [Test]
    public void StreamingPanel_RendersOnlyPersistedTransportConnections()
    {
        _harness.JsRuntime.SavedValues[StudioSettingsStore.StorageKey] = StudioSettings.Default with
        {
            Streaming = StudioSettings.Default.Streaming with
            {
                TransportConnections =
                [
                    AppTestData.GoLive.CreateLiveKitConnection()
                ]
            }
        };

        var cut = Render<SettingsPage>();

        cut.WaitForAssertion(() => Assert.Contains(UiTestIds.Settings.StreamingPanel, cut.Markup, StringComparison.Ordinal));
        cut.FindByTestId(UiTestIds.Settings.NavStreaming).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.FindByTestId(UiTestIds.Settings.StreamingProviderCard(GoLiveTargetCatalog.TargetIds.LiveKit)));
            Assert.Empty(cut.FindAll($"[data-testid='{UiTestIds.Settings.StreamingProviderCard(GoLiveTargetCatalog.TargetIds.Youtube)}']"));
            Assert.Empty(cut.FindAll($"[data-testid='{UiTestIds.Settings.StreamingProviderCard(GoLiveTargetCatalog.TargetIds.Twitch)}']"));
        });
    }

    [Test]
    public void AppearanceThemeChoice_PersistsAndCallsBrowserThemeInterop()
    {
        var cut = Render<SettingsPage>();

        cut.WaitForAssertion(() => Assert.Contains(UiTestIds.Settings.NavAppearance, cut.Markup, StringComparison.Ordinal));

        var applyInvocationCount = _harness.JsRuntime.InvocationRecords.Count(record =>
            string.Equals(record.Identifier, AppTestData.Theme.ApplySettingsInvocation, StringComparison.Ordinal));

        cut.FindByTestId(UiTestIds.Settings.NavAppearance).Click();
        cut.FindByTestId(UiTestIds.Settings.ThemeOption(AppTestData.Theme.LightColorScheme))
            .QuerySelector("input")!
            .Change(true);

        var savedPreferences = _harness.JsRuntime.GetSavedValue<SettingsPagePreferences>(SettingsPagePreferences.StorageKey);
        Assert.Equal(SettingsAppearanceValues.LightColorScheme, savedPreferences.ColorScheme);

        var themeInvocations = _harness.JsRuntime.InvocationRecords
            .Where(record => string.Equals(record.Identifier, AppTestData.Theme.ApplySettingsInvocation, StringComparison.Ordinal))
            .ToList();

        Assert.True(themeInvocations.Count > applyInvocationCount);

        var latestInvocation = themeInvocations[^1];
        Assert.Equal(AppTestData.Theme.LightColorScheme, latestInvocation.Arguments[0]?.ToString());
        Assert.Equal(savedPreferences.AccentColor, latestInvocation.Arguments[1]?.ToString());
        Assert.Equal(savedPreferences.UiDensity, latestInvocation.Arguments[2]?.ToString());
    }

    [Test]
    public void AppearanceSection_UsesSharedPreviewAndSwatchComponents_InsteadOfInlineStyleBlobs()
    {
        var cut = Render<SettingsPage>();

        cut.WaitForAssertion(() => Assert.Contains(UiTestIds.Settings.NavAppearance, cut.Markup, StringComparison.Ordinal));

        cut.FindByTestId(UiTestIds.Settings.NavAppearance).Click();

        cut.WaitForAssertion(() =>
        {
            var appearancePanel = cut.FindByTestId(UiTestIds.Settings.AppearancePanel);
            var appearanceMarkup = appearancePanel.OuterHtml;

            Assert.Contains("settings-option-preview--themedark", appearanceMarkup, StringComparison.Ordinal);
            Assert.Contains("settings-option-preview--densitydefault", appearanceMarkup, StringComparison.Ordinal);
            Assert.Contains("ui-color-swatch--gold", appearanceMarkup, StringComparison.Ordinal);
            Assert.Contains("ui-color-swatch--white", appearanceMarkup, StringComparison.Ordinal);
            Assert.DoesNotContain("style=\"background:linear-gradient", appearanceMarkup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("style=\"background:#", appearanceMarkup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("display:flex;align-items:center;justify-content:center;", appearanceMarkup, StringComparison.Ordinal);
        });
    }

    [Test]
    public void LanguageSection_PersistsSelectedCulture_FromDirectSettingsNavigation()
    {
        var cut = Render<SettingsPage>();

        cut.WaitForAssertion(() => Assert.Contains(UiTestIds.Settings.NavLanguage, cut.Markup, StringComparison.Ordinal));

        cut.FindByTestId(UiTestIds.Settings.NavLanguage).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.FindByTestId(UiTestIds.Settings.LanguagePanel));
            Assert.Contains(Text(UiTextKey.SettingsLanguageSectionDescription), cut.Markup, StringComparison.Ordinal);
        });

        cut.SelectSettingsOption(UiTestIds.Settings.LanguageSelect, AppCultureCatalog.FrenchCultureName);

        var savedPreferences = _harness.JsRuntime.GetSavedValue<SettingsPagePreferences>(SettingsPagePreferences.StorageKey);

        Assert.Equal(AppCultureCatalog.FrenchCultureName, savedPreferences.LanguageCulture);
    }

    [Test]
    public void AboutSection_RendersInjectedAppVersionMetadata_AndOfficialManagedCodeLinks()
    {
        var cut = Render<SettingsPage>();

        cut.WaitForAssertion(() => Assert.Contains(UiTestIds.Settings.NavAbout, cut.Markup, StringComparison.Ordinal));

        cut.FindByTestId(UiTestIds.Settings.NavAbout).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(
                AppTestData.About.VersionSubtitle,
                cut.FindByTestId(UiTestIds.Settings.AboutVersion).TextContent.Trim());
            Assert.NotNull(cut.FindByTestId(UiTestIds.Settings.AboutAppCard));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Settings.AboutCompanyCard));
            Assert.Equal(
                AboutLinks.ManagedCodeWebsiteUrl,
                cut.FindByTestId(UiTestIds.Settings.AboutCompanyWebsite).GetAttribute("href"));
            Assert.Equal(
                AboutLinks.ManagedCodeGitHubUrl,
                cut.FindByTestId(UiTestIds.Settings.AboutCompanyGitHub).GetAttribute("href"));
            Assert.Equal(
                AboutLinks.ProductRepositoryUrl,
                cut.FindByTestId(UiTestIds.Settings.AboutProductGitHub).GetAttribute("href"));
            Assert.Equal(
                AboutLinks.TpsRepositoryUrl,
                cut.FindByTestId(UiTestIds.Settings.AboutTpsGitHub).GetAttribute("href"));
            Assert.Equal(
                AboutLinks.ProductWebsiteUrl,
                cut.FindByTestId(UiTestIds.Settings.AboutProductWebsite).GetAttribute("href"));
            Assert.Equal(
                AboutLinks.ProductRepositoryUrl,
                cut.FindByTestId(UiTestIds.Settings.AboutRepositoryLink).GetAttribute("href"));
            Assert.Equal(
                AboutLinks.ProductReleasesUrl,
                cut.FindByTestId(UiTestIds.Settings.AboutReleasesLink).GetAttribute("href"));
            Assert.Equal(
                AboutLinks.ProductIssuesUrl,
                cut.FindByTestId(UiTestIds.Settings.AboutIssuesLink).GetAttribute("href"));
            Assert.Equal(
                AboutLinks.ClarityPrivacyDisclosureUrl,
                cut.FindByTestId(UiTestIds.Settings.AboutClarityDisclosure).GetAttribute("href"));
            Assert.DoesNotContain(FakeFounderName, cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain(FakeEngineerName, cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain(FakeInfrastructureName, cut.Markup, StringComparison.Ordinal);
        });
    }

    [Test]
    public void AboutSection_UsesTypedIconSurfaceClasses_InsteadOfInlineCardIconStyles()
    {
        var cut = Render<SettingsPage>();

        cut.WaitForAssertion(() => Assert.Contains(UiTestIds.Settings.NavAbout, cut.Markup, StringComparison.Ordinal));

        cut.FindByTestId(UiTestIds.Settings.NavAbout).Click();

        cut.WaitForAssertion(() =>
        {
            var appCard = cut.FindByTestId(UiTestIds.Settings.AboutAppCard);
            var iconSurface = appCard.QuerySelector(".ui-icon-surface--gold");

            Assert.NotNull(iconSurface);
            Assert.DoesNotContain("style=", iconSurface!.OuterHtml, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Test]
    public void CloudSection_UsesTypedProviderActionComponent_InsteadOfInlineBuilderStyleMarkup()
    {
        var cut = Render<SettingsPage>();

        cut.WaitForAssertion(() => Assert.Contains(UiTestIds.Settings.CloudPanel, cut.Markup, StringComparison.Ordinal));

        cut.WaitForAssertion(() =>
        {
            var providerCard = cut.FindByTestId(UiTestIds.Settings.CloudProviderCard(CloudStorageProviderIds.OneDrive));
            var actionRow = providerCard.QuerySelector(".set-path-field-actions");

            Assert.NotNull(actionRow);
            Assert.DoesNotContain("style=", actionRow!.OuterHtml, StringComparison.OrdinalIgnoreCase);
            Assert.NotNull(providerCard.QuerySelector("[data-testid='" + UiTestIds.Settings.CloudProviderConnect(CloudStorageProviderIds.OneDrive) + "']"));
        });
    }

    [Test]
    public void CameraPreview_AttachesSelectedCameraAfterMediaAccessIsGranted()
    {
        var cut = Render<SettingsPage>();

        cut.WaitForAssertion(() => Assert.Contains(UiTestIds.Settings.NavCameras, cut.Markup, StringComparison.Ordinal));

        cut.FindByTestId(UiTestIds.Settings.NavCameras).Click();
        cut.FindByTestId(UiTestIds.Settings.RequestMedia).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.True(_harness.PermissionService.Requested);
            Assert.NotNull(cut.FindByTestId(UiTestIds.Settings.CameraPreviewCard));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Settings.CameraPreviewVideo));
            Assert.Equal(
                AppTestData.Camera.FrontCamera,
                cut.FindByTestId(UiTestIds.Settings.CameraPreviewLabel).TextContent.Trim());
            Assert.Contains(AppTestData.Camera.AttachCameraInvocation, _harness.JsRuntime.Invocations, StringComparer.Ordinal);
        });
    }

    [Test]
    public void CameraDeviceAction_SelectsMatchingPreviewCamera()
    {
        var cut = Render<SettingsPage>();

        cut.WaitForAssertion(() => Assert.Contains(UiTestIds.Settings.NavCameras, cut.Markup, StringComparison.Ordinal));

        cut.FindByTestId(UiTestIds.Settings.NavCameras).Click();
        cut.FindByTestId(UiTestIds.Settings.RequestMedia).Click();

        cut.WaitForAssertion(() =>
            Assert.Equal(
                AppTestData.Camera.FrontCamera,
                cut.FindByTestId(UiTestIds.Settings.CameraPreviewLabel).TextContent.Trim()));

        cut.FindByTestId(UiTestIds.Settings.CameraDeviceAction(AppTestData.Camera.SecondDeviceId)).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(
                AppTestData.Camera.SideCamera,
                cut.FindByTestId(UiTestIds.Settings.CameraPreviewLabel).TextContent.Trim());

            var latestAttachInvocation = _harness.JsRuntime.InvocationRecords
                .Where(record => string.Equals(record.Identifier, AppTestData.Camera.AttachCameraInvocation, StringComparison.Ordinal))
                .Last();

            Assert.Equal(AppTestData.Camera.SecondDeviceId, latestAttachInvocation.Arguments[1]?.ToString());
        });
    }

    [Test]
    public void MicrophoneSection_StartsLiveMeterForSelectedMicrophoneAfterMediaAccessIsGranted()
    {
        var cut = Render<SettingsPage>();

        cut.WaitForAssertion(() => Assert.Contains(UiTestIds.Settings.NavCameras, cut.Markup, StringComparison.Ordinal));

        cut.FindByTestId(UiTestIds.Settings.NavCameras).Click();
        cut.FindByTestId(UiTestIds.Settings.RequestMedia).Click();
        cut.FindByTestId(UiTestIds.Settings.NavMics).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.FindByTestId(UiTestIds.Settings.MicPreviewCard));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Settings.MicPreviewMeter));
            Assert.Equal(
                AppTestData.Scripts.BroadcastMic,
                cut.FindByTestId(UiTestIds.Settings.MicPreviewLabel).TextContent.Trim());
            Assert.Contains(AppTestData.Microphone.StartLevelMonitorInvocation, _harness.JsRuntime.Invocations, StringComparer.Ordinal);
        });
    }

    private string Text(UiTextKey key) =>
        Services.GetRequiredService<IStringLocalizer<SharedResource>>()[key.ToString()];
}
