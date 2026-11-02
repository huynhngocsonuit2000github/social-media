namespace SocialMedia.Api.DTOs;

public record CreateCommentRequest(string? Content);

public record CommentResponse(
    Guid Id,
    Guid PostId,
    string UserId,
    string AuthorName,
    string Content,
    DateTimeOffset CreatedAt
);

public record PostResponse(
    Guid Id,
    string AuthorId,
    string Title,
    string Content,
    DateTimeOffset CreatedAt,
    IReadOnlyCollection<string>? AllowedViewerIds = null
);

public record NotificationResponse(
    Guid Id,
    string RecipientUserId,
    string ActorUserId,
    string ActorName,
    Guid PostId,
    Guid CommentId,
    string Message,
    DateTimeOffset CreatedAt,
    bool IsRead
);

public record ErrorResponse(string Error);
