using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Pages;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

public sealed class TeleprompterCueRenderingTests : BunitContext
{
    private const int CueCardIndex = 0;
    private const string BuildingFirstWord = "we";
    private const string BuildingLastWord = "together.";
    private const string CueScriptId = "test-reader-cue-script";
    private const string CueScriptTitle = "Reader Cue Probe";
    private const string EnergyWord = "steady";
    private const string LoudWord = "clear";
    private const string MelodyWord = "rhythm";
    private const string SoftWord = "gentle";
    private const string StressWord = "building.";
    private const string WhisperWord = "secret";

    [Test]
    public async Task TeleprompterPage_EmitsCueAttributesAndLayoutStableVariablesForReaderDeliverySemantics()
    {
        var harness = TestHarnessFactory.Create(this, seedLibraryData: false);
        await harness.Repository.SaveAsync(
                CueScriptTitle,
                """
                ---
                title: "Reader Cue Probe"
                base_wpm: 140
                ---

                ## [Cue Demo|neutral]

                ### [Delivery Block|neutral]
                [whisper]secret[/whisper] [soft]gentle[/soft] [loud]clear[/loud] build[stress]ing[/stress]. //
                [breath]
                [legato][energy:8]steady[/energy][/legato] [staccato][melody:4]rhythm[/melody][/staccato] //

                ### [Lift Block|neutral]
                [building]we rise together[/building].
                """,
                "reader-cue-probe.tps",
                CueScriptId);

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppRoutes.TeleprompterWithId(CueScriptId));
        var cut = Render<TeleprompterPage>();

        cut.WaitForAssertion(() =>
        {
            var whisper = FindReaderWordByText(cut, CueCardIndex, WhisperWord);
            var soft = FindReaderWordByText(cut, CueCardIndex, SoftWord);
            var loud = FindReaderWordByText(cut, CueCardIndex, LoudWord);
            var stress = FindReaderWordByText(cut, CueCardIndex, StressWord);
            var energy = FindReaderWordByText(cut, CueCardIndex, EnergyWord);
            var melody = FindReaderWordByText(cut, CueCardIndex, MelodyWord);
            var breath = FindBreathCue(cut, CueCardIndex);
            var buildingFirst = FindReaderWordByText(cut, 1, BuildingFirstWord);
            var buildingLast = FindReaderWordByText(cut, 1, BuildingLastWord);

            Assert.Equal(TpsVisualCueContracts.VolumeWhisper, whisper.GetAttribute(TpsVisualCueContracts.VolumeAttributeName));
            Assert.Equal(TpsVisualCueContracts.VolumeSoft, soft.GetAttribute(TpsVisualCueContracts.VolumeAttributeName));
            Assert.Equal(TpsVisualCueContracts.VolumeLoud, loud.GetAttribute(TpsVisualCueContracts.VolumeAttributeName));
            Assert.Equal(TpsVisualCueContracts.DeliveryModeBuilding, buildingFirst.GetAttribute(TpsVisualCueContracts.DeliveryAttributeName));
            Assert.Equal(TpsVisualCueContracts.DeliveryModeBuilding, buildingLast.GetAttribute(TpsVisualCueContracts.DeliveryAttributeName));
            Assert.Equal(TpsVisualCueContracts.StressAttributeValue, stress.GetAttribute(TpsVisualCueContracts.StressAttributeName));
            Assert.Equal(TpsVisualCueContracts.ArticulationLegato, energy.GetAttribute(TpsVisualCueContracts.ArticulationAttributeName));
            Assert.Equal("8", energy.GetAttribute(TpsVisualCueContracts.EnergyAttributeName));
            Assert.Equal(TpsVisualCueContracts.ArticulationStaccato, melody.GetAttribute(TpsVisualCueContracts.ArticulationAttributeName));
            Assert.Equal("4", melody.GetAttribute(TpsVisualCueContracts.MelodyAttributeName));
            Assert.Equal(TpsVisualCueContracts.BreathAttributeValue, breath.GetAttribute(TpsVisualCueContracts.BreathAttributeName));

            var whisperOpacity = ReadStyleVariable(whisper, TpsVisualCueContracts.CueOpacityVariableName);
            var softOpacity = ReadStyleVariable(soft, TpsVisualCueContracts.CueOpacityVariableName);
            var loudWeight = ReadStyleVariable(loud, TpsVisualCueContracts.CueWeightVariableName);
            var stressWeight = ReadStyleVariable(stress, TpsVisualCueContracts.CueWeightVariableName);
            var energyWeight = ReadStyleVariable(energy, TpsVisualCueContracts.CueWeightVariableName);
            var melodyWeight = ReadStyleVariable(melody, TpsVisualCueContracts.CueWeightVariableName);
            var energyLevel = ReadStyleVariable(energy, TpsVisualCueContracts.EnergyVariableName);
            var melodyLevel = ReadStyleVariable(melody, TpsVisualCueContracts.MelodyVariableName);
            var buildingFirstProgress = ReadStyleVariable(buildingFirst, TpsVisualCueContracts.CueBuildProgressVariableName);
            var buildingLastProgress = ReadStyleVariable(buildingLast, TpsVisualCueContracts.CueBuildProgressVariableName);
            var buildingFirstWeight = ReadStyleVariable(buildingFirst, TpsVisualCueContracts.CueWeightVariableName);
            var buildingLastWeight = ReadStyleVariable(buildingLast, TpsVisualCueContracts.CueWeightVariableName);

            Assert.True(whisperOpacity < softOpacity, $"Expected whisper opacity < soft opacity, got {whisperOpacity} and {softOpacity}.");
            Assert.True(softOpacity < 1d, $"Expected soft opacity below 1, got {softOpacity}.");
            Assert.True(loudWeight >= 800d, $"Expected loud weight to be strong, got {loudWeight}.");
            Assert.True(stressWeight >= 820d, $"Expected stress weight to be strong, got {stressWeight}.");
            Assert.True(energyWeight > 700d, $"Expected energy weight to rise, got {energyWeight}.");
            Assert.True(melodyWeight > 640d, $"Expected melody weight to rise, got {melodyWeight}.");
            Assert.Equal(0.778d, energyLevel);
            Assert.Equal(0.333d, melodyLevel);
            Assert.True(buildingLastProgress > buildingFirstProgress, $"Expected building progress to increase, got {buildingFirstProgress} then {buildingLastProgress}.");
            Assert.True(buildingLastWeight > buildingFirstWeight, $"Expected building weight to increase, got {buildingFirstWeight} then {buildingLastWeight}.");
        });
    }

    private static AngleSharp.Dom.IElement FindReaderWordByText(IRenderedComponent<TeleprompterPage> cut, int cardIndex, string text) =>
        cut.FindByTestId(UiTestIds.Teleprompter.CardText(cardIndex))
            .QuerySelectorAll(BunitTestSelectors.BuildTestIdPrefixSelector(UiTestIds.Teleprompter.CardWordPrefix(cardIndex)))
            .Single(element => string.Equals(element.TextContent.Trim(), text, StringComparison.Ordinal));

    private static AngleSharp.Dom.IElement FindBreathCue(IRenderedComponent<TeleprompterPage> cut, int cardIndex) =>
        cut.FindByTestId(UiTestIds.Teleprompter.CardText(cardIndex))
            .QuerySelector($"[{TpsVisualCueContracts.BreathAttributeName}='{TpsVisualCueContracts.BreathAttributeValue}']")
        ?? throw new InvalidOperationException("Expected reader breath cue to render.");

    private static double ReadStyleVariable(AngleSharp.Dom.IElement element, string variableName)
    {
        var style = element.GetAttribute("style") ?? string.Empty;
        var segments = style.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var segment in segments)
        {
            var parts = segment.Split(':', 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 2 && string.Equals(parts[0], variableName, StringComparison.Ordinal))
            {
                return double.Parse(parts[1], CultureInfo.InvariantCulture);
            }
        }

        return 1d;
    }
}
