using System.Security.Claims;
using SocialMedia.Api.DTOs;
using SocialMedia.Api.Models;
using SocialMedia.Api.Repositories;
using SocialMedia.Api.Validators;

namespace SocialMedia.Api.Endpoints;

public static class CommentEndpoints
{
    public static void MapCommentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/posts/{postId:guid}/comments");

        // GET /api/posts/{postId}/comments - Lấy danh sách bình luận (AC3: sắp xếp cũ nhất trước, mới nhất cuối)
        group.MapGet("/", async (
            Guid postId,
            HttpContext httpContext,
            IPostRepository postRepo,
            ICommentRepository commentRepo) =>
        {
            var post = await postRepo.GetByIdAsync(postId);
            if (post is null)
            {
                return Results.NotFound(new ErrorResponse("Bài đăng không tồn tại."));
            }

            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? httpContext.User.FindFirst("userId")?.Value;

            // AC2: Kiểm tra quyền xem bài đăng
            if (!post.CanView(userId))
            {
                return Results.Json(new ErrorResponse("Bạn không có quyền xem bài đăng này."), statusCode: StatusCodes.Status403Forbidden);
            }

            var comments = await commentRepo.GetByPostIdAsync(postId);
            var response = comments
                .Select(c => new CommentResponse(c.Id, c.PostId, c.UserId, c.AuthorName, c.Content, c.CreatedAt))
                .ToList();

            return Results.Ok(response);
        });

        // POST /api/posts/{postId}/comments - Tạo bình luận mới
        group.MapPost("/", async (
            Guid postId,
            CreateCommentRequest? request,
            HttpContext httpContext,
            IPostRepository postRepo,
            ICommentRepository commentRepo,
            INotificationRepository notificationRepo) =>
        {
            // Kiểm tra xác thực: Chưa đăng nhập
            if (httpContext.User.Identity?.IsAuthenticated != true)
            {
                return Results.Json(new ErrorResponse("Vui lòng đăng nhập để bình luận."), statusCode: StatusCodes.Status401Unauthorized);
            }

            // Kiểm tra bài đăng tồn tại
            var post = await postRepo.GetByIdAsync(postId);
            if (post is null)
            {
                return Results.NotFound(new ErrorResponse("Bài đăng không tồn tại."));
            }

            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? httpContext.User.FindFirst("userId")?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Results.Json(new ErrorResponse("Không thể xác định danh tính người dùng."), statusCode: StatusCodes.Status401Unauthorized);
            }

            // AC2: Kiểm tra quyền xem bài đăng
            if (!post.CanView(userId))
            {
                return Results.Json(new ErrorResponse("Bạn không có quyền xem bài đăng này."), statusCode: StatusCodes.Status403Forbidden);
            }

            // AC5, AC6, AC7: Validate nội dung (1-500 ký tự code point, không rỗng/khoảng trắng)
            var (isValid, errorMessage) = CommentValidator.ValidateContent(request?.Content);
            if (!isValid)
            {
                // errorMessage sẽ không bao giờ null khi isValid == false theo thiết kế của CommentValidator
                return Results.BadRequest(new ErrorResponse(errorMessage ?? "Nội dung bình luận không hợp lệ."));
            }

            var authorName = httpContext.User.FindFirst(ClaimTypes.Name)?.Value
                ?? httpContext.User.FindFirst("name")?.Value
                ?? userId;

            // Dùng ! vì CommentValidator.ValidateContent(request?.Content) ở trên đã xác thực
            // request và request.Content không null và không chỉ chứa khoảng trắng.
            var validContent = request!.Content!;

            var comment = new Comment
            {
                PostId = postId,
                UserId = userId,
                AuthorName = authorName,
                Content = validContent,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await commentRepo.AddAsync(comment);

            // AC4: Khi bình luận thành công trên bài đăng của người khác, gửi thông báo cho chủ bài đăng
            if (!string.Equals(userId, post.AuthorId, StringComparison.Ordinal))
            {
                var notification = new Notification
                {
                    RecipientUserId = post.AuthorId,
                    ActorUserId = userId,
                    ActorName = authorName,
                    PostId = postId,
                    CommentId = comment.Id,
                    Message = $"{authorName} đã bình luận về bài viết của bạn.",
                    CreatedAt = DateTimeOffset.UtcNow
                };

                await notificationRepo.AddAsync(notification);
            }

            var response = new CommentResponse(
                comment.Id,
                comment.PostId,
                comment.UserId,
                comment.AuthorName,
                comment.Content,
                comment.CreatedAt
            );

            return Results.Created($"/api/posts/{postId}/comments/{comment.Id}", response);
        });
    }
}
