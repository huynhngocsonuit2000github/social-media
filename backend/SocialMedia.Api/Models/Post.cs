namespace SocialMedia.Api.Models;

public class Post
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string AuthorId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
