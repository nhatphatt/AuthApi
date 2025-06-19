using AuthApi.Models;

namespace AuthApi.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(User user);
        bool ValidateToken(string token);
        string? GetUsernameFromToken(string token);
    }
} 