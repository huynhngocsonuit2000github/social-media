using System.Collections.Concurrent;
using SocialMedia.Api.Models;

namespace SocialMedia.Api.Repositories;

public class InMemoryCommentRepository : ICommentRepository
{
    private readonly ConcurrentDictionary<Guid, Comment> _comments = new();

    public Task<Comment> AddAsync(Comment comment)
    {
        _comments[comment.Id] = comment;
        return Task.FromResult(comment);
    }

    public Task<IReadOnlyList<Comment>> GetByPostIdAsync(Guid postId)
    {
        var list = _comments.Values
            .Where(c => c.PostId == postId)
            .OrderBy(c => c.CreatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<Comment>>(list);
    }

    public Task DeleteByPostIdAsync(Guid postId)
    {
        var keysToRemove = _comments.Values
            .Where(c => c.PostId == postId)
            .Select(c => c.Id)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _comments.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }
}
