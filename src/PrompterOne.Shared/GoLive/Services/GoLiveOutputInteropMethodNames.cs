using PrompterOne.Shared.Contracts;

namespace PrompterOne.Shared.Services;

public static class GoLiveOutputInteropMethodNames
{
    private const string NamespacePrefix = AppMediaRuntime.GoLive.OutputNamespace;

    public const string GetSessionState = NamespacePrefix + ".getSessionState";
    public const string StartLocalRecording = NamespacePrefix + ".startLocalRecording";
    public const string StartLiveKitSession = NamespacePrefix + ".startLiveKitSession";
    public const string StartVdoNinjaSession = NamespacePrefix + ".startVdoNinjaSession";
    public const string RotateLocalRecordingTake = NamespacePrefix + ".rotateLocalRecordingTake";
    public const string StopLocalRecording = NamespacePrefix + ".stopLocalRecording";
    public const string StopLiveKitSession = NamespacePrefix + ".stopLiveKitSession";
    public const string StopVdoNinjaSession = NamespacePrefix + ".stopVdoNinjaSession";
    public const string UpdateSessionDevices = NamespacePrefix + ".updateSessionDevices";
}
