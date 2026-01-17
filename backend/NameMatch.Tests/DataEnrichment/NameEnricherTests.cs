using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NameMatch.Domain.Entities;
using NameMatch.Domain.Enums;
using NameMatch.Infrastructure.DataEnrichment;
using NameMatch.Tests.Helpers;

namespace NameMatch.Tests.DataEnrichment;

public class NameEnricherTests
{
    [Fact]
    public async Task EnrichAllNamesAsync_EnrichesNamesWithSoundCategories()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var logger = new Mock<ILogger<NameEnricher>>();

        // Seed categories
        var shortCategory = new NameCategory { Code = "SHORT", DisplayName = "Short", CategoryType = "SOUND", DisplayOrder = 1 };
        var longCategory = new NameCategory { Code = "LONG", DisplayName = "Long", CategoryType = "SOUND", DisplayOrder = 2 };
        var softCategory = new NameCategory { Code = "SOFT", DisplayName = "Soft", CategoryType = "SOUND", DisplayOrder = 3 };
        var strongCategory = new NameCategory { Code = "STRONG", DisplayName = "Strong", CategoryType = "SOUND", DisplayOrder = 4 };
        context.NameCategories.AddRange(shortCategory, longCategory, softCategory, strongCategory);

        // Add test names
        var emma = new Name { NameText = "Emma", Gender = Gender.Female, PopularityScore = 90 };
        var jack = new Name { NameText = "Jack", Gender = Gender.Male, PopularityScore = 85 };
        var elizabeth = new Name { NameText = "Elizabeth", Gender = Gender.Female, PopularityScore = 75 };
        context.Names.AddRange(emma, jack, elizabeth);
        await context.SaveChangesAsync();

        var enricher = new NameEnricher(context, logger.Object);

        // Act
        var result = await enricher.EnrichAllNamesAsync();

        // Assert
        result.NamesProcessed.Should().Be(3);
        result.MappingsCreated.Should().BeGreaterThan(0);

        // Emma should be SHORT and SOFT
        var emmaReloaded = context.Names.First(n => n.NameText == "Emma");
        emmaReloaded.SyllableCount.Should().Be(2);
        emmaReloaded.SoundType.Should().Be("soft");
        emmaReloaded.CategoryMappings.Should().Contain(m => m.CategoryId == shortCategory.Id);
        emmaReloaded.CategoryMappings.Should().Contain(m => m.CategoryId == softCategory.Id);

        // Jack should be SHORT and STRONG
        var jackReloaded = context.Names.First(n => n.NameText == "Jack");
        jackReloaded.SyllableCount.Should().Be(1);
        jackReloaded.SoundType.Should().Be("strong");
        jackReloaded.CategoryMappings.Should().Contain(m => m.CategoryId == shortCategory.Id);
        jackReloaded.CategoryMappings.Should().Contain(m => m.CategoryId == strongCategory.Id);

        // Elizabeth should be LONG
        var elizabethReloaded = context.Names.First(n => n.NameText == "Elizabeth");
        elizabethReloaded.SyllableCount.Should().Be(4);
        elizabethReloaded.CategoryMappings.Should().Contain(m => m.CategoryId == longCategory.Id);
    }

    [Fact]
    public async Task EnrichAllNamesAsync_EnrichesNamesWithStyleCategories()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var logger = new Mock<ILogger<NameEnricher>>();

        // Seed style categories
        var classicCategory = new NameCategory { Code = "CLASSIC", DisplayName = "Classic", CategoryType = "STYLE", DisplayOrder = 1 };
        var biblicalCategory = new NameCategory { Code = "BIBLICAL", DisplayName = "Biblical", CategoryType = "STYLE", DisplayOrder = 2 };
        var natureCategory = new NameCategory { Code = "NATURE", DisplayName = "Nature", CategoryType = "STYLE", DisplayOrder = 3 };
        var shortCategory = new NameCategory { Code = "SHORT", DisplayName = "Short", CategoryType = "SOUND", DisplayOrder = 10 };
        context.NameCategories.AddRange(classicCategory, biblicalCategory, natureCategory, shortCategory);

        // Add test names
        var william = new Name { NameText = "William", Gender = Gender.Male, PopularityScore = 90 }; // Classic
        var david = new Name { NameText = "David", Gender = Gender.Male, PopularityScore = 85 };     // Biblical + Classic
        var rose = new Name { NameText = "Rose", Gender = Gender.Female, PopularityScore = 75 };     // Nature
        context.Names.AddRange(william, david, rose);
        await context.SaveChangesAsync();

        var enricher = new NameEnricher(context, logger.Object);

        // Act
        var result = await enricher.EnrichAllNamesAsync();

        // Assert
        result.NamesProcessed.Should().Be(3);

        // William should be CLASSIC
        var williamMappings = context.Names.First(n => n.NameText == "William").CategoryMappings;
        williamMappings.Should().Contain(m => m.CategoryId == classicCategory.Id);

        // David should be BIBLICAL (and possibly CLASSIC)
        var davidMappings = context.Names.First(n => n.NameText == "David").CategoryMappings;
        davidMappings.Should().Contain(m => m.CategoryId == biblicalCategory.Id);

        // Rose should be NATURE
        var roseMappings = context.Names.First(n => n.NameText == "Rose").CategoryMappings;
        roseMappings.Should().Contain(m => m.CategoryId == natureCategory.Id);
    }

    [Fact]
    public async Task EnrichAllNamesAsync_SkipsAlreadyEnrichedNames()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var logger = new Mock<ILogger<NameEnricher>>();

        var shortCategory = new NameCategory { Code = "SHORT", DisplayName = "Short", CategoryType = "SOUND", DisplayOrder = 1 };
        context.NameCategories.Add(shortCategory);

        var emma = new Name { NameText = "Emma", Gender = Gender.Female, PopularityScore = 90 };
        context.Names.Add(emma);
        await context.SaveChangesAsync();

        // Pre-enrich Emma
        context.NameCategoryMappings.Add(new NameCategoryMapping
        {
            NameId = emma.Id,
            CategoryId = shortCategory.Id,
            Confidence = 1.0f
        });
        await context.SaveChangesAsync();

        var enricher = new NameEnricher(context, logger.Object);

        // Act
        var result = await enricher.EnrichAllNamesAsync(forceReenrich: false);

        // Assert - Should skip already enriched names
        result.NamesProcessed.Should().Be(0);
        result.MappingsCreated.Should().Be(0);
    }

    [Fact]
    public async Task EnrichAllNamesAsync_ForceReenrichOverwrites()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var logger = new Mock<ILogger<NameEnricher>>();

        var shortCategory = new NameCategory { Code = "SHORT", DisplayName = "Short", CategoryType = "SOUND", DisplayOrder = 1 };
        var softCategory = new NameCategory { Code = "SOFT", DisplayName = "Soft", CategoryType = "SOUND", DisplayOrder = 2 };
        context.NameCategories.AddRange(shortCategory, softCategory);

        var emma = new Name { NameText = "Emma", Gender = Gender.Female, PopularityScore = 90 };
        context.Names.Add(emma);
        await context.SaveChangesAsync();

        // Pre-enrich Emma with just SHORT
        context.NameCategoryMappings.Add(new NameCategoryMapping
        {
            NameId = emma.Id,
            CategoryId = shortCategory.Id,
            Confidence = 0.5f // Low confidence
        });
        await context.SaveChangesAsync();

        var enricher = new NameEnricher(context, logger.Object);

        // Act - Force re-enrich
        var result = await enricher.EnrichAllNamesAsync(forceReenrich: true);

        // Assert - Should re-enrich
        result.NamesProcessed.Should().Be(1);
        result.MappingsCreated.Should().BeGreaterThan(0);

        // Emma should now have SOFT as well
        var emmaMappings = context.Names.First(n => n.NameText == "Emma").CategoryMappings;
        emmaMappings.Should().Contain(m => m.CategoryId == softCategory.Id);
    }

    [Fact]
    public async Task EnrichAllNamesAsync_DetectsTrendyNames()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var logger = new Mock<ILogger<NameEnricher>>();

        var trendyCategory = new NameCategory { Code = "TRENDY", DisplayName = "Trendy", CategoryType = "STYLE", DisplayOrder = 1 };
        var shortCategory = new NameCategory { Code = "SHORT", DisplayName = "Short", CategoryType = "SOUND", DisplayOrder = 10 };
        context.NameCategories.AddRange(trendyCategory, shortCategory);

        // High popularity + not classic = trendy
        var jaxon = new Name { NameText = "Jaxon", Gender = Gender.Male, PopularityScore = 85 };
        context.Names.Add(jaxon);
        await context.SaveChangesAsync();

        var enricher = new NameEnricher(context, logger.Object);

        // Act
        var result = await enricher.EnrichAllNamesAsync();

        // Assert
        var jaxonMappings = context.Names.First(n => n.NameText == "Jaxon").CategoryMappings;
        jaxonMappings.Should().Contain(m => m.CategoryId == trendyCategory.Id);
    }

    [Fact]
    public async Task EnrichAllNamesAsync_DetectsUniqueNames()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var logger = new Mock<ILogger<NameEnricher>>();

        var uniqueCategory = new NameCategory { Code = "UNIQUE", DisplayName = "Unique", CategoryType = "STYLE", DisplayOrder = 1 };
        var shortCategory = new NameCategory { Code = "SHORT", DisplayName = "Short", CategoryType = "SOUND", DisplayOrder = 10 };
        context.NameCategories.AddRange(uniqueCategory, shortCategory);

        // Low popularity = unique
        var zephyr = new Name { NameText = "Zephyr", Gender = Gender.Male, PopularityScore = 10 };
        context.Names.Add(zephyr);
        await context.SaveChangesAsync();

        var enricher = new NameEnricher(context, logger.Object);

        // Act
        var result = await enricher.EnrichAllNamesAsync();

        // Assert
        var zephyrMappings = context.Names.First(n => n.NameText == "Zephyr").CategoryMappings;
        zephyrMappings.Should().Contain(m => m.CategoryId == uniqueCategory.Id);
    }

    [Fact]
    public async Task EnrichAllNamesAsync_ReturnsZeroWhenNoCategories()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var logger = new Mock<ILogger<NameEnricher>>();

        // No categories seeded
        var emma = new Name { NameText = "Emma", Gender = Gender.Female, PopularityScore = 90 };
        context.Names.Add(emma);
        await context.SaveChangesAsync();

        var enricher = new NameEnricher(context, logger.Object);

        // Act
        var result = await enricher.EnrichAllNamesAsync();

        // Assert - Should return early with no work done
        result.NamesProcessed.Should().Be(0);
        result.MappingsCreated.Should().Be(0);
    }
}
