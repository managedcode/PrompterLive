using Microsoft.AspNetCore.Components;
using PrompterLive.Shared.Settings.Services;

namespace PrompterLive.Shared.Components.Settings;

public partial class SettingsAboutSection
{
    private const string AppCardId = "about-app";
    private const string LicensedStatusLabel = "Licensed";
    private const string OpenSourceCardId = "about-open-source";
    private const string ResourcesCardId = "about-resources";
    private const string TeamCardId = "about-team";

    private static readonly string[] ResourceLinks =
    [
        "What's New",
        "Help & Documentation",
        "Report a Bug",
        "Privacy Policy"
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

    private static readonly TeamMember[] TeamMembers =
    [
        new("M", "Mykola Kovalenko", "Founder · Product & Design", "background:linear-gradient(135deg,#C4A060,#8B6A3A);"),
        new("A", "Anna Petrenko", "Lead Engineer", "background:linear-gradient(135deg,#60A5FA,#2563EB);"),
        new("D", "Dmytro Shevchenko", "Backend & Infrastructure", "background:linear-gradient(135deg,#34D399,#059669);")
    ];

    [Inject] private IAppVersionProvider AppVersionProvider { get; set; } = null!;

    [Parameter] public string DisplayStyle { get; set; } = string.Empty;
    [Parameter] public Func<string, bool> IsCardOpen { get; set; } = static _ => false;
    [Parameter] public EventCallback<string> ToggleCard { get; set; }

    private string AppCardSubtitle => AppVersionProvider.Current.Subtitle;

    private sealed record AboutItem(string Name, string Description, string License);

    private sealed record TeamMember(string Initials, string Name, string Role, string AvatarStyle);
}
