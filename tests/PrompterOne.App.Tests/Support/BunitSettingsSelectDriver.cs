using Bunit;
using Microsoft.AspNetCore.Components;
using PrompterOne.Shared.Contracts;

namespace PrompterOne.App.Tests;

internal static class BunitSettingsSelectDriver
{
    private const string ValueAttributeName = "value";

    internal static void SelectSettingsOption<TComponent>(
        this IRenderedComponent<TComponent> cut,
        string triggerTestId,
        string optionValue)
        where TComponent : IComponent
    {
        cut.FindByTestId(triggerTestId).Click();
        cut.FindByTestId(UiTestIds.Settings.SelectOption(triggerTestId, optionValue)).Click();
    }

    internal static string? GetSettingsSelectValue<TComponent>(
        this IRenderedComponent<TComponent> cut,
        string triggerTestId)
        where TComponent : IComponent =>
        cut.FindByTestId(triggerTestId).GetAttribute(ValueAttributeName);
}
