using Microsoft.AspNetCore.Components;
using PrompterOne.Core.Models.Media;

namespace PrompterOne.Shared.Pages;

public partial class SettingsPage
{
    private const string BluetoothConnectionLabel = "Bluetooth";
    private const string CompactSampleRateLabel = "44.1 kHz";
    private const string MonoChannelLabel = "Mono";
    private const string StereoChannelLabel = "Stereo";
    private const string WideSampleRateLabel = "48 kHz";

    private async Task SelectPrimaryMicrophoneAsync(MediaDeviceInfo microphone)
    {
        if (!IsMicrophoneEnabled(microphone))
        {
            return;
        }

        var current = GetAudioInput(microphone);
        MediaSceneService.SetPrimaryMicrophone(microphone.DeviceId, microphone.Label);
        MediaSceneService.UpsertAudioInput(current with { IsMuted = false });

        _primaryMicrophoneId = microphone.DeviceId;
        await PersistSceneAsync();

        _studioSettings = _studioSettings with
        {
            Microphone = _studioSettings.Microphone with
            {
                DefaultMicrophoneId = microphone.DeviceId,
                InputLevelPercent = ClampPercent((int)Math.Round(current.Gain * 100d))
            }
        };

        await PersistStudioSettingsAsync();
        await NormalizeStudioSettingsAsync();
    }

    private async Task ToggleMicrophoneSelectionAsync(MediaDeviceInfo microphone)
    {
        var current = GetAudioInput(microphone);
        var isEnabled = IsMicrophoneEnabled(microphone);
        if (!isEnabled)
        {
            MediaSceneService.UpsertAudioInput(current with { IsMuted = false });
            MediaSceneService.SetPrimaryMicrophone(microphone.DeviceId, microphone.Label);
            _primaryMicrophoneId = microphone.DeviceId;
        }
        else
        {
            MediaSceneService.UpsertAudioInput(current with { IsMuted = true });

            if (string.Equals(_primaryMicrophoneId, microphone.DeviceId, StringComparison.Ordinal))
            {
                var nextPrimary = _microphoneDevices
                    .FirstOrDefault(device => !string.Equals(device.DeviceId, microphone.DeviceId, StringComparison.Ordinal)
                        && IsMicrophoneEnabled(device));
                MediaSceneService.SetPrimaryMicrophone(nextPrimary?.DeviceId, nextPrimary?.Label);
                _primaryMicrophoneId = nextPrimary?.DeviceId;
            }
        }

        await PersistSceneAsync();
        await NormalizeStudioSettingsAsync();
    }

    private async Task ToggleMuteAllMicrophonesAsync()
    {
        if (_microphoneDevices.Count == 0)
        {
            return;
        }

        var muteAll = !AllMicrophonesMutedOutsideGoLive;
        foreach (var microphone in _microphoneDevices)
        {
            var current = GetAudioInput(microphone);
            MediaSceneService.UpsertAudioInput(current with { IsMuted = muteAll });
        }

        if (muteAll)
        {
            MediaSceneService.SetPrimaryMicrophone(null);
            _primaryMicrophoneId = null;
        }
        else if (ResolveSelectedMicrophoneCandidate() is { } selectedMicrophone)
        {
            MediaSceneService.SetPrimaryMicrophone(selectedMicrophone.DeviceId, selectedMicrophone.Label);
            _primaryMicrophoneId = selectedMicrophone.DeviceId;
        }

        await PersistSceneAsync();
        await NormalizeStudioSettingsAsync();
    }

    private async Task UpdateAudioDelayAsync(MediaDeviceInfo microphone, ChangeEventArgs args)
    {
        if (!int.TryParse(args.Value?.ToString(), out var delay))
        {
            return;
        }

        var current = GetAudioInput(microphone);
        MediaSceneService.UpsertAudioInput(current with { DelayMs = Math.Clamp(delay, 0, 5000) });
        await PersistSceneAsync();
    }

    private async Task UpdateAudioGainAsync(MediaDeviceInfo microphone, ChangeEventArgs args)
    {
        if (!double.TryParse(args.Value?.ToString(), out var gainPercent))
        {
            return;
        }

        var current = GetAudioInput(microphone);
        var clampedGainPercent = Math.Clamp(gainPercent, 0d, 200d);
        MediaSceneService.UpsertAudioInput(current with { Gain = clampedGainPercent / 100d });
        await PersistSceneAsync();

        if (string.Equals(_primaryMicrophoneId, microphone.DeviceId, StringComparison.Ordinal))
        {
            _studioSettings = _studioSettings with
            {
                Microphone = _studioSettings.Microphone with
                {
                    InputLevelPercent = ClampPercent((int)Math.Round(clampedGainPercent))
                }
            };
            await PersistStudioSettingsAsync();
        }
    }

    private AudioInputState GetAudioInput(MediaDeviceInfo microphone) =>
        MediaSceneService.State.AudioBus.Inputs
            .FirstOrDefault(input => string.Equals(input.DeviceId, microphone.DeviceId, StringComparison.Ordinal))
        ?? new AudioInputState(microphone.DeviceId, microphone.Label);

    private async Task UpdatePrimaryMicLevelAsync(ChangeEventArgs args)
    {
        if (!double.TryParse(args.Value?.ToString(), out var percent))
        {
            return;
        }

        var clampedPercent = ClampPercent((int)Math.Round(percent));
        _studioSettings = _studioSettings with
        {
            Microphone = _studioSettings.Microphone with { InputLevelPercent = clampedPercent }
        };

        var primaryMicrophone = ResolveSelectedMicrophone();
        if (primaryMicrophone is not null)
        {
            var current = GetAudioInput(primaryMicrophone);
            MediaSceneService.UpsertAudioInput(current with { Gain = clampedPercent / 100d });
            await PersistSceneAsync();
        }

        await PersistStudioSettingsAsync();
    }

    private async Task ToggleNoiseSuppressionAsync()
    {
        _studioSettings = _studioSettings with
        {
            Microphone = _studioSettings.Microphone with
            {
                NoiseSuppression = !_studioSettings.Microphone.NoiseSuppression
            }
        };

        await PersistStudioSettingsAsync();
    }

    private async Task ToggleEchoCancellationAsync()
    {
        _studioSettings = _studioSettings with
        {
            Microphone = _studioSettings.Microphone with
            {
                EchoCancellation = !_studioSettings.Microphone.EchoCancellation
            }
        };

        await PersistStudioSettingsAsync();
    }

    private static string BuildLevelFillStyle(int percent) => $"width:{ClampDisplayPercent(percent)}%;";

    private static string BuildLevelThumbStyle(int percent) => $"left:{ClampDisplayPercent(percent)}%;";

    private static string BuildMicrophoneMeta(MediaDeviceInfo microphone) =>
        string.Join(
            " · ",
            ResolveMicrophoneConnectionLabel(microphone),
            ResolveMicrophoneSampleRateLabel(microphone),
            ResolveMicrophoneChannelLabel(microphone));

    private static int ClampDisplayPercent(int percent) => Math.Clamp(percent, 0, 100);

    private static int ConvertGainToPercent(double gain) =>
        Math.Clamp((int)Math.Round(gain * 100d), 0, 200);

    private bool IsMicrophoneEnabled(MediaDeviceInfo microphone)
    {
        var inputState = MediaSceneService.State.AudioBus.Inputs
            .FirstOrDefault(input => string.Equals(input.DeviceId, microphone.DeviceId, StringComparison.Ordinal));
        if (inputState is not null)
        {
            return !inputState.IsMuted;
        }

        return string.Equals(_primaryMicrophoneId, microphone.DeviceId, StringComparison.Ordinal);
    }

    private static string ResolveMicrophoneChannelLabel(MediaDeviceInfo microphone) =>
        microphone.Label.Contains("Blue Yeti", StringComparison.OrdinalIgnoreCase)
            ? StereoChannelLabel
            : MonoChannelLabel;

    private static string ResolveMicrophoneConnectionLabel(MediaDeviceInfo microphone)
    {
        if (microphone.Label.Contains(BluetoothConnectionLabel, StringComparison.OrdinalIgnoreCase)
            || microphone.Label.Contains("AirPods", StringComparison.OrdinalIgnoreCase))
        {
            return BluetoothConnectionLabel;
        }

        if (microphone.Label.Contains(BuiltInConnectionLabel, StringComparison.OrdinalIgnoreCase)
            || microphone.Label.Contains("MacBook", StringComparison.OrdinalIgnoreCase))
        {
            return BuiltInConnectionLabel;
        }

        return UsbConnectionLabel;
    }

    private static string ResolveMicrophoneSampleRateLabel(MediaDeviceInfo microphone) =>
        microphone.Label.Contains(BuiltInConnectionLabel, StringComparison.OrdinalIgnoreCase)
        || microphone.Label.Contains("AirPods", StringComparison.OrdinalIgnoreCase)
            ? CompactSampleRateLabel
            : WideSampleRateLabel;
}
