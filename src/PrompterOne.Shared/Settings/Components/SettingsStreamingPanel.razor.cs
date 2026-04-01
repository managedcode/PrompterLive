using Microsoft.AspNetCore.Components;
using PrompterOne.Core.Models.Media;
using PrompterOne.Core.Models.Streaming;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Core.Services.Streaming;
using PrompterOne.Shared.Settings.Components;
using PrompterOne.Shared.Settings.Models;

namespace PrompterOne.Shared.Components.Settings;

public partial class SettingsStreamingPanel
{
    private static readonly IReadOnlyList<SettingsSelectOption> OutputModeOptions =
    [
        new(VirtualCameraOutputModeValue, SettingsStreamingText.VirtualCameraOutputModeLabel),
        new(NdiOutputModeValue, SettingsStreamingText.NdiOutputModeLabel),
        new(DirectRtmpOutputModeValue, SettingsStreamingText.DirectRtmpOutputModeLabel),
        new(LocalRecordingOutputModeValue, SettingsStreamingText.LocalRecordingOutputModeLabel),
    ];

    private static readonly IReadOnlyList<SettingsSelectOption> OutputResolutionOptions =
    [
        new(nameof(StreamingResolutionPreset.FullHd1080p30), SettingsStreamingText.FullHd1080p30Label),
        new(nameof(StreamingResolutionPreset.FullHd1080p60), SettingsStreamingText.FullHd1080p60Label),
        new(nameof(StreamingResolutionPreset.Hd720p30), SettingsStreamingText.Hd720p30Label),
        new(nameof(StreamingResolutionPreset.UltraHd2160p30), SettingsStreamingText.UltraHd2160p30Label),
    ];

    private const string DirectRtmpOutputModeValue = "direct-rtmp";
    private const string LocalRecordingOutputModeValue = "local-recording";
    private const string NdiOutputModeValue = "ndi-output";
    private const string OnCssClass = "on";
    private const string SelectedStatusClass = "set-dest-ok";
    private const string VirtualCameraOutputModeValue = "virtual-camera";

    private bool _isAddDestinationMenuOpen;

    [Parameter, EditorRequired] public StreamStudioSettings Settings { get; set; } = default!;
    [Parameter] public IReadOnlyList<SceneCameraSource> Sources { get; set; } = [];
    [Parameter] public string SelectedOutputModeValue { get; set; } = VirtualCameraOutputModeValue;
    [Parameter] public Func<string, bool> IsCardOpen { get; set; } = static _ => false;
    [Parameter] public EventCallback<StreamingPlatformKind> AddExternalDestination { get; set; }
    [Parameter] public EventCallback<ChangeEventArgs> BitrateChanged { get; set; }
    [Parameter] public EventCallback<ChangeEventArgs> OutputModeChanged { get; set; }
    [Parameter] public EventCallback<ChangeEventArgs> OutputResolutionChanged { get; set; }
    [Parameter] public EventCallback<string> RemoveExternalDestination { get; set; }
    [Parameter] public EventCallback<string> ToggleCard { get; set; }
    [Parameter] public EventCallback<string> ToggleExternalDestination { get; set; }
    [Parameter] public EventCallback ToggleIncludeCamera { get; set; }
    [Parameter] public EventCallback ToggleNdi { get; set; }
    [Parameter] public EventCallback ToggleObs { get; set; }
    [Parameter] public EventCallback ToggleRecording { get; set; }
    [Parameter] public EventCallback<(string TargetId, string SourceId)> ToggleDestinationSource { get; set; }
    [Parameter] public EventCallback ToggleTextOverlay { get; set; }
    [Parameter] public EventCallback<(string DestinationId, string FieldId, string Value)> UpdateExternalDestinationField { get; set; }

    private string AddDestinationMenuCssClass =>
        _isAddDestinationMenuOpen ? "set-add-source-menu open" : "set-add-source-menu";

    private IReadOnlyList<StreamingProfile> ExternalDestinations => Settings.ExternalDestinations ?? Array.Empty<StreamingProfile>();

    private static IReadOnlyList<SettingsStreamingLocalTargetDefinition> LocalTargetCards => SettingsStreamingLocalTargetCatalog.All;

    private async Task AddDestinationAndOpenCardAsync(StreamingPlatformKind kind)
    {
        var existingIds = ExternalDestinations.Select(destination => destination.Id);
        var nextCardId = SettingsStreamingCardIds.ExternalDestination(
            StreamingPlatformCatalog.CreateProfile(kind, existingIds).Id);

        _isAddDestinationMenuOpen = false;
        await AddExternalDestination.InvokeAsync(kind);

        if (!IsCardOpen(nextCardId))
        {
            await ToggleCard.InvokeAsync(nextCardId);
        }
    }

    private IReadOnlyList<string> GetSelectedSourceIds(string targetId) =>
        GoLiveDestinationRouting.GetSelectedSourceIds(Settings, targetId, Sources);

    private bool BuildLocalTargetIsReady(string targetId)
    {
        if (!IsLocalTargetEnabled(targetId))
        {
            return false;
        }

        return GetSelectedSourceIds(targetId).Count > 0;
    }

    private string BuildLocalSummary(string targetId)
    {
        var selectedSources = GetSelectedSourceIds(targetId);
        return selectedSources.Count == 0
            ? SettingsStreamingText.DestinationNoSourceSummary
            : $"{selectedSources.Count}{SettingsStreamingText.LocalDestinationSummarySuffix}";
    }

    private string BuildLocalTargetStatusClass(string targetId) =>
        BuildLocalTargetIsReady(targetId) ? SelectedStatusClass : string.Empty;

    private string BuildLocalTargetStatusLabel(string targetId)
    {
        if (!IsLocalTargetEnabled(targetId))
        {
            return SettingsStreamingText.DestinationDisabledStatusLabel;
        }

        return BuildLocalTargetIsReady(targetId)
            ? SettingsStreamingText.DestinationReadyStatusLabel
            : SettingsStreamingText.DestinationNeedsSetupStatusLabel;
    }

    private bool IsLocalTargetEnabled(string targetId) => targetId switch
    {
        GoLiveTargetCatalog.TargetIds.Obs => Settings.ObsVirtualCameraEnabled,
        GoLiveTargetCatalog.TargetIds.Ndi => Settings.NdiOutputEnabled,
        GoLiveTargetCatalog.TargetIds.Recording => Settings.LocalRecordingEnabled,
        _ => false
    };

    private async Task ToggleLocalTargetAsync(string targetId)
    {
        _isAddDestinationMenuOpen = false;

        if (string.Equals(targetId, GoLiveTargetCatalog.TargetIds.Obs, StringComparison.Ordinal))
        {
            await ToggleObs.InvokeAsync();
            return;
        }

        if (string.Equals(targetId, GoLiveTargetCatalog.TargetIds.Ndi, StringComparison.Ordinal))
        {
            await ToggleNdi.InvokeAsync();
            return;
        }

        await ToggleRecording.InvokeAsync();
    }

    private void ToggleDestinationMenu() => _isAddDestinationMenuOpen = !_isAddDestinationMenuOpen;

    private async Task ToggleStreamingCardAsync(string cardId)
    {
        _isAddDestinationMenuOpen = false;
        await ToggleCard.InvokeAsync(cardId);
    }
}
