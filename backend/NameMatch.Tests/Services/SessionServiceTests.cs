using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using NameMatch.Application.DTOs.Session;
using NameMatch.Domain.Entities;
using NameMatch.Domain.Enums;
using NameMatch.Infrastructure.Identity;
using NameMatch.Infrastructure.Services;
using NameMatch.Tests.Helpers;

namespace NameMatch.Tests.Services;

public class SessionServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;

    public SessionServiceTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    [Fact]
    public async Task CreateSessionAsync_CreatesSession_WithUniqueCode()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var userId = "user-123";
        var user = new ApplicationUser { Id = userId, Email = "test@example.com", DisplayName = "Test User" };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        var service = new SessionService(context, _userManagerMock.Object);
        var request = new CreateSessionRequest { TargetGender = Gender.Female };

        // Act
        var result = await service.CreateSessionAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.InitiatorId.Should().Be(userId);
        result.TargetGender.Should().Be(Gender.Female);
        result.Status.Should().Be(SessionStatus.WaitingForPartner);
        result.JoinCode.Should().HaveLength(6);
        result.PartnerLink.Should().HaveLength(12);
        result.IsInitiator.Should().BeTrue();
        result.PartnerId.Should().BeNull();
    }

    [Fact]
    public async Task CreateSessionAsync_ThrowsException_WhenUserHasActiveSession()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var userId = "user-123";

        // Create an existing active session
        context.Sessions.Add(new Session
        {
            Id = Guid.NewGuid(),
            InitiatorId = userId,
            TargetGender = Gender.Male,
            JoinCode = "ABC123",
            PartnerLink = "link123",
            Status = SessionStatus.WaitingForPartner,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new SessionService(context, _userManagerMock.Object);
        var request = new CreateSessionRequest { TargetGender = Gender.Female };

        // Act & Assert
        var act = () => service.CreateSessionAsync(userId, request);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already have an active session*");
    }

    [Fact]
    public async Task JoinByCodeAsync_JoinsSession_Successfully()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var initiatorId = "initiator-123";
        var partnerId = "partner-456";

        var initiator = new ApplicationUser { Id = initiatorId, Email = "initiator@example.com", DisplayName = "Initiator" };
        var partner = new ApplicationUser { Id = partnerId, Email = "partner@example.com", DisplayName = "Partner" };

        _userManagerMock.Setup(x => x.FindByIdAsync(initiatorId)).ReturnsAsync(initiator);
        _userManagerMock.Setup(x => x.FindByIdAsync(partnerId)).ReturnsAsync(partner);

        var session = new Session
        {
            Id = Guid.NewGuid(),
            InitiatorId = initiatorId,
            TargetGender = Gender.Female,
            JoinCode = "ABC123",
            PartnerLink = "link123",
            Status = SessionStatus.WaitingForPartner,
            CreatedAt = DateTime.UtcNow
        };
        context.Sessions.Add(session);
        await context.SaveChangesAsync();

        var service = new SessionService(context, _userManagerMock.Object);

        // Act
        var result = await service.JoinByCodeAsync(partnerId, "ABC123");

        // Assert
        result.Should().NotBeNull();
        result.PartnerId.Should().Be(partnerId);
        result.Status.Should().Be(SessionStatus.Active);
        result.LinkedAt.Should().NotBeNull();
        result.IsInitiator.Should().BeFalse();
    }

    [Fact]
    public async Task JoinByCodeAsync_IsCaseInsensitive()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var initiatorId = "initiator-123";
        var partnerId = "partner-456";

        _userManagerMock.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync(new ApplicationUser { Id = "test", Email = "test@example.com" });

        context.Sessions.Add(new Session
        {
            Id = Guid.NewGuid(),
            InitiatorId = initiatorId,
            TargetGender = Gender.Female,
            JoinCode = "ABC123",
            PartnerLink = "link123",
            Status = SessionStatus.WaitingForPartner,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new SessionService(context, _userManagerMock.Object);

        // Act - lowercase code
        var result = await service.JoinByCodeAsync(partnerId, "abc123");

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(SessionStatus.Active);
    }

    [Fact]
    public async Task JoinByCodeAsync_ThrowsException_WhenCodeNotFound()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new SessionService(context, _userManagerMock.Object);

        // Act & Assert
        var act = () => service.JoinByCodeAsync("user-123", "INVALID");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task JoinByCodeAsync_ThrowsException_WhenJoiningOwnSession()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var userId = "user-123";

        context.Sessions.Add(new Session
        {
            Id = Guid.NewGuid(),
            InitiatorId = userId,
            TargetGender = Gender.Female,
            JoinCode = "ABC123",
            PartnerLink = "link123",
            Status = SessionStatus.WaitingForPartner,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new SessionService(context, _userManagerMock.Object);

        // Act & Assert
        var act = () => service.JoinByCodeAsync(userId, "ABC123");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cannot join your own session*");
    }

    [Fact]
    public async Task JoinByCodeAsync_ThrowsException_WhenSessionAlreadyHasPartner()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var initiatorId = "initiator-123";
        var existingPartnerId = "partner-456";
        var newPartnerId = "partner-789";

        context.Sessions.Add(new Session
        {
            Id = Guid.NewGuid(),
            InitiatorId = initiatorId,
            PartnerId = existingPartnerId,
            TargetGender = Gender.Female,
            JoinCode = "ABC123",
            PartnerLink = "link123",
            Status = SessionStatus.Active,
            CreatedAt = DateTime.UtcNow,
            LinkedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new SessionService(context, _userManagerMock.Object);

        // Act & Assert
        var act = () => service.JoinByCodeAsync(newPartnerId, "ABC123");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already has a partner*");
    }

    [Fact]
    public async Task JoinByCodeAsync_ThrowsException_WhenUserHasActiveSession()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var initiatorId = "initiator-123";
        var partnerId = "partner-456";

        // Session to join
        context.Sessions.Add(new Session
        {
            Id = Guid.NewGuid(),
            InitiatorId = initiatorId,
            TargetGender = Gender.Female,
            JoinCode = "ABC123",
            PartnerLink = "link123",
            Status = SessionStatus.WaitingForPartner,
            CreatedAt = DateTime.UtcNow
        });

        // Partner's existing session
        context.Sessions.Add(new Session
        {
            Id = Guid.NewGuid(),
            InitiatorId = partnerId,
            TargetGender = Gender.Male,
            JoinCode = "XYZ789",
            PartnerLink = "link789",
            Status = SessionStatus.WaitingForPartner,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new SessionService(context, _userManagerMock.Object);

        // Act & Assert
        var act = () => service.JoinByCodeAsync(partnerId, "ABC123");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already have an active session*");
    }

    [Fact]
    public async Task JoinByLinkAsync_JoinsSession_Successfully()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var initiatorId = "initiator-123";
        var partnerId = "partner-456";

        _userManagerMock.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync(new ApplicationUser { Id = "test", Email = "test@example.com" });

        context.Sessions.Add(new Session
        {
            Id = Guid.NewGuid(),
            InitiatorId = initiatorId,
            TargetGender = Gender.Female,
            JoinCode = "ABC123",
            PartnerLink = "uniquelink123",
            Status = SessionStatus.WaitingForPartner,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new SessionService(context, _userManagerMock.Object);

        // Act
        var result = await service.JoinByLinkAsync(partnerId, "uniquelink123");

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(SessionStatus.Active);
    }

    [Fact]
    public async Task JoinByLinkAsync_ThrowsException_WhenLinkNotFound()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new SessionService(context, _userManagerMock.Object);

        // Act & Assert
        var act = () => service.JoinByLinkAsync("user-123", "invalidlink");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task GetCurrentSessionAsync_ReturnsSession_WhenExists()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var userId = "user-123";
        var user = new ApplicationUser { Id = userId, Email = "test@example.com", DisplayName = "Test" };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);

        var session = new Session
        {
            Id = Guid.NewGuid(),
            InitiatorId = userId,
            TargetGender = Gender.Female,
            JoinCode = "ABC123",
            PartnerLink = "link123",
            Status = SessionStatus.WaitingForPartner,
            CreatedAt = DateTime.UtcNow
        };
        context.Sessions.Add(session);
        await context.SaveChangesAsync();

        var service = new SessionService(context, _userManagerMock.Object);

        // Act
        var result = await service.GetCurrentSessionAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(session.Id);
    }

    [Fact]
    public async Task GetCurrentSessionAsync_ReturnsNull_WhenNoSession()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new SessionService(context, _userManagerMock.Object);

        // Act
        var result = await service.GetCurrentSessionAsync("user-123");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentSessionAsync_ExcludesCompletedSessions()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var userId = "user-123";

        context.Sessions.Add(new Session
        {
            Id = Guid.NewGuid(),
            InitiatorId = userId,
            TargetGender = Gender.Female,
            JoinCode = "ABC123",
            PartnerLink = "link123",
            Status = SessionStatus.Completed,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new SessionService(context, _userManagerMock.Object);

        // Act
        var result = await service.GetCurrentSessionAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSessionByIdAsync_ReturnsSession_WhenUserIsParticipant()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var userId = "user-123";
        var user = new ApplicationUser { Id = userId, Email = "test@example.com" };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);

        var session = new Session
        {
            Id = Guid.NewGuid(),
            InitiatorId = userId,
            TargetGender = Gender.Female,
            JoinCode = "ABC123",
            PartnerLink = "link123",
            Status = SessionStatus.WaitingForPartner,
            CreatedAt = DateTime.UtcNow
        };
        context.Sessions.Add(session);
        await context.SaveChangesAsync();

        var service = new SessionService(context, _userManagerMock.Object);

        // Act
        var result = await service.GetSessionByIdAsync(session.Id, userId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(session.Id);
    }

    [Fact]
    public async Task GetSessionByIdAsync_ReturnsNull_WhenUserNotParticipant()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        var session = new Session
        {
            Id = Guid.NewGuid(),
            InitiatorId = "other-user",
            TargetGender = Gender.Female,
            JoinCode = "ABC123",
            PartnerLink = "link123",
            Status = SessionStatus.WaitingForPartner,
            CreatedAt = DateTime.UtcNow
        };
        context.Sessions.Add(session);
        await context.SaveChangesAsync();

        var service = new SessionService(context, _userManagerMock.Object);

        // Act
        var result = await service.GetSessionByIdAsync(session.Id, "different-user");

        // Assert
        result.Should().BeNull();
    }
}
