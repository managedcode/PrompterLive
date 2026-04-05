namespace PrompterOne.Shared.Services;

public sealed record RuntimeTelemetryOptions(
    string GoogleAnalyticsMeasurementId,
    string ClarityProjectId,
    bool HostEnabled,
    bool SentryConfigured)
{
    public const string ClarityProjectIdKey = "ClarityProjectId";
    public const string ConfigurationSectionName = "RuntimeTelemetry";
    public const string GoogleAnalyticsMeasurementIdKey = "GoogleAnalyticsMeasurementId";
    public const string HostEnabledKey = "HostEnabled";
    public const string SectionSeparator = ":";

    public static RuntimeTelemetryOptions Disabled { get; } =
        new(string.Empty, string.Empty, HostEnabled: false, SentryConfigured: false);

    public static string ClarityProjectIdPath =>
        string.Concat(ConfigurationSectionName, SectionSeparator, ClarityProjectIdKey);

    public static string GoogleAnalyticsMeasurementIdPath =>
        string.Concat(ConfigurationSectionName, SectionSeparator, GoogleAnalyticsMeasurementIdKey);

    public static string HostEnabledPath =>
        string.Concat(ConfigurationSectionName, SectionSeparator, HostEnabledKey);
}
