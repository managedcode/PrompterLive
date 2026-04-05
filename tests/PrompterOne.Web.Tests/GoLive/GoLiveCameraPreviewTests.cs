using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using PrompterOne.Core.Models.Media;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.GoLive.Models;
using PrompterOne.Shared.Localization;
using PrompterOne.Shared.Pages;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

public sealed class GoLiveCameraPreviewTests : BunitContext
{
    private const string LegacyPrimarySceneFallbackLabel = "Camera 1";
    private const string SceneSettingsStorageKey = "prompterone.scene";

    [Fact]
    public void GoLivePage_RendersLiveCameraPreviewForSelectedSceneCamera()
    {
        var harness = TestHarnessFactory.Create(this);
        harness.JsRuntime.SavedJsonValues[SceneSettingsStorageKey] = JsonSerializer.Serialize(CreateSingleCameraScene());

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.GoLiveDemo);

        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.PreviewCard));
            Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.PreviewVideo));
            Assert.Equal(
                AppTestData.Camera.FrontCamera,
                cut.FindByTestId(UiTestIds.GoLive.PreviewSourceLabel).TextContent.Trim());
            Assert.Contains(AppTestData.Camera.AttachCameraInvocation, harness.JsRuntime.Invocations, StringComparer.Ordinal);
        });
    }

    [Fact]
    public void GoLivePage_RendersEmptyPreviewStateWhenNoSceneCameraExists()
    {
        TestHarnessFactory.Create(
            this,
            devices:
            [
                new MediaDeviceInfo(
                    AppTestData.Camera.MicrophoneOnlyId,
                    AppTestData.Camera.MicrophoneOnlyLabel,
                    MediaDeviceKind.Microphone,
                    true)
            ]);

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.GoLiveDemo);

        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.PreviewCard));
            Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.PreviewEmpty));
            Assert.Empty(cut.FindAll(BunitTestSelectors.BuildTestIdSelector(UiTestIds.GoLive.PreviewVideo)));
        });
    }

    [Fact]
    public void GoLivePage_UsesExplicitNoCameraStateForPrimarySceneChipWhenSceneIsEmpty()
    {
        TestHarnessFactory.Create(
            this,
            devices:
            [
                new MediaDeviceInfo(
                    AppTestData.Camera.MicrophoneOnlyId,
                    AppTestData.Camera.MicrophoneOnlyLabel,
                    MediaDeviceKind.Microphone,
                    true)
            ]);

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.GoLiveDemo);

        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() =>
        {
            var primarySceneChip = cut.FindByTestId(UiTestIds.GoLive.SceneChip(GoLiveText.Surface.PrimarySceneId));
            Assert.Contains(Text(GoLiveText.Session.CameraFallbackLabel), primarySceneChip.TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain(LegacyPrimarySceneFallbackLabel, primarySceneChip.TextContent, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void GoLivePage_ShowsSingleLocalPreviewHintWhenConcurrentLocalCaptureIsUnavailable()
    {
        var harness = TestHarnessFactory.Create(this);
        harness.JsRuntime.CaptureCapabilities = new BrowserMediaCaptureCapabilities(false);
        harness.JsRuntime.SavedJsonValues[SceneSettingsStorageKey] = JsonSerializer.Serialize(CreateTwoCameraScene());

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.GoLiveDemo);

        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.SingleLocalPreviewHint));
            Assert.Empty(cut.FindAll(BunitTestSelectors.BuildTestIdSelector(UiTestIds.GoLive.SourceVideo(AppTestData.Camera.SecondSourceId))));
        });
    }

    private static MediaSceneState CreateSingleCameraScene() =>
        new(
            [
                new SceneCameraSource(
                    AppTestData.Camera.FirstSourceId,
                    AppTestData.Camera.FirstDeviceId,
                    AppTestData.Camera.FrontCamera,
                    new MediaSourceTransform(IncludeInOutput: true))
            ],
            null,
            null,
            AudioBusState.Empty);

    private static MediaSceneState CreateTwoCameraScene() =>
        new(
            [
                new SceneCameraSource(
                    AppTestData.Camera.FirstSourceId,
                    AppTestData.Camera.FirstDeviceId,
                    AppTestData.Camera.FrontCamera,
                    new MediaSourceTransform(IncludeInOutput: true)),
                new SceneCameraSource(
                    AppTestData.Camera.SecondSourceId,
                    AppTestData.Camera.SecondDeviceId,
                    AppTestData.Camera.SideCamera,
                    new MediaSourceTransform(IncludeInOutput: true))
            ],
            null,
            null,
            AudioBusState.Empty);

    private string Text(string key) =>
        Services.GetRequiredService<IStringLocalizer<SharedResource>>()[key];
}
