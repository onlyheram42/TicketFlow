using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

namespace TicketFlow.Desktop.Services
{
    public class SessionManager
    {
        private static SessionManager? _instance;
        public static SessionManager Instance => _instance ??= new SessionManager();

        public string? Token { get; private set; }
        public string? FullName { get; private set; }
        public string? Role { get; private set; }
        public int? UserId { get; private set; }

        public bool IsLoggedIn => !string.IsNullOrEmpty(Token);
        public bool IsAdmin => Role == "Admin";

        public event Action? SessionChanged;

        private SessionManager() { }

        public void Login(string token, string fullName, string role)
        {
            Token = token;
            FullName = fullName;
            Role = role;

            // Extract UserId from JWT Claims
            var handler = new JwtSecurityTokenHandler();
            if (handler.CanReadToken(token))
            {
                var jwtToken = handler.ReadJwtToken(token);
                var nameIdentifierClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "nameid" || c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
                if (nameIdentifierClaim != null && int.TryParse(nameIdentifierClaim.Value, out int userId))
                {
                    UserId = userId;
                }
            }

            SessionChanged?.Invoke();
        }

        public void Logout()
        {
            Token = null;
            FullName = null;
            Role = null;
            UserId = null;
            SessionChanged?.Invoke();
        }
    }
}
