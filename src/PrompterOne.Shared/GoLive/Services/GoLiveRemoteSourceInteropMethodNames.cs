namespace PrompterOne.Shared.Services;

public static class GoLiveRemoteSourceInteropMethodNames
{
    private const string NamespacePrefix = "PrompterOneGoLiveRemoteSources";

    public const string GetSessionState = NamespacePrefix + ".getSessionState";
    public const string StopSession = NamespacePrefix + ".stopSession";
    public const string SyncConnections = NamespacePrefix + ".syncConnections";
}
