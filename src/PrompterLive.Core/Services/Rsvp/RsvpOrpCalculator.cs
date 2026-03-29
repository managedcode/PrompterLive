namespace PrompterLive.Core.Services.Rsvp;

/// <summary>
/// Calculates Optimal Recognition Point (ORP) for words
/// Based on Spritz methodology for rapid reading
/// </summary>
public class RsvpOrpCalculator
{
    /// <summary>
    /// Calculates the Optimal Recognition Point (ORP) index for a word
    /// Based on Milestone 5 spec: 30% for 1-5 chars, 35% for 6-9, 40% for 10+
    /// </summary>
    /// <param name="word">The word to calculate ORP for</param>
    /// <returns>Zero-based index of the ORP character</returns>
    public int CalculateOrpIndex(string word)
    {
        if (string.IsNullOrEmpty(word))
        {
            return 0;
        }

        var cleanWord = word.TrimEnd('.', '!', '?', ',', ';', ':', '"', '\'', ')', ']', '}');
        var length = cleanWord.Length;

        if (length <= 1)
        {
            return 0;
        }

        // Milestone 5 spec: OrpCalculator: 30% position for 1-5 chars, 35% for 6-9, 40% for 10+
        var orpPosition = length switch
        {
            <= 5 => 0.30,   // 30% position
            <= 9 => 0.35,   // 35% position
            _ => 0.40        // 40% position for 10+ chars
        };

        var orpIndex = (int)Math.Floor(length * orpPosition);

        // Ensure within bounds
        return Math.Max(0, Math.Min(orpIndex, length - 1));
    }

    /// <summary>
    /// Splits a word into pre-ORP, ORP character, and post-ORP parts
    /// </summary>
    /// <param name="word">The word to split</param>
    /// <returns>Tuple of (preORP, orpChar, postORP)</returns>
    public (string PreORP, string OrpChar, string PostORP) SplitWordAtORP(string word)
    {
        if (string.IsNullOrEmpty(word))
        {
            return ("", "", "");
        }

        var orpIndex = CalculateOrpIndex(word);

        // Ensure ORP index is within bounds
        orpIndex = Math.Min(orpIndex, word.Length - 1);

        var preORP = orpIndex > 0 ? word.Substring(0, orpIndex) : "";
        var orpChar = word[orpIndex].ToString();
        var postORP = orpIndex < word.Length - 1 ? word.Substring(orpIndex + 1) : "";

        return (preORP, orpChar, postORP);
    }
}
