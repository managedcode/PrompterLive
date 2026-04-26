using PrompterOne.Shared.Contracts;

namespace PrompterOne.Shared.Services;

internal static class BrowserMediaInteropMethodNames
{
    private const string NamespacePrefix = AppMediaRuntime.BrowserMedia.InteropNamespace;

    public const string AttachCamera = NamespacePrefix + ".attachCamera";
    public const string DetachAllCameras = NamespacePrefix + ".detachAllCameras";
    public const string DetachCamera = NamespacePrefix + ".detachCamera";
    public const string GetCaptureCapabilities = NamespacePrefix + ".getCaptureCapabilities";
    public const string ListDevices = NamespacePrefix + ".listDevices";
    public const string QueryPermissions = NamespacePrefix + ".queryPermissions";
    public const string RequestPermissions = NamespacePrefix + ".requestPermissions";
    public const string StartReaderRecording = NamespacePrefix + ".startReaderRecording";
    public const string StartMicrophoneLevelMonitor = NamespacePrefix + ".startMicrophoneLevelMonitor";
    public const string StopReaderRecording = NamespacePrefix + ".stopReaderRecording";
    public const string StopMicrophoneLevelMonitor = NamespacePrefix + ".stopMicrophoneLevelMonitor";
}
