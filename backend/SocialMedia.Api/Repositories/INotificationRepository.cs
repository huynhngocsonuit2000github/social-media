using SocialMedia.Api.Models;

namespace SocialMedia.Api.Repositories;

public interface INotificationRepository
{
    Task<IReadOnlyList<Notification>> GetByUserIdAsync(string userId);
    Task<Notification> AddAsync(Notification notification);
    Task DeleteByPostIdAsync(Guid postId);
}
