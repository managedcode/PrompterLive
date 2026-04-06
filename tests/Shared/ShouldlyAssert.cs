using System.Diagnostics.CodeAnalysis;
using Shouldly;

namespace PrompterOne.Testing;

internal static class ShouldlyAssert
{
    public static void Equal<T>(T expected, T actual)
        => actual.ShouldBe(expected);

    public static void Equal<T>(IEnumerable<T> expected, IEnumerable<T> actual, IEqualityComparer<T> comparer)
        => actual.SequenceEqual(expected, comparer).ShouldBeTrue();

    public static void Equal(double expected, double actual, int precision)
        => Math.Round(actual, precision).ShouldBe(Math.Round(expected, precision));

    public static void Equal(decimal expected, decimal actual, int precision)
        => Math.Round(actual, precision).ShouldBe(Math.Round(expected, precision));

    public static void True(bool actual)
        => actual.ShouldBeTrue();

    public static void True(bool actual, string customMessage)
        => actual.ShouldBeTrue(customMessage);

    public static void False(bool actual)
        => actual.ShouldBeFalse();

    public static T NotNull<T>([NotNull] T? actual)
        where T : class
    {
        actual.ShouldNotBeNull();
        return actual;
    }

    public static void Null(object? actual)
        => actual.ShouldBeNull();

    public static void Empty<T>(IEnumerable<T> actual)
        => actual.ShouldBeEmpty();

    public static void NotEmpty<T>(IEnumerable<T> actual)
        => actual.ShouldNotBeEmpty();

    public static T Single<T>(IEnumerable<T> actual)
    {
        var materialized = actual.ToList();
        materialized.Count.ShouldBe(1);
        return materialized[0];
    }

    public static T Single<T>(IEnumerable<T> actual, Func<T, bool> predicate)
    {
        var materialized = actual.Where(predicate).ToList();
        materialized.Count.ShouldBe(1);
        return materialized[0];
    }

    public static void Contains(string expectedSubstring, string? actualString)
    {
        actualString.ShouldNotBeNull();
        actualString.ShouldContain(expectedSubstring);
    }

    public static void Contains(string expectedSubstring, string? actualString, StringComparison comparison)
    {
        actualString.ShouldNotBeNull();
        actualString.IndexOf(expectedSubstring, comparison).ShouldNotBe(-1);
    }

    public static void Contains<T>(T expected, IEnumerable<T> actual)
        => actual.ShouldContain(expected);

    public static void Contains<T>(T expected, IEnumerable<T> actual, IEqualityComparer<T> comparer)
        => actual.Any(item => comparer.Equals(item, expected)).ShouldBeTrue();

    public static void Contains<T>(IEnumerable<T> actual, Func<T, bool> predicate)
        => actual.Any(item => predicate(item)).ShouldBeTrue();

    public static void DoesNotContain(string unexpectedSubstring, string? actualString)
    {
        actualString.ShouldNotBeNull();
        actualString.ShouldNotContain(unexpectedSubstring);
    }

    public static void DoesNotContain(string unexpectedSubstring, string? actualString, StringComparison comparison)
    {
        actualString.ShouldNotBeNull();
        actualString.IndexOf(unexpectedSubstring, comparison).ShouldBe(-1);
    }

    public static void DoesNotContain<T>(T unexpected, IEnumerable<T> actual)
        => actual.ShouldNotContain(unexpected);

    public static void DoesNotContain<T>(T unexpected, IEnumerable<T> actual, IEqualityComparer<T> comparer)
        => actual.Any(item => comparer.Equals(item, unexpected)).ShouldBeFalse();

    public static void DoesNotContain<T>(IEnumerable<T> actual, Func<T, bool> predicate)
        => actual.Any(item => predicate(item)).ShouldBeFalse();

    public static void StartsWith(string expectedPrefix, string? actualString, StringComparison comparison)
    {
        actualString.ShouldNotBeNull();
        actualString.StartsWith(expectedPrefix, comparison).ShouldBeTrue();
    }

    public static void EndsWith(string expectedSuffix, string? actualString, StringComparison comparison)
    {
        actualString.ShouldNotBeNull();
        actualString.EndsWith(expectedSuffix, comparison).ShouldBeTrue();
    }

    public static T IsType<T>(object? actual)
        => actual.ShouldBeOfType<T>();

    public static TException Throws<TException>(Action action)
        where TException : Exception
        => Should.Throw<TException>(action);

    public static void Collection<T>(IEnumerable<T> actual, params Action<T>[] inspectors)
    {
        var materialized = actual.ToList();
        materialized.Count.ShouldBe(inspectors.Length);

        for (var index = 0; index < inspectors.Length; index++)
        {
            inspectors[index](materialized[index]);
        }
    }

    public static void All<T>(IEnumerable<T> actual, Action<T> inspector)
    {
        foreach (var item in actual)
        {
            inspector(item);
        }
    }

    public static void InRange<T>(T actual, T low, T high)
        where T : IComparable<T>
    {
        (actual.CompareTo(low) >= 0).ShouldBeTrue();
        (actual.CompareTo(high) <= 0).ShouldBeTrue();
    }
}
