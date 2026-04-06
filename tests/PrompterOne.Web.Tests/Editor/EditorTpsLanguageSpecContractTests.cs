using ManagedCode.Tps;
using PrompterOne.Shared.Services.Editor;

namespace PrompterOne.Web.Tests;

public sealed class EditorTpsLanguageSpecContractTests
{
    [Test]
    public void MonacoTpsCatalog_ContainsTheSameVocabularyAsVendoredSdk()
    {
        var catalog = EditorTpsCatalog.Current;

        Assert.Equal(TpsSpec.Archetypes, catalog.Archetypes, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(TpsSpec.Emotions, catalog.Emotions, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(TpsSpec.VolumeLevels, catalog.VolumeLevels, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(TpsSpec.DeliveryModes, catalog.DeliveryModes, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(TpsSpec.ArticulationStyles, catalog.ArticulationStyles, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(TpsSpec.RelativeSpeedTags, catalog.RelativeSpeedTags, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(TpsSpec.EditPointPriorities, catalog.EditPointPriorities, StringComparer.OrdinalIgnoreCase);
    }

    [Test]
    public void MonacoTpsCatalog_ProjectsVendoredArchetypeProfiles()
    {
        var catalog = EditorTpsCatalog.Current;

        foreach (var archetype in TpsSpec.Archetypes)
        {
            var descriptor = Assert.Single(
                catalog.ArchetypeDescriptors,
                candidate => string.Equals(candidate.Name, archetype, StringComparison.OrdinalIgnoreCase));
            var profile = TpsSpec.ArchetypeProfiles[archetype];

            Assert.Equal(TpsSpec.ArchetypeRecommendedWpm[archetype], descriptor.RecommendedWpm);
            Assert.Equal(profile.Articulation, descriptor.Articulation);
            Assert.Equal(profile.Energy.Min, descriptor.EnergyMin);
            Assert.Equal(profile.Energy.Max, descriptor.EnergyMax);
            Assert.Equal(profile.Melody.Min, descriptor.MelodyMin);
            Assert.Equal(profile.Melody.Max, descriptor.MelodyMax);
            Assert.Equal(profile.Speed.Min, descriptor.SpeedMin);
            Assert.Equal(profile.Speed.Max, descriptor.SpeedMax);
            Assert.Equal(profile.Volume, descriptor.Volume);
        }
    }
}
