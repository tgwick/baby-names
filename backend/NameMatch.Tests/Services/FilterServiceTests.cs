using FluentAssertions;
using NameMatch.Application.DTOs.Filters;
using NameMatch.Domain.Entities;
using NameMatch.Domain.Enums;
using NameMatch.Infrastructure.Services;
using NameMatch.Tests.Helpers;

namespace NameMatch.Tests.Services;

public class FilterServiceTests
{
    [Fact]
    public async Task GetFilterQuestionsAsync_ReturnsTwoQuestions()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new FilterService(context);

        // Act
        var questions = (await service.GetFilterQuestionsAsync()).ToList();

        // Assert
        questions.Should().HaveCount(2);
        questions.Select(q => q.QuestionId).Should().BeEquivalentTo(new[] { "name_style", "syllables" });
    }

    [Fact]
    public async Task GetFilterQuestionsAsync_NameStyleQuestion_HasCorrectOptions()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new FilterService(context);

        // Act
        var questions = (await service.GetFilterQuestionsAsync()).ToList();
        var nameStyleQuestion = questions.First(q => q.QuestionId == "name_style");

        // Assert
        nameStyleQuestion.FilterType.Should().Be("NAME_STYLE");
        nameStyleQuestion.Options.Should().HaveCount(4);
        nameStyleQuestion.Options.Should().Contain(o => o.OptionId == "trendy");
        nameStyleQuestion.Options.Should().Contain(o => o.OptionId == "classic");
        nameStyleQuestion.Options.Should().Contain(o => o.OptionId == "unique");
        nameStyleQuestion.Options.Should().Contain(o => o.OptionId == "no_pref");
    }

    [Fact]
    public async Task SubmitFiltersAsync_SavesFiltersCorrectly()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var userId = "user-123";

        var session = new Session
        {
            Id = Guid.NewGuid(),
            InitiatorId = userId,
            TargetGender = Gender.Neutral,
            JoinCode = "ABC123",
            PartnerLink = "link123",
            Status = SessionStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        context.Sessions.Add(session);
        await context.SaveChangesAsync();

        var service = new FilterService(context);

        var request = new SubmitFiltersRequest
        {
            Answers = new List<FilterAnswerDto>
            {
                new() { QuestionId = "name_style", SelectedOptionId = "unique" },
                new() { QuestionId = "syllables", SelectedOptionId = "short" }
            }
        };

        // Act
        var result = await service.SubmitFiltersAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.InitiatorCompleted.Should().BeTrue();
        result.InitiatorCompletedAt.Should().NotBeNull();

        // Verify saved filters
        var savedFilter = context.SessionFilters.First(f => f.UserId == userId && f.SessionId == session.Id);
        savedFilter.NameStyle.Should().Be(NameStyle.Unique);
        savedFilter.MinSyllables.Should().Be(1);
        savedFilter.MaxSyllables.Should().Be(2);
        savedFilter.AllowedEndingSounds.Should().BeNull();
    }

    [Fact]
    public async Task SubmitFiltersAsync_ThrowsException_WhenNoActiveSession()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new FilterService(context);

        var request = new SubmitFiltersRequest { Answers = new List<FilterAnswerDto>() };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SubmitFiltersAsync("user-123", request));
    }

    [Fact]
    public async Task SubmitFiltersAsync_UpdatesPartnerFilters_WhenPartnerSubmits()
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
        await context.SaveChangesAsync();

        var service = new FilterService(context);

        var request = new SubmitFiltersRequest
        {
            Answers = new List<FilterAnswerDto>
            {
                new() { QuestionId = "name_style", SelectedOptionId = "trendy" }
            }
        };

        // Act
        var result = await service.SubmitFiltersAsync(partnerId, request);

        // Assert
        result.PartnerCompleted.Should().BeTrue();
        result.InitiatorCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task GetFiltersStatusAsync_ReturnsNull_WhenNoSession()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new FilterService(context);

        // Act
        var result = await service.GetFiltersStatusAsync("user-123");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserFiltersAsync_ReturnsNull_WhenNoFilters()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var userId = "user-123";

        var session = new Session
        {
            Id = Guid.NewGuid(),
            InitiatorId = userId,
            TargetGender = Gender.Neutral,
            JoinCode = "ABC123",
            PartnerLink = "link123",
            Status = SessionStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        context.Sessions.Add(session);
        await context.SaveChangesAsync();

        var service = new FilterService(context);

        // Act
        var result = await service.GetUserFiltersAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCombinedSessionFiltersAsync_ReturnsSingleUserFilters_WhenOnlyOneUserHasFilters()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var sessionId = Guid.NewGuid();

        context.SessionFilters.Add(new SessionFilter
        {
            UserId = "user-123",
            SessionId = sessionId,
            MinPopularityScore = 60,
            MaxPopularityScore = 100,
            MinSyllables = 2,
            MaxSyllables = 3,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new FilterService(context);

        // Act
        var result = await service.GetCombinedSessionFiltersAsync(sessionId);

        // Assert
        result.Should().NotBeNull();
        result!.MinPopularityScore.Should().Be(60);
        result.MaxPopularityScore.Should().Be(100);
        result.MinSyllables.Should().Be(2);
        result.MaxSyllables.Should().Be(3);
    }

    [Fact]
    public async Task GetCombinedSessionFiltersAsync_UsesIntersection_WhenBothUsersHaveFilters()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var sessionId = Guid.NewGuid();

        // User 1: popularity 40-100, syllables 2-4
        context.SessionFilters.Add(new SessionFilter
        {
            UserId = "user-123",
            SessionId = sessionId,
            MinPopularityScore = 40,
            MaxPopularityScore = 100,
            MinSyllables = 2,
            MaxSyllables = 4,
            CreatedAt = DateTime.UtcNow
        });

        // User 2: popularity 60-80, syllables 1-3
        context.SessionFilters.Add(new SessionFilter
        {
            UserId = "partner-456",
            SessionId = sessionId,
            MinPopularityScore = 60,
            MaxPopularityScore = 80,
            MinSyllables = 1,
            MaxSyllables = 3,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new FilterService(context);

        // Act
        var result = await service.GetCombinedSessionFiltersAsync(sessionId);

        // Assert - should be intersection (most restrictive)
        result.Should().NotBeNull();
        result!.MinPopularityScore.Should().Be(60); // max of mins
        result.MaxPopularityScore.Should().Be(80);  // min of maxes
        result.MinSyllables.Should().Be(2);         // max of mins
        result.MaxSyllables.Should().Be(3);         // min of maxes
    }

    [Fact]
    public async Task GetCombinedSessionFiltersAsync_UsesIntersection_ForEndingSounds()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var sessionId = Guid.NewGuid();

        // User 1: likes a, ia endings
        context.SessionFilters.Add(new SessionFilter
        {
            UserId = "user-123",
            SessionId = sessionId,
            AllowedEndingSounds = "a,ia,ah",
            CreatedAt = DateTime.UtcNow
        });

        // User 2: likes a, n endings
        context.SessionFilters.Add(new SessionFilter
        {
            UserId = "partner-456",
            SessionId = sessionId,
            AllowedEndingSounds = "a,n,en",
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new FilterService(context);

        // Act
        var result = await service.GetCombinedSessionFiltersAsync(sessionId);

        // Assert - intersection should only include "a" (common to both)
        result.Should().NotBeNull();
        result!.AllowedEndingSounds.Should().NotBeNull();
        result.AllowedEndingSounds.Should().BeEquivalentTo(new[] { "a" });
    }

    [Fact]
    public async Task GetCombinedSessionFiltersAsync_UsesUserSounds_WhenOnlyOneHasPreference()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var sessionId = Guid.NewGuid();

        // User 1: likes specific endings
        context.SessionFilters.Add(new SessionFilter
        {
            UserId = "user-123",
            SessionId = sessionId,
            AllowedEndingSounds = "a,ia,ah",
            CreatedAt = DateTime.UtcNow
        });

        // User 2: no ending preference
        context.SessionFilters.Add(new SessionFilter
        {
            UserId = "partner-456",
            SessionId = sessionId,
            AllowedEndingSounds = null,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new FilterService(context);

        // Act
        var result = await service.GetCombinedSessionFiltersAsync(sessionId);

        // Assert - should use the one user's preference
        result.Should().NotBeNull();
        result!.AllowedEndingSounds.Should().BeEquivalentTo(new[] { "a", "ia", "ah" });
    }

    [Fact]
    public async Task GetCombinedSessionFiltersAsync_ReturnsNull_WhenNoFilters()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new FilterService(context);

        // Act
        var result = await service.GetCombinedSessionFiltersAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }
}
