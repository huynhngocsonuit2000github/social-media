namespace SocialMedia.Api.Repositories;

public record LikeToggleResult(bool IsLiked, int LikeCount);

public interface IPostLikeRepository
{
    Task<bool> HasUserLikedAsync(Guid postId, string userId);
    Task<int> GetLikeCountAsync(Guid postId);
    Task<LikeToggleResult> ToggleLikeAsync(Guid postId, string userId);
    Task DeleteByPostIdAsync(Guid postId);
}
