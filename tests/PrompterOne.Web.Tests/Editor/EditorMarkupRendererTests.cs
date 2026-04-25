using System.Net;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Rendering;

namespace PrompterOne.Web.Tests;

public sealed class EditorMarkupRendererTests
{
    [Test]
    public void Render_RendersNestedSpeedAndStressTagsInDesignOrder()
    {
        var markup = EditorMarkupRenderer.Render("But first, / let's address the [xslow][stress]elephant[/stress] in the room[/xslow].").Value;

        Assert.Contains("<span class=\"mk-tag\">[xslow]</span>", markup);
        Assert.Contains("<span class=\"mk-tag\">[stress]</span>", markup);
        Assert.Contains(
            $"<span class=\"mk-stress mk-xslow\" {TpsVisualCueContracts.SpeedAttributeName}=\"{TpsVisualCueContracts.SpeedCueXslow}\" {TpsVisualCueContracts.StressAttributeName}=\"{TpsVisualCueContracts.StressAttributeValue}\">elephant</span>",
            markup,
            StringComparison.Ordinal);
        Assert.Contains("<span class=\"mk-tag\">[/stress]</span>", markup);
        Assert.Contains(
            $"<span class=\"mk-xslow\" {TpsVisualCueContracts.SpeedAttributeName}=\"{TpsVisualCueContracts.SpeedCueXslow}\"> in the room</span>",
            markup,
            StringComparison.Ordinal);
        Assert.Contains("<span class=\"mk-tag\">[/xslow]</span>", markup);
    }

    [Test]
    public void Render_RendersPronunciationAndWpmBadges()
    {
        var markup = EditorMarkupRenderer.Render("[180WPM]Join us in building the future of [pronunciation:TELE-promp-ter]teleprompter[/pronunciation] technology.[/180WPM]").Value;

        Assert.Contains("<span class=\"mk-tag\">[180WPM]</span><span class=\"mk-wpm-badge\">180WPM</span>", markup);
        Assert.Contains("<span class=\"mk-tag\">[pronunciation:TELE-promp-ter]</span>", markup);
        Assert.Contains("<span class=\"mk-phonetic\">TELE-promp-ter</span> <span class=\"mk-phonetic-word\">teleprompter</span>", markup);
        Assert.Contains("technology.<span class=\"mk-tag\">[/180WPM]</span>", markup);
    }

    [Test]
    public void Render_RendersEmotionMarkerAttributeBesideEmotionCue()
    {
        var markup = EditorMarkupRenderer.Render("Open with [warm]confidence[/warm] today.").Value;

        Assert.Contains(
            $"<span class=\"mk-emo-warm\" {TpsVisualCueContracts.EmotionAttributeName}=\"warm\" {UiDataAttributes.Editor.EmotionMarker}=\"{WebUtility.HtmlEncode(TpsEmotionMarkers.ResolveMarker("warm"))}\">confidence</span>",
            markup,
            StringComparison.Ordinal);
    }

    [Test]
    public void Render_RendersPauseBreathAndEditPointMarkers()
    {
        var markup = EditorMarkupRenderer.Render("Good morning //\n\n[breath]\n\n[pause:2s]\n\n[edit_point:high]").Value;

        Assert.Contains("<span class=\"mk-pause\">//</span>", markup);
        Assert.Contains(
            $"<br><br><span class=\"mk-breath\" {TpsVisualCueContracts.BreathAttributeName}=\"{TpsVisualCueContracts.BreathAttributeValue}\">[breath]</span><br><br>",
            markup,
            StringComparison.Ordinal);
        Assert.Contains("<span class=\"mk-special\">[pause:2s]</span>", markup);
        Assert.Contains("<span class=\"mk-edit\">[edit_point:high]</span>", markup);
    }
}
