using Microsoft.Extensions.Configuration;
using Moq;
using TicketFlow.Api.DTOs.Auth;
using TicketFlow.Api.Models;
using TicketFlow.Api.Repositories.Interfaces;
using TicketFlow.Api.Services;

namespace TicketFlow.Api.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly IConfiguration _configuration;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();

        var inMemorySettings = new Dictionary<string, string?>
        {
            { "JwtSettings:SecretKey", "TestSecretKeyThatIsAtLeast32CharactersLong!" },
            { "JwtSettings:Issuer", "TestIssuer" },
            { "JwtSettings:Audience", "TestAudience" },
            { "JwtSettings:ExpiryInMinutes", "60" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _authService = new AuthService(_userRepositoryMock.Object, _configuration);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsTokenAndUserInfo()
    {
        // Arrange
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
        var user = new User
        {
            Id = 1,
            Username = "admin",
            PasswordHash = passwordHash,
            FullName = "System Admin",
            Role = "Admin"
        };

        _userRepositoryMock
            .Setup(r => r.GetByUsernameAsync("admin"))
            .ReturnsAsync(user);

        var request = new LoginRequest { Username = "admin", Password = "Admin@123" };

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
        Assert.Equal("System Admin", result.FullName);
        Assert.Equal("Admin", result.Role);
    }

    [Fact]
    public async Task LoginAsync_InvalidUsername_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _userRepositoryMock
            .Setup(r => r.GetByUsernameAsync("nonexistent"))
            .ReturnsAsync((User?)null);

        var request = new LoginRequest { Username = "nonexistent", Password = "password" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.LoginAsync(request));
        Assert.Equal("Invalid username or password.", ex.Message);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            FullName = "System Admin",
            Role = "Admin"
        };

        _userRepositoryMock
            .Setup(r => r.GetByUsernameAsync("admin"))
            .ReturnsAsync(user);

        var request = new LoginRequest { Username = "admin", Password = "WrongPassword" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.LoginAsync(request));
        Assert.Equal("Invalid username or password.", ex.Message);
    }

    [Fact]
    public async Task LoginAsync_ValidLogin_TokenContainsCorrectClaims()
    {
        // Arrange
        var user = new User
        {
            Id = 5,
            Username = "testuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@123"),
            FullName = "Test User",
            Role = "User"
        };

        _userRepositoryMock
            .Setup(r => r.GetByUsernameAsync("testuser"))
            .ReturnsAsync(user);

        var request = new LoginRequest { Username = "testuser", Password = "Test@123" };

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert — decode and verify claims
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result.Token);

        Assert.Equal("5", token.Claims.First(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier).Value);
        Assert.Equal("testuser", token.Claims.First(c => c.Type == System.Security.Claims.ClaimTypes.Name).Value);
        Assert.Equal("User", token.Claims.First(c => c.Type == System.Security.Claims.ClaimTypes.Role).Value);
        Assert.Equal("TestIssuer", token.Issuer);
    }
}
