using SocialMedia.Api.Models;

namespace SocialMedia.Api.Repositories;

public interface ICommentRepository
{
    Task<Comment> AddAsync(Comment comment);
    Task<IReadOnlyList<Comment>> GetByPostIdAsync(Guid postId);
    Task<int> GetCountByPostIdAsync(Guid postId);
    Task DeleteByPostIdAsync(Guid postId);
}
