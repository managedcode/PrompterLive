namespace PrompterOne.Web.UITests;

internal static partial class BrowserTestConstants
{
    public static class ScenarioArtifacts
    {
        public const int CaptureRetryCount = 3;
        public const int CaptureRetryDelayMs = 150;
        public const int CaptureVisibleTimeoutMs = 15_000;
        public const string DetachedElementMessageFragment = "Element is not attached to the DOM";
        public const string OutputDirectoryName = "output";
        public const string PlaywrightDirectoryName = "playwright";
        public const string ImageExtension = ".png";
        public const string RepositoryRootRelativePath = "../../../../../";
        public const string Separator = "-";
        public const string UnstableElementMessageFragment = "element is not stable";
    }

    public static class StudioWorkflow
    {
        public const string Name = "studio-workflow";
        public const string FolderCreateStep = "01-library-folder-overlay";
        public const string FolderCreatedStep = "02-library-folder-created";
        public const string ScriptMovedStep = "03-library-script-moved";
        public const string EditorInitialStep = "04-editor-initial";
        public const string EditorFormattedStep = "05-editor-formatted";
    }

    public static class ReaderWorkflow
    {
        public const string Name = "reader-workflow";
        public const string LearnInitialStep = "01-learn-initial";
        public const string LearnPlaybackStep = "02-learn-playback";
        public const string TeleprompterInitialStep = "03-teleprompter-initial";
        public const string TeleprompterCameraStep = "04-teleprompter-camera";
        public const string TeleprompterWidth = "82";
        public const string TeleprompterFocal = "35";
    }

    public static class TeleprompterFullFlow
    {
        public const string Name = "teleprompter-product-launch";
        public const string InitialStep = "01-product-launch-initial";
        public const string PlaybackStep = "02-product-launch-playback";
        public const string TransitionStep = "03-product-launch-transition";
    }

    public static class NewScriptWorkflow
    {
        public const string Name = "new-script-workflow";
        public const string LibraryInitialStep = "01-library-initial";
        public const string EditorEmptyStep = "02-editor-empty";
    }

    public static class LiveWorkflow
    {
        public const string Name = "live-workflow";
        public const string SettingsConfiguredStep = "01-settings-configured";
        public const string GoLiveInitialStep = "02-go-live-initial";
        public const string GoLivePreviewSwitchedStep = "03-go-live-preview-switched";
        public const string GoLiveConfiguredStep = "04-go-live-configured";
        public const string MicDelayMilliseconds = "180";
        public const string MicLevelPercent = "72";
    }
}
