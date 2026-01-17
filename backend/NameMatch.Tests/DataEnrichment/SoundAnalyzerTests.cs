using FluentAssertions;
using NameMatch.Infrastructure.DataEnrichment;

namespace NameMatch.Tests.DataEnrichment;

public class SoundAnalyzerTests
{
    [Theory]
    [InlineData("Kate", 1)]
    [InlineData("Emma", 2)]
    [InlineData("Olivia", 3)]  // O-liv-ia
    [InlineData("Elizabeth", 4)]
    [InlineData("Ann", 1)]
    [InlineData("Jo", 1)]
    [InlineData("Alexander", 4)]
    [InlineData("Ava", 2)]
    public void CountSyllables_ReturnsCorrectCount(string name, int expected)
    {
        var result = SoundAnalyzer.CountSyllables(name);
        result.Should().Be(expected);
    }

    [Fact]
    public void CountSyllables_HandlesEmptyString()
    {
        SoundAnalyzer.CountSyllables("").Should().Be(0);
        SoundAnalyzer.CountSyllables(null!).Should().Be(0);
    }

    [Theory]
    [InlineData("Emma", "ma")]
    [InlineData("William", "am")]
    [InlineData("Sophia", "ia")]
    [InlineData("Lyn", "lyn")]  // 3-char name returns full ending
    [InlineData("Brooklyn", "lyn")]
    public void GetEndingSound_ReturnsCorrectEnding(string name, string expected)
    {
        var result = SoundAnalyzer.GetEndingSound(name);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Emma", "soft")]      // Ends in vowel
    [InlineData("Sophia", "soft")]    // Ends in -ia
    [InlineData("William", "soft")]   // Ends in -m
    [InlineData("Jack", "strong")]    // Ends in -ck
    [InlineData("Robert", "strong")]  // Ends in -t
    [InlineData("David", "strong")]   // Ends in -d
    public void ClassifySoundType_ReturnsCorrectType(string name, string expected)
    {
        var result = SoundAnalyzer.ClassifySoundType(name);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, "SHORT")]
    [InlineData(2, "SHORT")]
    [InlineData(3, "LONG")]
    [InlineData(4, "LONG")]
    public void GetLengthCategory_ReturnsCorrectCategory(int syllables, string expected)
    {
        var result = SoundAnalyzer.GetLengthCategory(syllables);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("soft", "SOFT")]
    [InlineData("strong", "STRONG")]
    [InlineData("neutral", null)]
    public void GetSoundCategory_ReturnsCorrectCategory(string soundType, string? expected)
    {
        var result = SoundAnalyzer.GetSoundCategory(soundType);
        result.Should().Be(expected);
    }

    [Fact]
    public void Analyze_ReturnsCompleteAnalysis()
    {
        var result = SoundAnalyzer.Analyze("Elizabeth");

        result.SyllableCount.Should().Be(4);
        result.EndingSound.Should().Be("th");
        result.SoundType.Should().Be("strong"); // th ending is strong
        result.LengthCategory.Should().Be("LONG");
        result.SoundCategory.Should().Be("STRONG");
    }

    [Fact]
    public void Analyze_ShortStrongName()
    {
        var result = SoundAnalyzer.Analyze("Jack");

        result.SyllableCount.Should().Be(1);
        result.EndingSound.Should().Be("ck");
        result.SoundType.Should().Be("strong");
        result.LengthCategory.Should().Be("SHORT");
        result.SoundCategory.Should().Be("STRONG");
    }

    [Fact]
    public void Analyze_MediumSoftName()
    {
        var result = SoundAnalyzer.Analyze("Lily");

        result.SyllableCount.Should().Be(2);
        result.EndingSound.Should().Be("ly");
        result.SoundType.Should().Be("soft"); // -y ending
        result.LengthCategory.Should().Be("SHORT");
        result.SoundCategory.Should().Be("SOFT");
    }
}
