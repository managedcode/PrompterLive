namespace PrompterOne.App.UITests;

internal static partial class BrowserTestConstants
{
    public static class Media
    {
        public const string HarnessGlobal = "__prompterOneMediaHarness";
        public const string RecordingFileHarnessGlobal = "__prompterOneRecordingFileHarness";
        public const string PrimaryCameraId = "browser-cam-primary";
        public const string SecondaryCameraId = "browser-cam-secondary";
        public const string PrimaryCameraLabel = "Browser Camera A";
        public const string SecondaryCameraLabel = "Browser Camera B";
        public const string PrimaryMicrophoneId = "browser-mic-primary";
        public const string PrimaryMicrophoneLabel = "Browser Microphone";
        public const string VideoInputKind = "videoinput";
        public const string AudioInputKind = "audioinput";
        public const int ExpectedVideoTrackCount = 1;
        public const int ExpectedAudioTrackCount = 1;
        public const int LiveLevelThreshold = 5;
        public const int MinimumVisiblePixelCount = 16;
        public const string FabricatedCameraLabel = "Camera 1";
        public const string FabricatedMicrophoneLabel = "Microphone 1";
        public const string FabricatedUnnamedDeviceLabel = "Unnamed device";
        public const string ListDevicesScript = "() => window.__prompterOneMediaHarness.listDevices()";
        public const string ClearDeviceLabelsScript = "() => window.__prompterOneMediaHarness.clearDeviceLabels()";
        public const string DisableConcurrentLocalCameraCaptureScript =
            "window.__prompterOneMediaCapabilityOverride = { supportsConcurrentLocalCameraCaptures: false };";
        public const string ClearRequestLogScript = "() => window.__prompterOneMediaHarness.clearRequestLog()";
        public const string ConcealDeviceIdentityUntilRequestScript = "() => window.__prompterOneMediaHarness.concealDeviceIdentityUntilRequest()";
        public const string GetRequestLogScript = "() => window.__prompterOneMediaHarness.getRequestLog()";
        public const string GetElementStateScript = "elementId => window.__prompterOneMediaHarness.getElementState(elementId)";
        public const string RestoreDeviceIdentityScript = "() => window.__prompterOneMediaHarness.restoreDeviceIdentity()";
        public const string ElementHasVideoStreamScript =
            "elementId => { const state = window.__prompterOneMediaHarness.getElementState(elementId); return Boolean(state?.hasStream && state.videoTrackCount >= 1 && state.metadata?.isSynthetic === true); }";
        public const string ElementHasNoStreamScript =
            "elementId => { const state = window.__prompterOneMediaHarness.getElementState(elementId); return Boolean(state && !state.hasStream); }";
        public const string ElementHasLiveAudioLevelScript =
            "([elementId, minimumLevel]) => Number(document.getElementById(elementId)?.dataset.liveLevel ?? '0') >= minimumLevel";
        public const string ElementUsesVideoDeviceScript =
            "([elementId, deviceId]) => { const state = window.__prompterOneMediaHarness.getElementState(elementId); return Boolean(state?.hasStream && state.metadata?.videoDeviceId === deviceId); }";
        public const string HasAudioOnlyRequestScript =
            "([audioId]) => window.__prompterOneMediaHarness.getRequestLog().some(request => request.hasVideo === false && request.hasAudio === true && request.resolvedAudioDeviceId === audioId)";
        public const string HasAudioVideoRequestScript =
            "([videoId, audioId]) => window.__prompterOneMediaHarness.getRequestLog().some(request => request.hasVideo === true && request.hasAudio === true && request.resolvedVideoDeviceId === videoId && request.resolvedAudioDeviceId === audioId)";
        public const string HasVideoOnlyRequestScript =
            "([videoId]) => window.__prompterOneMediaHarness.getRequestLog().some(request => request.hasVideo === true && request.hasAudio === false && request.resolvedVideoDeviceId === videoId)";
        public const string GetSavedRecordingStateScript =
            "() => window.__prompterOneRecordingFileHarness.getSavedRecordingState()";
        public const string AnalyzeSavedRecordingScript =
            "() => window.__prompterOneRecordingFileHarness.analyzeSavedRecording()";
        public const string ResetSavedRecordingScript =
            "() => window.__prompterOneRecordingFileHarness.reset()";
        public const string SavedRecordingReadyScript =
            "() => Boolean(window.__prompterOneRecordingFileHarness.getSavedRecordingState()?.hasBlob && (window.__prompterOneRecordingFileHarness.getSavedRecordingState()?.sizeBytes ?? 0) > 0)";
    }
}
