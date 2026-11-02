namespace SocialMedia.Api.Models;

public class Post
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string AuthorId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Danh sách User ID được phép xem bài đăng. Nếu null hoặc rỗng, bài đăng ở chế độ công khai (mọi người đều có quyền xem).
    /// </summary>
    public HashSet<string>? AllowedViewerIds { get; set; }

    /// <summary>
    /// Kiểm tra người dùng có quyền xem bài đăng hay không.
    /// </summary>
    public bool CanView(string? userId)
    {
        // Tác giả luôn có quyền xem bài viết của chính mình
        if (!string.IsNullOrEmpty(userId) && userId == AuthorId)
        {
            return true;
        }

        // Bài viết công khai: mọi người đều có quyền xem
        if (AllowedViewerIds is null || AllowedViewerIds.Count == 0)
        {
            return true;
        }

        // Bài viết giới hạn quyền xem: yêu cầu người dùng nằm trong danh sách AllowedViewerIds
        return !string.IsNullOrEmpty(userId) && AllowedViewerIds.Contains(userId);
    }
}
