using SocialMedia.Api.DTOs;
using SocialMedia.Api.Models;
using SocialMedia.Api.Repositories;

namespace SocialMedia.Api.Endpoints;

public record CreatePostRequest(string Title, string Content, string? AuthorId);

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

        group.MapPost("/", async (CreatePostRequest request, IPostRepository postRepo) =>
        {
            var post = new Post
            {
                Title = request.Title,
                Content = request.Content,
                AuthorId = request.AuthorId ?? "anonymous",
                CreatedAt = DateTimeOffset.UtcNow
            };

            await postRepo.AddAsync(post);
            var response = new PostResponse(post.Id, post.AuthorId, post.Title, post.Content, post.CreatedAt);
            return Results.Created($"/api/posts/{post.Id}", response);
        });

        group.MapDelete("/{id:guid}", async (Guid id, IPostRepository postRepo) =>
        {
            var deleted = await postRepo.DeleteAsync(id);
            if (!deleted)
            {
                return Results.NotFound(new ErrorResponse("Bài đăng không tồn tại."));
            }

            return Results.NoContent();
        });
    }
}
