namespace PrompterOne.Testing;

/// <summary>
/// Resolves shared test-environment markers such as CI-hosted execution.
/// </summary>
public static class TestEnvironment
{
    private const string AzurePipelinesEnv = "TF_BUILD";
    private const string CiEnv = "CI";
    private const string GitHubActionsEnv = "GITHUB_ACTIONS";
    private const string NumericTrueValue = "1";

    public static bool IsCiEnvironment =>
        IsEnabled(Environment.GetEnvironmentVariable(CiEnv))
        || IsEnabled(Environment.GetEnvironmentVariable(AzurePipelinesEnv))
        || IsEnabled(Environment.GetEnvironmentVariable(GitHubActionsEnv));

    private static bool IsEnabled(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return string.Equals(value, bool.TrueString, StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, NumericTrueValue, StringComparison.OrdinalIgnoreCase);
    }
}
