using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace SocialMedia.Api.Auth;

public class SimpleAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "SimpleAuth";

    public SimpleAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? userId = null;
        string? userName = null;

        // 1. Check X-User-Id header
        if (Request.Headers.TryGetValue("X-User-Id", out var xUserIdHeader) && !string.IsNullOrWhiteSpace(xUserIdHeader))
        {
            userId = xUserIdHeader.ToString().Trim();
            if (Request.Headers.TryGetValue("X-User-Name", out var xUserNameHeader) && !string.IsNullOrWhiteSpace(xUserNameHeader))
            {
                userName = xUserNameHeader.ToString().Trim();
            }
        }
        // 2. Check Authorization Bearer header
        else if (Request.Headers.TryGetValue("Authorization", out var authHeader) && !string.IsNullOrWhiteSpace(authHeader))
        {
            var headerValue = authHeader.ToString().Trim();
            if (headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = headerValue["Bearer ".Length..].Trim();
                if (!string.IsNullOrWhiteSpace(token))
                {
                    userId = token;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        userName ??= userId;

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, userName),
            new Claim("userId", userId),
            new Claim("name", userName)
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
