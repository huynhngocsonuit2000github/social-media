using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace SocialMedia.Api.Auth;

/// <summary>
/// Cơ chế xác thực giả lập tạm thời phục vụ Development và Testing theo giả định SM-4.
/// CẢNH BÁO: Không sử dụng trên môi trường Production vì định danh có thể bị giả mạo.
/// Môi trường Production bắt buộc tích hợp hệ thống xác thực tập trung (OAuth2 / OpenID Connect / JWT).
/// </summary>
public class SimpleAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "SimpleAuth";
    private readonly IHostEnvironment _environment;

    public SimpleAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IHostEnvironment environment)
        : base(options, logger, encoder)
    {
        _environment = environment;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Chặn hoàn toàn SimpleAuthenticationHandler trên môi trường Production để ngăn ngừa giả mạo danh tính
        if (_environment.IsProduction())
        {
            return Task.FromResult(AuthenticateResult.Fail(
                "SimpleAuthenticationHandler chỉ được phép sử dụng trong môi trường Development và Testing. " +
                "Production yêu cầu tích hợp dịch vụ xác thực thật (OAuth2/JWT)."));
        }

        string? userId = null;
        string? userName = null;

        // 1. Kiểm tra header X-User-Id
        if (Request.Headers.TryGetValue("X-User-Id", out var xUserIdHeader) && !string.IsNullOrWhiteSpace(xUserIdHeader))
        {
            userId = xUserIdHeader.ToString().Trim();
            if (Request.Headers.TryGetValue("X-User-Name", out var xUserNameHeader) && !string.IsNullOrWhiteSpace(xUserNameHeader))
            {
                userName = xUserNameHeader.ToString().Trim();
            }
        }
        // 2. Kiểm tra header Authorization Bearer
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
