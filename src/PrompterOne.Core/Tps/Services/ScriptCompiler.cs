using ManagedCode.Tps;
using PrompterOne.Core.Models.CompiledScript;
using PrompterOne.Core.Models.Tps;

namespace PrompterOne.Core.Services;

public sealed class ScriptCompiler(TpsExporter exporter)
{
    private readonly TpsExporter _exporter = exporter;

    public ScriptCompiler()
        : this(new TpsExporter())
    {
    }

    public async Task<CompiledScript> CompileAsync(TpsDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var source = !string.IsNullOrWhiteSpace(document.RawSource)
            ? document.RawSource
            : await _exporter.ExportAsync(document).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(source))
        {
            return new CompiledScript
            {
                Metadata = new Dictionary<string, string>(document.Metadata, StringComparer.OrdinalIgnoreCase)
            };
        }

        var result = TpsRuntime.Compile(source);
        return TpsSdkMapper.ToLocalCompiledScript(result.Script);
    }
}
