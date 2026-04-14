using System.Globalization;
using System.Text;

namespace PrompterOne.Web.UITests;

internal static class EditorLargeDraftPerformanceTestData
{
    public const int BlockCountPerSegment = 4;
    public const int CiMaxTypingLatencyMs = 250;
    public const int CiMaxHugeFollowupLongTaskMs = 550;
    public const int CiMaxHugeTypingLatencyMs = 425;
    public const int CiMaxPasteLongTaskMs = 375;
    public const string FollowupTypingText = " x";
    public const int HugeDraftMinimumLength = 250_000;
    public const int HugeDraftReadyTimeoutMs = 30_000;
    public const int LargeDraftMinimumLength = 32_000;
    public const int LocalMaxTypingLatencyMs = 100;
    public const int LocalMaxHugeFollowupLongTaskMs = 500;
    public const int LocalMaxHugeTypingLatencyMs = 250;
    public const int LocalMaxPasteLongTaskMs = 325;
    public const int NavigationTargetSegmentIndex = 14;
    public const int ObservationDelayMs = 2_200;
    private const string Author = "Managed Code";
    private const int BaseWpm = 140;
    private const string Created = "2026-04-03";
    private const string Profile = "Actor";
    private const string Version = "1.0";
    private const string FrontMatterOpeningDelimiter = "---\n";
    private const string FrontMatterClosingDelimiter = "\n---\n\n";

    public static int MaxHugeFollowupLongTaskMs =>
        PrompterOne.Testing.TestEnvironment.IsCiEnvironment
            ? CiMaxHugeFollowupLongTaskMs
            : LocalMaxHugeFollowupLongTaskMs;

    public static int MaxHugeTypingLatencyMs =>
        PrompterOne.Testing.TestEnvironment.IsCiEnvironment
            ? CiMaxHugeTypingLatencyMs
            : LocalMaxHugeTypingLatencyMs;

    public static int MaxTypingLatencyMs =>
        PrompterOne.Testing.TestEnvironment.IsCiEnvironment
            ? CiMaxTypingLatencyMs
            : LocalMaxTypingLatencyMs;

    public static int MaxPasteLongTaskMs =>
        PrompterOne.Testing.TestEnvironment.IsCiEnvironment
            ? CiMaxPasteLongTaskMs
            : LocalMaxPasteLongTaskMs;

    private static readonly string[] BlockBodies =
    [
        "Before you scale the service, / measure the real bottleneck, / verify the queue depth, / and [emphasis]keep the write path honest[/emphasis]. //",
        "A stateless node can fail safely / only when cache invalidation, / persistence, / and [highlight]retry behavior[/highlight] stay predictable. //",
        "Operators need [professional]calm telemetry[/professional], / not noisy dashboards, / while the system is under pressure. //",
        "[slow]Good architecture[/slow] is not a bigger diagram. / It is the ability to change one part / without collapsing the rest. //"
    ];

    public static string BuildLargeDraft()
    {
        return BuildDraft(BrowserTestConstants.Scripts.LargeDraftTitle, LargeDraftMinimumLength);
    }

    public static string BuildHugeDraft()
    {
        return BuildDraft(BrowserTestConstants.Scripts.HugeDraftTitle, HugeDraftMinimumLength);
    }

    public static int GetVisibleDraftLength(string draft)
    {
        var source = draft ?? string.Empty;
        if (!source.StartsWith(FrontMatterOpeningDelimiter, StringComparison.Ordinal))
        {
            return source.Length;
        }

        var closingIndex = source.IndexOf(
            FrontMatterClosingDelimiter,
            FrontMatterOpeningDelimiter.Length,
            StringComparison.Ordinal);

        return closingIndex < 0
            ? source.Length
            : Math.Max(0, source.Length - (closingIndex + FrontMatterClosingDelimiter.Length));
    }

    private static string BuildDraft(string title, int minimumLength)
    {
        var builder = new StringBuilder();
        builder.Append(BuildFrontMatter(title));
        var segmentIndex = 1;

        while (builder.Length < minimumLength)
        {
            AppendSegment(builder, segmentIndex);
            segmentIndex += 1;
        }

        return builder.ToString();
    }

    private static string BuildFrontMatter(string title)
    {
        var builder = new StringBuilder();
        builder.AppendLine(FrontMatterOpeningDelimiter.TrimEnd('\n'));
        builder.AppendLine(CultureInfo.InvariantCulture, $"title: \"{title}\"");
        builder.AppendLine(CultureInfo.InvariantCulture, $"profile: {Profile}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"author: \"{Author}\"");
        builder.AppendLine(CultureInfo.InvariantCulture, $"created: \"{Created}\"");
        builder.AppendLine(CultureInfo.InvariantCulture, $"version: \"{Version}\"");
        builder.AppendLine(CultureInfo.InvariantCulture, $"base_wpm: {BaseWpm}");
        builder.AppendLine(FrontMatterOpeningDelimiter.TrimEnd('\n'));
        builder.AppendLine();
        return builder.ToString();
    }

    private static void AppendSegment(StringBuilder builder, int segmentIndex)
    {
        builder.AppendLine(CultureInfo.InvariantCulture, $"## [Architecture Episode {segmentIndex}|{BaseWpm}WPM|Professional]");
        builder.AppendLine();

        for (var blockIndex = 1; blockIndex <= BlockCountPerSegment; blockIndex++)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"### [System Block {segmentIndex}.{blockIndex}|{BaseWpm}WPM|Focused]");
            builder.AppendLine(BlockBodies[(blockIndex - 1) % BlockBodies.Length]);
            builder.AppendLine("[pause:2s]");
            builder.AppendLine();
        }
    }

    public static string GetSegmentHeader(int segmentIndex) =>
        FormattableString.Invariant($"## [Architecture Episode {segmentIndex}|{BaseWpm}WPM|Professional]");

    public static string GetSegmentLabel(int segmentIndex) =>
        FormattableString.Invariant($"Architecture Episode {segmentIndex}");
}
