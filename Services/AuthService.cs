using Microsoft.EntityFrameworkCore;
using AuthApi.Data;
using AuthApi.DTOs;
using AuthApi.Interfaces;
using AuthApi.Models;

namespace AuthApi.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly IConfiguration _configuration;

        public AuthService(ApplicationDbContext context, IJwtService jwtService, IConfiguration configuration)
        {
            _context = context;
            _jwtService = jwtService;
            _configuration = configuration;
        }

        public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request)
        {
            try
            {
                // Check if user already exists
                if (await UserExistsAsync(request.Username))
                {
                    return new ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "Username already exists"
                    };
                }

                // Hash password
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

                // Create new user
                var user = new User
                {
                    Username = request.Username,
                    PasswordHash = passwordHash,
                    Role = request.Role,
                    Tokens = 500, // Initial tokens for new users
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Generate JWT token
                var token = _jwtService.GenerateToken(user);
                var expiryInMinutes = int.Parse(_configuration["JwtSettings:ExpiryInMinutes"] ?? "60");

                return new ApiResponse<AuthResponse>
                {
                    Success = true,
                    Message = "User registered successfully",
                    Data = new AuthResponse
                    {
                        Token = token,
                        Username = user.Username,
                        Role = user.Role,
                        ExpiresAt = DateTime.UtcNow.AddMinutes(expiryInMinutes)
                    }
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = $"Registration failed: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request)
        {
            try
            {
                // Find user by username
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
                if (user == null)
                {
                    return new ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "Invalid username or password"
                    };
                }

                // Verify password
                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    return new ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "Invalid username or password"
                    };
                }

                // Generate JWT token
                var token = _jwtService.GenerateToken(user);
                var expiryInMinutes = int.Parse(_configuration["JwtSettings:ExpiryInMinutes"] ?? "60");

                return new ApiResponse<AuthResponse>
                {
                    Success = true,
                    Message = "Login successful",
                    Data = new AuthResponse
                    {
                        Token = token,
                        Username = user.Username,
                        Role = user.Role,
                        ExpiresAt = DateTime.UtcNow.AddMinutes(expiryInMinutes)
                    }
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = $"Login failed: {ex.Message}"
                };
            }
        }

        public async Task<bool> UserExistsAsync(string username)
        {
            return await _context.Users.AnyAsync(u => u.Username == username);
        }
    }
} 