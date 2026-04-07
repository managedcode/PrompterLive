using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PrompterOne.Core.Models.Media;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Pages;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

public sealed class TeleprompterSceneTests : BunitContext
{
    [Test]
    public void TeleprompterPage_UsesSingleBackgroundCameraLayer()
    {
        var harness = TestHarnessFactory.Create(this,
        [
            new MediaDeviceInfo("cam-1", "Front camera", MediaDeviceKind.Camera, true),
            new MediaDeviceInfo("cam-2", "Desk camera", MediaDeviceKind.Camera),
            new MediaDeviceInfo("mic-1", "Broadcast mic", MediaDeviceKind.Microphone, true)
        ]);

        harness.SceneService.ApplyState(new MediaSceneState(
            Cameras:
            [
                new SceneCameraSource(
                    "cam-source-1",
                    "cam-1",
                    "Front camera",
                    new MediaSourceTransform(X: 0.82, Y: 0.82, Width: 0.28, Height: 0.28, ZIndex: 1)),
                new SceneCameraSource(
                    "cam-source-2",
                    "cam-2",
                    "Desk camera",
                    new MediaSourceTransform(X: 0.18, Y: 0.18, Width: 0.24, Height: 0.24, MirrorHorizontal: false, ZIndex: 2))
            ],
            PrimaryMicrophoneId: "mic-1",
            PrimaryMicrophoneLabel: "Broadcast mic",
            AudioBus: new AudioBusState([new AudioInputState("mic-1", "Broadcast mic")])));

        harness.JsRuntime.SavedValues[StudioSettingsStore.StorageKey] = new StudioSettings(
            new CameraStudioSettings(DefaultCameraId: "cam-2"),
            new MicrophoneStudioSettings(DefaultMicrophoneId: "mic-1"),
            new StreamStudioSettings());

        var cut = Render<TeleprompterPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("id=\"rd-camera\"", cut.Markup);
            Assert.Contains("data-test=\"teleprompter-camera-layer-primary\"", cut.Markup);
            Assert.Contains("data-camera-device-id=\"cam-2\"", cut.Markup);
            Assert.DoesNotContain("rd-camera-overlay-", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("data-camera-role=\"overlay\"", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Test]
    public void TeleprompterPage_RendersReadableWordSpacingInsideReaderCards()
    {
        var harness = TestHarnessFactory.Create(this);
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(navigation.GetUriWithQueryParameter(AppRoutes.ScriptIdQueryKey, AppTestData.Scripts.DemoId));
        var cut = Render<TeleprompterPage>();

        cut.WaitForAssertion(() =>
        {
            var activeText = cut.FindByTestId(UiTestIds.Teleprompter.CardText(0)).TextContent;

            Assert.Contains("Good morning everyone", activeText, StringComparison.Ordinal);
            Assert.Contains("what I believe", activeText, StringComparison.Ordinal);
            Assert.DoesNotContain("Goodmorningeveryone", activeText, StringComparison.Ordinal);
        });
    }
}
