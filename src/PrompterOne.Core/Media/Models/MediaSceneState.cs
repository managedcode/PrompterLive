namespace PrompterOne.Core.Models.Media;

public sealed record MediaSourceTransform(
    double X = 0.5,
    double Y = 0.5,
    double Width = 0.32,
    double Height = 0.32,
    double Rotation = 0,
    bool MirrorHorizontal = false,
    bool MirrorVertical = false,
    bool Visible = true,
    bool IncludeInOutput = true,
    int ZIndex = 0,
    double Opacity = 1.0);

public sealed record SceneCameraSource(
    string SourceId,
    string DeviceId,
    string Label,
    MediaSourceTransform Transform);

public sealed record MediaSceneState(
    IReadOnlyList<SceneCameraSource> Cameras,
    string? PrimaryMicrophoneId,
    string? PrimaryMicrophoneLabel,
    AudioBusState AudioBus)
{
    public static MediaSceneState Empty { get; } = new(
        Array.Empty<SceneCameraSource>(),
        null,
        null,
        AudioBusState.Empty);
}
