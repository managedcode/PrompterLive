namespace PrompterOne.Shared.Components.GoLive;

public enum GoLiveStudioMode
{
    Director,
    Studio
}

public enum GoLiveStudioTab
{
    Stream,
    Audio,
    Room
}

public enum GoLiveSceneLayout
{
    Full,
    Split,
    PictureInPicture
}

public enum GoLiveTransitionKind
{
    Cut,
    Fade,
    Wipe
}

public enum GoLiveTransitionDuration
{
    Quick,
    Standard,
    Extended
}

public enum GoLiveCropPreset
{
    Full,
    HeadAndShoulders,
    Face
}

public enum GoLiveSceneChipKind
{
    Camera,
    Split,
    Slides,
    PictureInPicture,
    Custom
}

public sealed record GoLiveSceneChipViewModel(
    string Id,
    string Name,
    GoLiveSceneChipKind Kind,
    string? SourceId);

public sealed record GoLiveDestinationSummaryViewModel(
    string Id,
    string Name,
    string PlatformLabel,
    bool IsEnabled,
    bool IsReady,
    string Summary,
    string StatusLabel,
    string Tone);

public sealed record GoLiveMetricViewModel(
    string Id,
    string Value,
    string Label);

public sealed record GoLiveAudioChannelViewModel(
    string Id,
    string Name,
    string DetailLabel,
    int LevelPercent);

public sealed record GoLiveRecordingBlockCueViewModel(
    string TestId,
    string RoleLabel,
    string Title,
    string Text,
    bool IsActive);

public sealed record GoLiveRoomParticipantViewModel(
    string Id,
    string Initial,
    string Name,
    string Role,
    int LevelPercent,
    bool IsOnline);

public sealed record GoLiveUtilitySourceViewModel(
    string Id,
    string Title,
    string Subtitle,
    string BadgeLabel);
