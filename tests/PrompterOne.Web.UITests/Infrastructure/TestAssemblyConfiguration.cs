using PrompterOne.Testing;

namespace PrompterOne.Web.UITests;

public sealed class MaxParallelTestsForPipeline : EnvironmentAwareParallelLimitBase
{
}

internal static class UiTestParallelization
{
    public const string EditorAuthoringConstraintKey = nameof(EditorAuthoringConstraintKey);
    public const string EditorPerformanceConstraintKey = nameof(EditorPerformanceConstraintKey);
}
