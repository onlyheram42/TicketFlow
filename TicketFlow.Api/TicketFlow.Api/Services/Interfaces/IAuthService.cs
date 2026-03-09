using TicketFlow.Api.DTOs.Auth;

namespace TicketFlow.Api.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
}
