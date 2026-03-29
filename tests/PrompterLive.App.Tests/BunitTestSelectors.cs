using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace PrompterLive.App.Tests;

internal static class BunitTestSelectors
{
    private const string DataTestIdAttributeName = "data-testid";
    private const string SelectorOpen = "[";
    private const string AttributeValuePrefix = "='";
    private const string AttributeValueSuffix = "']";

    internal static IElement FindByTestId<TComponent>(this IRenderedComponent<TComponent> cut, string testId)
        where TComponent : IComponent =>
        cut.Find(BuildTestIdSelector(testId));

    internal static string BuildTestIdSelector(string testId) =>
        string.Concat(
            SelectorOpen,
            DataTestIdAttributeName,
            AttributeValuePrefix,
            testId,
            AttributeValueSuffix);
}
