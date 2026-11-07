using System.Collections.Concurrent;
using SocialMedia.Api.Models;

namespace SocialMedia.Api.Repositories;

public class InMemoryPostRepository : IPostRepository
{
    private readonly ConcurrentDictionary<Guid, Post> _posts = new();
    private readonly ICommentRepository _commentRepository;
    private readonly INotificationRepository? _notificationRepository;
    private readonly IPostLikeRepository? _postLikeRepository;

    public static readonly Guid DefaultPostId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public InMemoryPostRepository(
        ICommentRepository commentRepository,
        INotificationRepository? notificationRepository = null,
        IPostLikeRepository? postLikeRepository = null)
    {
        _commentRepository = commentRepository;
        _notificationRepository = notificationRepository;
        _postLikeRepository = postLikeRepository;

        // Seed a default post for viewing and commenting
        var defaultPost = new Post
        {
            Id = DefaultPostId,
            AuthorId = "author-system",
            Title = "Chào mừng bạn đến với mạng xã hội",
            Content = "Đây là bài đăng mẫu. Mọi người hãy để lại bình luận để chia sẻ ý kiến nhé!",
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-1)
        };
        _posts[defaultPost.Id] = defaultPost;
    }

    public Task<Post?> GetByIdAsync(Guid id)
    {
        _posts.TryGetValue(id, out var post);
        return Task.FromResult(post);
    }

    public Task<IReadOnlyList<Post>> GetAllAsync()
    {
        var list = _posts.Values.OrderByDescending(p => p.CreatedAt).ToList();
        return Task.FromResult<IReadOnlyList<Post>>(list);
    }

    public Task<Post> AddAsync(Post post)
    {
        _posts[post.Id] = post;
        return Task.FromResult(post);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        if (_posts.TryRemove(id, out _))
        {
            // Cascade delete comments belonging to this post as requested by PO
            await _commentRepository.DeleteByPostIdAsync(id);
            if (_notificationRepository is not null)
            {
                await _notificationRepository.DeleteByPostIdAsync(id);
            }
            if (_postLikeRepository is not null)
            {
                await _postLikeRepository.DeleteByPostIdAsync(id);
            }
            return true;
        }

        return false;
    }
}
