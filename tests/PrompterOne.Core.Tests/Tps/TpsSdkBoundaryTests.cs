using PrompterOne.Core.Services;

namespace PrompterOne.Core.Tests;

public sealed class TpsSdkBoundaryTests
{
    [Fact]
    public void CoreAssembly_DoesNotShipLocalTpsSpecShadowType()
    {
        var shadowType = typeof(ScriptCompiler).Assembly.GetType("PrompterOne.Core.Services.TpsSpec", throwOnError: false);

        Assert.Null(shadowType);
    }
}
