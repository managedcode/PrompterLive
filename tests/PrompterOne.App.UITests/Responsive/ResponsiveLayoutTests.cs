using PrompterOne.Shared.Contracts;

namespace PrompterOne.App.UITests;

public sealed class ResponsiveLayoutTests(StandaloneAppFixture fixture) : AppUiTestBase(fixture), IClassFixture<StandaloneAppFixture>
{
    public static TheoryData<ResponsiveViewport> ResponsiveViewports =>
        BrowserTestConstants.ResponsiveLayout.Viewports.Aggregate(
            new TheoryData<ResponsiveViewport>(),
            static (data, viewport) =>
            {
                data.Add(viewport);
                return data;
            });

    [Theory]
    [MemberData(nameof(ResponsiveViewports))]
    public Task LibraryRoute_KeepsPrimaryControlsVisibleAcrossResponsiveViewports(ResponsiveViewport viewport) =>
        RunPageAsync(page => ResponsiveLayoutAssertions.AssertRouteControlsVisibleAsync(
            page,
            BrowserTestConstants.ResponsiveLayout.LibraryRouteName,
            BrowserTestConstants.Routes.Library,
            viewport,
            UiTestIds.Library.Page,
            UiTestIds.Library.FolderAll,
            UiTestIds.Library.OpenSettings,
            UiTestIds.Header.LibrarySearch,
            UiTestIds.Header.GoLive,
            UiTestIds.Header.LibraryNewScript));

    [Theory]
    [MemberData(nameof(ResponsiveViewports))]
    public Task EditorRoute_KeepsPrimaryControlsVisibleAcrossResponsiveViewports(ResponsiveViewport viewport) =>
        RunPageAsync(page => ResponsiveLayoutAssertions.AssertRouteControlsVisibleAsync(
            page,
            BrowserTestConstants.ResponsiveLayout.EditorRouteName,
            BrowserTestConstants.Routes.EditorDemo,
            viewport,
            UiTestIds.Editor.Page,
            UiTestIds.Header.Back,
            UiTestIds.Header.EditorLearn,
            UiTestIds.Header.EditorRead,
            UiTestIds.Editor.MainPanel,
            UiTestIds.Editor.SourceInput));

    [Theory]
    [MemberData(nameof(ResponsiveViewports))]
    public Task LearnRoute_KeepsPrimaryControlsVisibleAcrossResponsiveViewports(ResponsiveViewport viewport) =>
        RunPageAsync(page => ResponsiveLayoutAssertions.AssertRouteControlsVisibleAsync(
            page,
            BrowserTestConstants.ResponsiveLayout.LearnRouteName,
            BrowserTestConstants.Routes.LearnDemo,
            viewport,
            UiTestIds.Learn.Page,
            UiTestIds.Learn.Display,
            UiTestIds.Learn.SpeedDown,
            UiTestIds.Learn.SpeedUp,
            UiTestIds.Learn.PlayToggle,
            UiTestIds.Learn.ProgressLabel));

    [Theory]
    [MemberData(nameof(ResponsiveViewports))]
    public Task TeleprompterRoute_KeepsPrimaryControlsVisibleAcrossResponsiveViewports(ResponsiveViewport viewport) =>
        RunPageAsync(page => ResponsiveLayoutAssertions.AssertRouteControlsVisibleAsync(
            page,
            BrowserTestConstants.ResponsiveLayout.TeleprompterRouteName,
            BrowserTestConstants.Routes.TeleprompterDemo,
            viewport,
            UiTestIds.Teleprompter.Page,
            UiTestIds.Teleprompter.Back,
            UiTestIds.Teleprompter.MirrorControls,
            UiTestIds.Teleprompter.Sliders,
            UiTestIds.Teleprompter.Stage,
            UiTestIds.Teleprompter.Controls));

    [Theory]
    [MemberData(nameof(ResponsiveViewports))]
    public Task SettingsRoute_KeepsPrimaryControlsVisibleAcrossResponsiveViewports(ResponsiveViewport viewport) =>
        RunPageAsync(page => ResponsiveLayoutAssertions.AssertRouteControlsVisibleAsync(
            page,
            BrowserTestConstants.ResponsiveLayout.SettingsRouteName,
            BrowserTestConstants.Routes.Settings,
            viewport,
            UiTestIds.Settings.Page,
            UiTestIds.Settings.Title,
            UiTestIds.Settings.NavCloud,
            UiTestIds.Settings.NavStreaming,
            UiTestIds.Settings.NavAbout,
            UiTestIds.Settings.CloudPanel));

    [Theory]
    [MemberData(nameof(ResponsiveViewports))]
    public Task GoLiveRoute_KeepsPrimaryControlsVisibleAcrossResponsiveViewports(ResponsiveViewport viewport) =>
        RunPageAsync(page => ResponsiveLayoutAssertions.AssertRouteControlsVisibleAsync(
            page,
            BrowserTestConstants.ResponsiveLayout.GoLiveRouteName,
            BrowserTestConstants.Routes.GoLiveDemo,
            viewport,
            UiTestIds.GoLive.Page,
            UiTestIds.GoLive.SessionBar,
            UiTestIds.GoLive.Back,
            UiTestIds.GoLive.OpenSettings,
            UiTestIds.GoLive.StartRecording,
            UiTestIds.GoLive.StartStream,
            UiTestIds.GoLive.ProgramCard,
            UiTestIds.GoLive.SourceRail,
            UiTestIds.GoLive.PreviewRail));
}
