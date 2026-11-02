using Microsoft.AspNetCore.Authentication;
using SocialMedia.Api.Auth;
using SocialMedia.Api.Endpoints;
using SocialMedia.Api.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Configure CORS for frontend access
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Configure Authentication
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
