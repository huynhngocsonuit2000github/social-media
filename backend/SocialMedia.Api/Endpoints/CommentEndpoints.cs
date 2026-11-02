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

        // GET /api/posts/{postId}/comments - Lấy danh sách bình luận (AC5: sắp xếp cũ nhất trước, mới nhất cuối)
        group.MapGet("/", async (Guid postId, IPostRepository postRepo, ICommentRepository commentRepo) =>
        {
            var post = await postRepo.GetByIdAsync(postId);
            if (post is null)
            {
                return Results.NotFound(new ErrorResponse("Bài đăng không tồn tại."));
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
            ICommentRepository commentRepo) =>
        {
            // AC7: Người dùng chưa đăng nhập
            if (httpContext.User.Identity?.IsAuthenticated != true)
            {
                return Results.Json(new ErrorResponse("Vui lòng đăng nhập để bình luận."), statusCode: StatusCodes.Status401Unauthorized);
            }

            // AC8: Bình luận trên bài đăng không tồn tại
            var post = await postRepo.GetByIdAsync(postId);
            if (post is null)
            {
                return Results.NotFound(new ErrorResponse("Bài đăng không tồn tại."));
            }

            // AC3, AC4: Validate nội dung (1-500 ký tự code point, không rỗng/khoảng trắng)
            var (isValid, errorMessage) = CommentValidator.ValidateContent(request?.Content);
            if (!isValid)
            {
                // errorMessage sẽ không bao giờ null khi isValid == false theo thiết kế của CommentValidator
                return Results.BadRequest(new ErrorResponse(errorMessage ?? "Nội dung bình luận không hợp lệ."));
            }

            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? httpContext.User.FindFirst("userId")?.Value
                ?? "anonymous";
            var authorName = httpContext.User.FindFirst(ClaimTypes.Name)?.Value
                ?? httpContext.User.FindFirst("name")?.Value
                ?? userId;

            // request và request.Content đã được kiểm tra tính hợp lệ qua CommentValidator.ValidateContent
            var comment = new Comment
            {
                PostId = postId,
                UserId = userId,
                AuthorName = authorName,
                Content = request?.Content ?? string.Empty,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await commentRepo.AddAsync(comment);

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
