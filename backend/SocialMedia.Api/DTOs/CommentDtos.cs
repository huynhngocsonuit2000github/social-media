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
    DateTimeOffset CreatedAt
);

public record ErrorResponse(string Error);
