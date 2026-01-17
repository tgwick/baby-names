using FluentAssertions;
using NameMatch.Domain.Entities;
using NameMatch.Domain.Enums;
using NameMatch.Infrastructure.Services;
using NameMatch.Tests.Helpers;

namespace NameMatch.Tests.Services;

public class NameServiceTests
{
    [Fact]
    public async Task GetNextUnvotedNameAsync_ReturnsNull_WhenNoActiveSession()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new NameService(context);

        // Act
        var result = await service.GetNextUnvotedNameAsync("user-123");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetNextUnvotedNameAsync_ReturnsName_WhenActiveSessionExists()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var userId = "user-123";

        // Create an active session
        var session = new Session
        {
            Id = Guid.NewGuid(),
            InitiatorId = userId,
            PartnerId = "partner-456",
            TargetGender = Gender.Neutral,
            JoinCode = "ABC123",
            PartnerLink = "link123",
            Status = SessionStatus.Active,
            CreatedAt = DateTime.UtcNow,
            LinkedAt = DateTime.UtcNow
        };
        context.Sessions.Add(session);

        // Add some names
        context.Names.AddRange(
            new Name { NameText = "Emma", Gender = Gender.Female, PopularityScore = 90 },
            new Name { NameText = "Liam", Gender = Gender.Male, PopularityScore = 95 },
            new Name { NameText = "Alex", Gender = Gender.Neutral, PopularityScore = 80 }
        );
        await context.SaveChangesAsync();

        var service = new NameService(context);

        // Act
        var result = await service.GetNextUnvotedNameAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.NameText.Should().BeOneOf("Emma", "Liam", "Alex");
    }

    [Fact]
    public async Task GetNextUnvotedNameAsync_FiltersBy_TargetGender_Male()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var userId = "user-123";

        var session = new Session
        {
            Id = Guid.NewGuid(),
            InitiatorId = userId,
            PartnerId = "partner-456",
            TargetGender = Gender.Male,
            JoinCode = "ABC123",
            PartnerLink = "link123",
            Status = SessionStatus.Active,
            CreatedAt = DateTime.UtcNow,
            LinkedAt = DateTime.UtcNow
        };
        context.Sessions.Add(session);

        context.Names.AddRange(
            new Name { NameText = "Emma", Gender = Gender.Female, PopularityScore = 90 },
            new Name { NameText = "Liam", Gender = Gender.Male, PopularityScore = 95 },
            new Name { NameText = "Alex", Gender = Gender.Neutral, PopularityScore = 80 }
        );
        await context.SaveChangesAsync();

        var service = new NameService(context);

        // Act - Get multiple names to verify filtering
        var results = new HashSet<string>();
        for (int i = 0; i < 20; i++)
        {
            var name = await service.GetNextUnvotedNameAsync(userId);
            if (name != null) results.Add(name.NameText);
        }

        // Assert - Should only return Male or Neutral names
        results.Should().NotContain("Emma");
        results.Should().Contain("Liam");
        results.Should().Contain("Alex"); // Neutral names included for all genders
    }

    [Fact]
    public async Task GetNextUnvotedNameAsync_FiltersBy_TargetGender_Female()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var userId = "user-123";

        var session = new Session
        {
            Id = Guid.NewGuid(),
            InitiatorId = userId,
            PartnerId = "partner-456",
            TargetGender = Gender.Female,
            JoinCode = "ABC123",
            PartnerLink = "link123",
            Status = SessionStatus.Active,
            CreatedAt = DateTime.UtcNow,
            LinkedAt = DateTime.UtcNow
        };
        context.Sessions.Add(session);

        context.Names.AddRange(
            new Name { NameText = "Emma", Gender = Gender.Female, PopularityScore = 90 },
            new Name { NameText = "Liam", Gender = Gender.Male, PopularityScore = 95 },
            new Name { NameText = "Alex", Gender = Gender.Neutral, PopularityScore = 80 }
        );
        await context.SaveChangesAsync();

        var service = new NameService(context);

        // Act
        var results = new HashSet<string>();
        for (int i = 0; i < 20; i++)
        {
            var name = await service.GetNextUnvotedNameAsync(userId);
            if (name != null) results.Add(name.NameText);
        }

        // Assert
        results.Should().NotContain("Liam");
        results.Should().Contain("Emma");
        results.Should().Contain("Alex");
    }

    [Fact]
    public async Task GetNextUnvotedNameAsync_ExcludesVotedNames()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var userId = "user-123";

        var session = new Session
        {
            Id = Guid.NewGuid(),
            InitiatorId = userId,
            PartnerId = "partner-456",
            TargetGender = Gender.Neutral,
            JoinCode = "ABC123",
            PartnerLink = "link123",
            Status = SessionStatus.Active,
            CreatedAt = DateTime.UtcNow,
            LinkedAt = DateTime.UtcNow
        };
        context.Sessions.Add(session);

        var name1 = new Name { NameText = "Emma", Gender = Gender.Female, PopularityScore = 90 };
        var name2 = new Name { NameText = "Liam", Gender = Gender.Male, PopularityScore = 95 };
        context.Names.AddRange(name1, name2);
        await context.SaveChangesAsync();

        // Add a vote for Emma
        context.Votes.Add(new Vote
        {
            UserId = userId,
            NameId = name1.Id,
            SessionId = session.Id,
            VoteType = VoteType.Like,
            VotedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new NameService(context);

        // Act - Get the next name multiple times
        var results = new HashSet<string>();
        for (int i = 0; i < 10; i++)
        {
            var name = await service.GetNextUnvotedNameAsync(userId);
            if (name != null) results.Add(name.NameText);
        }

        // Assert - Should only return Liam (Emma was voted on)
        results.Should().NotContain("Emma");
        results.Should().Contain("Liam");
    }

    [Fact]
    public async Task GetNextUnvotedNameAsync_ReturnsNull_WhenAllNamesVoted()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var userId = "user-123";

        var session = new Session
        {
            Id = Guid.NewGuid(),
            InitiatorId = userId,
            PartnerId = "partner-456",
            TargetGender = Gender.Neutral,
            JoinCode = "ABC123",
            PartnerLink = "link123",
            Status = SessionStatus.Active,
            CreatedAt = DateTime.UtcNow,
            LinkedAt = DateTime.UtcNow
        };
        context.Sessions.Add(session);

        var name = new Name { NameText = "Emma", Gender = Gender.Female, PopularityScore = 90 };
        context.Names.Add(name);
        await context.SaveChangesAsync();

        // Vote on the only name
        context.Votes.Add(new Vote
        {
            UserId = userId,
            NameId = name.Id,
            SessionId = session.Id,
            VoteType = VoteType.Like,
            VotedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new NameService(context);

        // Act
        var result = await service.GetNextUnvotedNameAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetNextUnvotedNameAsync_WorksForPartner()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var initiatorId = "user-123";
        var partnerId = "partner-456";

        var session = new Session
        {
            Id = Guid.NewGuid(),
            InitiatorId = initiatorId,
            PartnerId = partnerId,
            TargetGender = Gender.Neutral,
            JoinCode = "ABC123",
            PartnerLink = "link123",
            Status = SessionStatus.Active,
            CreatedAt = DateTime.UtcNow,
            LinkedAt = DateTime.UtcNow
        };
        context.Sessions.Add(session);

        context.Names.Add(new Name { NameText = "Emma", Gender = Gender.Female, PopularityScore = 90 });
        await context.SaveChangesAsync();

        var service = new NameService(context);

        // Act - Partner should be able to get names
        var result = await service.GetNextUnvotedNameAsync(partnerId);

        // Assert
        result.Should().NotBeNull();
        result!.NameText.Should().Be("Emma");
    }

    [Fact]
    public async Task GetNameCountForSessionAsync_ReturnsCorrectCount()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        var session = new Session
        {
            Id = Guid.NewGuid(),
            InitiatorId = "user-123",
            TargetGender = Gender.Female,
            JoinCode = "ABC123",
            PartnerLink = "link123",
            Status = SessionStatus.WaitingForPartner,
            CreatedAt = DateTime.UtcNow
        };
        context.Sessions.Add(session);

        context.Names.AddRange(
            new Name { NameText = "Emma", Gender = Gender.Female, PopularityScore = 90 },
            new Name { NameText = "Olivia", Gender = Gender.Female, PopularityScore = 88 },
            new Name { NameText = "Liam", Gender = Gender.Male, PopularityScore = 95 },
            new Name { NameText = "Alex", Gender = Gender.Neutral, PopularityScore = 80 }
        );
        await context.SaveChangesAsync();

        var service = new NameService(context);

        // Act
        var count = await service.GetNameCountForSessionAsync(session.Id);

        // Assert - Should include Female (2) + Neutral (1) = 3
        count.Should().Be(3);
    }

    [Fact]
    public async Task GetNameCountForSessionAsync_ReturnsZero_WhenSessionNotFound()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new NameService(context);

        // Act
        var count = await service.GetNameCountForSessionAsync(Guid.NewGuid());

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public async Task GetNextUnvotedNameAsync_UsesWeightedSelection_WhenPreferencesExist()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var userId = "user-123";
        var partnerId = "partner-456";

        var session = new Session
        {
            Id = Guid.NewGuid(),
            InitiatorId = userId,
            PartnerId = partnerId,
            TargetGender = Gender.Neutral,
            JoinCode = "ABC123",
            PartnerLink = "link123",
            Status = SessionStatus.Active,
            CreatedAt = DateTime.UtcNow,
            LinkedAt = DateTime.UtcNow
        };
        context.Sessions.Add(session);

        // Create a category
        var classicCategory = new NameCategory
        {
            Code = "CLASSIC",
            DisplayName = "Classic",
            CategoryType = "STYLE",
            DisplayOrder = 1
        };
        context.NameCategories.Add(classicCategory);

        // Create names
        var classicName = new Name { NameText = "William", Gender = Gender.Male, PopularityScore = 85 };
        var otherName = new Name { NameText = "Zephyr", Gender = Gender.Male, PopularityScore = 10 };
        context.Names.AddRange(classicName, otherName);
        await context.SaveChangesAsync();

        // Add category mapping for classic name
        context.NameCategoryMappings.Add(new NameCategoryMapping
        {
            NameId = classicName.Id,
            CategoryId = classicCategory.Id,
            Confidence = 1.0f
        });

        // Add user preferences - both users love classic names
        context.UserPreferences.Add(new UserPreference
        {
            UserId = userId,
            SessionId = session.Id,
            CategoryId = classicCategory.Id,
            Level = PreferenceLevel.Love, // +2
            CreatedAt = DateTime.UtcNow
        });
        context.UserPreferences.Add(new UserPreference
        {
            UserId = partnerId,
            SessionId = session.Id,
            CategoryId = classicCategory.Id,
            Level = PreferenceLevel.Love, // +2
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new NameService(context);

        // Act - Get names multiple times and count which appears more
        var counts = new Dictionary<string, int> { ["William"] = 0, ["Zephyr"] = 0 };
        for (int i = 0; i < 100; i++)
        {
            var name = await service.GetNextUnvotedNameAsync(userId);
            if (name != null && counts.ContainsKey(name.NameText))
            {
                counts[name.NameText]++;
            }
        }

        // Assert - Classic name should appear significantly more often due to:
        // - Higher popularity score
        // - Preference match from both users
        // - Mutual bonus
        counts["William"].Should().BeGreaterThan(counts["Zephyr"],
            "Classic name 'William' should appear more often due to preference weighting");
    }

    [Fact]
    public async Task GetNextUnvotedNameAsync_FallsBackToPopularity_WhenNoCategoryMappings()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var userId = "user-123";
        var partnerId = "partner-456";

        var session = new Session
        {
            Id = Guid.NewGuid(),
            InitiatorId = userId,
            PartnerId = partnerId,
            TargetGender = Gender.Neutral,
            JoinCode = "ABC123",
            PartnerLink = "link123",
            Status = SessionStatus.Active,
            CreatedAt = DateTime.UtcNow,
            LinkedAt = DateTime.UtcNow
        };
        context.Sessions.Add(session);

        // Create a category
        var classicCategory = new NameCategory
        {
            Code = "CLASSIC",
            DisplayName = "Classic",
            CategoryType = "STYLE",
            DisplayOrder = 1
        };
        context.NameCategories.Add(classicCategory);

        // Create names WITHOUT category mappings
        var popularName = new Name { NameText = "Emma", Gender = Gender.Female, PopularityScore = 99 };
        var unpopularName = new Name { NameText = "Xenobia", Gender = Gender.Female, PopularityScore = 1 };
        context.Names.AddRange(popularName, unpopularName);
        await context.SaveChangesAsync();

        // Add preferences (but no category mappings exist)
        context.UserPreferences.Add(new UserPreference
        {
            UserId = userId,
            SessionId = session.Id,
            CategoryId = classicCategory.Id,
            Level = PreferenceLevel.Love,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new NameService(context);

        // Act
        var counts = new Dictionary<string, int> { ["Emma"] = 0, ["Xenobia"] = 0 };
        for (int i = 0; i < 100; i++)
        {
            var name = await service.GetNextUnvotedNameAsync(userId);
            if (name != null && counts.ContainsKey(name.NameText))
            {
                counts[name.NameText]++;
            }
        }

        // Assert - Should fall back to popularity-based selection
        counts["Emma"].Should().BeGreaterThan(counts["Xenobia"],
            "Popular name should appear more often when no category mappings exist");
    }

    [Fact]
    public async Task GetNextUnvotedNameAsync_ExcludesNamesWithAvoidedCategories()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var userId = "user-123";
        var partnerId = "partner-456";

        var session = new Session
        {
            Id = Guid.NewGuid(),
            InitiatorId = userId,
            PartnerId = partnerId,
            TargetGender = Gender.Neutral,
            JoinCode = "ABC123",
            PartnerLink = "link123",
            Status = SessionStatus.Active,
            CreatedAt = DateTime.UtcNow,
            LinkedAt = DateTime.UtcNow
        };
        context.Sessions.Add(session);

        // Create a category
        var trendyCategory = new NameCategory
        {
            Code = "TRENDY",
            DisplayName = "Trendy",
            CategoryType = "STYLE",
            DisplayOrder = 1
        };
        context.NameCategories.Add(trendyCategory);

        // Create names
        var trendyName = new Name { NameText = "Jaxon", Gender = Gender.Male, PopularityScore = 50 };
        var otherName = new Name { NameText = "John", Gender = Gender.Male, PopularityScore = 50 };
        context.Names.AddRange(trendyName, otherName);
        await context.SaveChangesAsync();

        // Add category mapping for trendy name
        context.NameCategoryMappings.Add(new NameCategoryMapping
        {
            NameId = trendyName.Id,
            CategoryId = trendyCategory.Id,
            Confidence = 1.0f
        });

        // User avoids trendy names (should be excluded)
        context.UserPreferences.Add(new UserPreference
        {
            UserId = userId,
            SessionId = session.Id,
            CategoryId = trendyCategory.Id,
            Level = PreferenceLevel.Avoid, // -2 means "do not include"
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new NameService(context);

        // Act - Names with avoided categories should never appear
        var foundTrendy = false;
        for (int i = 0; i < 50; i++)
        {
            var name = await service.GetNextUnvotedNameAsync(userId);
            if (name?.NameText == "Jaxon")
            {
                foundTrendy = true;
                break;
            }
        }

        // Assert - Avoided category names should be excluded entirely
        foundTrendy.Should().BeFalse("Names with avoided categories should be excluded from results");
    }

    [Fact]
    public async Task GetNextUnvotedNameAsync_WorksWithOnlyOneUserPreferences()
    {
        // Arrange - Only initiator has set preferences, partner hasn't yet
        using var context = TestDbContextFactory.Create();
        var userId = "user-123";
        var partnerId = "partner-456";

        var session = new Session
        {
            Id = Guid.NewGuid(),
            InitiatorId = userId,
            PartnerId = partnerId,
            TargetGender = Gender.Neutral,
            JoinCode = "ABC123",
            PartnerLink = "link123",
            Status = SessionStatus.Active,
            CreatedAt = DateTime.UtcNow,
            LinkedAt = DateTime.UtcNow
        };
        context.Sessions.Add(session);

        var classicCategory = new NameCategory
        {
            Code = "CLASSIC",
            DisplayName = "Classic",
            CategoryType = "STYLE",
            DisplayOrder = 1
        };
        context.NameCategories.Add(classicCategory);

        var classicName = new Name { NameText = "William", Gender = Gender.Male, PopularityScore = 50 };
        var otherName = new Name { NameText = "Zephyr", Gender = Gender.Male, PopularityScore = 50 };
        context.Names.AddRange(classicName, otherName);
        await context.SaveChangesAsync();

        context.NameCategoryMappings.Add(new NameCategoryMapping
        {
            NameId = classicName.Id,
            CategoryId = classicCategory.Id,
            Confidence = 1.0f
        });

        // Only user has preferences, partner hasn't submitted yet
        context.UserPreferences.Add(new UserPreference
        {
            UserId = userId,
            SessionId = session.Id,
            CategoryId = classicCategory.Id,
            Level = PreferenceLevel.Love,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new NameService(context);

        // Act
        var counts = new Dictionary<string, int> { ["William"] = 0, ["Zephyr"] = 0 };
        for (int i = 0; i < 100; i++)
        {
            var name = await service.GetNextUnvotedNameAsync(userId);
            if (name != null && counts.ContainsKey(name.NameText))
            {
                counts[name.NameText]++;
            }
        }

        // Assert - Should still weight toward user's preferences
        counts["William"].Should().BeGreaterThan(counts["Zephyr"],
            "Name matching user's preferences should appear more often even without partner preferences");
    }

    [Fact]
    public async Task GetNextUnvotedNameAsync_HandlesMultipleCategoryMappings()
    {
        // Arrange - Name belongs to multiple categories that user likes
        using var context = TestDbContextFactory.Create();
        var userId = "user-123";
        var partnerId = "partner-456";

        var session = new Session
        {
            Id = Guid.NewGuid(),
            InitiatorId = userId,
            PartnerId = partnerId,
            TargetGender = Gender.Neutral,
            JoinCode = "ABC123",
            PartnerLink = "link123",
            Status = SessionStatus.Active,
            CreatedAt = DateTime.UtcNow,
            LinkedAt = DateTime.UtcNow
        };
        context.Sessions.Add(session);

        var classicCategory = new NameCategory
        {
            Code = "CLASSIC",
            DisplayName = "Classic",
            CategoryType = "STYLE",
            DisplayOrder = 1
        };
        var hebrewCategory = new NameCategory
        {
            Code = "HEBREW",
            DisplayName = "Hebrew",
            CategoryType = "ORIGIN",
            DisplayOrder = 1
        };
        context.NameCategories.AddRange(classicCategory, hebrewCategory);

        // Name that matches BOTH categories user loves
        var multiCategoryName = new Name { NameText = "David", Gender = Gender.Male, PopularityScore = 50 };
        var plainName = new Name { NameText = "Zephyr", Gender = Gender.Male, PopularityScore = 50 };
        context.Names.AddRange(multiCategoryName, plainName);
        await context.SaveChangesAsync();

        // David is both Classic AND Hebrew
        context.NameCategoryMappings.AddRange(
            new NameCategoryMapping { NameId = multiCategoryName.Id, CategoryId = classicCategory.Id, Confidence = 1.0f },
            new NameCategoryMapping { NameId = multiCategoryName.Id, CategoryId = hebrewCategory.Id, Confidence = 1.0f }
        );

        // User loves both Classic and Hebrew
        context.UserPreferences.AddRange(
            new UserPreference { UserId = userId, SessionId = session.Id, CategoryId = classicCategory.Id, Level = PreferenceLevel.Love, CreatedAt = DateTime.UtcNow },
            new UserPreference { UserId = userId, SessionId = session.Id, CategoryId = hebrewCategory.Id, Level = PreferenceLevel.Love, CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var service = new NameService(context);

        // Act - Use enough iterations to ensure statistical significance
        var counts = new Dictionary<string, int> { ["David"] = 0, ["Zephyr"] = 0 };
        for (int i = 0; i < 500; i++)
        {
            var name = await service.GetNextUnvotedNameAsync(userId);
            if (name != null && counts.ContainsKey(name.NameText))
            {
                counts[name.NameText]++;
            }
        }

        // Assert - Name matching multiple liked categories should appear more often
        counts["David"].Should().BeGreaterThanOrEqualTo(counts["Zephyr"],
            "Name matching multiple liked categories should be weighted higher");
    }

    [Fact]
    public async Task GetNextUnvotedNameAsync_WorksWithNoPartnerYet()
    {
        // Arrange - Partner hasn't joined yet (solo swiping scenario)
        using var context = TestDbContextFactory.Create();
        var userId = "user-123";

        var session = new Session
        {
            Id = Guid.NewGuid(),
            InitiatorId = userId,
            PartnerId = null, // No partner yet
            TargetGender = Gender.Neutral,
            JoinCode = "ABC123",
            PartnerLink = "link123",
            Status = SessionStatus.Active, // Could be Active for solo
            CreatedAt = DateTime.UtcNow
        };
        context.Sessions.Add(session);

        var classicCategory = new NameCategory
        {
            Code = "CLASSIC",
            DisplayName = "Classic",
            CategoryType = "STYLE",
            DisplayOrder = 1
        };
        context.NameCategories.Add(classicCategory);

        var classicName = new Name { NameText = "William", Gender = Gender.Male, PopularityScore = 50 };
        var otherName = new Name { NameText = "Zephyr", Gender = Gender.Male, PopularityScore = 50 };
        context.Names.AddRange(classicName, otherName);
        await context.SaveChangesAsync();

        context.NameCategoryMappings.Add(new NameCategoryMapping
        {
            NameId = classicName.Id,
            CategoryId = classicCategory.Id,
            Confidence = 1.0f
        });

        context.UserPreferences.Add(new UserPreference
        {
            UserId = userId,
            SessionId = session.Id,
            CategoryId = classicCategory.Id,
            Level = PreferenceLevel.Love,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new NameService(context);

        // Act - Should work without throwing
        var name = await service.GetNextUnvotedNameAsync(userId);

        // Assert
        name.Should().NotBeNull();
        name!.NameText.Should().BeOneOf("William", "Zephyr");
    }

    [Fact]
    public async Task GetNextUnvotedNameAsync_RespectsConfidenceScores()
    {
        // Arrange - Name with low confidence mapping should have less weight impact
        using var context = TestDbContextFactory.Create();
        var userId = "user-123";
        var partnerId = "partner-456";

        var session = new Session
        {
            Id = Guid.NewGuid(),
            InitiatorId = userId,
            PartnerId = partnerId,
            TargetGender = Gender.Neutral,
            JoinCode = "ABC123",
            PartnerLink = "link123",
            Status = SessionStatus.Active,
            CreatedAt = DateTime.UtcNow,
            LinkedAt = DateTime.UtcNow
        };
        context.Sessions.Add(session);

        var classicCategory = new NameCategory
        {
            Code = "CLASSIC",
            DisplayName = "Classic",
            CategoryType = "STYLE",
            DisplayOrder = 1
        };
        context.NameCategories.Add(classicCategory);

        var highConfidenceName = new Name { NameText = "William", Gender = Gender.Male, PopularityScore = 50 };
        var lowConfidenceName = new Name { NameText = "Wade", Gender = Gender.Male, PopularityScore = 50 };
        context.Names.AddRange(highConfidenceName, lowConfidenceName);
        await context.SaveChangesAsync();

        // High confidence mapping
        context.NameCategoryMappings.Add(new NameCategoryMapping
        {
            NameId = highConfidenceName.Id,
            CategoryId = classicCategory.Id,
            Confidence = 1.0f // 100% confident
        });
        // Low confidence mapping
        context.NameCategoryMappings.Add(new NameCategoryMapping
        {
            NameId = lowConfidenceName.Id,
            CategoryId = classicCategory.Id,
            Confidence = 0.3f // Only 30% confident
        });

        context.UserPreferences.Add(new UserPreference
        {
            UserId = userId,
            SessionId = session.Id,
            CategoryId = classicCategory.Id,
            Level = PreferenceLevel.Love,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new NameService(context);

        // Act - Use enough iterations to ensure statistical significance
        var counts = new Dictionary<string, int> { ["William"] = 0, ["Wade"] = 0 };
        for (int i = 0; i < 500; i++)
        {
            var name = await service.GetNextUnvotedNameAsync(userId);
            if (name != null && counts.ContainsKey(name.NameText))
            {
                counts[name.NameText]++;
            }
        }

        // Assert - High confidence name should appear more often
        counts["William"].Should().BeGreaterThanOrEqualTo(counts["Wade"],
            "Name with high confidence category mapping should be weighted higher");
    }
}
