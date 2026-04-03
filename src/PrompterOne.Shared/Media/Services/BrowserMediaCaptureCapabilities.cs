namespace PrompterOne.Shared.Services;

public sealed record BrowserMediaCaptureCapabilities(bool SupportsConcurrentLocalCameraCaptures)
{
    public static BrowserMediaCaptureCapabilities Default { get; } = new(true);
}
