using System.Text.RegularExpressions;
using Microsoft.Playwright;
using PrompterLive.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterLive.App.UITests;

public sealed class StudioWorkflowScenarioTests(StandaloneAppFixture fixture) : IClassFixture<StandaloneAppFixture>
{
    private readonly StandaloneAppFixture _fixture = fixture;

    [Fact]
    public async Task StudioWorkflow_LibraryToEditorAuthoring_CapturesArtifacts()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.StudioWorkflow.Name);
            await CreateRoadshowsFolderAsync(page);
            await MoveDemoScriptIntoRoadshowsAsync(page);
            await OpenDemoScriptInEditorAsync(page);
            await ApplyEditorFormattingAsync(page);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task StudioWorkflow_LearnAndTeleprompterReader_CapturesArtifacts()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.ReaderWorkflow.Name);
            await OpenQuantumLearnFromLibraryAsync(page);
            await ExerciseLearnReaderAsync(page);
            await ReturnHomeAsync(page);
            await OpenQuantumTeleprompterFromLibraryAsync(page);
            await ExerciseTeleprompterAsync(page);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task StudioWorkflow_SettingsAndGoLiveStudio_CapturesArtifacts()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.LiveWorkflow.Name);
            await ConfigureMediaSettingsAsync(page);
            await OpenGoLiveFromSettingsAsync(page);
            await SwitchGoLivePreviewAsync(page);
            await ConfigureGoLiveDestinationsAsync(page);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    private static async Task CreateRoadshowsFolderAsync(IPage page)
    {
        await page.GotoAsync(BrowserTestConstants.Routes.Library);
        await Expect(page.GetByTestId(UiTestIds.Library.Page)).ToBeVisibleAsync();
        await page.GetByTestId(UiTestIds.Library.FolderCreateStart).ClickAsync();
        await Expect(page.GetByTestId(UiTestIds.Library.NewFolderOverlay)).ToBeVisibleAsync();
        await UiScenarioArtifacts.CapturePageAsync(page, BrowserTestConstants.StudioWorkflow.Name, BrowserTestConstants.StudioWorkflow.FolderCreateStep);

        await page.GetByTestId(UiTestIds.Library.NewFolderName).FillAsync(BrowserTestConstants.Folders.RoadshowsName);
        await page.GetByTestId(UiTestIds.Library.NewFolderParent).SelectOptionAsync([BrowserTestConstants.Folders.PresentationsId]);
        await page.GetByTestId(UiTestIds.Library.NewFolderSubmit).ClickAsync();

        await Expect(page.GetByTestId(BrowserTestConstants.Elements.RoadshowsFolder)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Header.LibraryBreadcrumbCurrent)).ToHaveTextAsync(BrowserTestConstants.Folders.RoadshowsName);
        await UiScenarioArtifacts.CapturePageAsync(page, BrowserTestConstants.StudioWorkflow.Name, BrowserTestConstants.StudioWorkflow.FolderCreatedStep);
    }

    private static async Task MoveDemoScriptIntoRoadshowsAsync(IPage page)
    {
        await page.GetByTestId(UiTestIds.Library.FolderAll).ClickAsync();
        await page.GetByTestId(UiTestIds.Library.CardMenu(BrowserTestConstants.Scripts.DemoId)).ClickAsync();
        await page.GetByTestId(UiTestIds.Library.Move(BrowserTestConstants.Scripts.DemoId, BrowserTestConstants.Folders.RoadshowsId)).ClickAsync();
        await page.GetByTestId(BrowserTestConstants.Elements.RoadshowsFolder).ClickAsync();

        await Expect(page.GetByTestId(BrowserTestConstants.Elements.DemoCard)).ToContainTextAsync(BrowserTestConstants.Scripts.ProductLaunchTitle);
        await Expect(page.GetByTestId(UiTestIds.Header.LibraryBreadcrumbCurrent)).ToHaveTextAsync(BrowserTestConstants.Folders.RoadshowsName);
        await UiScenarioArtifacts.CapturePageAsync(page, BrowserTestConstants.StudioWorkflow.Name, BrowserTestConstants.StudioWorkflow.ScriptMovedStep);
    }

    private static async Task OpenDemoScriptInEditorAsync(IPage page)
    {
        await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);
        await Expect(page.GetByTestId(UiTestIds.Editor.Page)).ToBeVisibleAsync();
        await UiScenarioArtifacts.CapturePageAsync(page, BrowserTestConstants.StudioWorkflow.Name, BrowserTestConstants.StudioWorkflow.EditorInitialStep);
    }

    private static async Task ApplyEditorFormattingAsync(IPage page)
    {
        await SelectSourceTextAsync(page, BrowserTestConstants.Editor.Welcome);
        await page.GetByTestId(UiTestIds.Editor.FormatTrigger).ClickAsync();
        await page.GetByTestId(UiTestIds.Editor.FormatHighlight).ClickAsync();
        await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput)).ToHaveValueAsync(new Regex(Regex.Escape(BrowserTestConstants.Editor.HighlightFragment)));

        await page.GetByTestId(UiTestIds.Editor.PauseTrigger).ClickAsync();
        await page.GetByTestId(UiTestIds.Editor.PauseTwoSeconds).ClickAsync();
        await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput)).ToHaveValueAsync(new Regex(Regex.Escape(BrowserTestConstants.Editor.PauseFragment)));

        await page.GetByTestId(UiTestIds.Editor.Duration).FillAsync(BrowserTestConstants.Editor.DisplayDuration);
        await page.GetByTestId(UiTestIds.Editor.Version).ClickAsync();
        await Expect(page.GetByTestId(UiTestIds.Editor.Duration)).ToHaveValueAsync(BrowserTestConstants.Editor.DisplayDuration);
        await UiScenarioArtifacts.CapturePageAsync(page, BrowserTestConstants.StudioWorkflow.Name, BrowserTestConstants.StudioWorkflow.EditorFormattedStep);
    }

    private static async Task OpenQuantumLearnFromLibraryAsync(IPage page)
    {
        await page.GotoAsync(BrowserTestConstants.Routes.Library);
        await Expect(page.GetByTestId(UiTestIds.Library.Page)).ToBeVisibleAsync();
        await page.GetByTestId(UiTestIds.Library.CardLearn(BrowserTestConstants.Scripts.QuantumId)).ClickAsync();
        await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(BrowserTestConstants.Routes.LearnQuantum));
        await Expect(page.GetByTestId(UiTestIds.Learn.Page)).ToBeVisibleAsync();
        await UiScenarioArtifacts.CapturePageAsync(page, BrowserTestConstants.ReaderWorkflow.Name, BrowserTestConstants.ReaderWorkflow.LearnInitialStep);
    }

    private static async Task ExerciseLearnReaderAsync(IPage page)
    {
        await page.GetByTestId(UiTestIds.Learn.SpeedUp).ClickAsync();
        await page.GetByTestId(UiTestIds.Learn.StepForwardLarge).ClickAsync();
        await page.GetByTestId(UiTestIds.Learn.StepForward).ClickAsync();
        await page.GetByTestId(UiTestIds.Learn.PlayToggle).ClickAsync();
        await page.WaitForTimeoutAsync(BrowserTestConstants.Timing.LearnPlaybackDelayMs);
        await Expect(page.GetByTestId(UiTestIds.Learn.Word)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Learn.NextPhrase)).Not.ToHaveTextAsync(BrowserTestConstants.Learn.EndOfScriptText);
        await UiScenarioArtifacts.CapturePageAsync(page, BrowserTestConstants.ReaderWorkflow.Name, BrowserTestConstants.ReaderWorkflow.LearnPlaybackStep);
    }

    private static async Task ReturnHomeAsync(IPage page)
    {
        await page.GetByTestId(UiTestIds.Header.Home).ClickAsync();
        await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(AppRoutes.Library));
        await Expect(page.GetByTestId(UiTestIds.Library.Page)).ToBeVisibleAsync();
    }

    private static async Task OpenQuantumTeleprompterFromLibraryAsync(IPage page)
    {
        await page.GetByTestId(UiTestIds.Library.CardRead(BrowserTestConstants.Scripts.QuantumId)).ClickAsync();
        await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(BrowserTestConstants.Routes.TeleprompterQuantum));
        await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page)).ToBeVisibleAsync();
        await UiScenarioArtifacts.CapturePageAsync(page, BrowserTestConstants.ReaderWorkflow.Name, BrowserTestConstants.ReaderWorkflow.TeleprompterInitialStep);
    }

    private static async Task ExerciseTeleprompterAsync(IPage page)
    {
        await SetRangeValueAsync(page.GetByTestId(UiTestIds.Teleprompter.WidthSlider), BrowserTestConstants.ReaderWorkflow.TeleprompterWidth);
        await SetRangeValueAsync(page.GetByTestId(UiTestIds.Teleprompter.FocalSlider), BrowserTestConstants.ReaderWorkflow.TeleprompterFocal);
        await page.GetByTestId(UiTestIds.Teleprompter.FontUp).ClickAsync();
        await page.GetByTestId(UiTestIds.Teleprompter.PlayToggle).ClickAsync();
        await page.WaitForTimeoutAsync(BrowserTestConstants.Timing.ReaderPlaybackDelayMs);
        await page.GetByTestId(UiTestIds.Teleprompter.NextBlock).ClickAsync();
        await page.GetByTestId(UiTestIds.Teleprompter.PreviousBlock).ClickAsync();
        await page.GetByTestId(UiTestIds.Teleprompter.NextWord).ClickAsync();
        await TeleprompterCameraDriver.EnsureEnabledAsync(page);
        await UiScenarioArtifacts.CapturePageAsync(page, BrowserTestConstants.ReaderWorkflow.Name, BrowserTestConstants.ReaderWorkflow.TeleprompterCameraStep);
    }

    private static async Task ConfigureMediaSettingsAsync(IPage page)
    {
        await page.GotoAsync(BrowserTestConstants.Routes.Settings);
        await Expect(page.GetByTestId(UiTestIds.Settings.Page)).ToBeVisibleAsync();
        await page.GetByTestId(UiTestIds.Settings.NavCameras).ClickAsync();
        await page.GetByTestId(UiTestIds.Settings.RequestMedia).ClickAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.CameraDevice(BrowserTestConstants.Media.PrimaryCameraId))).ToBeVisibleAsync();
        await page.GetByTestId(UiTestIds.Settings.DefaultCamera).SelectOptionAsync([BrowserTestConstants.Media.PrimaryCameraId]);
        await page.GetByTestId(UiTestIds.Settings.CameraResolution).SelectOptionAsync([BrowserTestConstants.Streaming.ResolutionHd720]);
        await page.GetByTestId(UiTestIds.Settings.CameraFrameRate).SelectOptionAsync([BrowserTestConstants.Streaming.CameraFrameRateFps24]);
        await page.GetByTestId(UiTestIds.Settings.CameraDeviceAction(BrowserTestConstants.Media.SecondaryCameraId)).ClickAsync();

        await page.GetByTestId(UiTestIds.Settings.NavMics).ClickAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.MicDevice(BrowserTestConstants.Media.PrimaryMicrophoneId))).ToBeVisibleAsync();
        await page.GetByTestId(UiTestIds.Settings.PrimaryMic).SelectOptionAsync([BrowserTestConstants.Media.PrimaryMicrophoneId]);
        await SetRangeValueAsync(page.GetByTestId(UiTestIds.Settings.MicLevel), BrowserTestConstants.LiveWorkflow.MicLevelPercent);
        await SetRangeValueAsync(page.GetByTestId(UiTestIds.Settings.MicDelay(BrowserTestConstants.Media.PrimaryMicrophoneId)), BrowserTestConstants.LiveWorkflow.MicDelayMilliseconds);
        await UiScenarioArtifacts.CapturePageAsync(page, BrowserTestConstants.LiveWorkflow.Name, BrowserTestConstants.LiveWorkflow.SettingsConfiguredStep);
    }

    private static async Task OpenGoLiveFromSettingsAsync(IPage page)
    {
        await page.GetByTestId(UiTestIds.Settings.CameraRoutingCta).ClickAsync();
        await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(AppRoutes.GoLive));
        await Expect(page.GetByTestId(UiTestIds.GoLive.Page)).ToBeVisibleAsync();
        await UiScenarioArtifacts.CapturePageAsync(page, BrowserTestConstants.LiveWorkflow.Name, BrowserTestConstants.LiveWorkflow.GoLiveInitialStep);
    }

    private static async Task ConfigureGoLiveDestinationsAsync(IPage page)
    {
        await page.GetByTestId(UiTestIds.GoLive.ObsToggle).ClickAsync();
        await page.GetByTestId(UiTestIds.GoLive.RecordingToggle).ClickAsync();
        await page.GetByTestId(UiTestIds.GoLive.LiveKitToggle).ClickAsync();
        await page.GetByTestId(UiTestIds.GoLive.LiveKitServer).FillAsync(BrowserTestConstants.GoLive.LiveKitServer);
        await page.GetByTestId(UiTestIds.GoLive.LiveKitRoom).FillAsync(BrowserTestConstants.GoLive.LiveKitRoom);
        await page.GetByTestId(UiTestIds.GoLive.LiveKitToken).FillAsync(BrowserTestConstants.GoLive.LiveKitToken);
        await page.GetByTestId(UiTestIds.GoLive.YoutubeToggle).ClickAsync();
        await page.GetByTestId(UiTestIds.GoLive.YoutubeUrl).FillAsync(BrowserTestConstants.GoLive.YoutubeUrl);
        await page.GetByTestId(UiTestIds.GoLive.YoutubeKey).FillAsync(BrowserTestConstants.GoLive.YoutubeKey);
        await page.GetByTestId(UiTestIds.GoLive.StreamTextOverlay).ClickAsync();
        await page.GetByTestId(UiTestIds.GoLive.StreamIncludeCamera).ClickAsync();
        await UiScenarioArtifacts.CapturePageAsync(page, BrowserTestConstants.LiveWorkflow.Name, BrowserTestConstants.LiveWorkflow.GoLiveConfiguredStep);
    }

    private static async Task SwitchGoLivePreviewAsync(IPage page)
    {
        await page.WaitForFunctionAsync(
            BrowserTestConstants.Media.ElementUsesVideoDeviceScript,
            new object[]
            {
                UiDomIds.GoLive.PreviewVideo,
                BrowserTestConstants.Media.PrimaryCameraId
            },
            new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
        await page.GetByTestId(UiTestIds.GoLive.SourceCameraAction(BrowserTestConstants.Media.PrimaryCameraId)).ClickAsync();
        await page.WaitForFunctionAsync(
            BrowserTestConstants.Media.ElementUsesVideoDeviceScript,
            new object[]
            {
                UiDomIds.GoLive.PreviewVideo,
                BrowserTestConstants.Media.SecondaryCameraId
            },
            new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
        await UiScenarioArtifacts.CapturePageAsync(page, BrowserTestConstants.LiveWorkflow.Name, BrowserTestConstants.LiveWorkflow.GoLivePreviewSwitchedStep);
    }

    private static Task SelectSourceTextAsync(IPage page, string targetText) =>
        page.GetByTestId(UiTestIds.Editor.SourceInput).EvaluateAsync(
            """
            (element, target) => {
                const start = element.value.indexOf(target);
                element.focus();
                element.setSelectionRange(start, start + target.length);
                element.dispatchEvent(new Event("select", { bubbles: true }));
                element.dispatchEvent(new Event("keyup", { bubbles: true }));
            }
            """,
            targetText);

    private static Task SetRangeValueAsync(ILocator locator, string value) =>
        locator.EvaluateAsync(
            """
            (element, nextValue) => {
                element.value = nextValue;
                element.dispatchEvent(new Event("input", { bubbles: true }));
                element.dispatchEvent(new Event("change", { bubbles: true }));
            }
            """,
            value);
}
