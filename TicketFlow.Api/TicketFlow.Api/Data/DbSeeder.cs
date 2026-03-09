using Dapper;
using TicketFlow.Api.Data;

namespace TicketFlow.Api;

public class DbSeeder
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<DbSeeder> _logger;

    public DbSeeder(IDbConnectionFactory connectionFactory, ILogger<DbSeeder> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        using var connection = _connectionFactory.CreateConnection();

        var existingCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Users");
        if (existingCount > 0)
        {
            _logger.LogInformation("Database already seeded. Skipping.");
            return;
        }

        _logger.LogInformation("Seeding database with default users...");

        var adminHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
        var userHash = BCrypt.Net.BCrypt.HashPassword("User@123");

        const string sql = @"
            INSERT INTO Users (Username, PasswordHash, FullName, Role)
            VALUES (@Username, @PasswordHash, @FullName, @Role)";

        await connection.ExecuteAsync(sql, new { Username = "admin", PasswordHash = adminHash, FullName = "System Admin", Role = "Admin" });
        await connection.ExecuteAsync(sql, new { Username = "john", PasswordHash = userHash, FullName = "John Doe", Role = "User" });

        _logger.LogInformation("Database seeded successfully.");
    }
}
