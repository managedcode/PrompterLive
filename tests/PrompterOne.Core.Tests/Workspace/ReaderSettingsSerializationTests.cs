using System.Text.Json;
using PrompterOne.Core.Models.Workspace;

namespace PrompterOne.Core.Tests;

public sealed class ReaderSettingsSerializationTests
{
    private const string LegacyReaderSettingsJson =
        """
        {
          "CountdownSeconds": 3,
          "FontScale": 1.11,
          "TextWidth": 0.8182,
          "ScrollSpeed": 1,
          "MirrorText": false,
          "ShowFocusLine": true,
          "ShowProgress": true,
          "ShowCameraScene": true
        }
        """;

    [Test]
    public void ReaderSettings_DeserializesLegacyPayload_WithDefaultFocalPointPercent()
    {
        var settings = JsonSerializer.Deserialize<ReaderSettings>(LegacyReaderSettingsJson);

        Assert.NotNull(settings);
        Assert.Equal(ReaderSettingsDefaults.FocalPointPercent, settings.FocalPointPercent);
        Assert.Equal(ReaderSettingsDefaults.TextAlignment, settings.TextAlignment);
        Assert.Equal(ReaderSettingsDefaults.MirrorVertical, settings.MirrorVertical);
        Assert.Equal(ReaderSettingsDefaults.TextOrientation, settings.TextOrientation);
        Assert.Equal(1.11d, settings.FontScale);
        Assert.Equal(0.8182d, settings.TextWidth);
    }
}
