namespace NameMatch.Infrastructure.DataEnrichment;

/// <summary>
/// Analyzes names for sound characteristics like syllable count, ending sound, and sound type.
/// </summary>
public static class SoundAnalyzer
{
    // Vowels for syllable counting
    private static readonly HashSet<char> Vowels = new() { 'a', 'e', 'i', 'o', 'u', 'y' };

    // Soft ending sounds (gentle, flowing)
    private static readonly HashSet<string> SoftEndings = new()
    {
        "a", "ah", "ia", "ya", "na", "la", "ra",
        "ie", "ee", "ey", "y", "ie", "leigh",
        "el", "le", "elle",
        "en", "an", "in",
        "er", "or",
        "lyn", "lin", "ine",
        "lia", "mia", "nia", "ria", "sia", "tia", "via"
    };

    // Strong ending sounds (bold, powerful)
    private static readonly HashSet<string> StrongEndings = new()
    {
        "ck", "k", "x",
        "t", "tt", "d", "dd",
        "p", "b",
        "g", "gg",
        "th", "ch", "sh",
        "on", "ton", "don", "son",
        "ax", "ex", "ix", "ox",
        "ard", "art", "ert", "ort"
    };

    /// <summary>
    /// Counts syllables in a name using vowel group detection.
    /// </summary>
    public static int CountSyllables(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return 0;

        name = name.ToLowerInvariant();
        var syllables = 0;
        var previousWasVowel = false;

        foreach (var c in name)
        {
            var isVowel = Vowels.Contains(c);
            if (isVowel && !previousWasVowel)
            {
                syllables++;
            }
            previousWasVowel = isVowel;
        }

        // Handle silent 'e' at the end (e.g., "Kate" = 1 syllable, not 2)
        if (name.Length > 2 && name.EndsWith('e') && !Vowels.Contains(name[^2]))
        {
            syllables = Math.Max(1, syllables - 1);
        }

        // Every name has at least 1 syllable
        return Math.Max(1, syllables);
    }

    /// <summary>
    /// Gets the ending sound pattern of a name (last 1-3 characters).
    /// </summary>
    public static string GetEndingSound(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length < 1)
            return "";

        name = name.ToLowerInvariant();

        // Try to get a meaningful ending (2-3 chars preferred)
        if (name.Length >= 3)
        {
            var last3 = name[^3..];
            var last2 = name[^2..];

            // Check for common 3-char endings
            if (last3 is "lyn" or "lin" or "ton" or "son" or "ley" or "lee" or "ney" or "nie" or "lia" or "mia" or "nia" or "ria" or "sia" or "tia" or "via" or "ell" or "ard" or "art")
                return last3;

            // Return 2-char ending
            return last2;
        }

        return name.Length >= 2 ? name[^2..] : name[^1..];
    }

    /// <summary>
    /// Classifies sound type as "soft", "strong", or "neutral".
    /// </summary>
    public static string ClassifySoundType(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "neutral";

        name = name.ToLowerInvariant();

        // Check various ending lengths
        for (var len = Math.Min(4, name.Length); len >= 1; len--)
        {
            var ending = name[^len..];
            if (SoftEndings.Contains(ending))
                return "soft";
            if (StrongEndings.Contains(ending))
                return "strong";
        }

        // Check last character
        var lastChar = name[^1];

        // Vowel endings are typically soft
        if (Vowels.Contains(lastChar))
            return "soft";

        // These consonants at the end sound strong
        if (lastChar is 'k' or 't' or 'd' or 'p' or 'b' or 'g' or 'x')
            return "strong";

        // These consonants at the end sound soft
        if (lastChar is 'l' or 'm' or 'n' or 's' or 'h')
            return "soft";

        return "neutral";
    }

    /// <summary>
    /// Gets the length category based on syllable count.
    /// </summary>
    public static string GetLengthCategory(int syllableCount)
    {
        return syllableCount <= 2 ? "SHORT" : "LONG";
    }

    /// <summary>
    /// Gets the sound category code based on sound type.
    /// </summary>
    public static string? GetSoundCategory(string soundType)
    {
        return soundType.ToUpperInvariant() switch
        {
            "SOFT" => "SOFT",
            "STRONG" => "STRONG",
            _ => null
        };
    }

    /// <summary>
    /// Performs full sound analysis on a name.
    /// </summary>
    public static SoundAnalysis Analyze(string name)
    {
        var syllableCount = CountSyllables(name);
        var endingSound = GetEndingSound(name);
        var soundType = ClassifySoundType(name);

        return new SoundAnalysis
        {
            SyllableCount = syllableCount,
            EndingSound = endingSound,
            SoundType = soundType,
            LengthCategory = GetLengthCategory(syllableCount),
            SoundCategory = GetSoundCategory(soundType)
        };
    }
}

public class SoundAnalysis
{
    public int SyllableCount { get; init; }
    public string EndingSound { get; init; } = "";
    public string SoundType { get; init; } = "neutral";
    public string LengthCategory { get; init; } = "SHORT";
    public string? SoundCategory { get; init; }
}
