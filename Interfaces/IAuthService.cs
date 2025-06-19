using AuthApi.DTOs;

namespace AuthApi.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request);
        Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request);
        Task<bool> UserExistsAsync(string username);
    }
} 