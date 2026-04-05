using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using PrompterOne.Core.Models.Media;
using PrompterOne.Core.Models.Streaming;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Core.Services.Streaming;
using PrompterOne.Shared.Localization;
using PrompterOne.Shared.Settings.Components;
using PrompterOne.Shared.Settings.Models;

namespace PrompterOne.Shared.Components.Settings;

public partial class SettingsStreamingPanel
{
    private const string OnCssClass = "on";
    private const string SelectedStatusClass = "set-dest-ok";

    private bool _isAddDistributionTargetMenuOpen;
    private bool _isAddTransportConnectionMenuOpen;

    [Inject] private IStringLocalizer<SharedResource> Localizer { get; set; } = null!;

    [Parameter, EditorRequired] public StreamStudioSettings Settings { get; set; } = default!;
    [Parameter] public IReadOnlyList<SceneCameraSource> Sources { get; set; } = [];
    [Parameter] public Func<string, bool> IsCardOpen { get; set; } = static _ => false;
    [Parameter] public EventCallback<StreamingPlatformKind> AddDistributionTarget { get; set; }
    [Parameter] public EventCallback<StreamingPlatformKind> AddTransportConnection { get; set; }
    [Parameter] public EventCallback<ChangeEventArgs> BitrateChanged { get; set; }
    [Parameter] public EventCallback<ChangeEventArgs> OutputResolutionChanged { get; set; }
    [Parameter] public EventCallback<string> RemoveDistributionTarget { get; set; }
    [Parameter] public EventCallback<string> RemoveTransportConnection { get; set; }
    [Parameter] public EventCallback<string> ToggleCard { get; set; }
    [Parameter] public EventCallback<string> ToggleDistributionTarget { get; set; }
    [Parameter] public EventCallback<(string TargetId, string ConnectionId)> ToggleDistributionTargetTransport { get; set; }
    [Parameter] public EventCallback ToggleIncludeCamera { get; set; }
    [Parameter] public EventCallback ToggleRecording { get; set; }
    [Parameter] public EventCallback<(string TargetId, string SourceId)> ToggleTransportConnectionSource { get; set; }
    [Parameter] public EventCallback<string> ToggleTransportConnection { get; set; }
    [Parameter] public EventCallback ToggleTextOverlay { get; set; }
    [Parameter] public EventCallback<(string TargetId, string FieldId, string Value)> UpdateDistributionTargetField { get; set; }
    [Parameter] public EventCallback<(string ConnectionId, string FieldId, string Value)> UpdateTransportConnectionField { get; set; }
    [Parameter] public EventCallback<(string ConnectionId, string Value)> UpdateTransportConnectionRole { get; set; }

    private string AddDistributionTargetMenuCssClass =>
        _isAddDistributionTargetMenuOpen ? "set-add-source-menu open" : "set-add-source-menu";

    private string AddTransportConnectionMenuCssClass =>
        _isAddTransportConnectionMenuOpen ? "set-add-source-menu open" : "set-add-source-menu";

    private IReadOnlyList<DistributionTargetProfile> DistributionTargets =>
        Settings.DistributionTargets ?? Array.Empty<DistributionTargetProfile>();

    private static IReadOnlyList<SettingsStreamingLocalTargetDefinition> LocalTargetCards =>
        SettingsStreamingLocalTargetCatalog.All;

    private ProgramCaptureProfile ProgramCapture => Settings.ProgramCaptureSettings;

    private RecordingProfile Recording => Settings.RecordingSettings;

    private IReadOnlyList<TransportConnectionProfile> TransportConnections =>
        Settings.TransportConnections ?? Array.Empty<TransportConnectionProfile>();

    private IReadOnlyList<SettingsSelectOption> OutputResolutionOptions =>
    [
        new(nameof(StreamingResolutionPreset.FullHd1080p30), Text(SettingsStreamingText.FullHd1080p30Label)),
        new(nameof(StreamingResolutionPreset.FullHd1080p60), Text(SettingsStreamingText.FullHd1080p60Label)),
        new(nameof(StreamingResolutionPreset.Hd720p30), Text(SettingsStreamingText.Hd720p30Label)),
        new(nameof(StreamingResolutionPreset.UltraHd2160p30), Text(SettingsStreamingText.UltraHd2160p30Label)),
    ];

    private async Task AddDistributionTargetAndOpenCardAsync(StreamingPlatformKind kind)
    {
        var nextCardId = SettingsStreamingCardIds.DistributionTarget(
            StreamingPlatformCatalog.CreateDistributionTarget(kind, DistributionTargets.Select(target => target.Id)).Id);

        _isAddDistributionTargetMenuOpen = false;
        await AddDistributionTarget.InvokeAsync(kind);

        if (!IsCardOpen(nextCardId))
        {
            await ToggleCard.InvokeAsync(nextCardId);
        }
    }

    private async Task AddTransportConnectionAndOpenCardAsync(StreamingPlatformKind kind)
    {
        var nextCardId = SettingsStreamingCardIds.TransportConnection(
            StreamingPlatformCatalog.CreateTransportConnection(kind, TransportConnections.Select(connection => connection.Id)).Id);

        _isAddTransportConnectionMenuOpen = false;
        await AddTransportConnection.InvokeAsync(kind);

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
            ? Text(SettingsStreamingText.DestinationNoSourceSummary)
            : Format(SettingsStreamingText.LocalDestinationSummaryFormat, selectedSources.Count);
    }

    private string BuildLocalTargetStatusClass(string targetId) =>
        BuildLocalTargetIsReady(targetId) ? SelectedStatusClass : string.Empty;

    private string BuildLocalTargetStatusLabel(string targetId)
    {
        if (!IsLocalTargetEnabled(targetId))
        {
            return Text(SettingsStreamingText.DestinationDisabledStatusLabel);
        }

        return BuildLocalTargetIsReady(targetId)
            ? Text(SettingsStreamingText.DestinationReadyStatusLabel)
            : Text(SettingsStreamingText.DestinationNeedsSetupStatusLabel);
    }

    private bool IsLocalTargetEnabled(string targetId) => targetId switch
    {
        GoLiveTargetCatalog.TargetIds.Recording => Recording.IsEnabled,
        _ => false
    };

    private async Task ToggleLocalTargetAsync(string targetId)
    {
        _isAddTransportConnectionMenuOpen = false;
        _isAddDistributionTargetMenuOpen = false;

        if (string.Equals(targetId, GoLiveTargetCatalog.TargetIds.Recording, StringComparison.Ordinal))
        {
            await ToggleRecording.InvokeAsync();
        }
    }

    private void ToggleDistributionTargetMenu()
    {
        _isAddTransportConnectionMenuOpen = false;
        _isAddDistributionTargetMenuOpen = !_isAddDistributionTargetMenuOpen;
    }

    private void ToggleTransportConnectionMenu()
    {
        _isAddDistributionTargetMenuOpen = false;
        _isAddTransportConnectionMenuOpen = !_isAddTransportConnectionMenuOpen;
    }

    private async Task ToggleStreamingCardAsync(string cardId)
    {
        _isAddTransportConnectionMenuOpen = false;
        _isAddDistributionTargetMenuOpen = false;
        await ToggleCard.InvokeAsync(cardId);
    }

    private string Format(string key, params object[] arguments) =>
        string.Format(CultureInfo.CurrentCulture, Text(key), arguments);

    private string Text(string key) => Localizer[key];
}
