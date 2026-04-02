namespace PrompterOne.Shared.Services;

public static class GoLiveOutputInteropMethodNames
{
    private const string NamespacePrefix = "PrompterOneGoLiveOutput";

    public const string GetSessionState = NamespacePrefix + ".getSessionState";
    public const string StartLocalRecording = NamespacePrefix + ".startLocalRecording";
    public const string StartLiveKitSession = NamespacePrefix + ".startLiveKitSession";
    public const string StartObsBrowserOutput = NamespacePrefix + ".startObsBrowserOutput";
    public const string StartVdoNinjaSession = NamespacePrefix + ".startVdoNinjaSession";
    public const string StopLocalRecording = NamespacePrefix + ".stopLocalRecording";
    public const string StopLiveKitSession = NamespacePrefix + ".stopLiveKitSession";
    public const string StopObsBrowserOutput = NamespacePrefix + ".stopObsBrowserOutput";
    public const string StopVdoNinjaSession = NamespacePrefix + ".stopVdoNinjaSession";
    public const string UpdateSessionDevices = NamespacePrefix + ".updateSessionDevices";
}
