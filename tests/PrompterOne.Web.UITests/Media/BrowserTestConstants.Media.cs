using System.Text.RegularExpressions;
using PrompterOne.Shared.Contracts;

namespace PrompterOne.Web.UITests;

internal static partial class BrowserTestConstants
{
    public static class Media
    {
        public const string HarnessGlobal = AppMediaRuntime.BrowserMedia.SyntheticHarnessGlobalName;
        public const string MediaCapabilityOverrideGlobal = AppMediaRuntime.BrowserMedia.CaptureCapabilitiesOverrideGlobalName;
        public const string RecordingFileHarnessGlobal = AppMediaRuntime.BrowserMedia.RecordingFileHarnessGlobalName;
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
        public const int MinimumVisibleChannelValue = 12;
        public const int MinimumVisiblePixelCount = 16;
        public static string RuntimeContractInitializationScript => $$"""
            window["{{AppMediaRuntime.Runtime.GlobalName}}"] = window["{{AppMediaRuntime.Runtime.GlobalName}}"] || {};
            window["{{AppMediaRuntime.Runtime.GlobalName}}"]["{{AppMediaRuntime.Runtime.ContractProperty}}"] = {
                browserMediaInteropNamespace: "{{AppMediaRuntime.BrowserMedia.InteropNamespace}}",
                captureCapabilitiesOverrideGlobalName: "{{AppMediaRuntime.BrowserMedia.CaptureCapabilitiesOverrideGlobalName}}",
                concealDeviceIdentitySessionFlag: "{{AppMediaRuntime.BrowserMedia.ConcealDeviceIdentitySessionFlag}}",
                defaultVdoNinjaBaseUrl: "{{AppMediaRuntime.GoLive.DefaultVdoNinjaBaseUrl}}",
                defaultVdoNinjaStreamLabel: "{{AppMediaRuntime.GoLive.DefaultVdoNinjaStreamLabel}}",
                goLiveMediaComposerNamespace: "{{AppMediaRuntime.GoLive.MediaComposerNamespace}}",
                goLiveOutputNamespace: "{{AppMediaRuntime.GoLive.OutputNamespace}}",
                goLiveOutputSupportNamespace: "{{AppMediaRuntime.GoLive.OutputSupportNamespace}}",
                goLiveOutputVdoNinjaNamespace: "{{AppMediaRuntime.GoLive.OutputVdoNinjaNamespace}}",
                goLiveRemoteSourcesNamespace: "{{AppMediaRuntime.GoLive.RemoteSourcesNamespace}}",
                liveKitClientGlobalName: "{{AppMediaRuntime.Vendor.LiveKitClientGlobalName}}",
                mediaHarnessEnabledProperty: "{{AppMediaRuntime.Runtime.HarnessEnabledProperty}}",
                recordingFileHarnessGlobalName: "{{AppMediaRuntime.BrowserMedia.RecordingFileHarnessGlobalName}}",
                remoteSourceSeedGlobalName: "{{AppMediaRuntime.BrowserMedia.RemoteSourceSeedGlobalName}}",
                runtimeGlobalName: "{{AppMediaRuntime.Runtime.GlobalName}}",
                syntheticHarnessGlobalName: "{{AppMediaRuntime.BrowserMedia.SyntheticHarnessGlobalName}}",
                syntheticMetadataProperty: "{{AppMediaRuntime.BrowserMedia.SyntheticMetadataProperty}}",
                vdoNinjaLegacyGlobalName: "{{AppMediaRuntime.Vendor.VdoNinjaLegacyGlobalName}}",
                vdoNinjaSdkGlobalName: "{{AppMediaRuntime.Vendor.VdoNinjaSdkGlobalName}}"
            };
            window["{{AppMediaRuntime.Runtime.GlobalName}}"]["{{AppMediaRuntime.Runtime.HarnessEnabledProperty}}"] = true;
            """;
        public static string ListDevicesScript => $$"""() => window["{{HarnessGlobal}}"].listDevices()""";
        public static string ClearDeviceLabelsScript => $$"""() => window["{{HarnessGlobal}}"].clearDeviceLabels()""";
        public static string DisableConcurrentLocalCameraCaptureScript =>
            $$"""window["{{MediaCapabilityOverrideGlobal}}"] = { supportsConcurrentLocalCameraCaptures: false };""";
        public static string ClearRequestLogScript => $$"""() => window["{{HarnessGlobal}}"].clearRequestLog()""";
        public static string ConcealDeviceIdentityUntilRequestScript => $$"""() => window["{{HarnessGlobal}}"].concealDeviceIdentityUntilRequest()""";
        public static string GetRequestLogScript => $$"""() => window["{{HarnessGlobal}}"].getRequestLog()""";
        public static string GetElementStateScript =>
            $$"""testId => { const element = document.querySelector(`[data-test="${testId}"]`); return window["{{HarnessGlobal}}"].getElementState(element?.id ?? ""); }""";
        public static string GetActiveVideoTrackCountScript =>
            $$"""() => window["{{HarnessGlobal}}"].getActiveTrackCount({ kind: "video" })""";
        public static string GetActiveVideoTrackCountForDeviceScript =>
            $$"""deviceId => window["{{HarnessGlobal}}"].getActiveTrackCount({ kind: "video", deviceId })""";
        public static string GetActiveVideoTracksScript =>
            $$"""() => window["{{HarnessGlobal}}"].getActiveTracks({ kind: "video" })""";
        public static string HasNoActiveVideoTracksScript =>
            $$"""() => window["{{HarnessGlobal}}"].getActiveTrackCount({ kind: "video" }) === 0""";
        public static string HasNoActiveVideoTrackForDeviceScript =>
            $$"""deviceId => window["{{HarnessGlobal}}"].getActiveTrackCount({ kind: "video", deviceId }) === 0""";
        public static string RestoreDeviceIdentityScript => $$"""() => window["{{HarnessGlobal}}"].restoreDeviceIdentity()""";
        public static string ElementHasVideoStreamScript =>
            $$"""testId => { const element = document.querySelector(`[data-test="${testId}"]`); const state = window["{{HarnessGlobal}}"].getElementState(element?.id ?? ""); return Boolean(state?.hasStream && state.videoTrackCount >= 1 && state.metadata?.isSynthetic === true); }""";
        public static string ElementHasNoStreamScript =>
            $$"""testId => { const element = document.querySelector(`[data-test="${testId}"]`); const state = window["{{HarnessGlobal}}"].getElementState(element?.id ?? ""); return Boolean(state && !state.hasStream); }""";
        public const string ElementHasLiveAudioLevelScript =
            "([testId, minimumLevel]) => Number(document.querySelector(`[data-test=\"${testId}\"]`)?.dataset.liveLevel ?? '0') >= minimumLevel";
        public static string ElementUsesVideoDeviceScript =>
            $$"""([testId, deviceId]) => { const element = document.querySelector(`[data-test="${testId}"]`); const state = window["{{HarnessGlobal}}"].getElementState(element?.id ?? ""); return Boolean(state?.hasStream && state.metadata?.videoDeviceId === deviceId); }""";
        public static string ElementHasVisibleVideoScript =>
            $$"""([testId, minimumVisiblePixelCount]) => { const element = document.querySelector(`[data-test="${testId}"]`); if (!(element instanceof HTMLVideoElement) || element.readyState < 2) { return false; } const width = Math.max(1, element.videoWidth); const height = Math.max(1, element.videoHeight); const canvas = document.createElement("canvas"); canvas.width = width; canvas.height = height; const context = canvas.getContext("2d"); if (!context) { return false; } context.drawImage(element, 0, 0, width, height); const pixels = context.getImageData(0, 0, width, height).data; let nonBlackPixelCount = 0; for (let index = 0; index < pixels.length; index += 4) { if (pixels[index] >= {{MinimumVisibleChannelValue}} || pixels[index + 1] >= {{MinimumVisibleChannelValue}} || pixels[index + 2] >= {{MinimumVisibleChannelValue}}) { nonBlackPixelCount += 1; if (nonBlackPixelCount >= minimumVisiblePixelCount) { return true; } } } return false; }""";
        public static string HasAudioOnlyRequestScript =>
            $$"""([audioId]) => window["{{HarnessGlobal}}"].getRequestLog().some(request => request.hasVideo === false && request.hasAudio === true && request.resolvedAudioDeviceId === audioId)""";
        public static string HasAudioVideoRequestScript =>
            $$"""([videoId, audioId]) => window["{{HarnessGlobal}}"].getRequestLog().some(request => request.hasVideo === true && request.hasAudio === true && request.resolvedVideoDeviceId === videoId && request.resolvedAudioDeviceId === audioId)""";
        public static string HasVideoOnlyRequestScript =>
            $$"""([videoId]) => window["{{HarnessGlobal}}"].getRequestLog().some(request => request.hasVideo === true && request.hasAudio === false && request.resolvedVideoDeviceId === videoId)""";
        public static string GetSavedRecordingStateScript =>
            $$"""() => window["{{RecordingFileHarnessGlobal}}"].getSavedRecordingState()""";
        public static string GetSavedRecordingsStateScript =>
            $$"""() => window["{{RecordingFileHarnessGlobal}}"].getSavedRecordingsState()""";
        public static string AnalyzeSavedRecordingScript =>
            $$"""() => window["{{RecordingFileHarnessGlobal}}"].analyzeSavedRecording()""";
        public static Regex BlankTextRegex { get; } = new(@"^\s*$", RegexOptions.Compiled);
        public static string ResetSavedRecordingScript =>
            $$"""() => window["{{RecordingFileHarnessGlobal}}"].reset()""";
        public static string EnableSyntheticRecordingEncoderScript =>
            $$"""() => window["{{RecordingFileHarnessGlobal}}"].enableSyntheticMediaRecorder()""";
        public static string SavedRecordingReadyScript =>
            $$"""() => Boolean(window["{{RecordingFileHarnessGlobal}}"].getSavedRecordingState()?.hasBlob && (window["{{RecordingFileHarnessGlobal}}"].getSavedRecordingState()?.sizeBytes ?? 0) > 0)""";
        public static string SavedRecordingCountReadyScript =>
            $$"""count => (window["{{RecordingFileHarnessGlobal}}"].getSavedRecordingState()?.savedRecordingCount ?? 0) >= count""";
    }
}
