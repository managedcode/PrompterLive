namespace PrompterLive.Shared.Settings.Models;

public sealed record AppVersionInfo(string Version, string BuildNumber)
{
    private const string BuildLabelPrefix = "Build ";
    private const string SubtitleSeparator = " · ";
    private const string VersionLabelPrefix = "Version ";

    public string Subtitle =>
        string.IsNullOrWhiteSpace(BuildNumber)
            ? VersionLabel
            : string.Concat(VersionLabel, SubtitleSeparator, BuildLabelPrefix, BuildNumber);

    public string VersionLabel => string.Concat(VersionLabelPrefix, Version);
}
