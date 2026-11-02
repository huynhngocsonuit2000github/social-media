using System.Security.Claims;
using SocialMedia.Api.DTOs;
using SocialMedia.Api.Repositories;

namespace SocialMedia.Api.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications");

        // GET /api/notifications - Lấy danh sách thông báo trong Notification Center của người dùng đăng nhập
        group.MapGet("/", async (HttpContext httpContext, INotificationRepository notificationRepo) =>
        {
            if (httpContext.User.Identity?.IsAuthenticated != true)
            {
                return Results.Json(new ErrorResponse("Vui lòng đăng nhập để xem thông báo."), statusCode: StatusCodes.Status401Unauthorized);
            }

            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? httpContext.User.FindFirst("userId")?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Results.Json(new ErrorResponse("Không thể xác định danh tính người dùng."), statusCode: StatusCodes.Status401Unauthorized);
            }

            var notifications = await notificationRepo.GetByUserIdAsync(userId);
            var response = notifications
                .Select(n => new NotificationResponse(
                    n.Id,
                    n.RecipientUserId,
                    n.ActorUserId,
                    n.ActorName,
                    n.PostId,
                    n.CommentId,
                    n.Message,
                    n.CreatedAt,
                    n.IsRead
                ))
                .ToList();

            return Results.Ok(response);
        });
    }
}
