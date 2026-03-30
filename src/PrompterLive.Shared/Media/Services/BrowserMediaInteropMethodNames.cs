namespace PrompterLive.Shared.Services;

internal static class BrowserMediaInteropMethodNames
{
    private const string NamespacePrefix = "BrowserMediaInterop";

    public const string AttachCamera = NamespacePrefix + ".attachCamera";
    public const string DetachCamera = NamespacePrefix + ".detachCamera";
    public const string ListDevices = NamespacePrefix + ".listDevices";
    public const string QueryPermissions = NamespacePrefix + ".queryPermissions";
    public const string RequestPermissions = NamespacePrefix + ".requestPermissions";
    public const string StartMicrophoneLevelMonitor = NamespacePrefix + ".startMicrophoneLevelMonitor";
    public const string StopMicrophoneLevelMonitor = NamespacePrefix + ".stopMicrophoneLevelMonitor";
}
