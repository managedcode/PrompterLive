using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Rendering;

namespace PrompterOne.App.Tests;

public sealed class EditorCueRenderingTests
{
    private const string BuildingAttribute = TpsVisualCueContracts.DeliveryAttributeName + "=\"" + TpsVisualCueContracts.DeliveryModeBuilding + "\"";
    private const string LoudAttribute = TpsVisualCueContracts.VolumeAttributeName + "=\"" + TpsVisualCueContracts.VolumeLoud + "\"";
    private const string SoftAttribute = TpsVisualCueContracts.VolumeAttributeName + "=\"" + TpsVisualCueContracts.VolumeSoft + "\"";
    private const string StressAttribute = TpsVisualCueContracts.StressAttributeName + "=\"" + TpsVisualCueContracts.StressAttributeValue + "\"";
    private const string XslowAttribute = TpsVisualCueContracts.SpeedAttributeName + "=\"" + TpsVisualCueContracts.SpeedCueXslow + "\"";

    [Fact]
    public void Render_EmitsStableCueAttributesForVolumeDeliveryStressAndSpeed()
    {
        var markup = EditorMarkupRenderer.Render(
            """
            [loud][building]Rise carefully[/building][/loud] and [soft][xslow]listen[stress]ing[/stress][/xslow][/soft].
            """).Value;

        Assert.Contains(LoudAttribute, markup, StringComparison.Ordinal);
        Assert.Contains(BuildingAttribute, markup, StringComparison.Ordinal);
        Assert.Contains(SoftAttribute, markup, StringComparison.Ordinal);
        Assert.Contains(StressAttribute, markup, StringComparison.Ordinal);
        Assert.Contains(XslowAttribute, markup, StringComparison.Ordinal);
    }
}
