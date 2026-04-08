using PrompterOne.Testing;

namespace PrompterOne.Web.UITests;

public sealed class MaxParallelTestsForPipeline : EnvironmentAwareParallelLimitBase
{
    protected override int LocalLimit { get; } = 15;
}

internal static class UiTestParallelization
{
    public const string EditorAuthoringConstraintKey = nameof(EditorAuthoringConstraintKey);
    public const string EditorPerformanceConstraintKey = nameof(EditorPerformanceConstraintKey);
}
