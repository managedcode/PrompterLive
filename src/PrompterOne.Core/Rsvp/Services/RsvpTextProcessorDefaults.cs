using ManagedCode.Tps;

namespace PrompterOne.Core.Services.Rsvp;

internal static class RsvpTextProcessorDefaults
{
    public const int DefaultSegmentSpeed = 250;
    public const double ActorPacingMultiplier = 1.35;
    public const int MinimumPhraseDurationMs = 450;
    public const int MinimumWordDurationMs = 180;
    public const int DefaultPauseMs = 400;
    public const int LongPauseMs = 800;
    public const string DefaultSegmentTitle = TpsSpec.DefaultImplicitSegmentName;
}
