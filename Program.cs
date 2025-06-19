using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using AuthApi.Data;
using AuthApi.Interfaces;
using AuthApi.Services;
using AuthApi.Helpers;

var builder = WebApplication.CreateBuilder(args);

// Configure port for cloud deployment (Railway, Heroku, etc.)
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Add services to the container
builder.Services.AddControllers();

// Database configuration
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL") 
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=authapi.db"; // Default to SQLite for production

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (connectionString.Contains("Data Source=") || connectionString.EndsWith(".db"))
    {
        // Use SQLite for local development or when SQLite connection string is provided
        options.UseSqlite(connectionString);
    }
    else
    {
        // Use SQL Server for other connection strings
        options.UseSqlServer(connectionString);
    }
});

// JWT configuration
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Register custom services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IChatService, ChatService>();

// Register HttpClient for OpenAI service
builder.Services.AddHttpClient<IOpenAIService, OpenAIService>();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Auth API",
        Version = "v1",
        Description = "A simple authentication API using JWT tokens"
    });

    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
// Enable Swagger in all environments for API testing
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Auth API V1");
    c.RoutePrefix = "swagger"; // Set Swagger UI at /swagger
});

// Only use HTTPS redirection in development
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Create database if it doesn't exist
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Attempting to create database...");
        context.Database.EnsureCreated();
        logger.LogInformation("Database created successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while creating the database. The application will continue to run but database features may not work.");
        // Don't throw the exception - let the app continue to start
        // This allows health checks to work even if database is unavailable
    }
}

app.Run();
