using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Layout;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Tests;

namespace PrompterOne.App.Tests;

public sealed class MainLayoutGoLiveSessionTests : BunitContext
{
    private const string StreamingStateValue = "streaming";

    [Fact]
    public void MainLayout_HidesSharedHeader_OnGoLiveScreen()
    {
        _ = TestHarnessFactory.Create(this);
        Services.GetRequiredService<NavigationManager>().NavigateTo(AppRoutes.GoLive);

        var cut = RenderLayout();

        cut.WaitForAssertion(() =>
        {
            Assert.Empty(cut.FindAll($"[data-testid='{UiTestIds.Header.GoLive}']"));
        });
    }

    [Fact]
    public void MainLayout_ShowsPersistentWidget_WhenLiveSessionIsActiveOutsideGoLive()
    {
        _ = TestHarnessFactory.Create(this);
        Services.GetRequiredService<NavigationManager>().NavigateTo(AppRoutes.Library);
        Services.GetRequiredService<GoLiveSessionService>().SetState(CreateActiveSession(isRecordingActive: false));

        var cut = RenderLayout();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.FindByTestId(UiTestIds.Header.LiveWidget));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Header.LiveWidgetPreview));
        });
        cut.Markup.Contains(AppTestData.Camera.FrontCamera, StringComparison.Ordinal);
    }

    [Fact]
    public void MainLayout_LiveWidget_UsesOperationalMetadataInsteadOfScriptSubtitle()
    {
        _ = TestHarnessFactory.Create(this);
        Services.GetRequiredService<NavigationManager>().NavigateTo(AppRoutes.Library);
        Services.GetRequiredService<GoLiveSessionService>().SetState(CreateActiveSession(isRecordingActive: false));

        var cut = RenderLayout();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(
                AppTestData.Camera.FrontCamera,
                cut.FindByTestId(UiTestIds.Header.LiveWidgetTitle).TextContent.Trim());
            Assert.Equal(
                AppTestData.Scripts.BroadcastMic,
                cut.FindByTestId(UiTestIds.Header.LiveWidgetDetail).TextContent.Trim());
            Assert.DoesNotContain(
                "Intro",
                cut.FindByTestId(UiTestIds.Header.LiveWidgetDetail).TextContent,
                StringComparison.Ordinal);
        });
    }

    [Fact]
    public void MainLayout_LiveWidget_FallsBackToGenericGoLiveLabel_WhenNoSourceLabelExists()
    {
        _ = TestHarnessFactory.Create(this);
        Services.GetRequiredService<NavigationManager>().NavigateTo(AppRoutes.Library);
        Services.GetRequiredService<GoLiveSessionService>().SetState(CreateActiveSession(isRecordingActive: false) with
        {
            SelectedSourceId = string.Empty,
            SelectedSourceLabel = string.Empty,
            ActiveSourceId = string.Empty,
            ActiveSourceLabel = string.Empty
        });

        var cut = RenderLayout();

        cut.WaitForAssertion(() =>
            Assert.Equal(
                "Go Live",
                cut.FindByTestId(UiTestIds.Header.LiveWidgetTitle).TextContent.Trim()));
    }

    [Fact]
    public void MainLayout_MarksGoLiveIndicator_AsRecording_WhenRecordingSessionIsActive()
    {
        _ = TestHarnessFactory.Create(this);
        Services.GetRequiredService<NavigationManager>().NavigateTo(AppRoutes.Library);
        Services.GetRequiredService<GoLiveSessionService>().SetState(CreateActiveSession(isRecordingActive: true));

        var cut = RenderLayout();

        cut.WaitForAssertion(() =>
            Assert.Equal(
                "recording",
                cut.FindByTestId(UiTestIds.Header.GoLive).GetAttribute("data-live-state")));
    }

    [Fact]
    public void MainLayout_MarksGoLiveIndicator_AsStreaming_WhenStreamSessionIsActive()
    {
        _ = TestHarnessFactory.Create(this);
        Services.GetRequiredService<NavigationManager>().NavigateTo(AppRoutes.Library);
        Services.GetRequiredService<GoLiveSessionService>().SetState(CreateActiveSession(isRecordingActive: false));

        var cut = RenderLayout();

        cut.WaitForAssertion(() =>
            Assert.Equal(
                StreamingStateValue,
                cut.FindByTestId(UiTestIds.Header.GoLive).GetAttribute("data-live-state")));
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

    private IRenderedComponent<MainLayout> RenderLayout() =>
        Render<MainLayout>(parameters => parameters
            .Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<div>Body</div>"))));
}
