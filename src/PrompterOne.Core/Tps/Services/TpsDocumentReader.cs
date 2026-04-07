using ManagedCode.Tps;
using PrompterOne.Core.Models.Tps;

namespace PrompterOne.Core.Services;

public sealed class TpsDocumentReader
{
    public async Task<TpsDocument> ReadFileAsync(string filePath)
    {
        var content = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
        return Read(content);
    }

    public Task<TpsDocument> ReadAsync(string tpsContent) =>
        Task.FromResult(Read(tpsContent));

    public TpsDocument Read(string tpsContent)
    {
        var normalizedText = TpsSourceNormalizer.NormalizeLineEndings(tpsContent);
        if (string.IsNullOrWhiteSpace(normalizedText))
        {
            return new TpsDocument { RawSource = string.Empty };
        }

        var result = TpsRuntime.Parse(normalizedText);
        var document = TpsSdkMapper.ToLocalDocument(result.Document);
        document.RawSource = normalizedText;
        return document;
    }
}
