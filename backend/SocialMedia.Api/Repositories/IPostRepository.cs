using SocialMedia.Api.Models;

namespace SocialMedia.Api.Repositories;

public interface IPostRepository
{
    Task<Post?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Post>> GetAllAsync();
    Task<Post> AddAsync(Post post);
    Task<bool> DeleteAsync(Guid id);
}
