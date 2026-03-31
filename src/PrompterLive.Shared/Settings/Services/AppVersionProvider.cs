using System.Reflection;
using PrompterLive.Shared.Settings.Models;

namespace PrompterLive.Shared.Settings.Services;

public interface IAppVersionProvider
{
    AppVersionInfo Current { get; }
}

public sealed class StaticAppVersionProvider(AppVersionInfo current) : IAppVersionProvider
{
    public AppVersionInfo Current { get; } = current;
}

public static class AppVersionProviderFactory
{
    private const string DefaultVersion = "0.1.0";
    private const char MetadataSeparator = '+';
    private const char VersionPartSeparator = '.';

    public static IAppVersionProvider CreateFromAssembly(Assembly assembly)
    {
        var version = ResolveVersion(assembly);
        var buildNumber = ResolveBuildNumber(version);
        return new StaticAppVersionProvider(new AppVersionInfo(version, buildNumber));
    }

    private static string ResolveVersion(Assembly assembly)
    {
        var informationalVersion = NormalizeInformationalVersion(
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);

        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            return informationalVersion;
        }

        var assemblyVersion = assembly.GetName().Version?.ToString(3);
        return string.IsNullOrWhiteSpace(assemblyVersion)
            ? DefaultVersion
            : assemblyVersion;
    }

    private static string NormalizeInformationalVersion(string? informationalVersion)
    {
        if (string.IsNullOrWhiteSpace(informationalVersion))
        {
            return string.Empty;
        }

        var metadataSeparatorIndex = informationalVersion.IndexOf(MetadataSeparator);
        return metadataSeparatorIndex >= 0
            ? informationalVersion[..metadataSeparatorIndex]
            : informationalVersion;
    }

    private static string ResolveBuildNumber(string version)
    {
        var versionParts = version.Split(VersionPartSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return versionParts.Length < 3
            ? string.Empty
            : ExtractLeadingDigits(versionParts[2]);
    }

    private static string ExtractLeadingDigits(string versionPart)
    {
        var digits = versionPart.TakeWhile(char.IsDigit).ToArray();
        return digits.Length == 0
            ? string.Empty
            : new string(digits);
    }
}
