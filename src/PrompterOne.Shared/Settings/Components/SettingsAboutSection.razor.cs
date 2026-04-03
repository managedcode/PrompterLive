using Microsoft.AspNetCore.Components;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Settings.Services;

namespace PrompterOne.Shared.Components.Settings;

public partial class SettingsAboutSection
{
    private const string AboutSectionTitle = "About";
    private const string AboutSectionDescription = "PrompterOne is a professional teleprompter for creators, broadcasters, and public speakers.";
    private const string AppCardTitle = AboutLinks.ProductName;
    private const string AppCardId = "about-app";
    private const string AutomaticUpdatesLabel = "Check for updates automatically";
    private const string CompanyCardId = "about-company";
    private const string FooterText = "Built and maintained by Managed Code.";
    private const string LicensedStatusLabel = "Licensed";
    private const string ManagedCodeCardCopy = "Everything in PrompterOne is designed, built, and maintained by Managed Code. Use the official links below for the company site, the public GitHub organization, and the live product site.";
    private const string ManagedCodeCardSubtitle = "Product, design, and engineering by Managed Code";
    private const string ManagedCodeCardTitle = AboutLinks.ManagedCodeName;
    private const string OpenSourceCardId = "about-open-source";
    private const string OpenSourceCardSubtitle = "Libraries & licenses";
    private const string OpenSourceCardTitle = "Open Source";
    private const string ResourcesCardId = "about-resources";
    private const string ResourcesCardSubtitle = "Live app, releases, and support";
    private const string ResourcesCardTitle = "Help & Resources";
    private const string SoftwareUpdatesLabel = "Software Updates";
    private const string TpsGitHubLabel = "TPS on GitHub";
    private const string TpsGitHubDescription = "TelePrompterScript format, docs, and examples";
    private const string UpToDateLabel = "Up to date";

    private static readonly AboutLinkItem[] AppLinks =
    [
        new(
            UiTestIds.Settings.AboutProductGitHub,
            "PrompterOne on GitHub",
            "Source code, docs, and milestones",
            AboutLinks.ProductRepositoryUrl),
        new(
            UiTestIds.Settings.AboutTpsGitHub,
            TpsGitHubLabel,
            TpsGitHubDescription,
            AboutLinks.TpsRepositoryUrl)
    ];

    private static readonly AboutLinkItem[] CompanyLinks =
    [
        new(
            UiTestIds.Settings.AboutCompanyWebsite,
            "Managed Code website",
            "Official company website",
            AboutLinks.ManagedCodeWebsiteUrl),
        new(
            UiTestIds.Settings.AboutCompanyGitHub,
            "Managed Code on GitHub",
            "Official GitHub organization",
            AboutLinks.ManagedCodeGitHubUrl),
        new(
            UiTestIds.Settings.AboutProductWebsite,
            "PrompterOne app",
            "Live standalone WebAssembly build",
            AboutLinks.ProductWebsiteUrl)
    ];

    private static readonly AboutItem[] Libraries =
    [
        new("Inter", "UI typeface · Rasmus Andersson", "OFL"),
        new("JetBrains Mono", "Monospace · JetBrains", "OFL"),
        new("Playfair Display", "Display serif · Claus Eggers Sorensen", "OFL"),
        new("Feather Icons", "Open source icon set", "MIT"),
        new("WebRTC", "Real-time communication APIs", "BSD"),
        new("MediaRecorder API", "Browser media recording", "W3C"),
        new("Web Audio API", "High-level audio processing", "W3C")
    ];

    private static readonly AboutLinkItem[] ResourceLinks =
    [
        new(
            UiTestIds.Settings.AboutRepositoryLink,
            "PrompterOne repository",
            "Source code, docs, and milestones",
            AboutLinks.ProductRepositoryUrl),
        new(
            UiTestIds.Settings.AboutReleasesLink,
            "Release notes",
            "Published builds and changelog",
            AboutLinks.ProductReleasesUrl),
        new(
            UiTestIds.Settings.AboutIssuesLink,
            "Report an issue",
            "Bug reports and product feedback",
            AboutLinks.ProductIssuesUrl)
    ];

    [Inject] private IAppVersionProvider AppVersionProvider { get; set; } = null!;

    [Parameter] public string DisplayStyle { get; set; } = string.Empty;
    [Parameter] public Func<string, bool> IsCardOpen { get; set; } = static _ => false;
    [Parameter] public EventCallback<string> ToggleCard { get; set; }

    private string AppCardSubtitle => AppVersionProvider.Current.Subtitle;

    private sealed record AboutItem(string Name, string Description, string License);
    private sealed record AboutLinkItem(string TestId, string Label, string Description, string Href);
}
