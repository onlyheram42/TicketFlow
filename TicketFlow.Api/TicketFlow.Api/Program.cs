using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TicketFlow.Api.Data;
using TicketFlow.Api.Middleware;
using TicketFlow.Api.Repositories;
using TicketFlow.Api.Repositories.Interfaces;
using TicketFlow.Api.Services;
using TicketFlow.Api.Services.Interfaces;

namespace TicketFlow.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var configuration = builder.Configuration;

        // ── Data Access ──────────────────────────────────────────
        builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();

        // ── Repositories ─────────────────────────────────────────
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<ITicketRepository, TicketRepository>();
        builder.Services.AddScoped<ITicketCommentRepository, TicketCommentRepository>();
        builder.Services.AddScoped<ITicketStatusHistoryRepository, TicketStatusHistoryRepository>();

        // ── Services ─────────────────────────────────────────────
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<ITicketService, TicketService>();

        // ── JWT Authentication ───────────────────────────────────
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey not configured.");

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
            };
        });

        builder.Services.AddAuthorization();

        // ── Controllers ──────────────────────────────────────────
        builder.Services.AddControllers();

        // ── Swagger / OpenAPI ────────────────────────────────────
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "TicketFlow API",
                Version = "v1",
                Description = "Customer Support Ticket System Web API"
            });

            // Add JWT bearer auth to Swagger UI
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter your JWT token. Example: eyJhbGciOi..."
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        // ── CORS ─────────────────────────────────────────────────
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowDesktopApp", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        var app = builder.Build();

        // ── Middleware Pipeline ───────────────────────────────────
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "TicketFlow API v1");
            });
        }

        app.UseHttpsRedirection();
        app.UseCors("AllowDesktopApp");
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        // Seed default data on first run
        using (var scope = app.Services.CreateScope())
        {
            var seeder = new DbSeeder(
                scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>(),
                scope.ServiceProvider.GetRequiredService<ILogger<DbSeeder>>());
            await seeder.SeedAsync();
        }

        app.Run();
    }
}
