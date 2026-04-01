using System.Globalization;
using PrompterOne.Core.Models.CompiledScript;

namespace PrompterOne.Shared.Pages;

public partial class TeleprompterPage
{
    private const double FastLetterSpacingDeadZoneRatio = 0.05d;
    private const double FastLetterSpacingFloorEm = -0.015d;
    private const double MaximumFastLetterSpacingEm = -0.028d;
    private const double MaximumSlowClassLetterSpacingEm = 0.055d;
    private const int MinimumReaderReferenceWpm = 60;
    private const string PronunciationTitlePrefix = "Pronunciation: ";
    private const string ReaderSpeedTitlePrefix = "Speed: ";
    private const string ReaderWordLetterSpacingVariable = "--tps-word-letter-spacing";
    private const double SlowLetterSpacingFloorEm = 0.028d;
    private const double SlowLetterSpacingRangeRatio = 0.4d;
    private const double FastLetterSpacingRangeRatio = 0.55d;
    private const double MaximumSlowLetterSpacingEm = 0.058d;
    private const string WpmSuffix = " WPM";

    private static string? BuildReaderWordStyle(WordMetadata? metadata, int targetWpm, int effectiveWpm)
    {
        if (metadata is null)
        {
            return null;
        }

        var referenceWpm = Math.Max(MinimumReaderReferenceWpm, targetWpm);
        var speedRatio = effectiveWpm / (double)referenceWpm;
        if (Math.Abs(speedRatio - 1d) <= FastLetterSpacingDeadZoneRatio)
        {
            return null;
        }

        var letterSpacingEm = speedRatio < 1d
            ? Math.Min(
                MaximumSlowLetterSpacingEm,
                MaximumSlowLetterSpacingEm * (1d - speedRatio) / SlowLetterSpacingRangeRatio)
            : -Math.Min(
                Math.Abs(MaximumFastLetterSpacingEm),
                Math.Abs(MaximumFastLetterSpacingEm) * (speedRatio - 1d) / FastLetterSpacingRangeRatio);

        if (speedRatio <= 0.65d)
        {
            letterSpacingEm = Math.Max(letterSpacingEm, MaximumSlowClassLetterSpacingEm);
        }
        else if (speedRatio < 0.95d)
        {
            letterSpacingEm = Math.Max(letterSpacingEm, SlowLetterSpacingFloorEm);
        }
        else if (speedRatio >= 1.45d)
        {
            letterSpacingEm = Math.Min(letterSpacingEm, MaximumFastLetterSpacingEm);
        }
        else if (speedRatio > 1.05d)
        {
            letterSpacingEm = Math.Min(letterSpacingEm, FastLetterSpacingFloorEm);
        }

        if (Math.Abs(letterSpacingEm) < 0.001d)
        {
            return null;
        }

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{ReaderWordLetterSpacingVariable}:{letterSpacingEm:0.###}em;");
    }

    private static string? BuildReaderWordTitle(WordMetadata? metadata, int targetWpm, int effectiveWpm)
    {
        if (metadata is null)
        {
            return null;
        }

        var titleParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(metadata.PronunciationGuide))
        {
            titleParts.Add(PronunciationTitlePrefix + metadata.PronunciationGuide.Trim());
        }

        if (effectiveWpm != targetWpm)
        {
            titleParts.Add(ReaderSpeedTitlePrefix + effectiveWpm.ToString(CultureInfo.InvariantCulture) + WpmSuffix);
        }

        return titleParts.Count == 0
            ? null
            : string.Join(" · ", titleParts);
    }

    private static int ResolveEffectiveWpm(WordMetadata? metadata, int targetWpm)
    {
        var referenceWpm = Math.Max(MinimumReaderReferenceWpm, targetWpm);
        if (metadata?.SpeedOverride is int speedOverride)
        {
            return Math.Max(MinimumReaderReferenceWpm, speedOverride);
        }

        if (metadata?.SpeedMultiplier is float speedMultiplier && speedMultiplier > 0f)
        {
            return Math.Max(
                MinimumReaderReferenceWpm,
                (int)Math.Round(referenceWpm * speedMultiplier, MidpointRounding.AwayFromZero));
        }

        return referenceWpm;
    }
}
