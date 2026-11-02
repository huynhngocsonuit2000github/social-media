using SocialMedia.Api.DTOs;
using SocialMedia.Api.Repositories;

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
                .Select(p => new PostResponse(p.Id, p.AuthorId, p.Title, p.Content, p.CreatedAt, p.AllowedViewerIds?.ToList()))
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

            var response = new PostResponse(post.Id, post.AuthorId, post.Title, post.Content, post.CreatedAt, post.AllowedViewerIds?.ToList());
            return Results.Ok(response);
        });
    }
}
