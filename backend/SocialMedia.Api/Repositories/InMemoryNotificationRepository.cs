using System.Collections.Concurrent;
using SocialMedia.Api.Models;

namespace SocialMedia.Api.Repositories;

public class InMemoryNotificationRepository : INotificationRepository
{
    private readonly ConcurrentDictionary<Guid, Notification> _notifications = new();

    public Task<IReadOnlyList<Notification>> GetByUserIdAsync(string userId)
    {
        var list = _notifications.Values
            .Where(n => string.Equals(n.RecipientUserId, userId, StringComparison.Ordinal))
            .OrderByDescending(n => n.CreatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<Notification>>(list);
    }

    public Task<Notification> AddAsync(Notification notification)
    {
        _notifications[notification.Id] = notification;
        return Task.FromResult(notification);
    }

    public Task DeleteByPostIdAsync(Guid postId)
    {
        var toRemove = _notifications.Values
            .Where(n => n.PostId == postId)
            .Select(n => n.Id)
            .ToList();

        foreach (var id in toRemove)
        {
            _notifications.TryRemove(id, out _);
        }

        return Task.CompletedTask;
    }
}
