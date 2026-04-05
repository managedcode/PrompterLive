using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Localization;
using PrompterOne.Shared.Settings.Services;

namespace PrompterOne.Shared.Components.Settings;

public partial class SettingsAboutSection
{
    private const string AppCardId = "about-app";
    private const string CompanyCardId = "about-company";
    private const string OpenSourceCardId = "about-open-source";
    private const string ResourcesCardId = "about-resources";

    private const string AboutSectionDescriptionKey = "SettingsAboutSectionDescription";
    private const string AutomaticUpdatesLabelKey = "SettingsAboutAutomaticUpdatesLabel";
    private const string FooterTextKey = "SettingsAboutFooterText";
    private const string LicensedStatusLabelKey = "SettingsAboutLicensedStatusLabel";
    private const string ManagedCodeCardCopyKey = "SettingsAboutManagedCodeCardCopy";
    private const string ManagedCodeCardSubtitleKey = "SettingsAboutManagedCodeCardSubtitle";
    private const string OpenSourceCardSubtitleKey = "SettingsAboutOpenSourceCardSubtitle";
    private const string OpenSourceCardTitleKey = "SettingsAboutOpenSourceCardTitle";
    private const string ResourcesCardSubtitleKey = "SettingsAboutResourcesCardSubtitle";
    private const string ResourcesCardTitleKey = "SettingsAboutResourcesCardTitle";
    private const string SoftwareUpdatesLabelKey = "SettingsAboutSoftwareUpdatesLabel";
    private const string ClarityDisclosureLabelKey = "SettingsAboutClarityDisclosureLabel";
    private const string ClarityDisclosureDescriptionKey = "SettingsAboutClarityDisclosureDescription";
    private const string TpsGitHubLabelKey = "SettingsAboutTpsGitHubLabel";
    private const string TpsGitHubDescriptionKey = "SettingsAboutTpsGitHubDescription";
    private const string UpToDateLabelKey = "SettingsAboutUpToDateLabel";
    private const string ProductGitHubLabelKey = "SettingsAboutProductGitHubLabel";
    private const string ProductGitHubDescriptionKey = "SettingsAboutProductGitHubDescription";
    private const string CompanyWebsiteLabelKey = "SettingsAboutCompanyWebsiteLabel";
    private const string CompanyWebsiteDescriptionKey = "SettingsAboutCompanyWebsiteDescription";
    private const string CompanyGitHubLabelKey = "SettingsAboutCompanyGitHubLabel";
    private const string CompanyGitHubDescriptionKey = "SettingsAboutCompanyGitHubDescription";
    private const string ProductWebsiteLabelKey = "SettingsAboutProductWebsiteLabel";
    private const string ProductWebsiteDescriptionKey = "SettingsAboutProductWebsiteDescription";
    private const string InterDescriptionKey = "SettingsAboutLibraryInterDescription";
    private const string JetBrainsMonoDescriptionKey = "SettingsAboutLibraryJetBrainsMonoDescription";
    private const string PlayfairDisplayDescriptionKey = "SettingsAboutLibraryPlayfairDisplayDescription";
    private const string FeatherIconsDescriptionKey = "SettingsAboutLibraryFeatherIconsDescription";
    private const string WebRtcDescriptionKey = "SettingsAboutLibraryWebRtcDescription";
    private const string MediaRecorderDescriptionKey = "SettingsAboutLibraryMediaRecorderDescription";
    private const string WebAudioDescriptionKey = "SettingsAboutLibraryWebAudioDescription";
    private const string RepositoryLinkLabelKey = "SettingsAboutRepositoryLinkLabel";
    private const string RepositoryLinkDescriptionKey = "SettingsAboutRepositoryLinkDescription";
    private const string ReleasesLinkLabelKey = "SettingsAboutReleasesLinkLabel";
    private const string ReleasesLinkDescriptionKey = "SettingsAboutReleasesLinkDescription";
    private const string IssuesLinkLabelKey = "SettingsAboutIssuesLinkLabel";
    private const string IssuesLinkDescriptionKey = "SettingsAboutIssuesLinkDescription";

    [Inject] private IAppVersionProvider AppVersionProvider { get; set; } = null!;
    [Inject] private IStringLocalizer<SharedResource> Localizer { get; set; } = null!;

    [Parameter] public string DisplayStyle { get; set; } = string.Empty;
    [Parameter] public Func<string, bool> IsCardOpen { get; set; } = static _ => false;
    [Parameter] public EventCallback<string> ToggleCard { get; set; }

    private string AboutSectionDescription => Text(AboutSectionDescriptionKey);
    private string AboutSectionTitle => Text(UiTextKey.SettingsNavAbout);
    private IReadOnlyList<AboutLinkItem> AppLinks =>
    [
        new(
            UiTestIds.Settings.AboutProductGitHub,
            Text(ProductGitHubLabelKey),
            Text(ProductGitHubDescriptionKey),
            AboutLinks.ProductRepositoryUrl),
        new(
            UiTestIds.Settings.AboutTpsGitHub,
            Text(TpsGitHubLabelKey),
            Text(TpsGitHubDescriptionKey),
            AboutLinks.TpsRepositoryUrl)
    ];
    private string AppCardSubtitle => AppVersionProvider.Current.Subtitle;
    private const string AppCardTitle = AboutLinks.ProductName;
    private string AutomaticUpdatesLabel => Text(AutomaticUpdatesLabelKey);
    private IReadOnlyList<AboutLinkItem> CompanyLinks =>
    [
        new(
            UiTestIds.Settings.AboutCompanyWebsite,
            Text(CompanyWebsiteLabelKey),
            Text(CompanyWebsiteDescriptionKey),
            AboutLinks.ManagedCodeWebsiteUrl),
        new(
            UiTestIds.Settings.AboutCompanyGitHub,
            Text(CompanyGitHubLabelKey),
            Text(CompanyGitHubDescriptionKey),
            AboutLinks.ManagedCodeGitHubUrl),
        new(
            UiTestIds.Settings.AboutProductWebsite,
            Text(ProductWebsiteLabelKey),
            Text(ProductWebsiteDescriptionKey),
            AboutLinks.ProductWebsiteUrl)
    ];
    private string FooterText => Text(FooterTextKey);
    private string LicensedStatusLabel => Text(LicensedStatusLabelKey);
    private IReadOnlyList<AboutItem> Libraries =>
    [
        new("Inter", Text(InterDescriptionKey), "OFL"),
        new("JetBrains Mono", Text(JetBrainsMonoDescriptionKey), "OFL"),
        new("Playfair Display", Text(PlayfairDisplayDescriptionKey), "OFL"),
        new("Feather Icons", Text(FeatherIconsDescriptionKey), "MIT"),
        new("WebRTC", Text(WebRtcDescriptionKey), "BSD"),
        new("MediaRecorder API", Text(MediaRecorderDescriptionKey), "W3C"),
        new("Web Audio API", Text(WebAudioDescriptionKey), "W3C")
    ];
    private string ManagedCodeCardCopy => Text(ManagedCodeCardCopyKey);
    private string ManagedCodeCardSubtitle => Text(ManagedCodeCardSubtitleKey);
    private const string ManagedCodeCardTitle = AboutLinks.ManagedCodeName;
    private string OpenSourceCardSubtitle => Text(OpenSourceCardSubtitleKey);
    private string OpenSourceCardTitle => Text(OpenSourceCardTitleKey);
    private IReadOnlyList<AboutLinkItem> ResourceLinks =>
    [
        new(
            UiTestIds.Settings.AboutRepositoryLink,
            Text(RepositoryLinkLabelKey),
            Text(RepositoryLinkDescriptionKey),
            AboutLinks.ProductRepositoryUrl),
        new(
            UiTestIds.Settings.AboutReleasesLink,
            Text(ReleasesLinkLabelKey),
            Text(ReleasesLinkDescriptionKey),
            AboutLinks.ProductReleasesUrl),
        new(
            UiTestIds.Settings.AboutIssuesLink,
            Text(IssuesLinkLabelKey),
            Text(IssuesLinkDescriptionKey),
            AboutLinks.ProductIssuesUrl),
        new(
            UiTestIds.Settings.AboutClarityDisclosure,
            Text(ClarityDisclosureLabelKey),
            Text(ClarityDisclosureDescriptionKey),
            AboutLinks.ClarityPrivacyDisclosureUrl)
    ];
    private string ResourcesCardSubtitle => Text(ResourcesCardSubtitleKey);
    private string ResourcesCardTitle => Text(ResourcesCardTitleKey);
    private string SoftwareUpdatesLabel => Text(SoftwareUpdatesLabelKey);
    private string UpToDateLabel => Text(UpToDateLabelKey);

    private string Text(string key) => Localizer[key];

    private string Text(UiTextKey key) => Localizer[key.ToString()];

    private sealed record AboutItem(string Name, string Description, string License);

    private sealed record AboutLinkItem(string TestId, string Label, string Description, string Href);
}
