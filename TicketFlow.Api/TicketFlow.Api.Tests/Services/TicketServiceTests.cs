using Moq;
using TicketFlow.Api.DTOs.Tickets;
using TicketFlow.Api.Models;
using TicketFlow.Api.Repositories.Interfaces;
using TicketFlow.Api.Services;

namespace TicketFlow.Api.Tests.Services;

public class TicketServiceTests
{
    private readonly Mock<ITicketRepository> _ticketRepoMock;
    private readonly Mock<ITicketCommentRepository> _commentRepoMock;
    private readonly Mock<ITicketStatusHistoryRepository> _historyRepoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly TicketService _ticketService;

    public TicketServiceTests()
    {
        _ticketRepoMock = new Mock<ITicketRepository>();
        _commentRepoMock = new Mock<ITicketCommentRepository>();
        _historyRepoMock = new Mock<ITicketStatusHistoryRepository>();
        _userRepoMock = new Mock<IUserRepository>();

        _ticketService = new TicketService(
            _ticketRepoMock.Object,
            _commentRepoMock.Object,
            _historyRepoMock.Object,
            _userRepoMock.Object);
    }

    #region CreateTicketAsync

    [Fact]
    public async Task CreateTicketAsync_ValidRequest_CreatesTicketWithOpenStatus()
    {
        // Arrange
        _ticketRepoMock.Setup(r => r.GetNextTicketNumberAsync()).ReturnsAsync("TKT-00001");
        _ticketRepoMock.Setup(r => r.CreateAsync(It.IsAny<Ticket>())).ReturnsAsync(1);

        var request = new CreateTicketRequest
        {
            Subject = "Test Ticket",
            Description = "Test Description",
            Priority = "High"
        };

        // Act
        var ticketId = await _ticketService.CreateTicketAsync(request, userId: 2);

        // Assert
        Assert.Equal(1, ticketId);

        _ticketRepoMock.Verify(r => r.CreateAsync(It.Is<Ticket>(t =>
            t.TicketNumber == "TKT-00001" &&
            t.CreatedByUserId == 2 &&
            t.Subject == "Test Ticket" &&
            t.Status == "Open" &&
            t.Priority == "High"
        )), Times.Once);

        // Verify initial status history was logged
        _historyRepoMock.Verify(r => r.AddAsync(It.Is<TicketStatusHistory>(h =>
            h.TicketId == 1 &&
            h.OldStatus == "" &&
            h.NewStatus == "Open" &&
            h.ChangedByUserId == 2
        )), Times.Once);
    }

    #endregion

    #region GetTicketsAsync

    [Fact]
    public async Task GetTicketsAsync_AdminRole_ReturnsAllTickets()
    {
        // Arrange
        var tickets = new List<Ticket>
        {
            new() { Id = 1, TicketNumber = "TKT-00001", Subject = "T1", Priority = "High", Status = "Open", CreatedByUserId = 2, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, TicketNumber = "TKT-00002", Subject = "T2", Priority = "Low", Status = "Open", CreatedByUserId = 3, CreatedAt = DateTime.UtcNow }
        };

        _ticketRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(tickets);

        // Act
        var result = (await _ticketService.GetTicketsAsync(userId: 1, role: "Admin")).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        _ticketRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _ticketRepoMock.Verify(r => r.GetByUserIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetTicketsAsync_UserRole_ReturnsOnlyOwnTickets()
    {
        // Arrange
        var tickets = new List<Ticket>
        {
            new() { Id = 1, TicketNumber = "TKT-00001", Subject = "My Ticket", Priority = "Medium", Status = "Open", CreatedByUserId = 5, CreatedAt = DateTime.UtcNow }
        };

        _ticketRepoMock.Setup(r => r.GetByUserIdAsync(5)).ReturnsAsync(tickets);

        // Act
        var result = (await _ticketService.GetTicketsAsync(userId: 5, role: "User")).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("My Ticket", result[0].Subject);
        _ticketRepoMock.Verify(r => r.GetByUserIdAsync(5), Times.Once);
        _ticketRepoMock.Verify(r => r.GetAllAsync(), Times.Never);
    }

    [Fact]
    public async Task GetTicketsAsync_TicketWithAssignment_ResolvesAssignedName()
    {
        // Arrange
        var tickets = new List<Ticket>
        {
            new() { Id = 1, TicketNumber = "TKT-00001", Subject = "T", Priority = "Low", Status = "Open", CreatedByUserId = 2, AssignedToUserId = 10, CreatedAt = DateTime.UtcNow }
        };
        _ticketRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(tickets);
        _userRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new User { Id = 10, FullName = "Admin User" });

        // Act
        var result = (await _ticketService.GetTicketsAsync(1, "Admin")).ToList();

        // Assert
        Assert.Equal("Admin User", result[0].AssignedToName);
    }

    #endregion

    #region GetTicketDetailAsync

    [Fact]
    public async Task GetTicketDetailAsync_TicketNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _ticketRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Ticket?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _ticketService.GetTicketDetailAsync(99, userId: 1, role: "User"));
    }

    [Fact]
    public async Task GetTicketDetailAsync_UserAccessingOtherUsersTicket_ThrowsUnauthorized()
    {
        // Arrange
        var ticket = new Ticket { Id = 1, CreatedByUserId = 5, Status = "Open" };
        _ticketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _ticketService.GetTicketDetailAsync(1, userId: 99, role: "User"));
    }

    [Fact]
    public async Task GetTicketDetailAsync_AdminAccessingAnyTicket_Succeeds()
    {
        // Arrange
        var ticket = new Ticket { Id = 1, CreatedByUserId = 5, Subject = "Test", Description = "Desc", Priority = "High", Status = "Open", TicketNumber = "TKT-00001", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _ticketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);
        _userRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new User { Id = 5, FullName = "John" });
        _commentRepoMock.Setup(r => r.GetByTicketIdAsync(1)).ReturnsAsync(new List<TicketComment>());
        _historyRepoMock.Setup(r => r.GetByTicketIdAsync(1)).ReturnsAsync(new List<TicketStatusHistory>());

        // Act
        var result = await _ticketService.GetTicketDetailAsync(1, userId: 1, role: "Admin");

        // Assert
        Assert.Equal("TKT-00001", result.TicketNumber);
        Assert.Equal("John", result.CreatedByName);
    }

    [Fact]
    public async Task GetTicketDetailAsync_UserRole_FiltersInternalComments()
    {
        // Arrange
        var ticket = new Ticket { Id = 1, CreatedByUserId = 5, Subject = "T", Description = "D", Priority = "High", Status = "Open", TicketNumber = "TKT-00001", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _ticketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);
        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new User { Id = 5, FullName = "John" });

        var comments = new List<TicketComment>
        {
            new() { Id = 1, TicketId = 1, UserId = 5, Comment = "Public comment", IsInternal = false, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, TicketId = 1, UserId = 1, Comment = "Internal note", IsInternal = true, CreatedAt = DateTime.UtcNow }
        };
        _commentRepoMock.Setup(r => r.GetByTicketIdAsync(1)).ReturnsAsync(comments);
        _historyRepoMock.Setup(r => r.GetByTicketIdAsync(1)).ReturnsAsync(new List<TicketStatusHistory>());

        // Act
        var result = await _ticketService.GetTicketDetailAsync(1, userId: 5, role: "User");

        // Assert — internal comment should be filtered out for regular users
        Assert.Single(result.Comments);
        Assert.Equal("Public comment", result.Comments[0].Comment);
    }

    [Fact]
    public async Task GetTicketDetailAsync_AdminRole_SeesAllComments()
    {
        // Arrange
        var ticket = new Ticket { Id = 1, CreatedByUserId = 5, Subject = "T", Description = "D", Priority = "High", Status = "Open", TicketNumber = "TKT-00001", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _ticketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);
        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new User { Id = 1, FullName = "Admin" });

        var comments = new List<TicketComment>
        {
            new() { Id = 1, TicketId = 1, UserId = 5, Comment = "Public", IsInternal = false, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, TicketId = 1, UserId = 1, Comment = "Internal", IsInternal = true, CreatedAt = DateTime.UtcNow }
        };
        _commentRepoMock.Setup(r => r.GetByTicketIdAsync(1)).ReturnsAsync(comments);
        _historyRepoMock.Setup(r => r.GetByTicketIdAsync(1)).ReturnsAsync(new List<TicketStatusHistory>());

        // Act
        var result = await _ticketService.GetTicketDetailAsync(1, userId: 1, role: "Admin");

        // Assert — admin should see all comments including internal
        Assert.Equal(2, result.Comments.Count);
    }

    #endregion

    #region AssignTicketAsync

    [Fact]
    public async Task AssignTicketAsync_TicketNotFound_ThrowsKeyNotFoundException()
    {
        _ticketRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Ticket?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _ticketService.AssignTicketAsync(99, new AssignTicketRequest { AssignToUserId = 1 }));
    }

    [Fact]
    public async Task AssignTicketAsync_ClosedTicket_ThrowsInvalidOperation()
    {
        var ticket = new Ticket { Id = 1, Status = "Closed" };
        _ticketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _ticketService.AssignTicketAsync(1, new AssignTicketRequest { AssignToUserId = 1 }));
        Assert.Contains("closed", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AssignTicketAsync_AssignToNonAdmin_ThrowsInvalidOperation()
    {
        var ticket = new Ticket { Id = 1, Status = "Open" };
        _ticketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);
        _userRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new User { Id = 5, Role = "User" });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _ticketService.AssignTicketAsync(1, new AssignTicketRequest { AssignToUserId = 5 }));
        Assert.Contains("admin", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AssignTicketAsync_ValidAssignment_CallsRepository()
    {
        var ticket = new Ticket { Id = 1, Status = "Open" };
        _ticketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);
        _userRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new User { Id = 10, Role = "Admin" });

        await _ticketService.AssignTicketAsync(1, new AssignTicketRequest { AssignToUserId = 10 });

        _ticketRepoMock.Verify(r => r.UpdateAssignmentAsync(1, 10), Times.Once);
    }

    #endregion

    #region UpdateStatusAsync

    [Fact]
    public async Task UpdateStatusAsync_TicketNotFound_ThrowsKeyNotFoundException()
    {
        _ticketRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Ticket?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _ticketService.UpdateStatusAsync(99, new UpdateTicketStatusRequest { Status = "InProgress" }, 1));
    }

    [Fact]
    public async Task UpdateStatusAsync_ClosedTicket_ThrowsInvalidOperation()
    {
        var ticket = new Ticket { Id = 1, Status = "Closed" };
        _ticketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _ticketService.UpdateStatusAsync(1, new UpdateTicketStatusRequest { Status = "Open" }, 1));
        Assert.Contains("closed", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateStatusAsync_OpenToInProgress_Succeeds()
    {
        var ticket = new Ticket { Id = 1, Status = "Open" };
        _ticketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);

        await _ticketService.UpdateStatusAsync(1, new UpdateTicketStatusRequest { Status = "InProgress" }, adminUserId: 10);

        _ticketRepoMock.Verify(r => r.UpdateStatusAsync(1, "InProgress"), Times.Once);
        _historyRepoMock.Verify(r => r.AddAsync(It.Is<TicketStatusHistory>(h =>
            h.TicketId == 1 &&
            h.OldStatus == "Open" &&
            h.NewStatus == "InProgress" &&
            h.ChangedByUserId == 10
        )), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_InProgressToClosed_Succeeds()
    {
        var ticket = new Ticket { Id = 1, Status = "InProgress" };
        _ticketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);

        await _ticketService.UpdateStatusAsync(1, new UpdateTicketStatusRequest { Status = "Closed" }, adminUserId: 10);

        _ticketRepoMock.Verify(r => r.UpdateStatusAsync(1, "Closed"), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_OpenToClosed_ThrowsInvalidOperation()
    {
        // Cannot skip InProgress
        var ticket = new Ticket { Id = 1, Status = "Open" };
        _ticketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _ticketService.UpdateStatusAsync(1, new UpdateTicketStatusRequest { Status = "Closed" }, 1));
        Assert.Contains("Invalid status transition", ex.Message);
    }

    [Fact]
    public async Task UpdateStatusAsync_InProgressToOpen_ThrowsInvalidOperation()
    {
        // Cannot go backwards
        var ticket = new Ticket { Id = 1, Status = "InProgress" };
        _ticketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _ticketService.UpdateStatusAsync(1, new UpdateTicketStatusRequest { Status = "Open" }, 1));
        Assert.Contains("Invalid status transition", ex.Message);
    }

    #endregion

    #region AddCommentAsync

    [Fact]
    public async Task AddCommentAsync_TicketNotFound_ThrowsKeyNotFoundException()
    {
        _ticketRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Ticket?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _ticketService.AddCommentAsync(99, new AddCommentRequest { Comment = "test" }, 1, "User"));
    }

    [Fact]
    public async Task AddCommentAsync_ClosedTicket_ThrowsInvalidOperation()
    {
        var ticket = new Ticket { Id = 1, Status = "Closed", CreatedByUserId = 5 };
        _ticketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _ticketService.AddCommentAsync(1, new AddCommentRequest { Comment = "test" }, 5, "User"));
    }

    [Fact]
    public async Task AddCommentAsync_UserCommentingOnOtherUsersTicket_ThrowsUnauthorized()
    {
        var ticket = new Ticket { Id = 1, Status = "Open", CreatedByUserId = 5 };
        _ticketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _ticketService.AddCommentAsync(1, new AddCommentRequest { Comment = "test" }, userId: 99, role: "User"));
    }

    [Fact]
    public async Task AddCommentAsync_UserAddingInternalComment_ThrowsUnauthorized()
    {
        var ticket = new Ticket { Id = 1, Status = "Open", CreatedByUserId = 5 };
        _ticketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _ticketService.AddCommentAsync(1, new AddCommentRequest { Comment = "secret", IsInternal = true }, userId: 5, role: "User"));
    }

    [Fact]
    public async Task AddCommentAsync_AdminAddingInternalComment_Succeeds()
    {
        var ticket = new Ticket { Id = 1, Status = "Open", CreatedByUserId = 5 };
        _ticketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);

        await _ticketService.AddCommentAsync(1,
            new AddCommentRequest { Comment = "Internal note", IsInternal = true },
            userId: 10, role: "Admin");

        _commentRepoMock.Verify(r => r.AddAsync(It.Is<TicketComment>(c =>
            c.TicketId == 1 &&
            c.UserId == 10 &&
            c.IsInternal == true &&
            c.Comment == "Internal note"
        )), Times.Once);
    }

    [Fact]
    public async Task AddCommentAsync_UserCommentingOnOwnTicket_Succeeds()
    {
        var ticket = new Ticket { Id = 1, Status = "Open", CreatedByUserId = 5 };
        _ticketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ticket);

        await _ticketService.AddCommentAsync(1,
            new AddCommentRequest { Comment = "My comment", IsInternal = false },
            userId: 5, role: "User");

        _commentRepoMock.Verify(r => r.AddAsync(It.Is<TicketComment>(c =>
            c.TicketId == 1 &&
            c.UserId == 5 &&
            c.Comment == "My comment"
        )), Times.Once);
    }

    #endregion

    #region GetAdminUsersAsync

    [Fact]
    public async Task GetAdminUsersAsync_ReturnsAdminUsersAsDtos()
    {
        var admins = new List<User>
        {
            new() { Id = 1, FullName = "Admin A" },
            new() { Id = 2, FullName = "Admin B" }
        };
        _userRepoMock.Setup(r => r.GetAdminUsersAsync()).ReturnsAsync(admins);

        var result = (await _ticketService.GetAdminUsersAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("Admin A", result[0].FullName);
        Assert.Equal(1, result[0].Id);
    }

    #endregion

    #region GetTicketHistoryAsync

    [Fact]
    public async Task GetTicketHistoryAsync_ReturnsHistoryWithResolvedNames()
    {
        var history = new List<TicketStatusHistory>
        {
            new() { Id = 1, TicketId = 1, OldStatus = "", NewStatus = "Open", ChangedByUserId = 5, ChangedAt = DateTime.UtcNow },
            new() { Id = 2, TicketId = 1, OldStatus = "Open", NewStatus = "InProgress", ChangedByUserId = 10, ChangedAt = DateTime.UtcNow }
        };
        _historyRepoMock.Setup(r => r.GetByTicketIdAsync(1)).ReturnsAsync(history);
        _userRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new User { Id = 5, FullName = "John" });
        _userRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new User { Id = 10, FullName = "Admin" });

        var result = (await _ticketService.GetTicketHistoryAsync(1)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("John", result[0].ChangedByName);
        Assert.Equal("Admin", result[1].ChangedByName);
        Assert.Equal("Open", result[0].NewStatus);
        Assert.Equal("InProgress", result[1].NewStatus);
    }

    #endregion
}
