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
}
