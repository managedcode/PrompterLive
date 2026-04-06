using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PrompterOne.Core.Models.Media;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Layout;
using PrompterOne.Shared.Pages;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Storage;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

public sealed class GoLiveCrossTabTests : BunitContext
{
    private const string IdleStateValue = "idle";
    private const string RecordingStateValue = "recording";
    private const string RemoteInstanceId = "remote-tab";

    [Test]
    public void GoLivePage_StartRecording_PublishesGoLiveSessionChangedMessage()
    {
        var harness = TestHarnessFactory.Create(this);
        SeedSceneState(harness, CreateTwoCameraScene());
        Services.GetRequiredService<NavigationManager>().NavigateTo(AppTestData.Routes.GoLiveDemo);

        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() => Assert.NotNull(cut.FindByTestId(UiTestIds.GoLive.Page)));
        cut.FindByTestId(UiTestIds.GoLive.StartRecording).Click();

        var envelope = harness.JsRuntime.InvocationRecords
            .Where(record => string.Equals(record.Identifier, CrossTabInteropMethodNames.Publish, StringComparison.Ordinal))
            .Select(record => Assert.IsType<CrossTabMessageEnvelope>(record.Arguments[1]))
            .Single(message => string.Equals(message.MessageType, CrossTabMessageTypes.GoLiveSessionChanged, StringComparison.Ordinal));

        var payload = envelope.DeserializePayload<GoLiveSessionState>();
        Assert.NotNull(payload);
        Assert.True(payload.IsRecordingActive);
        Assert.Equal(AppTestData.Camera.FrontCamera, payload.ActiveSourceLabel);
    }

    [Test]
    public async Task GoLiveSessionService_RespondsToRemoteStateRequest_WhenSessionIsActive()
    {
        var harness = TestHarnessFactory.Create(this);
        var service = Services.GetRequiredService<GoLiveSessionService>();
        service.SetState(CreateActiveSession(isRecordingActive: true));

        await service.StartCrossTabSyncAsync();

        var publishCountBeforeRequest = harness.JsRuntime.InvocationRecords.Count(record =>
            string.Equals(record.Identifier, CrossTabInteropMethodNames.Publish, StringComparison.Ordinal));

        await harness.CrossTabMessageBus.ReceiveAsync(
            CrossTabMessageEnvelope.Create(
                CrossTabMessageTypes.GoLiveSessionRequested,
                RemoteInstanceId,
                new { }));

        var publishRecord = harness.JsRuntime.InvocationRecords
            .Where(record => string.Equals(record.Identifier, CrossTabInteropMethodNames.Publish, StringComparison.Ordinal))
            .Skip(publishCountBeforeRequest)
            .Single();
        var envelope = Assert.IsType<CrossTabMessageEnvelope>(publishRecord.Arguments[1]);

        Assert.Equal(CrossTabMessageTypes.GoLiveSessionChanged, envelope.MessageType);

        var payload = envelope.DeserializePayload<GoLiveSessionState>();
        Assert.NotNull(payload);
        Assert.True(payload.IsRecordingActive);
        Assert.Equal(AppTestData.Camera.FrontCamera, payload.ActiveSourceLabel);
    }

    [Test]
    public async Task MainLayout_UpdatesGoLiveIndicator_WhenAnotherTabChangesGoLiveSessionState()
    {
        var harness = TestHarnessFactory.Create(this);
        Services.GetRequiredService<NavigationManager>().NavigateTo(AppRoutes.Library);

        var cut = Render<MainLayout>(parameters => parameters
            .Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<div>Body</div>"))));

        await harness.CrossTabMessageBus.ReceiveAsync(
            CrossTabMessageEnvelope.Create(
                CrossTabMessageTypes.GoLiveSessionChanged,
                RemoteInstanceId,
                CreateActiveSession(isRecordingActive: true)));

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(
                RecordingStateValue,
                cut.FindByTestId(UiTestIds.Header.GoLive).GetAttribute("data-live-state"));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Header.LiveWidget));
        });

        await harness.CrossTabMessageBus.ReceiveAsync(
            CrossTabMessageEnvelope.Create(
                CrossTabMessageTypes.GoLiveSessionChanged,
                RemoteInstanceId,
                GoLiveSessionState.Default));

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(
                IdleStateValue,
                cut.FindByTestId(UiTestIds.Header.GoLive).GetAttribute("data-live-state"));
            Assert.Empty(cut.FindAll(BunitTestSelectors.BuildTestIdSelector(UiTestIds.Header.LiveWidget)));
        });
    }

    private static GoLiveSessionState CreateActiveSession(bool isRecordingActive) =>
        new(
            ScriptId: AppTestData.Scripts.DemoId,
            ScriptTitle: AppTestData.Scripts.DemoTitle,
            ScriptSubtitle: "Intro",
            SelectedSourceId: AppTestData.Camera.FirstSourceId,
            SelectedSourceLabel: AppTestData.Camera.FrontCamera,
            ActiveSourceId: AppTestData.Camera.FirstSourceId,
            ActiveSourceLabel: AppTestData.Camera.FrontCamera,
            PrimaryMicrophoneLabel: AppTestData.Scripts.BroadcastMic,
            OutputResolution: StreamingResolutionPreset.FullHd1080p30,
            BitrateKbps: AppTestData.Streaming.BitrateKbps,
            IsStreamActive: true,
            IsRecordingActive: isRecordingActive,
            StreamStartedAt: DateTimeOffset.UtcNow.AddMinutes(-2),
            RecordingStartedAt: isRecordingActive ? DateTimeOffset.UtcNow.AddMinutes(-1) : null);

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
            AppTestData.Microphone.PrimaryDeviceId,
            AppTestData.Scripts.BroadcastMic,
            new AudioBusState([new AudioInputState(AppTestData.Microphone.PrimaryDeviceId, AppTestData.Scripts.BroadcastMic)]));

    private static void SeedSceneState(AppHarness harness, MediaSceneState scene)
    {
        harness.JsRuntime.SavedValues[BrowserAppSettingsKeys.SceneSettings] = scene;
    }
}
