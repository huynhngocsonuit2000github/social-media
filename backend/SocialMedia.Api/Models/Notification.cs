namespace SocialMedia.Api.Models;

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string RecipientUserId { get; set; } = string.Empty;
    public string ActorUserId { get; set; } = string.Empty;
    public string ActorName { get; set; } = string.Empty;
    public Guid PostId { get; set; }
    public Guid CommentId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsRead { get; set; }
}
