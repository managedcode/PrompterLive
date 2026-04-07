using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PrompterOne.Core.Models.Media;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Pages;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Storage;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

public sealed class GoLiveLiveIndicatorsTests : BunitContext
{
    private const string IdleStateValue = "idle";
    private const string LiveBadgeLabel = "On air";
    private const string RecordingStateValue = "recording";
    private const string SceneSettingsStorageKey = BrowserAppSettingsKeys.SceneSettings;

    private readonly AppHarness _harness;

    public GoLiveLiveIndicatorsTests()
    {
        _harness = TestHarnessFactory.Create(this);
    }

    [Test]
    public void GoLivePage_IdleSession_KeepsSourceBadgeAndPreviewDotNonLive()
    {
        SeedSceneState(CreateTwoCameraScene());
        Services.GetRequiredService<NavigationManager>().NavigateTo(AppTestData.Routes.GoLiveDemo);

        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() =>
        {
            var activeSourceBadge = cut.FindByTestId(UiTestIds.GoLive.SourceCameraBadge(AppTestData.Camera.FirstSourceId));
            var previewLiveDot = cut.FindByTestId(UiTestIds.GoLive.PreviewLiveDot);

            Assert.Equal(IdleStateValue, activeSourceBadge.GetAttribute("data-live-state") ?? string.Empty);
            Assert.DoesNotContain(LiveBadgeLabel, activeSourceBadge.TextContent, StringComparison.Ordinal);
            Assert.Equal(IdleStateValue, previewLiveDot.GetAttribute("data-live-state") ?? string.Empty);
        });
    }

    [Test]
    public void GoLivePage_RecordingSession_ShowsSourceBadgeAndPreviewDotAsLive()
    {
        SeedSceneState(CreateTwoCameraScene());
        Services.GetRequiredService<NavigationManager>().NavigateTo(AppTestData.Routes.GoLiveDemo);

        var cut = Render<GoLivePage>();
        cut.WaitForAssertion(() => Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.Page)));

        cut.FindByTestId(UiTestIds.GoLive.StartRecording).Click();

        cut.WaitForAssertion(() =>
        {
            var sessionState = Services.GetRequiredService<GoLiveSessionService>().State;
            var activeSourceBadge = cut.FindByTestId(UiTestIds.GoLive.SourceCameraBadge(AppTestData.Camera.FirstSourceId));
            var previewLiveDot = cut.FindByTestId(UiTestIds.GoLive.PreviewLiveDot);

            Assert.True(sessionState.IsRecordingActive);
            Assert.Equal(RecordingStateValue, activeSourceBadge.GetAttribute("data-live-state") ?? string.Empty);
            Assert.Contains(LiveBadgeLabel, activeSourceBadge.TextContent, StringComparison.Ordinal);
            Assert.Equal(RecordingStateValue, previewLiveDot.GetAttribute("data-live-state") ?? string.Empty);
        });
    }

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

    private void SeedSceneState(MediaSceneState sceneState)
    {
        _harness.JsRuntime.SavedJsonValues[SceneSettingsStorageKey] = JsonSerializer.Serialize(sceneState);
    }
}
