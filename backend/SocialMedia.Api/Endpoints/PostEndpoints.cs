using System.Security.Claims;
using SocialMedia.Api.DTOs;
using SocialMedia.Api.Repositories;

namespace SocialMedia.Api.Endpoints;

public static class PostEndpoints
{
    public static void MapPostEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/posts");

        group.MapGet("/", async (
            HttpContext httpContext,
            IPostRepository postRepo,
            IPostLikeRepository likeRepo) =>
        {
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? httpContext.User.FindFirst("userId")?.Value;

            var posts = await postRepo.GetAllAsync();
            var response = new List<PostResponse>();
            foreach (var p in posts)
            {
                var likeCount = await likeRepo.GetLikeCountAsync(p.Id);
                var isLiked = !string.IsNullOrEmpty(userId) && await likeRepo.HasUserLikedAsync(p.Id, userId);
                response.Add(new PostResponse(p.Id, p.AuthorId, p.Title, p.Content, p.CreatedAt, likeCount, isLiked));
            }

            return Results.Ok(response);
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            HttpContext httpContext,
            IPostRepository postRepo,
            IPostLikeRepository likeRepo) =>
        {
            var post = await postRepo.GetByIdAsync(id);
            if (post is null)
            {
                return Results.NotFound(new ErrorResponse("Bài đăng không tồn tại."));
            }

            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? httpContext.User.FindFirst("userId")?.Value;

            var likeCount = await likeRepo.GetLikeCountAsync(post.Id);
            var isLiked = !string.IsNullOrEmpty(userId) && await likeRepo.HasUserLikedAsync(post.Id, userId);
            var response = new PostResponse(post.Id, post.AuthorId, post.Title, post.Content, post.CreatedAt, likeCount, isLiked);
            return Results.Ok(response);
        });

        // POST /api/posts/{id}/like - Thích / bỏ thích (toggle) bài đăng (AC1, AC2)
        group.MapPost("/{id:guid}/like", async (
            Guid id,
            HttpContext httpContext,
            IPostRepository postRepo,
            IPostLikeRepository likeRepo) =>
        {
            // Kiểm tra xác thực (AC4)
            if (httpContext.User.Identity?.IsAuthenticated != true)
            {
                return Results.Json(new ErrorResponse("Vui lòng đăng nhập để thích bài đăng."), statusCode: StatusCodes.Status401Unauthorized);
            }

            var post = await postRepo.GetByIdAsync(id);
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

            if (!post.CanView(userId))
            {
                return Results.Json(new ErrorResponse("Bạn không có quyền xem bài đăng này."), statusCode: StatusCodes.Status403Forbidden);
            }

            var result = await likeRepo.ToggleLikeAsync(id, userId);
            return Results.Ok(new LikeResponse(id, result.IsLiked, result.LikeCount));
        });

        // DELETE /api/posts/{id}/like - Bỏ thích bài đăng một cách tường minh
        group.MapDelete("/{id:guid}/like", async (
            Guid id,
            HttpContext httpContext,
            IPostRepository postRepo,
            IPostLikeRepository likeRepo) =>
        {
            if (httpContext.User.Identity?.IsAuthenticated != true)
            {
                return Results.Json(new ErrorResponse("Vui lòng đăng nhập để bỏ thích bài đăng."), statusCode: StatusCodes.Status401Unauthorized);
            }

            var post = await postRepo.GetByIdAsync(id);
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

            if (!post.CanView(userId))
            {
                return Results.Json(new ErrorResponse("Bạn không có quyền xem bài đăng này."), statusCode: StatusCodes.Status403Forbidden);
            }

            var isLiked = await likeRepo.HasUserLikedAsync(id, userId);
            if (isLiked)
            {
                var result = await likeRepo.ToggleLikeAsync(id, userId);
                return Results.Ok(new LikeResponse(id, result.IsLiked, result.LikeCount));
            }

            var count = await likeRepo.GetLikeCountAsync(id);
            return Results.Ok(new LikeResponse(id, false, count));
        });
    }
}
