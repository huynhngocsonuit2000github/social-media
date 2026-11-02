using Microsoft.AspNetCore.Authentication;
using SocialMedia.Api.Auth;
using SocialMedia.Api.Endpoints;
using SocialMedia.Api.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình CORS: Giới hạn theo origin của frontend (không dùng AllowAnyOrigin)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:4200", "http://127.0.0.1:4200"];
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configure Authentication (Sử dụng SimpleAuthenticationHandler trong Development/Testing)
builder.Services.AddAuthentication(SimpleAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, SimpleAuthenticationHandler>(
        SimpleAuthenticationHandler.SchemeName, null);
builder.Services.AddAuthorization();

// Register Repositories
builder.Services.AddSingleton<ICommentRepository, InMemoryCommentRepository>();
builder.Services.AddSingleton<IPostRepository, InMemoryPostRepository>();

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Endpoints
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapPostEndpoints();
app.MapCommentEndpoints();

app.Run();

public partial class Program { }
