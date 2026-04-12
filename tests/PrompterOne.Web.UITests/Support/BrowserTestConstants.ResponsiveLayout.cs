namespace PrompterOne.Web.UITests;

public readonly record struct ResponsiveViewport(string Name, int Width, int Height);

internal readonly record struct ResponsiveDevice(string Name, int PortraitWidth, int PortraitHeight)
{
    public IReadOnlyList<ResponsiveViewport> BuildViewports() =>
    [
        new($"{Name}-portrait", PortraitWidth, PortraitHeight),
        new($"{Name}-landscape", PortraitHeight, PortraitWidth)
    ];
}

internal static partial class BrowserTestConstants
{
    public static class ResponsiveLayout
    {
        public const string IpadAirPortraitName = "ipad-air-portrait";
        public const string IpadMiniPortraitName = "ipad-mini-portrait";
        public const string IpadProPortraitName = "ipad-pro-portrait";
        public const string ScenarioPrefix = "responsive-layout";
        public const string InitialStep = "01-initial";
        public const int ViewportEdgeTolerancePx = 2;

        public const string LibraryRouteName = "library";
        public const string EditorRouteName = "editor";
        public const string LearnRouteName = "learn";
        public const string TeleprompterRouteName = "teleprompter";
        public const string SettingsRouteName = "settings";
        public const string GoLiveRouteName = "go-live";

        public const int AndroidPhoneSmallWidth = 360;
        public const int AndroidPhoneSmallHeight = 640;
        public const int IphoneSmallWidth = 375;
        public const int IphoneSmallHeight = 667;
        public const int IphoneMediumWidth = 390;
        public const int IphoneMediumHeight = 844;
        public const int AndroidPhoneMediumWidth = 412;
        public const int AndroidPhoneMediumHeight = 915;
        public const int IphoneLargeWidth = 430;
        public const int IphoneLargeHeight = 932;
        public const int IpadMiniWidth = 768;
        public const int IpadMiniHeight = 1024;
        public const int AndroidTabletWidth = 800;
        public const int AndroidTabletHeight = 1280;
        public const int IpadAirWidth = 820;
        public const int IpadAirHeight = 1180;
        public const int IpadProWidth = 1024;
        public const int IpadProHeight = 1366;

        private static IReadOnlyList<ResponsiveDevice> Devices { get; } =
        [
            new("android-phone-small", AndroidPhoneSmallWidth, AndroidPhoneSmallHeight),
            new("iphone-small", IphoneSmallWidth, IphoneSmallHeight),
            new("iphone-medium", IphoneMediumWidth, IphoneMediumHeight),
            new("android-phone-medium", AndroidPhoneMediumWidth, AndroidPhoneMediumHeight),
            new("iphone-large", IphoneLargeWidth, IphoneLargeHeight),
            new("ipad-mini", IpadMiniWidth, IpadMiniHeight),
            new("android-tablet", AndroidTabletWidth, AndroidTabletHeight),
            new("ipad-air", IpadAirWidth, IpadAirHeight),
            new("ipad-pro", IpadProWidth, IpadProHeight)
        ];

        public static IReadOnlyList<ResponsiveViewport> Viewports { get; } =
            Devices.SelectMany(static device => device.BuildViewports()).ToArray();

        public static IReadOnlyList<ResponsiveViewport> IpadPortraitViewports { get; } =
            Viewports
                .Where(static viewport => viewport.Name is IpadMiniPortraitName or IpadAirPortraitName or IpadProPortraitName)
                .ToArray();

        public static IReadOnlyList<ResponsiveViewport> HeaderChromeViewports { get; } =
        [
            new("iphone-medium-portrait", IphoneMediumWidth, IphoneMediumHeight),
            new("iphone-medium-landscape", IphoneMediumHeight, IphoneMediumWidth),
            new("ipad-mini-portrait", IpadMiniWidth, IpadMiniHeight),
            new("ipad-mini-landscape", IpadMiniHeight, IpadMiniWidth)
        ];
    }
}
