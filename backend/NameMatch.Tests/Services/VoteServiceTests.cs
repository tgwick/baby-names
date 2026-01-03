using FluentAssertions;
using NameMatch.Domain.Entities;
using NameMatch.Domain.Enums;
using NameMatch.Infrastructure.Services;
using NameMatch.Tests.Helpers;

namespace NameMatch.Tests.Services;

public class VoteServiceTests
{
    [Fact]
    public async Task SubmitVoteAsync_ThrowsException_WhenNoActiveSession()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new VoteService(context);

        // Act & Assert
        var act = async () => await service.SubmitVoteAsync("user-123", 1, VoteType.Like);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*active session*");
    }

    [Fact]
    public async Task SubmitVoteAsync_ThrowsException_WhenNameNotFound()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var userId = "user-123";

        var session = CreateActiveSession(userId, "partner-456");
        context.Sessions.Add(session);
        await context.SaveChangesAsync();

        var service = new VoteService(context);

        // Act & Assert
        var act = async () => await service.SubmitVoteAsync(userId, 9999, VoteType.Like);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Name not found*");
    }

    [Fact]
    public async Task SubmitVoteAsync_CreatesVote_WhenValid()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var userId = "user-123";

        var session = CreateActiveSession(userId, "partner-456");
        context.Sessions.Add(session);

        var name = new Name { NameText = "Emma", Gender = Gender.Female, PopularityScore = 90 };
        context.Names.Add(name);
        await context.SaveChangesAsync();

        var service = new VoteService(context);

        // Act
        var result = await service.SubmitVoteAsync(userId, name.Id, VoteType.Like);

        // Assert
        result.Should().NotBeNull();
        result.VoteId.Should().BeGreaterThan(0);
        result.IsMatch.Should().BeFalse(); // Partner hasn't voted yet
        result.Match.Should().BeNull();

        // Verify vote was saved
        context.Votes.Should().ContainSingle(v =>
            v.UserId == userId &&
            v.NameId == name.Id &&
            v.SessionId == session.Id &&
            v.VoteType == VoteType.Like);
    }

    [Fact]
    public async Task SubmitVoteAsync_UpdatesExistingVote_WhenVotingAgain()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var userId = "user-123";

        var session = CreateActiveSession(userId, "partner-456");
        context.Sessions.Add(session);

        var name = new Name { NameText = "Emma", Gender = Gender.Female, PopularityScore = 90 };
        context.Names.Add(name);
        await context.SaveChangesAsync();

        var service = new VoteService(context);

        // First vote - Like
        await service.SubmitVoteAsync(userId, name.Id, VoteType.Like);

        // Act - Change to Dislike
        var result = await service.SubmitVoteAsync(userId, name.Id, VoteType.Dislike);

        // Assert
        result.Should().NotBeNull();
        context.Votes.Should().ContainSingle(); // Still only one vote
        context.Votes.First().VoteType.Should().Be(VoteType.Dislike);
    }

    [Fact]
    public async Task SubmitVoteAsync_ReturnsMatch_WhenBothUsersLike()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var userId = "user-123";
        var partnerId = "partner-456";

        var session = CreateActiveSession(userId, partnerId);
        context.Sessions.Add(session);

        var name = new Name { NameText = "Emma", Gender = Gender.Female, PopularityScore = 90, Origin = "Germanic" };
        context.Names.Add(name);
        await context.SaveChangesAsync();

        // Partner votes first
        context.Votes.Add(new Vote
        {
            UserId = partnerId,
            NameId = name.Id,
            SessionId = session.Id,
            VoteType = VoteType.Like,
            VotedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new VoteService(context);

        // Act - User votes Like (should match)
        var result = await service.SubmitVoteAsync(userId, name.Id, VoteType.Like);

        // Assert
        result.IsMatch.Should().BeTrue();
        result.Match.Should().NotBeNull();
        result.Match!.NameId.Should().Be(name.Id);
        result.Match.NameText.Should().Be("Emma");
        result.Match.Gender.Should().Be(Gender.Female);
        result.Match.Origin.Should().Be("Germanic");
    }

    [Fact]
    public async Task SubmitVoteAsync_NoMatch_WhenPartnerDisliked()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var userId = "user-123";
        var partnerId = "partner-456";

        var session = CreateActiveSession(userId, partnerId);
        context.Sessions.Add(session);

        var name = new Name { NameText = "Emma", Gender = Gender.Female, PopularityScore = 90 };
        context.Names.Add(name);
        await context.SaveChangesAsync();

        // Partner votes Dislike
        context.Votes.Add(new Vote
        {
            UserId = partnerId,
            NameId = name.Id,
            SessionId = session.Id,
            VoteType = VoteType.Dislike,
            VotedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new VoteService(context);

        // Act - User votes Like
        var result = await service.SubmitVoteAsync(userId, name.Id, VoteType.Like);

        // Assert
        result.IsMatch.Should().BeFalse();
        result.Match.Should().BeNull();
    }

    [Fact]
    public async Task GetMatchesAsync_ReturnsEmpty_WhenNoSession()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new VoteService(context);

        // Act
        var matches = await service.GetMatchesAsync("user-123");

        // Assert
        matches.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMatchesAsync_ReturnsEmpty_WhenNoPartner()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var userId = "user-123";

        var session = new Session
        {
            Id = Guid.NewGuid(),
            InitiatorId = userId,
            PartnerId = null,
            TargetGender = Gender.Neutral,
            JoinCode = "ABC123",
            PartnerLink = "link123",
            Status = SessionStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        context.Sessions.Add(session);
        await context.SaveChangesAsync();

        var service = new VoteService(context);

        // Act
        var matches = await service.GetMatchesAsync(userId);

        // Assert
        matches.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMatchesAsync_ReturnsMatches_WhenBothUsersLiked()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var userId = "user-123";
        var partnerId = "partner-456";

        var session = CreateActiveSession(userId, partnerId);
        context.Sessions.Add(session);

        var name1 = new Name { NameText = "Emma", Gender = Gender.Female, PopularityScore = 90 };
        var name2 = new Name { NameText = "Liam", Gender = Gender.Male, PopularityScore = 95 };
        var name3 = new Name { NameText = "Olivia", Gender = Gender.Female, PopularityScore = 88 };
        context.Names.AddRange(name1, name2, name3);
        await context.SaveChangesAsync();

        // Both like Emma
        context.Votes.AddRange(
            new Vote { UserId = userId, NameId = name1.Id, SessionId = session.Id, VoteType = VoteType.Like, VotedAt = DateTime.UtcNow },
            new Vote { UserId = partnerId, NameId = name1.Id, SessionId = session.Id, VoteType = VoteType.Like, VotedAt = DateTime.UtcNow }
        );
        // Both like Liam
        context.Votes.AddRange(
            new Vote { UserId = userId, NameId = name2.Id, SessionId = session.Id, VoteType = VoteType.Like, VotedAt = DateTime.UtcNow },
            new Vote { UserId = partnerId, NameId = name2.Id, SessionId = session.Id, VoteType = VoteType.Like, VotedAt = DateTime.UtcNow }
        );
        // Only user likes Olivia
        context.Votes.Add(new Vote { UserId = userId, NameId = name3.Id, SessionId = session.Id, VoteType = VoteType.Like, VotedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var service = new VoteService(context);

        // Act
        var matches = (await service.GetMatchesAsync(userId)).ToList();

        // Assert
        matches.Should().HaveCount(2);
        matches.Select(m => m.NameText).Should().Contain("Emma");
        matches.Select(m => m.NameText).Should().Contain("Liam");
        matches.Select(m => m.NameText).Should().NotContain("Olivia");
    }

    [Fact]
    public async Task GetMatchCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var userId = "user-123";
        var partnerId = "partner-456";

        var session = CreateActiveSession(userId, partnerId);
        context.Sessions.Add(session);

        var name1 = new Name { NameText = "Emma", Gender = Gender.Female, PopularityScore = 90 };
        var name2 = new Name { NameText = "Liam", Gender = Gender.Male, PopularityScore = 95 };
        context.Names.AddRange(name1, name2);
        await context.SaveChangesAsync();

        // Both like Emma and Liam
        context.Votes.AddRange(
            new Vote { UserId = userId, NameId = name1.Id, SessionId = session.Id, VoteType = VoteType.Like, VotedAt = DateTime.UtcNow },
            new Vote { UserId = partnerId, NameId = name1.Id, SessionId = session.Id, VoteType = VoteType.Like, VotedAt = DateTime.UtcNow },
            new Vote { UserId = userId, NameId = name2.Id, SessionId = session.Id, VoteType = VoteType.Like, VotedAt = DateTime.UtcNow },
            new Vote { UserId = partnerId, NameId = name2.Id, SessionId = session.Id, VoteType = VoteType.Like, VotedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var service = new VoteService(context);

        // Act
        var count = await service.GetMatchCountAsync(userId);

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public async Task GetUserVotesAsync_ReturnsUserVotes()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var userId = "user-123";
        var partnerId = "partner-456";

        var session = CreateActiveSession(userId, partnerId);
        context.Sessions.Add(session);

        var name1 = new Name { NameText = "Emma", Gender = Gender.Female, PopularityScore = 90 };
        var name2 = new Name { NameText = "Liam", Gender = Gender.Male, PopularityScore = 95 };
        context.Names.AddRange(name1, name2);
        await context.SaveChangesAsync();

        context.Votes.AddRange(
            new Vote { UserId = userId, NameId = name1.Id, SessionId = session.Id, VoteType = VoteType.Like, VotedAt = DateTime.UtcNow.AddMinutes(-1) },
            new Vote { UserId = userId, NameId = name2.Id, SessionId = session.Id, VoteType = VoteType.Dislike, VotedAt = DateTime.UtcNow },
            new Vote { UserId = partnerId, NameId = name1.Id, SessionId = session.Id, VoteType = VoteType.Like, VotedAt = DateTime.UtcNow } // Partner's vote
        );
        await context.SaveChangesAsync();

        var service = new VoteService(context);

        // Act
        var votes = (await service.GetUserVotesAsync(userId)).ToList();

        // Assert
        votes.Should().HaveCount(2);
        votes.Should().OnlyContain(v => v.NameText == "Emma" || v.NameText == "Liam");
    }

    [Fact]
    public async Task GetVoteStatsAsync_ReturnsCorrectStats()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var userId = "user-123";
        var partnerId = "partner-456";

        var session = CreateActiveSession(userId, partnerId);
        context.Sessions.Add(session);

        var name1 = new Name { NameText = "Emma", Gender = Gender.Female, PopularityScore = 90 };
        var name2 = new Name { NameText = "Liam", Gender = Gender.Male, PopularityScore = 95 };
        var name3 = new Name { NameText = "Olivia", Gender = Gender.Female, PopularityScore = 88 };
        var name4 = new Name { NameText = "Noah", Gender = Gender.Male, PopularityScore = 92 };
        context.Names.AddRange(name1, name2, name3, name4);
        await context.SaveChangesAsync();

        // User: 2 likes, 1 dislike
        context.Votes.AddRange(
            new Vote { UserId = userId, NameId = name1.Id, SessionId = session.Id, VoteType = VoteType.Like, VotedAt = DateTime.UtcNow },
            new Vote { UserId = userId, NameId = name2.Id, SessionId = session.Id, VoteType = VoteType.Like, VotedAt = DateTime.UtcNow },
            new Vote { UserId = userId, NameId = name3.Id, SessionId = session.Id, VoteType = VoteType.Dislike, VotedAt = DateTime.UtcNow }
        );
        // Partner also likes Emma (1 match)
        context.Votes.Add(new Vote { UserId = partnerId, NameId = name1.Id, SessionId = session.Id, VoteType = VoteType.Like, VotedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var service = new VoteService(context);

        // Act
        var stats = await service.GetVoteStatsAsync(userId);

        // Assert
        stats.TotalVotes.Should().Be(3);
        stats.LikeCount.Should().Be(2);
        stats.DislikeCount.Should().Be(1);
        stats.MatchCount.Should().Be(1);
        stats.NamesRemaining.Should().Be(1); // 4 names - 3 voted = 1 remaining
    }

    [Fact]
    public async Task GetConflictsAsync_ReturnsEmpty_WhenNoSession()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new VoteService(context);

        // Act
        var conflicts = await service.GetConflictsAsync("user-123");

        // Assert
        conflicts.Should().BeEmpty();
    }

    [Fact]
    public async Task GetConflictsAsync_ReturnsConflicts_WhenUsersDisagree()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var userId = "user-123";
        var partnerId = "partner-456";

        var session = CreateActiveSession(userId, partnerId);
        context.Sessions.Add(session);

        var name1 = new Name { NameText = "Emma", Gender = Gender.Female, PopularityScore = 90, Origin = "Germanic" };
        var name2 = new Name { NameText = "Liam", Gender = Gender.Male, PopularityScore = 95 };
        context.Names.AddRange(name1, name2);
        await context.SaveChangesAsync();

        // User likes Emma, partner dislikes Emma
        context.Votes.AddRange(
            new Vote { UserId = userId, NameId = name1.Id, SessionId = session.Id, VoteType = VoteType.Like, VotedAt = DateTime.UtcNow },
            new Vote { UserId = partnerId, NameId = name1.Id, SessionId = session.Id, VoteType = VoteType.Dislike, VotedAt = DateTime.UtcNow }
        );
        // User dislikes Liam, partner likes Liam
        context.Votes.AddRange(
            new Vote { UserId = userId, NameId = name2.Id, SessionId = session.Id, VoteType = VoteType.Dislike, VotedAt = DateTime.UtcNow },
            new Vote { UserId = partnerId, NameId = name2.Id, SessionId = session.Id, VoteType = VoteType.Like, VotedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var service = new VoteService(context);

        // Act
        var conflicts = (await service.GetConflictsAsync(userId)).ToList();

        // Assert
        conflicts.Should().HaveCount(2);
        conflicts.Should().Contain(c => c.NameText == "Emma" && c.ILikedIt == true);
        conflicts.Should().Contain(c => c.NameText == "Liam" && c.ILikedIt == false);
    }

    [Fact]
    public async Task GetConflictsAsync_ExcludesMatches()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var userId = "user-123";
        var partnerId = "partner-456";

        var session = CreateActiveSession(userId, partnerId);
        context.Sessions.Add(session);

        var name1 = new Name { NameText = "Emma", Gender = Gender.Female, PopularityScore = 90 };
        var name2 = new Name { NameText = "Liam", Gender = Gender.Male, PopularityScore = 95 };
        context.Names.AddRange(name1, name2);
        await context.SaveChangesAsync();

        // Both like Emma (this is a match, not a conflict)
        context.Votes.AddRange(
            new Vote { UserId = userId, NameId = name1.Id, SessionId = session.Id, VoteType = VoteType.Like, VotedAt = DateTime.UtcNow },
            new Vote { UserId = partnerId, NameId = name1.Id, SessionId = session.Id, VoteType = VoteType.Like, VotedAt = DateTime.UtcNow }
        );
        // User likes Liam, partner dislikes (this is a conflict)
        context.Votes.AddRange(
            new Vote { UserId = userId, NameId = name2.Id, SessionId = session.Id, VoteType = VoteType.Like, VotedAt = DateTime.UtcNow },
            new Vote { UserId = partnerId, NameId = name2.Id, SessionId = session.Id, VoteType = VoteType.Dislike, VotedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var service = new VoteService(context);

        // Act
        var conflicts = (await service.GetConflictsAsync(userId)).ToList();

        // Assert
        conflicts.Should().HaveCount(1);
        conflicts.First().NameText.Should().Be("Liam");
    }

    [Fact]
    public async Task ClearDislikeAsync_ThrowsException_WhenNoSession()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new VoteService(context);

        // Act & Assert
        var act = async () => await service.ClearDislikeAsync("user-123", 1);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*active session*");
    }

    [Fact]
    public async Task ClearDislikeAsync_ThrowsException_WhenVoteNotFound()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var userId = "user-123";

        var session = CreateActiveSession(userId, "partner-456");
        context.Sessions.Add(session);
        await context.SaveChangesAsync();

        var service = new VoteService(context);

        // Act & Assert
        var act = async () => await service.ClearDislikeAsync(userId, 9999);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Vote not found*");
    }

    [Fact]
    public async Task ClearDislikeAsync_ThrowsException_WhenVoteIsLike()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var userId = "user-123";

        var session = CreateActiveSession(userId, "partner-456");
        context.Sessions.Add(session);

        var name = new Name { NameText = "Emma", Gender = Gender.Female, PopularityScore = 90 };
        context.Names.Add(name);
        await context.SaveChangesAsync();

        context.Votes.Add(new Vote
        {
            UserId = userId,
            NameId = name.Id,
            SessionId = session.Id,
            VoteType = VoteType.Like,
            VotedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new VoteService(context);

        // Act & Assert
        var act = async () => await service.ClearDislikeAsync(userId, name.Id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*only clear a dislike*");
    }

    [Fact]
    public async Task ClearDislikeAsync_RemovesVote_WhenValid()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var userId = "user-123";
        var partnerId = "partner-456";

        var session = CreateActiveSession(userId, partnerId);
        context.Sessions.Add(session);

        var name = new Name { NameText = "Emma", Gender = Gender.Female, PopularityScore = 90 };
        context.Names.Add(name);
        await context.SaveChangesAsync();

        // User dislikes Emma, partner likes Emma
        context.Votes.AddRange(
            new Vote { UserId = userId, NameId = name.Id, SessionId = session.Id, VoteType = VoteType.Dislike, VotedAt = DateTime.UtcNow },
            new Vote { UserId = partnerId, NameId = name.Id, SessionId = session.Id, VoteType = VoteType.Like, VotedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var service = new VoteService(context);

        // Act
        var result = await service.ClearDislikeAsync(userId, name.Id);

        // Assert
        result.Should().BeTrue();

        // Verify vote was removed
        context.Votes.Should().ContainSingle(v => v.UserId == partnerId);
        context.Votes.Should().NotContain(v => v.UserId == userId && v.NameId == name.Id);
    }

    [Fact]
    public async Task ClearDislikeAsync_RemovesConflict()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var userId = "user-123";
        var partnerId = "partner-456";

        var session = CreateActiveSession(userId, partnerId);
        context.Sessions.Add(session);

        var name = new Name { NameText = "Emma", Gender = Gender.Female, PopularityScore = 90 };
        context.Names.Add(name);
        await context.SaveChangesAsync();

        // User dislikes Emma, partner likes Emma (conflict)
        context.Votes.AddRange(
            new Vote { UserId = userId, NameId = name.Id, SessionId = session.Id, VoteType = VoteType.Dislike, VotedAt = DateTime.UtcNow },
            new Vote { UserId = partnerId, NameId = name.Id, SessionId = session.Id, VoteType = VoteType.Like, VotedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var service = new VoteService(context);

        // Verify conflict exists before clearing
        var conflictsBefore = (await service.GetConflictsAsync(userId)).ToList();
        conflictsBefore.Should().HaveCount(1);

        // Act
        await service.ClearDislikeAsync(userId, name.Id);

        // Assert - conflict should be gone
        var conflictsAfter = (await service.GetConflictsAsync(userId)).ToList();
        conflictsAfter.Should().BeEmpty();
    }

    private static Session CreateActiveSession(string initiatorId, string partnerId)
    {
        return new Session
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
    }
}
