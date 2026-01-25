using FluentAssertions;
using NameMatch.Domain.Entities;
using NameMatch.Domain.Enums;
using NameMatch.Infrastructure.Services;
using NameMatch.Tests.Helpers;

namespace NameMatch.Tests.Services;

public class PreferenceServiceTests
{
    [Fact]
    public async Task GetCategoriesAsync_ReturnsAllCategories()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        context.NameCategories.AddRange(
            new NameCategory { Code = "CLASSIC", DisplayName = "Classic", CategoryType = "STYLE", DisplayOrder = 1 },
            new NameCategory { Code = "SHORT", DisplayName = "Short", CategoryType = "SOUND", DisplayOrder = 2 }
        );
        await context.SaveChangesAsync();

        var service = new PreferenceService(context);

        // Act
        var result = await service.GetCategoriesAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetCategoriesAsync_FiltersbyType()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        context.NameCategories.AddRange(
            new NameCategory { Code = "CLASSIC", DisplayName = "Classic", CategoryType = "STYLE", DisplayOrder = 1 },
            new NameCategory { Code = "MODERN", DisplayName = "Modern", CategoryType = "STYLE", DisplayOrder = 2 },
            new NameCategory { Code = "SHORT", DisplayName = "Short", CategoryType = "SOUND", DisplayOrder = 3 }
        );
        await context.SaveChangesAsync();

        var service = new PreferenceService(context);

        // Act
        var result = await service.GetCategoriesAsync("STYLE");

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(c => c.CategoryType == "STYLE");
    }

    [Fact]
    public async Task GetQuestionsAsync_Returns2Questions()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new PreferenceService(context);

        // Act
        var result = await service.GetQuestionsAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetQuestionsAsync_QuestionsHaveOptions()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new PreferenceService(context);

        // Act
        var result = (await service.GetQuestionsAsync()).ToList();

        // Assert
        result.Should().OnlyContain(q => q.Options.Count >= 2);
    }

    [Fact]
    public async Task SubmitPreferencesAsync_CreatesUserPreferences()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var userId = "user-123";

        // Create session
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

        // Create categories
        var classicCat = new NameCategory { Code = "CLASSIC", DisplayName = "Classic", CategoryType = "STYLE", DisplayOrder = 1 };
        var shortCat = new NameCategory { Code = "SHORT", DisplayName = "Short", CategoryType = "SOUND", DisplayOrder = 2 };
        context.NameCategories.AddRange(classicCat, shortCat);
        await context.SaveChangesAsync();

        var service = new PreferenceService(context);

        var request = new Application.DTOs.Preferences.SubmitPreferencesRequest
        {
            Answers = new List<Application.DTOs.Preferences.PreferenceAnswerDto>
            {
                new() { QuestionId = "style", SelectedOptionIds = new List<string> { "classic" } },
                new() { QuestionId = "length", SelectedOptionIds = new List<string> { "short" } }
            }
        };

        // Act
        var result = await service.SubmitPreferencesAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.InitiatorCompleted.Should().BeTrue();

        var userPrefs = context.UserPreferences.Where(p => p.UserId == userId).ToList();
        userPrefs.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task SubmitPreferencesAsync_ThrowsWhenNoActiveSession()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new PreferenceService(context);

        var request = new Application.DTOs.Preferences.SubmitPreferencesRequest
        {
            Answers = new List<Application.DTOs.Preferences.PreferenceAnswerDto>()
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SubmitPreferencesAsync("user-123", request));
    }

    [Fact]
    public async Task GetUserPreferencesAsync_ReturnsUserPreferences()
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

        var category = new NameCategory { Code = "CLASSIC", DisplayName = "Classic", CategoryType = "STYLE", DisplayOrder = 1 };
        context.NameCategories.Add(category);
        await context.SaveChangesAsync();

        context.UserPreferences.Add(new UserPreference
        {
            UserId = userId,
            SessionId = session.Id,
            CategoryId = category.Id,
            Level = PreferenceLevel.Love,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new PreferenceService(context);

        // Act
        var result = await service.GetUserPreferencesAsync(userId);

        // Assert
        result.Should().HaveCount(1);
        result.First().CategoryCode.Should().Be("CLASSIC");
        result.First().Level.Should().Be(PreferenceLevel.Love);
    }

    [Fact]
    public async Task GetSessionPreferencesStatusAsync_ReturnsCorrectStatus()
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
            SetupStatus = SessionSetupStatus.PendingInitiatorPreferences,
            CreatedAt = DateTime.UtcNow,
            LinkedAt = DateTime.UtcNow
        };
        context.Sessions.Add(session);
        await context.SaveChangesAsync();

        var service = new PreferenceService(context);

        // Act
        var result = await service.GetSessionPreferencesStatusAsync(initiatorId);

        // Assert
        result.Should().NotBeNull();
        result!.SetupStatus.Should().Be((int)SessionSetupStatus.PendingInitiatorPreferences);
        result.InitiatorCompleted.Should().BeFalse();
        result.PartnerCompleted.Should().BeFalse();
        result.BothCompleted.Should().BeFalse();
        result.CanStartVoting.Should().BeFalse();
    }

    [Fact]
    public async Task GetSessionPreferencesStatusAsync_ReturnsNullWhenNoSession()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new PreferenceService(context);

        // Act
        var result = await service.GetSessionPreferencesStatusAsync("nonexistent-user");

        // Assert
        result.Should().BeNull();
    }
}
