namespace PrompterOne.Web.Tests;

public sealed class LibraryFixtureContractTests
{
    private static readonly string RepoRoot = ResolveRepoRoot();
    private static readonly string RootDesignFolder = Path.Combine(RepoRoot, "design");
    private static readonly string[] FixtureRoots =
    [
        Path.Combine(RepoRoot, "src", "PrompterOne.Shared", "Library", "SeedData"),
        Path.Combine(RepoRoot, "tests", "TestData", "Scripts")
    ];

    private static readonly string[] ForbiddenDisplayMetricKeys =
    [
        "display_word_count:",
        "display_segment_count:",
        "display_wpm:"
    ];

    [Test]
    public void RootPrototypeDesignFolder_IsAbsent()
    {
        Assert.False(Directory.Exists(RootDesignFolder));
    }

    [Test]
    public void RepoOwnedTpsFixtures_DoNotContainDisplayMetricOverrides()
    {
        var offenders = FixtureRoots
            .SelectMany(root => Directory.EnumerateFiles(root, "*.tps", SearchOption.TopDirectoryOnly))
            .Select(file => new
            {
                File = file,
                Keys = ForbiddenDisplayMetricKeys
                    .Where(key => File.ReadAllText(file).Contains(key, StringComparison.Ordinal))
                    .ToArray()
            })
            .Where(result => result.Keys.Length > 0)
            .Select(result => $"{Path.GetRelativePath(RepoRoot, result.File)} => {string.Join(", ", result.Keys)}")
            .ToArray();

        Assert.True(offenders.Length == 0, string.Join(Environment.NewLine, offenders));
    }

    private static string ResolveRepoRoot() =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
}
