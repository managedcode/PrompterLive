using PrompterOne.Core.Models.CompiledScript;
using PrompterOne.Shared.Contracts;

namespace PrompterOne.Shared.Pages;

public partial class TeleprompterPage
{
    private const double BuildingScaleRange = 0.18d;
    private const int BuildingWeightBase = 700;
    private const int BuildingWeightRange = 140;
    private const double FastLetterSpacingDeadZoneRatio = 0.03d;
    private const double FastLetterSpacingFloorEm = -0.024d;
    private const double MaximumFastLetterSpacingEm = -0.05d;
    private const double MaximumSlowClassLetterSpacingEm = 0.085d;
    private const int MinimumReaderReferenceWpm = 60;
    private const string ReaderWordLetterSpacingVariable = "--tps-word-letter-spacing";
    private const double SlowLetterSpacingFloorEm = 0.045d;
    private const double SlowLetterSpacingRangeRatio = 0.32d;
    private const double FastLetterSpacingRangeRatio = 0.45d;
    private const double MaximumSlowLetterSpacingEm = 0.09d;
    private const double ReaderCueScaleDefault = 1d;
    private const double ReaderCueScaleLoud = 1.18d;
    private const double ReaderCueScaleSoft = 0.9d;
    private const double ReaderCueScaleStress = 1.12d;
    private const double ReaderCueScaleWhisper = 0.78d;
    private const double ReaderCueOpacityAside = 0.88d;
    private const double ReaderCueOpacityDefault = 1d;
    private const double ReaderCueOpacitySarcasm = 0.92d;
    private const double ReaderCueOpacitySoft = 0.86d;
    private const double ReaderCueOpacityWhisper = 0.76d;
    private const int ReaderCueWeightLoud = 800;
    private const int ReaderCueWeightStress = 820;

    private static string? BuildReaderWordStyle(WordMetadata? metadata, int targetWpm, int effectiveWpm, double cueProgress)
    {
        if (metadata is null)
        {
            return null;
        }

        var styles = new List<string>(3);
        var letterSpacingEm = ResolveReaderLetterSpacing(targetWpm, effectiveWpm);
        if (letterSpacingEm is double letterSpacing)
        {
            styles.Add(CreateStyleVariable(ReaderWordLetterSpacingVariable, letterSpacing, "em"));
        }

        var cueMetrics = ResolveReaderCueMetrics(metadata, cueProgress);
        if (Math.Abs(cueMetrics.Scale - ReaderCueScaleDefault) > 0.001d)
        {
            styles.Add(CreateStyleVariable(TpsVisualCueContracts.CueScaleVariableName, cueMetrics.Scale));
        }

        if (Math.Abs(cueMetrics.Opacity - ReaderCueOpacityDefault) > 0.001d)
        {
            styles.Add(CreateStyleVariable(TpsVisualCueContracts.CueOpacityVariableName, cueMetrics.Opacity));
        }

        if (cueMetrics.Weight is int cueWeight)
        {
            styles.Add(FormattableString.Invariant($"{TpsVisualCueContracts.CueWeightVariableName}:{cueWeight};"));
        }

        return styles.Count == 0 ? null : string.Concat(styles);
    }

    private static double? ResolveReaderLetterSpacing(int targetWpm, int effectiveWpm)
    {
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

        return letterSpacingEm;
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

    private static string? ResolveReaderSpeedCueValue(int targetWpm, int effectiveWpm)
    {
        var speedRatio = effectiveWpm / (double)Math.Max(MinimumReaderReferenceWpm, targetWpm);
        if (speedRatio <= 0.65d)
        {
            return TpsVisualCueContracts.SpeedCueXslow;
        }

        if (speedRatio < 0.95d)
        {
            return TpsVisualCueContracts.SpeedCueSlow;
        }

        if (speedRatio >= 1.45d)
        {
            return TpsVisualCueContracts.SpeedCueXfast;
        }

        return speedRatio > 1.05d
            ? TpsVisualCueContracts.SpeedCueFast
            : null;
    }

    private static ReaderCueMetrics ResolveReaderCueMetrics(WordMetadata metadata, double cueProgress)
    {
        var scale = ReaderCueScaleDefault;
        var opacity = ReaderCueOpacityDefault;
        int? weight = null;

        switch (NormalizeCueValue(metadata.VolumeLevel))
        {
            case TpsVisualCueContracts.VolumeLoud:
                scale = Math.Max(scale, ReaderCueScaleLoud);
                weight = MaxCueWeight(weight, ReaderCueWeightLoud);
                break;
            case TpsVisualCueContracts.VolumeSoft:
                scale = Math.Min(scale, ReaderCueScaleSoft);
                opacity = Math.Min(opacity, ReaderCueOpacitySoft);
                break;
            case TpsVisualCueContracts.VolumeWhisper:
                scale = Math.Min(scale, ReaderCueScaleWhisper);
                opacity = Math.Min(opacity, ReaderCueOpacityWhisper);
                break;
        }

        switch (NormalizeCueValue(metadata.DeliveryMode))
        {
            case TpsVisualCueContracts.DeliveryModeBuilding:
                scale = Math.Max(scale, 1.02d + (BuildingScaleRange * cueProgress));
                weight = MaxCueWeight(
                    weight,
                    BuildingWeightBase + (int)Math.Round(BuildingWeightRange * cueProgress, MidpointRounding.AwayFromZero));
                break;
            case "aside":
                scale = Math.Min(scale, 0.96d);
                opacity = Math.Min(opacity, ReaderCueOpacityAside);
                break;
            case "rhetorical":
                scale = Math.Max(scale, 1.02d);
                break;
            case "sarcasm":
                opacity = Math.Min(opacity, ReaderCueOpacitySarcasm);
                break;
        }

        if (!string.IsNullOrWhiteSpace(metadata.StressText) || !string.IsNullOrWhiteSpace(metadata.StressGuide))
        {
            scale = Math.Max(scale, ReaderCueScaleStress);
            weight = MaxCueWeight(weight, ReaderCueWeightStress);
        }

        return new ReaderCueMetrics(scale, opacity, weight);
    }

    private static int MaxCueWeight(int? currentWeight, int nextWeight) =>
        currentWeight is int existingWeight
            ? Math.Max(existingWeight, nextWeight)
            : nextWeight;

    private static string CreateStyleVariable(string name, double value, string suffix = "") =>
        FormattableString.Invariant($"{name}:{value:0.###}{suffix};");

    private readonly record struct ReaderCueMetrics(double Scale, double Opacity, int? Weight);
}
