using ManagedCode.Tps;

namespace PrompterOne.Shared.Services.Editor;

internal static class EditorTpsCatalog
{
    public static EditorTpsCatalogModel Current { get; } = Create();

    private static EditorTpsCatalogModel Create()
    {
        return new EditorTpsCatalogModel(
            TpsSpec.Archetypes.ToArray(),
            TpsSpec.Emotions.ToArray(),
            TpsSpec.VolumeLevels.ToArray(),
            TpsSpec.DeliveryModes.ToArray(),
            TpsSpec.ArticulationStyles.ToArray(),
            TpsSpec.RelativeSpeedTags.ToArray(),
            TpsSpec.EditPointPriorities.ToArray(),
            TpsSpec.Archetypes
                .Select(CreateArchetypeDescriptor)
                .ToArray());
    }

    private static EditorTpsArchetypeDescriptor CreateArchetypeDescriptor(string archetype)
    {
        var profile = TpsSpec.ArchetypeProfiles[archetype];
        var recommendedWpm = TpsSpec.ArchetypeRecommendedWpm[archetype];

        return new EditorTpsArchetypeDescriptor(
            archetype,
            ToDisplayLabel(archetype),
            recommendedWpm,
            profile.Articulation,
            profile.Energy.Min,
            profile.Energy.Max,
            profile.Melody.Min,
            profile.Melody.Max,
            profile.Volume,
            profile.Speed.Min,
            profile.Speed.Max);
    }

    private static string ToDisplayLabel(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return char.ToUpperInvariant(value[0]) + value[1..];
    }
}

internal sealed record EditorTpsCatalogModel(
    IReadOnlyList<string> Archetypes,
    IReadOnlyList<string> Emotions,
    IReadOnlyList<string> VolumeLevels,
    IReadOnlyList<string> DeliveryModes,
    IReadOnlyList<string> ArticulationStyles,
    IReadOnlyList<string> RelativeSpeedTags,
    IReadOnlyList<string> EditPointPriorities,
    IReadOnlyList<EditorTpsArchetypeDescriptor> ArchetypeDescriptors);

internal sealed record EditorTpsArchetypeDescriptor(
    string Name,
    string Label,
    int RecommendedWpm,
    string Articulation,
    int EnergyMin,
    int EnergyMax,
    int MelodyMin,
    int MelodyMax,
    string Volume,
    int SpeedMin,
    int SpeedMax);
