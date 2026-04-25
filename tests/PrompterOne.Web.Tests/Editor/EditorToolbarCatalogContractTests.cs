using PrompterOne.Shared.Components.Editor;

namespace PrompterOne.Web.Tests;

public sealed class EditorToolbarCatalogContractTests
{
    public static IEnumerable<string> VoiceDropdownGroupLabels =>
        EditorToolbarCatalog.Sections
            .Single(candidate => string.Equals(candidate.Key, "voice", StringComparison.Ordinal))
            .DropdownGroups
            .Select(group => group.Label);

    [Test]
    public void TopToolbarSections_PrioritizeImpactAndSpeed_BeforeGeneralizedEmotion()
    {
        var sectionKeys = EditorToolbarCatalog.Sections.Select(section => section.Key).ToArray();

        Assert.True(IndexOf(sectionKeys, "voice") < IndexOf(sectionKeys, "speed"));
        Assert.True(IndexOf(sectionKeys, "speed") < IndexOf(sectionKeys, "emotion"));
    }

    [Test]
    public void TopToolbarDropdowns_FollowVendoredTpsActionOrder()
    {
        var emotionGroup = FindTopDropdownGroup("emotion", "TPS Emotions");
        var deliveryGroup = FindTopDropdownGroup("emotion", "Delivery Modes");
        var speedGroup = FindTopDropdownGroup("speed", "Speed Presets");
        var emotionGroups = FindTopSection("emotion").DropdownGroups;

        Assert.True(IndexOfGroup(emotionGroups, "Delivery Modes") < IndexOfGroup(emotionGroups, "TPS Emotions"));

        Assert.Equal(
            [
                "emotion-neutral",
                "emotion-warm",
                "emotion-professional",
                "emotion-focused",
                "emotion-concerned",
                "emotion-urgent",
                "emotion-motivational",
                "emotion-excited",
                "emotion-happy",
                "emotion-sad",
                "emotion-calm",
                "emotion-energetic"
            ],
            emotionGroup.Actions.Select(action => action.Key));

        Assert.Equal(
            ["delivery-sarcasm", "delivery-aside", "delivery-rhetorical", "delivery-building"],
            deliveryGroup.Actions.Select(action => action.Key));

        Assert.Equal(
            ["speed-xslow-menu", "speed-slow-menu", "speed-fast-menu", "speed-xfast-menu", "speed-normal-menu"],
            speedGroup.Actions.Select(action => action.Key));
    }

    [Test]
    public void FloatingToolbarDropdowns_FollowVendoredTpsActionOrder()
    {
        var emotionGroup = FindFloatingDropdownGroup(EditorToolbarMenuIds.FloatingEmotion, "TPS Emotions");
        var deliveryGroup = FindFloatingDropdownGroup(EditorToolbarMenuIds.FloatingEmotion, "Delivery Modes");
        var speedGroup = FindFloatingDropdownGroup(EditorToolbarMenuIds.FloatingSpeed, "Speed Presets");
        var emotionGroups = FindFloatingMenu(EditorToolbarMenuIds.FloatingEmotion).DropdownGroups;

        Assert.True(IndexOfGroup(emotionGroups, "Delivery Modes") < IndexOfGroup(emotionGroups, "TPS Emotions"));

        Assert.Equal(
            [
                "float-emotion-neutral",
                "float-emotion-warm",
                "float-emotion-professional",
                "float-emotion-focused",
                "float-emotion-concerned",
                "float-emotion-urgent",
                "float-emotion-motivational",
                "float-emotion-excited",
                "float-emotion-happy",
                "float-emotion-sad",
                "float-emotion-calm",
                "float-emotion-energetic"
            ],
            emotionGroup.Actions.Select(action => action.Key));

        Assert.Equal(
            ["float-delivery-sarcasm", "float-delivery-aside", "float-delivery-rhetorical", "float-delivery-building"],
            deliveryGroup.Actions.Select(action => action.Key));

        Assert.Equal(
            ["float-speed-xslow-menu", "float-speed-slow-menu", "float-speed-fast-menu", "float-speed-xfast-menu", "float-speed-normal-menu"],
            speedGroup.Actions.Select(action => action.Key));
    }

    [Test]
    [MethodDataSource(nameof(VoiceDropdownGroupLabels))]
    public void VoiceDropdownGroup_StaysAligned_BetweenTopToolbarAndFloatingToolbar(string groupLabel)
    {
        var topVoiceGroups = FindTopSection("voice").DropdownGroups;
        var floatingVoiceGroups = FindFloatingMenu(EditorToolbarMenuIds.FloatingVoice).DropdownGroups;
        var topGroup = Assert.Single(topVoiceGroups, group => string.Equals(group.Label, groupLabel, StringComparison.Ordinal));
        var floatingGroup = Assert.Single(floatingVoiceGroups, group => string.Equals(group.Label, groupLabel, StringComparison.Ordinal));

        Assert.Equal(
            topGroup.Actions.Select(action => NormalizeFloatingKey(action.Key)),
            floatingGroup.Actions.Select(action => NormalizeFloatingKey(action.Key)));
    }

    [Test]
    public void FloatingInsertMenu_ExposesTheSameStructureHelpers_AsTopToolbarInsertMenu()
    {
        var topStructureGroup = FindTopDropdownGroup("insert", "Structure");
        var floatingStructureGroup = FindFloatingDropdownGroup(EditorToolbarMenuIds.FloatingInsert, "Structure");

        Assert.Equal(
            topStructureGroup.Actions.Select(action => NormalizeFloatingKey(action.Key)),
            floatingStructureGroup.Actions.Select(action => NormalizeFloatingKey(action.Key)));
    }

    private static EditorToolbarDropdownGroupDescriptor FindTopDropdownGroup(string sectionKey, string groupLabel)
    {
        var section = FindTopSection(sectionKey);
        return Assert.Single(section.DropdownGroups, candidate => string.Equals(candidate.Label, groupLabel, StringComparison.Ordinal));
    }

    private static EditorToolbarDropdownGroupDescriptor FindFloatingDropdownGroup(string menuId, string groupLabel)
    {
        var menu = FindFloatingMenu(menuId);
        return Assert.Single(menu.DropdownGroups, candidate => string.Equals(candidate.Label, groupLabel, StringComparison.Ordinal));
    }

    private static EditorToolbarSectionDescriptor FindTopSection(string sectionKey) =>
        Assert.Single(EditorToolbarCatalog.Sections, candidate => string.Equals(candidate.Key, sectionKey, StringComparison.Ordinal));

    private static EditorFloatingMenuDescriptor FindFloatingMenu(string menuId) =>
        Assert.Single(EditorToolbarCatalog.FloatingMenus, candidate => string.Equals(candidate.MenuId, menuId, StringComparison.Ordinal));

    private static int IndexOf(IReadOnlyList<string> values, string value)
    {
        var index = Array.IndexOf(values.ToArray(), value);
        Assert.True(index >= 0, $"Expected '{value}' to be present.");
        return index;
    }

    private static int IndexOfGroup(IReadOnlyList<EditorToolbarDropdownGroupDescriptor> groups, string label)
    {
        for (var index = 0; index < groups.Count; index++)
        {
            if (string.Equals(groups[index].Label, label, StringComparison.Ordinal))
            {
                return index;
            }
        }

        throw new InvalidOperationException($"Expected group '{label}' to be present.");
    }

    private static string NormalizeFloatingKey(string key) =>
        key.Replace("float-", string.Empty, StringComparison.Ordinal)
            .Replace("-menu", string.Empty, StringComparison.Ordinal);
}
