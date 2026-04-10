using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Localization;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class LibraryScreenOpenScriptFlowTests(StandaloneAppFixture fixture) : AppUiTestBase(fixture)
{
    private const string BodyOnlyFileName = "Quarterly Town Hall Notes.txt";
    private const string BodyOnlyTitle = "Quarterly Town Hall Notes";
    private const string BodyOnlyDocument =
        """
        ## [Quarterly Town Hall|140WPM|Professional]
        ### [Opening|140WPM|Warm]
        We need to align the whole team around the roadmap.
        """;

    private const string FirstImportFileName = "First Import Draft.txt";
    private const string FirstImportTitle = "First Import Draft";
    private const string FirstImportDocument =
        """
        ## [First Import|140WPM|Professional]
        ### [Opening|140WPM|Warm]
        First imported draft.
        """;

    private const string SecondImportBody =
        """
        ## [Second Import|140WPM|Professional]
        ### [Closing|140WPM|Focused]
        Second imported draft.
        """;

    private const string SecondImportFileName = "second-import.md.tps";
    private const string SecondImportTitle = "Second Import Story";
    private const string SecondImportDocument =
        """
        ---
        title: "Second Import Story"
        profile: Actor
        ---

        ## [Second Import|140WPM|Professional]
        ### [Closing|140WPM|Focused]
        Second imported draft.
        """;

    private const string UnsupportedImportFileName = "unsupported-script.exe";
    private const string UnsupportedImportText = "This should be rejected by the import descriptor.";

    [Test]
    public Task LibraryScreen_OpenScriptImportsBodyOnlyTextFile_UsingFileNameAsTitle() =>
        RunPageAsync(async page =>
        {
            var importPath = await CreateImportedScriptAsync(BodyOnlyFileName, BodyOnlyDocument);

            try
            {
                await ShellRouteDriver.OpenLibraryAsync(page);

                await page.GetByTestId(UiTestIds.Header.LibraryOpenScriptInput)
                    .SetInputFilesAsync(importPath);

                await EditorMonacoDriver.WaitUntilReadyAsync(page);
                await Expect(page.GetByTestId(UiTestIds.Header.Title)).ToHaveTextAsync(BodyOnlyTitle);
                await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput)).ToHaveValueAsync(BodyOnlyDocument);
            }
            finally
            {
                DeleteImportedScript(importPath);
            }
        });

    [Test]
    public Task LibraryScreen_OpenScriptRejectsUnsupportedFileExtension_AndKeepsUserOnLibrary() =>
        RunPageAsync(async page =>
        {
            var importPath = await CreateImportedScriptAsync(UnsupportedImportFileName, UnsupportedImportText);

            try
            {
                await ShellRouteDriver.OpenLibraryAsync(page);

                await page.GetByTestId(UiTestIds.Header.LibraryOpenScriptInput)
                    .SetInputFilesAsync(importPath);

                await ShellRouteDriver.WaitForLibraryReadyAsync(page);
                await Expect(page.GetByTestId(UiTestIds.Diagnostics.Banner)).ToBeVisibleAsync();
                await Expect(page.GetByTestId(UiTestIds.Diagnostics.Banner)).ToContainTextAsync(SharedUiText.Text(UiTextKey.ImportScriptMessage));
                await Expect(page.GetByTestId(UiTestIds.Diagnostics.Banner)).ToContainTextAsync(SharedUiText.Text(UiTextKey.ImportScriptUnsupportedDetail));

                await Assert.That(new Uri(page.Url).AbsolutePath).IsEqualTo(BrowserTestConstants.Routes.Library);
            }
            finally
            {
                DeleteImportedScript(importPath);
            }
        });

    [Test]
    public Task LibraryScreen_OpenScriptCanImportASecondFile_AfterPickerResets() =>
        RunPageAsync(async page =>
        {
            var firstImportPath = await CreateImportedScriptAsync(FirstImportFileName, FirstImportDocument);
            var secondImportPath = await CreateImportedScriptAsync(SecondImportFileName, SecondImportDocument);

            try
            {
                await ShellRouteDriver.OpenLibraryAsync(page);

                await page.GetByTestId(UiTestIds.Header.LibraryOpenScriptInput)
                    .SetInputFilesAsync(firstImportPath);

                await EditorMonacoDriver.WaitUntilReadyAsync(page);
                await Expect(page.GetByTestId(UiTestIds.Header.Title)).ToHaveTextAsync(FirstImportTitle);
                await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput)).ToHaveValueAsync(FirstImportDocument);

                await ShellRouteDriver.OpenLibraryAsync(page);

                await page.GetByTestId(UiTestIds.Header.LibraryOpenScriptInput)
                    .SetInputFilesAsync(secondImportPath);

                await EditorMonacoDriver.WaitUntilReadyAsync(page);
                await Expect(page.GetByTestId(UiTestIds.Header.Title)).ToHaveTextAsync(SecondImportTitle);
                await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput)).ToHaveValueAsync(SecondImportBody);
                await Assert.That(new Uri(page.Url).Query).Contains($"{AppRoutes.ScriptIdQueryKey}=");
            }
            finally
            {
                DeleteImportedScript(firstImportPath);
                DeleteImportedScript(secondImportPath);
            }
        });

    private static async Task<string> CreateImportedScriptAsync(string fileName, string contents)
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, fileName);
        await File.WriteAllTextAsync(path, contents);
        return path;
    }

    private static void DeleteImportedScript(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
