using Bunit;
using PrompterOne.Core.Models.Media;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Pages;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Settings.Models;
using PrompterOne.Shared.Storage;
using PrompterOne.Shared.Storage.Cloud;
using PrompterOne.Shared.Tests;

namespace PrompterOne.App.Tests;

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

    private readonly AppHarness _harness;

    public SettingsInteractionTests()
    {
        _harness = TestHarnessFactory.Create(this);
    }

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
    public void SettingsPage_DoesNotStartMicrophoneMonitor_WhenMicSectionIsInactive()
    {
        var cut = Render<SettingsPage>();

        cut.WaitForAssertion(() => Assert.Contains(UiTestIds.Settings.NavCloud, cut.Markup, StringComparison.Ordinal));

        Assert.DoesNotContain(
            AppTestData.Microphone.StartLevelMonitorInvocation,
            _harness.JsRuntime.Invocations,
            StringComparer.Ordinal);
    }

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
    public void FileStorageSection_RendersBrowserLocalStorageLabels_InsteadOfDesktopPaths()
    {
        var cut = Render<SettingsPage>();

        cut.WaitForAssertion(() => Assert.Contains(UiTestIds.Settings.FilesPanel, cut.Markup, StringComparison.Ordinal));

        var markup = cut.Markup;
        Assert.DoesNotContain("/Users/you/", markup, StringComparison.Ordinal);
        Assert.Contains(BrowserStorageKeys.DocumentLibrary, markup, StringComparison.Ordinal);
        Assert.Contains(PrompterStorageDefaults.BrowserContainerDisplayPrefix, markup, StringComparison.Ordinal);
    }

    [Fact]
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
            "Saved locally in this browser. Runtime connection testing is not available yet.",
            cut.FindByTestId(UiTestIds.Settings.AiProviderMessage(SettingsAiProviderIds.OpenAi)).TextContent.Trim());
    }

    [Fact]
    public void StreamingPanel_RendersOnlyPersistedExternalDestinations()
    {
        _harness.JsRuntime.SavedValues[StudioSettingsStore.StorageKey] = StudioSettings.Default with
        {
            Streaming = StudioSettings.Default.Streaming with
            {
                ExternalDestinations =
                [
                    AppTestData.GoLive.CreateLiveKitDestination()
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

    [Fact]
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

    [Fact]
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
            Assert.DoesNotContain(FakeFounderName, cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain(FakeEngineerName, cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain(FakeInfrastructureName, cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
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

    [Fact]
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
}
