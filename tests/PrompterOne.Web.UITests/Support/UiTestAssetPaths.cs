namespace PrompterOne.Web.UITests;

internal static class UiTestAssetPaths
{
    private const string EditorFileSaveHarnessFileName = "editor-file-save-harness.js";
    private const string EditorFolderName = "Editor";
    private const string EditorProjectFolderName = "PrompterOne.Web.UITests.Editor";
    private const string MediaFolderName = "Media";
    private const string RecordingFileHarnessFileName = "recording-file-harness.js";
    private const string SolutionFileName = "PrompterOne.slnx";
    private const string SolutionRootNotFoundMessage = "Could not locate the PrompterOne solution root from the UI test base directory.";
    private const string SyntheticMediaHarnessFileName = "synthetic-media-harness.js";
    private const string TestsFolderName = "tests";
    private const string UiTestsBaseProjectFolderName = "PrompterOne.Web.UITests";

    public static string GetEditorFileSaveHarnessScriptPath() =>
        ResolveRepositoryRelativePath(TestsFolderName, EditorProjectFolderName, EditorFolderName, EditorFileSaveHarnessFileName);

    public static string GetRecordingFileHarnessScriptPath() =>
        ResolveRepositoryRelativePath(TestsFolderName, UiTestsBaseProjectFolderName, MediaFolderName, RecordingFileHarnessFileName);

    public static string GetSyntheticMediaHarnessScriptPath() =>
        ResolveRepositoryRelativePath(TestsFolderName, UiTestsBaseProjectFolderName, MediaFolderName, SyntheticMediaHarnessFileName);

    private static string ResolveRepositoryRelativePath(params string[] segments)
    {
        var solutionRoot = FindSolutionRoot();
        return Path.GetFullPath(Path.Combine(solutionRoot, Path.Combine(segments)));
    }

    private static string FindSolutionRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(SolutionRootNotFoundMessage);
    }
}
