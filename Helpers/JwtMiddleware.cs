using AuthApi.Interfaces;
using Microsoft.EntityFrameworkCore;
using AuthApi.Data;

namespace AuthApi.Helpers
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;

        public JwtMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, IJwtService jwtService, ApplicationDbContext dbContext)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (token != null)
            {
                await AttachUserToContext(context, jwtService, dbContext, token);
            }

            await _next(context);
        }

        private async Task AttachUserToContext(HttpContext context, IJwtService jwtService, ApplicationDbContext dbContext, string token)
        {
            try
            {
                if (jwtService.ValidateToken(token))
                {
                    var username = jwtService.GetUsernameFromToken(token);
                    if (!string.IsNullOrEmpty(username))
                    {
                        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
                        if (user != null)
                        {
                            // Attach user to context for further use in controllers
                            context.Items["User"] = user;
                        }
                    }
                }
            }
            catch
            {
                // Do nothing if JWT validation fails
                // User will not be attached to context
            }
        }
    }
} 