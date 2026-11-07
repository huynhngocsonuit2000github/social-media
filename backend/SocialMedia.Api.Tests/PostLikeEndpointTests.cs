using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SocialMedia.Api.DTOs;
using SocialMedia.Api.Models;
using SocialMedia.Api.Repositories;
using Xunit;

namespace SocialMedia.Api.Tests;

public class PostLikeEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public PostLikeEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private HttpClient CreateAuthenticatedClient(string userId = "test-user-1", string userName = "Test User")
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Id", userId);
        client.DefaultRequestHeaders.Add("X-User-Name", userName);
        return client;
    }

    [Fact]
    public async Task AC1_LikePost_WhenNotLikedYet_MarksAsLikedAndIncreasesCountByOne()
    {
        // Arrange: Tạo bài viết mới để kiểm tra độc lập
        var postRepo = _factory.Services.GetRequiredService<IPostRepository>();
        var post = new Post
        {
            Id = Guid.NewGuid(),
            Title = "Bài viết kiểm tra AC1",
            Content = "Nội dung bài viết",
            AuthorId = "author-ac1",
            CreatedAt = DateTimeOffset.UtcNow
        };
        await postRepo.AddAsync(post);

        using var client = CreateAuthenticatedClient("user-ac1", "Người dùng AC1");

        // Act: Bấm nút 'Thích'
        var response = await client.PostAsync($"/api/posts/{post.Id}/like", null);

        // Assert: 200 OK, IsLiked = true, LikeCount = 1
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var likeRes = await response.Content.ReadFromJsonAsync<LikeResponse>(JsonOptions);
        Assert.NotNull(likeRes);
        Assert.Equal(post.Id, likeRes.PostId);
        Assert.True(likeRes.IsLiked);
        Assert.Equal(1, likeRes.LikeCount);

        // Verify qua GET bài viết
        var getPostRes = await client.GetAsync($"/api/posts/{post.Id}");
        Assert.Equal(HttpStatusCode.OK, getPostRes.StatusCode);
        var postDetails = await getPostRes.Content.ReadFromJsonAsync<PostResponse>(JsonOptions);
        Assert.NotNull(postDetails);
        Assert.True(postDetails.IsLiked);
        Assert.Equal(1, postDetails.LikeCount);
    }

    [Fact]
    public async Task AC2_LikePost_WhenAlreadyLiked_UnmarksAndDecreasesCountByOne()
    {
        // Arrange: Tạo bài viết và thích trước đó
        var postRepo = _factory.Services.GetRequiredService<IPostRepository>();
        var post = new Post
        {
            Id = Guid.NewGuid(),
            Title = "Bài viết kiểm tra AC2",
            Content = "Nội dung bài viết",
            AuthorId = "author-ac2",
            CreatedAt = DateTimeOffset.UtcNow
        };
        await postRepo.AddAsync(post);

        using var client = CreateAuthenticatedClient("user-ac2", "Người dùng AC2");

        // Thích lần 1
        var firstLikeRes = await client.PostAsync($"/api/posts/{post.Id}/like", null);
        Assert.Equal(HttpStatusCode.OK, firstLikeRes.StatusCode);

        // Act: Bấm nút 'Thích' lần 2 (để bỏ thích)
        var secondLikeRes = await client.PostAsync($"/api/posts/{post.Id}/like", null);

        // Assert: 200 OK, IsLiked = false, LikeCount = 0
        Assert.Equal(HttpStatusCode.OK, secondLikeRes.StatusCode);
        var likeRes = await secondLikeRes.Content.ReadFromJsonAsync<LikeResponse>(JsonOptions);
        Assert.NotNull(likeRes);
        Assert.Equal(post.Id, likeRes.PostId);
        Assert.False(likeRes.IsLiked);
        Assert.Equal(0, likeRes.LikeCount);

        // Verify qua GET bài viết
        var getPostRes = await client.GetAsync($"/api/posts/{post.Id}");
        Assert.Equal(HttpStatusCode.OK, getPostRes.StatusCode);
        var postDetails = await getPostRes.Content.ReadFromJsonAsync<PostResponse>(JsonOptions);
        Assert.NotNull(postDetails);
        Assert.False(postDetails.IsLiked);
        Assert.Equal(0, postDetails.LikeCount);
    }

    [Fact]
    public async Task AC3_GetPostsList_DisplaysTotalLikesAndCurrentUserLikedStatusClearly()
    {
        // Arrange: Tạo bài viết và có 2 người thích bài viết này
        var postRepo = _factory.Services.GetRequiredService<IPostRepository>();
        var post = new Post
        {
            Id = Guid.NewGuid(),
            Title = "Bài viết kiểm tra AC3",
            Content = "Nội dung bài viết danh sách",
            AuthorId = "author-ac3",
            CreatedAt = DateTimeOffset.UtcNow
        };
        await postRepo.AddAsync(post);

        using var user1Client = CreateAuthenticatedClient("user-ac3-1", "User AC3 1");
        using var user2Client = CreateAuthenticatedClient("user-ac3-2", "User AC3 2");
        using var user3Client = CreateAuthenticatedClient("user-ac3-3", "User AC3 3");

        // user1 và user2 thích bài viết
        await user1Client.PostAsync($"/api/posts/{post.Id}/like", null);
        await user2Client.PostAsync($"/api/posts/{post.Id}/like", null);

        // Act & Assert 1: user1 xem danh sách bài đăng
        var listResUser1 = await user1Client.GetAsync("/api/posts");
        Assert.Equal(HttpStatusCode.OK, listResUser1.StatusCode);
        var postsUser1 = await listResUser1.Content.ReadFromJsonAsync<List<PostResponse>>(JsonOptions);
        Assert.NotNull(postsUser1);
        var targetPostUser1 = postsUser1.FirstOrDefault(p => p.Id == post.Id);
        Assert.NotNull(targetPostUser1);
        Assert.Equal(2, targetPostUser1.LikeCount);
        Assert.True(targetPostUser1.IsLiked);

        // Act & Assert 2: user3 (chưa thích) xem danh sách bài đăng
        var listResUser3 = await user3Client.GetAsync("/api/posts");
        Assert.Equal(HttpStatusCode.OK, listResUser3.StatusCode);
        var postsUser3 = await listResUser3.Content.ReadFromJsonAsync<List<PostResponse>>(JsonOptions);
        Assert.NotNull(postsUser3);
        var targetPostUser3 = postsUser3.FirstOrDefault(p => p.Id == post.Id);
        Assert.NotNull(targetPostUser3);
        Assert.Equal(2, targetPostUser3.LikeCount);
        Assert.False(targetPostUser3.IsLiked);
    }

    [Fact]
    public async Task AC4_LikePost_WhenUnauthenticated_ReturnsUnauthorizedAndDoesNotChangeLikeCount()
    {
        // Arrange: Tạo bài đăng
        var postRepo = _factory.Services.GetRequiredService<IPostRepository>();
        var likeRepo = _factory.Services.GetRequiredService<IPostLikeRepository>();
        var post = new Post
        {
            Id = Guid.NewGuid(),
            Title = "Bài viết kiểm tra AC4",
            Content = "Nội dung bài viết",
            AuthorId = "author-ac4",
            CreatedAt = DateTimeOffset.UtcNow
        };
        await postRepo.AddAsync(post);

        // Giả sử đã có 1 lượt thích từ trước
        await likeRepo.ToggleLikeAsync(post.Id, "existing-user");

        using var unauthenticatedClient = _factory.CreateClient();

        // Act: Người dùng chưa đăng nhập bấm 'Thích'
        var response = await unauthenticatedClient.PostAsync($"/api/posts/{post.Id}/like", null);

        // Assert: 401 Unauthorized kèm thông báo yêu cầu đăng nhập
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        Assert.NotNull(error);
        Assert.Contains("Vui lòng đăng nhập", error.Error);

        // Verify: Số lượt thích không thay đổi
        var currentLikeCount = await likeRepo.GetLikeCountAsync(post.Id);
        Assert.Equal(1, currentLikeCount);
    }

    [Fact]
    public async Task LikePost_NonExistentPost_ReturnsNotFound()
    {
        using var client = CreateAuthenticatedClient();
        var nonExistentId = Guid.NewGuid();

        var response = await client.PostAsync($"/api/posts/{nonExistentId}/like", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        Assert.NotNull(error);
        Assert.Contains("Bài đăng không tồn tại", error.Error);
    }

    [Fact]
    public async Task LikePost_UserWithoutPermission_ReturnsForbidden()
    {
        var postRepo = _factory.Services.GetRequiredService<IPostRepository>();
        var restrictedPost = new Post
        {
            Id = Guid.NewGuid(),
            Title = "Bài viết riêng tư",
            Content = "Nội dung chỉ cho vip",
            AuthorId = "author-vip",
            AllowedViewerIds = ["vip-only"],
            CreatedAt = DateTimeOffset.UtcNow
        };
        await postRepo.AddAsync(restrictedPost);

        using var client = CreateAuthenticatedClient("regular-user", "Regular User");

        var response = await client.PostAsync($"/api/posts/{restrictedPost.Id}/like", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        Assert.NotNull(error);
        Assert.Contains("không có quyền xem", error.Error);
    }

    [Fact]
    public async Task PostDeletion_CascadesDeleteAllLikesForPost()
    {
        // Arrange
        var postRepo = _factory.Services.GetRequiredService<IPostRepository>();
        var likeRepo = _factory.Services.GetRequiredService<IPostLikeRepository>();

        var post = new Post
        {
            Id = Guid.NewGuid(),
            Title = "Bài viết sắp bị xóa để kiểm tra cascade like",
            Content = "Nội dung",
            AuthorId = "author-cascade",
            CreatedAt = DateTimeOffset.UtcNow
        };
        await postRepo.AddAsync(post);

        // Thêm 2 lượt thích
        await likeRepo.ToggleLikeAsync(post.Id, "user-cascade-1");
        await likeRepo.ToggleLikeAsync(post.Id, "user-cascade-2");

        var countBefore = await likeRepo.GetLikeCountAsync(post.Id);
        Assert.Equal(2, countBefore);

        // Act: Xóa bài viết
        var deleted = await postRepo.DeleteAsync(post.Id);
        Assert.True(deleted);

        // Assert: Tất cả lượt thích của bài viết đã bị xóa khỏi hệ thống
        var countAfter = await likeRepo.GetLikeCountAsync(post.Id);
        Assert.Equal(0, countAfter);

        var hasLiked = await likeRepo.HasUserLikedAsync(post.Id, "user-cascade-1");
        Assert.False(hasLiked);
    }

    [Fact]
    public async Task ExplicitDeleteLike_RemovesLikeWhenPreviouslyLiked()
    {
        var postRepo = _factory.Services.GetRequiredService<IPostRepository>();
        var post = new Post
        {
            Id = Guid.NewGuid(),
            Title = "Bài viết kiểm tra DELETE like",
            Content = "Nội dung",
            AuthorId = "author-delete-like",
            CreatedAt = DateTimeOffset.UtcNow
        };
        await postRepo.AddAsync(post);

        using var client = CreateAuthenticatedClient("user-del-like", "User Del Like");

        // Thích trước
        await client.PostAsync($"/api/posts/{post.Id}/like", null);

        // Act: Gửi DELETE /api/posts/{id}/like
        var deleteResponse = await client.DeleteAsync($"/api/posts/{post.Id}/like");

        // Assert: 200 OK, IsLiked = false, LikeCount = 0
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        var likeRes = await deleteResponse.Content.ReadFromJsonAsync<LikeResponse>(JsonOptions);
        Assert.NotNull(likeRes);
        Assert.False(likeRes.IsLiked);
        Assert.Equal(0, likeRes.LikeCount);
    }
}
