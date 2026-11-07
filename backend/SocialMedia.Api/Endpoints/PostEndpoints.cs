using System.Security.Claims;
using SocialMedia.Api.DTOs;
using SocialMedia.Api.Models;
using SocialMedia.Api.Repositories;
using SocialMedia.Api.Validators;

namespace SocialMedia.Api.Endpoints;

public static class PostEndpoints
{
    public static void MapPostEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/posts");

        group.MapGet("/", async (IPostRepository postRepo) =>
        {
            var posts = await postRepo.GetAllAsync();
            var response = posts
                .Select(p => new PostResponse(p.Id, p.AuthorId, p.Title, p.Content, p.CreatedAt))
                .ToList();

            return Results.Ok(response);
        });

        group.MapGet("/{id:guid}", async (Guid id, IPostRepository postRepo) =>
        {
            var post = await postRepo.GetByIdAsync(id);
            if (post is null)
            {
                return Results.NotFound(new ErrorResponse("Bài đăng không tồn tại."));
            }

            var response = new PostResponse(post.Id, post.AuthorId, post.Title, post.Content, post.CreatedAt);
            return Results.Ok(response);
        });

        // POST /api/posts - Tạo bài đăng mới (Áp dụng giới hạn ký tự và kiểm tra nội dung rỗng)
        group.MapPost("/", async (
            CreatePostRequest? request,
            HttpContext httpContext,
            IPostRepository postRepo) =>
        {
            if (httpContext.User.Identity?.IsAuthenticated != true)
            {
                return Results.Json(new ErrorResponse("Vui lòng đăng nhập để tạo bài đăng."), statusCode: StatusCodes.Status401Unauthorized);
            }

            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? httpContext.User.FindFirst("userId")?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Results.Json(new ErrorResponse("Không thể xác định danh tính người dùng."), statusCode: StatusCodes.Status401Unauthorized);
            }

            // AC5, AC6, AC7, AC8: Kiểm tra giới hạn 1-500 ký tự code point và kiểm tra rỗng / khoảng trắng
            var (isValid, errorMessage) = PostValidator.ValidateContent(request?.Content);
            if (!isValid)
            {
                return Results.BadRequest(new ErrorResponse(errorMessage ?? "Nội dung bài đăng không hợp lệ."));
            }

            // Dùng ! vì PostValidator.ValidateContent đã đảm bảo request và request.Content không null
            var validContent = request!.Content!;

            var post = new Post
            {
                Id = Guid.NewGuid(),
                AuthorId = userId,
                Title = request.Title ?? string.Empty,
                Content = validContent,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await postRepo.AddAsync(post);

            var response = new PostResponse(post.Id, post.AuthorId, post.Title, post.Content, post.CreatedAt);
            return Results.Created($"/api/posts/{post.Id}", response);
        });
    }
}
