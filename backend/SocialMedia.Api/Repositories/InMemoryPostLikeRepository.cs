using System.Collections.Concurrent;

namespace SocialMedia.Api.Repositories;

public class InMemoryPostLikeRepository : IPostLikeRepository
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, DateTimeOffset>> _likes = new();

    public Task<bool> HasUserLikedAsync(Guid postId, string userId)
    {
        if (_likes.TryGetValue(postId, out var userLikes))
        {
            return Task.FromResult(userLikes.ContainsKey(userId));
        }

        return Task.FromResult(false);
    }

    public Task<int> GetLikeCountAsync(Guid postId)
    {
        if (_likes.TryGetValue(postId, out var userLikes))
        {
            return Task.FromResult(userLikes.Count);
        }

        return Task.FromResult(0);
    }

    public Task<LikeToggleResult> ToggleLikeAsync(Guid postId, string userId)
    {
        var userLikes = _likes.GetOrAdd(postId, _ => new ConcurrentDictionary<string, DateTimeOffset>());

        bool isLiked;
        lock (userLikes)
        {
            if (userLikes.ContainsKey(userId))
            {
                userLikes.TryRemove(userId, out _);
                isLiked = false;
            }
            else
            {
                userLikes[userId] = DateTimeOffset.UtcNow;
                isLiked = true;
            }
        }

        return Task.FromResult(new LikeToggleResult(isLiked, userLikes.Count));
    }

    public Task DeleteByPostIdAsync(Guid postId)
    {
        _likes.TryRemove(postId, out _);
        return Task.CompletedTask;
    }
}
