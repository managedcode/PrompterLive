using Microsoft.JSInterop;

namespace PrompterOne.Shared.Services;

public sealed class ReaderRecordingInterop(IJSRuntime jsRuntime)
{
    private readonly IJSRuntime _jsRuntime = jsRuntime;

    public Task<ReaderRecordingSnapshot> StartAsync(ReaderRecordingRequest request)
    {
        return _jsRuntime.InvokeAsync<ReaderRecordingSnapshot>(
            BrowserMediaInteropMethodNames.StartReaderRecording,
            request).AsTask();
    }

    public Task<ReaderRecordingSnapshot> StopAsync()
    {
        return _jsRuntime.InvokeAsync<ReaderRecordingSnapshot>(
            BrowserMediaInteropMethodNames.StopReaderRecording).AsTask();
    }
}

public sealed record ReaderRecordingRequest(
    string Mode,
    string? CameraDeviceId,
    string? MicrophoneDeviceId,
    string FileStem,
    bool EchoCancellation,
    bool NoiseSuppression,
    bool AutoGainControl,
    bool VoiceIsolation,
    int? ChannelCount,
    int? SampleRate,
    int? SampleSize);

public sealed record ReaderRecordingSnapshot(
    bool Active,
    string FileName,
    string MimeType,
    string Mode,
    long SizeBytes);
