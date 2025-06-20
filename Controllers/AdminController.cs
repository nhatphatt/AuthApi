using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AuthApi.Data;
using AuthApi.Models;

namespace AuthApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApplicationDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get database information and table counts
        /// </summary>
        [HttpGet("database-info")]
        public async Task<IActionResult> GetDatabaseInfo()
        {
            try
            {
                var userCount = await _context.Users.CountAsync();
                var chatHistoryCount = await _context.ChatHistories.CountAsync();
                var subscriptionCount = await _context.Subscriptions.CountAsync();

                var info = new
                {
                    DatabaseType = _context.Database.ProviderName,
                    ConnectionString = _context.Database.GetConnectionString()?.Substring(0, 50) + "...", // Hide sensitive info
                    Tables = new
                    {
                        Users = userCount,
                        ChatHistories = chatHistoryCount,
                        Subscriptions = subscriptionCount
                    },
                    ServerTime = DateTime.UtcNow,
                    CanConnect = _context.Database.CanConnect()
                };

                return Ok(info);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting database info");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get all users (for development only)
        /// </summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _context.Users
                    .Select(u => new
                    {
                        u.Id,
                        u.Username,
                        u.Role,
                        u.CreatedAt,
                        u.UpdatedAt,
                        // Don't expose password hash
                        HasPassword = !string.IsNullOrEmpty(u.PasswordHash)
                    })
                    .ToListAsync();

                return Ok(new { count = users.Count, users });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get chat histories
        /// </summary>
        [HttpGet("chat-histories")]
        public async Task<IActionResult> GetChatHistories([FromQuery] int limit = 50)
        {
            try
            {
                var chats = await _context.ChatHistories
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(limit)
                    .Select(c => new
                    {
                        c.Id,
                        c.UserId,
                        UserName = c.User != null ? c.User.Username : "Unknown",
                        c.UserMessage,
                        c.AiResponse,
                        c.TokensUsed,
                        c.Model,
                        c.CreatedAt
                    })
                    .ToListAsync();

                return Ok(new { count = chats.Count, limit, chats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chat histories");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get subscriptions
        /// </summary>
        [HttpGet("subscriptions")]
        public async Task<IActionResult> GetSubscriptions()
        {
            try
            {
                var subscriptions = await _context.Subscriptions
                    .Select(s => new
                    {
                        s.Id,
                        s.UserId,
                        UserName = s.User != null ? s.User.Username : "Unknown",
                        s.PlanType,
                        s.Amount,
                        s.IsPaid,
                        s.PaidAt,
                        s.ExpiresAt,
                        s.ChatTokensUsed,
                        s.ChatTokensLimit,
                        s.CreatedAt,
                        s.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(new { count = subscriptions.Count, subscriptions });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscriptions");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Execute custom SQL query (careful!)
        /// </summary>
        [HttpPost("query")]
        public async Task<IActionResult> ExecuteQuery([FromBody] QueryRequest request)
        {
            try
            {
                // Only allow SELECT queries for safety
                if (!request.Sql.Trim().ToUpper().StartsWith("SELECT"))
                {
                    return BadRequest(new { error = "Only SELECT queries are allowed" });
                }

                // Simple protection against dangerous queries
                var dangerousKeywords = new[] { "DROP", "DELETE", "TRUNCATE", "ALTER", "CREATE", "INSERT", "UPDATE" };
                var upperSql = request.Sql.ToUpper();
                
                if (dangerousKeywords.Any(keyword => upperSql.Contains(keyword)))
                {
                    return BadRequest(new { error = "Query contains dangerous keywords" });
                }

                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = request.Sql;
                
                using var reader = await command.ExecuteReaderAsync();
                var results = new List<Dictionary<string, object>>();

                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.GetValue(i);
                    }
                    results.Add(row);
                }

                return Ok(new { query = request.Sql, results, count = results.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing query: {Query}", request.Sql);
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    public class QueryRequest
    {
        public string Sql { get; set; } = string.Empty;
    }
} 