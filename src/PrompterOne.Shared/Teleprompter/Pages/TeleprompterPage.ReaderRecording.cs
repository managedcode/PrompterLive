using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PrompterOne.Core.Models.Media;
using PrompterOne.Shared.Localization;
using PrompterOne.Shared.Services;

namespace PrompterOne.Shared.Pages;

public partial class TeleprompterPage
{
    private const string ReaderRecordingAudioOnlyMode = "audio";
    private const string ReaderRecordingDefaultFileStem = "reader-rehearsal";
    private const string ReaderRecordingLevelMonitorElementId = "teleprompter-reader-recording-level";
    private const string ReaderRecordingOperation = "Teleprompter reader recording";
    private const string ReaderRecordingVideoAndAudioMode = "video-audio";

    private DotNetObjectReference<MicrophoneLevelObserver>? _readerRecordingLevelObserverReference;
    private bool _isReaderRecording;
    private int _readerRecordingAudioLevelPercent;
    private string _readerRecordingCameraId = string.Empty;
    private string _readerRecordingMicrophoneId = string.Empty;
    private string _readerRecordingMode = ReaderRecordingVideoAndAudioMode;
    private string _readerRecordingStatus = string.Empty;

    private string ReaderRecordingStartLabel => Text(UiTextKey.CommonRecord);

    private string ReaderRecordingStopLabel => Text(UiTextKey.CommonStop);

    private string ReaderRecordingToggleTooltip => _isReaderRecording
        ? Text(UiTextKey.TeleprompterStopRecordingTooltip)
        : Text(UiTextKey.TeleprompterStartRecordingTooltip);

    private string ReaderRecordingLevelLabel => Text(UiTextKey.TeleprompterRecordingLevelLabel);

    private string ReaderRecordingStatusLabel =>
        string.IsNullOrWhiteSpace(_readerRecordingStatus)
            ? Text(UiTextKey.TeleprompterRecordingReadyStatus)
            : _readerRecordingStatus;

    private void UpdateReaderRecordingDeviceDefaults(IReadOnlyList<MediaDeviceInfo> devices)
    {
        _readerRecordingCameras = devices
            .Where(device => device.Kind == MediaDeviceKind.Camera)
            .ToList();
        _readerRecordingMicrophones = devices
            .Where(device => device.Kind == MediaDeviceKind.Microphone)
            .ToList();

        var preferredCameraId = _studioSettings.Camera.DefaultCameraId;
        var preferredMicrophoneId = MediaSceneService.State.PrimaryMicrophoneId ?? _studioSettings.Microphone.DefaultMicrophoneId;

        _readerRecordingCameraId = ResolveRecordingDeviceId(_readerRecordingCameras, _readerRecordingCameraId, preferredCameraId);
        _readerRecordingMicrophoneId = ResolveRecordingDeviceId(_readerRecordingMicrophones, _readerRecordingMicrophoneId, preferredMicrophoneId);
    }

    private static string ResolveRecordingDeviceId(
        IReadOnlyList<MediaDeviceInfo> devices,
        string currentDeviceId,
        string? preferredDeviceId)
    {
        if (!string.IsNullOrWhiteSpace(currentDeviceId)
            && devices.Any(device => string.Equals(device.DeviceId, currentDeviceId, StringComparison.Ordinal)))
        {
            return currentDeviceId;
        }

        if (!string.IsNullOrWhiteSpace(preferredDeviceId)
            && devices.Any(device => string.Equals(device.DeviceId, preferredDeviceId, StringComparison.Ordinal)))
        {
            return preferredDeviceId;
        }

        return devices.FirstOrDefault(device => device.IsDefault)?.DeviceId
            ?? (devices.Count > 0 ? devices[0].DeviceId : null)
            ?? string.Empty;
    }

    private Task HandleReaderRecordingModeChangeAsync(ChangeEventArgs args)
    {
        var mode = args.Value?.ToString();
        _readerRecordingMode = string.Equals(mode, ReaderRecordingAudioOnlyMode, StringComparison.Ordinal)
            ? ReaderRecordingAudioOnlyMode
            : ReaderRecordingVideoAndAudioMode;
        return Task.CompletedTask;
    }

    private Task HandleReaderRecordingCameraChangeAsync(ChangeEventArgs args)
    {
        _readerRecordingCameraId = args.Value?.ToString() ?? string.Empty;
        return Task.CompletedTask;
    }

    private Task HandleReaderRecordingMicrophoneChangeAsync(ChangeEventArgs args)
    {
        _readerRecordingMicrophoneId = args.Value?.ToString() ?? string.Empty;
        return Task.CompletedTask;
    }

    private async Task ToggleReaderRecordingAsync()
    {
        if (_isReaderRecording)
        {
            await StopReaderRecordingAsync(updateUi: true);
            return;
        }

        await StartReaderRecordingAsync();
    }

    private async Task StartReaderRecordingAsync()
    {
        if (string.IsNullOrWhiteSpace(_readerRecordingMicrophoneId))
        {
            _readerRecordingStatus = Text(UiTextKey.TeleprompterRecordingNoMicrophoneStatus);
            return;
        }

        if (string.Equals(_readerRecordingMode, ReaderRecordingVideoAndAudioMode, StringComparison.Ordinal)
            && string.IsNullOrWhiteSpace(_readerRecordingCameraId))
        {
            _readerRecordingStatus = Text(UiTextKey.TeleprompterRecordingNoCameraStatus);
            return;
        }

        await Diagnostics.RunAsync(
            ReaderRecordingOperation,
            Text(UiTextKey.TeleprompterStartRecordingMessage),
            async () =>
            {
                var request = new ReaderRecordingRequest(
                    Mode: _readerRecordingMode,
                    CameraDeviceId: _readerRecordingCameraId,
                    MicrophoneDeviceId: _readerRecordingMicrophoneId,
                    FileStem: BuildReaderRecordingFileStem(),
                    EchoCancellation: _studioSettings.Microphone.EchoCancellation,
                    NoiseSuppression: _studioSettings.Microphone.NoiseSuppression,
                    AutoGainControl: _studioSettings.Microphone.AutoGainControl,
                    VoiceIsolation: _studioSettings.Microphone.VoiceIsolation,
                    ChannelCount: _studioSettings.Microphone.ChannelCount,
                    SampleRate: _studioSettings.Microphone.SampleRate,
                    SampleSize: _studioSettings.Microphone.SampleSize);

                var snapshot = await ReaderRecordingInterop.StartAsync(request);
                _isReaderRecording = snapshot.Active;
                _readerRecordingStatus = Text(UiTextKey.TeleprompterRecordingActiveStatus);
                await StartReaderRecordingLevelMonitorAsync();
            });
    }

    private async Task StopReaderRecordingAsync(bool updateUi)
    {
        if (!_isReaderRecording)
        {
            await StopReaderRecordingLevelMonitorAsync();
            return;
        }

        try
        {
            var snapshot = await ReaderRecordingInterop.StopAsync();
            _isReaderRecording = false;
            _readerRecordingAudioLevelPercent = 0;
            _readerRecordingStatus = string.IsNullOrWhiteSpace(snapshot.FileName)
                ? Text(UiTextKey.TeleprompterRecordingReadyStatus)
                : string.Format(
                    CultureInfo.CurrentCulture,
                    Text(UiTextKey.TeleprompterRecordingSavedStatusFormat),
                    snapshot.FileName);
        }
        finally
        {
            await StopReaderRecordingLevelMonitorAsync();
        }

        if (updateUi)
        {
            StateHasChanged();
        }
    }

    private async Task StartReaderRecordingLevelMonitorAsync()
    {
        await StopReaderRecordingLevelMonitorAsync();

        _readerRecordingLevelObserverReference = DotNetObjectReference.Create(
            new MicrophoneLevelObserver(level =>
            {
                _readerRecordingAudioLevelPercent = level;
                return InvokeAsync(StateHasChanged);
            }));

        await MicrophoneLevelInterop.StartAsync(
            ReaderRecordingLevelMonitorElementId,
            _readerRecordingMicrophoneId,
            _readerRecordingLevelObserverReference);
    }

    private async Task StopReaderRecordingLevelMonitorAsync()
    {
        if (_readerRecordingLevelObserverReference is null)
        {
            return;
        }

        await MicrophoneLevelInterop.StopAsync(ReaderRecordingLevelMonitorElementId);
        _readerRecordingLevelObserverReference?.Dispose();
        _readerRecordingLevelObserverReference = null;
    }

    private string BuildReaderRecordingFileStem()
    {
        var source = string.IsNullOrWhiteSpace(_screenTitle)
            ? ReaderRecordingDefaultFileStem
            : _screenTitle;
        var safe = new string(source
            .Select(static character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray())
            .Trim('-')
            .ToLowerInvariant();

        return string.IsNullOrWhiteSpace(safe) ? ReaderRecordingDefaultFileStem : safe;
    }
}
