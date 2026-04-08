using PrompterOne.Shared.Contracts;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class ResponsiveLayoutTests(StandaloneAppFixture fixture) : AppUiTestBase(fixture)
{
    public static IEnumerable<ResponsiveViewport> ResponsiveViewports =>
        BrowserTestConstants.ResponsiveLayout.Viewports;

    public static IEnumerable<ResponsiveViewport> IpadPortraitViewports =>
        BrowserTestConstants.ResponsiveLayout.IpadPortraitViewports;

    [Test]
    [MethodDataSource(nameof(ResponsiveViewports))]
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
            UiTestIds.Header.LibraryOpenScript,
            UiTestIds.Header.LibraryNewScript));

    [Test]
    [MethodDataSource(nameof(IpadPortraitViewports))]
    public Task LibraryRoute_KeepsHeaderBrandingVisibleAcrossIpadPortraitViewports(ResponsiveViewport viewport) =>
        RunPageAsync(page => ResponsiveLayoutAssertions.AssertRouteControlsVisibleAsync(
            page,
            BrowserTestConstants.ResponsiveLayout.LibraryRouteName,
            BrowserTestConstants.Routes.Library,
            viewport,
            UiTestIds.Library.Page,
            UiTestIds.Header.Home,
            UiTestIds.Header.Brand,
            UiTestIds.Header.LibraryBreadcrumbCurrent,
            UiTestIds.Header.LibrarySearch,
            UiTestIds.Header.GoLive,
            UiTestIds.Header.LibraryOpenScript,
            UiTestIds.Header.LibraryNewScript));

    [Test]
    [MethodDataSource(nameof(ResponsiveViewports))]
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

    [Test]
    [MethodDataSource(nameof(ResponsiveViewports))]
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

    [Test]
    [MethodDataSource(nameof(ResponsiveViewports))]
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

    [Test]
    [MethodDataSource(nameof(ResponsiveViewports))]
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

    [Test]
    [MethodDataSource(nameof(ResponsiveViewports))]
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
