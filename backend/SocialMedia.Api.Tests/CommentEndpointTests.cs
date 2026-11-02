using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SocialMedia.Api.DTOs;
using SocialMedia.Api.Models;
using SocialMedia.Api.Repositories;
using Xunit;

namespace SocialMedia.Api.Tests;

public class CommentEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public CommentEndpointTests(WebApplicationFactory<Program> factory)
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
    public async Task AC1_PostComment_OnOtherUserPostWithPermission_SavesAndDisplaysComment()
    {
        // Arrange: Bài đăng thuộc về author-system (người khác), test-user-1 có quyền xem (bài công khai)
        using var client = CreateAuthenticatedClient("test-user-1", "Nguyễn Văn A");
        var postId = InMemoryPostRepository.DefaultPostId;
        var request = new CreateCommentRequest("Bình luận kiểm thử hợp lệ trên bài người khác.");

        // Act: Gửi bình luận
        var postResponse = await client.PostAsJsonAsync($"/api/posts/{postId}/comments", request);

        // Assert: Hệ thống lưu và phản hồi Created (201)
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
        var createdComment = await postResponse.Content.ReadFromJsonAsync<CommentResponse>(JsonOptions);
        Assert.NotNull(createdComment);
        Assert.Equal(postId, createdComment.PostId);
        Assert.Equal("Bình luận kiểm thử hợp lệ trên bài người khác.", createdComment.Content);
        Assert.Equal("test-user-1", createdComment.UserId);
        Assert.Equal("Nguyễn Văn A", createdComment.AuthorName);

        // Verify: Hiển thị trong danh sách bình luận của bài đăng
        var listResponse = await client.GetAsync($"/api/posts/{postId}/comments");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var comments = await listResponse.Content.ReadFromJsonAsync<List<CommentResponse>>(JsonOptions);
        Assert.NotNull(comments);
        Assert.Contains(comments, c => c.Id == createdComment.Id);
    }

    [Fact]
    public async Task AC2_PostComment_UserWithoutPermission_RejectsAndDoesNotCreateComment()
    {
        // Arrange: Tạo bài đăng có giới hạn quyền xem (chỉ user-allowed được xem)
        var postRepo = _factory.Services.GetRequiredService<IPostRepository>();
        var commentRepo = _factory.Services.GetRequiredService<ICommentRepository>();

        var restrictedPost = new Post
        {
            Id = Guid.NewGuid(),
            Title = "Bài đăng giới hạn quyền xem",
            Content = "Nội dung chỉ dành cho người được mời",
            AuthorId = "author-private-1",
            AllowedViewerIds = ["user-allowed-only"],
            CreatedAt = DateTimeOffset.UtcNow
        };
        await postRepo.AddAsync(restrictedPost);

        // Client là user-no-permission (không nằm trong AllowedViewerIds)
        using var client = CreateAuthenticatedClient("user-no-permission", "Kẻ tò mò");
        var request = new CreateCommentRequest("Cố gắng bình luận vào bài không có quyền xem.");

        // Act
        var postResponse = await client.PostAsJsonAsync($"/api/posts/{restrictedPost.Id}/comments", request);

        // Assert: Bị từ chối với mã 403 Forbidden
        Assert.Equal(HttpStatusCode.Forbidden, postResponse.StatusCode);
        var error = await postResponse.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        Assert.NotNull(error);
        Assert.Contains("không có quyền xem", error.Error);

        // Đảm bảo không có bình luận nào được tạo trong repository
        var comments = await commentRepo.GetByPostIdAsync(restrictedPost.Id);
        Assert.Empty(comments);
    }

    [Fact]
    public async Task AC2_GetComments_UserWithoutPermission_ReturnsForbidden()
    {
        // Arrange: Bài đăng giới hạn quyền xem
        var postRepo = _factory.Services.GetRequiredService<IPostRepository>();
        var restrictedPost = new Post
        {
            Id = Guid.NewGuid(),
            Title = "Bài đăng bảo mật",
            Content = "Nội dung bảo mật",
            AuthorId = "author-sec",
            AllowedViewerIds = ["vip-user"],
            CreatedAt = DateTimeOffset.UtcNow
        };
        await postRepo.AddAsync(restrictedPost);

        using var client = CreateAuthenticatedClient("standard-user", "Người dùng thường");

        // Act: Cố gắng lấy danh sách bình luận
        var response = await client.GetAsync($"/api/posts/{restrictedPost.Id}/comments");

        // Assert: 403 Forbidden
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AC3_GetComments_MultipleComments_OrderedByCreatedAtAscending()
    {
        // Arrange: Tạo bài đăng riêng biệt để kiểm tra thứ tự sắp xếp
        var postRepo = _factory.Services.GetRequiredService<IPostRepository>();
        var post = new Post
        {
            Id = Guid.NewGuid(),
            Title = "Bài đăng kiểm tra thứ tự bình luận",
            Content = "Nội dung bài đăng",
            AuthorId = "author-1",
            CreatedAt = DateTimeOffset.UtcNow
        };
        await postRepo.AddAsync(post);

        using var client = CreateAuthenticatedClient();

        // Gửi 3 bình luận tuần tự
        var res1 = await client.PostAsJsonAsync($"/api/posts/{post.Id}/comments", new CreateCommentRequest("Bình luận thứ nhất"));
        await Task.Delay(20);
        var res2 = await client.PostAsJsonAsync($"/api/posts/{post.Id}/comments", new CreateCommentRequest("Bình luận thứ hai"));
        await Task.Delay(20);
        var res3 = await client.PostAsJsonAsync($"/api/posts/{post.Id}/comments", new CreateCommentRequest("Bình luận thứ ba"));

        Assert.Equal(HttpStatusCode.Created, res1.StatusCode);
        Assert.Equal(HttpStatusCode.Created, res2.StatusCode);
        Assert.Equal(HttpStatusCode.Created, res3.StatusCode);

        // Act: Xem danh sách bình luận
        var listResponse = await client.GetAsync($"/api/posts/{post.Id}/comments");

        // Assert: Thứ tự thời gian tăng dần (cũ nhất trước, mới nhất cuối)
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var comments = await listResponse.Content.ReadFromJsonAsync<List<CommentResponse>>(JsonOptions);
        Assert.NotNull(comments);
        Assert.True(comments.Count >= 3);

        for (int i = 0; i < comments.Count - 1; i++)
        {
            Assert.True(comments[i].CreatedAt <= comments[i + 1].CreatedAt,
                $"Comment at {i} should be before or equal to {i + 1}");
        }

        var indexedComments = comments.Where(c => c.Content.StartsWith("Bình luận thứ ")).ToList();
        Assert.Equal("Bình luận thứ nhất", indexedComments[0].Content);
        Assert.Equal("Bình luận thứ hai", indexedComments[1].Content);
        Assert.Equal("Bình luận thứ ba", indexedComments[2].Content);
    }

    [Fact]
    public async Task AC3_GetComments_SameTimestamp_PreservesInsertionOrderViaSequenceNumber()
    {
        // Arrange: Seed post and 2 comments with exact same CreatedAt timestamp
        var postRepo = _factory.Services.GetRequiredService<IPostRepository>();
        var commentRepo = _factory.Services.GetRequiredService<ICommentRepository>();

        var post = new Post
        {
            Id = Guid.NewGuid(),
            Title = "Bài đăng kiểm tra trùng timestamp",
            Content = "Nội dung bài đăng",
            AuthorId = "author-same-time",
            CreatedAt = DateTimeOffset.UtcNow
        };
        await postRepo.AddAsync(post);

        var fixedTime = new DateTimeOffset(2026, 11, 2, 10, 0, 0, TimeSpan.Zero);
        var comment1 = new Comment
        {
            PostId = post.Id,
            UserId = "user-1",
            AuthorName = "User 1",
            Content = "Bình luận cùng giờ 1",
            CreatedAt = fixedTime
        };
        var comment2 = new Comment
        {
            PostId = post.Id,
            UserId = "user-2",
            AuthorName = "User 2",
            Content = "Bình luận cùng giờ 2",
            CreatedAt = fixedTime
        };

        await commentRepo.AddAsync(comment1);
        await commentRepo.AddAsync(comment2);

        using var client = CreateAuthenticatedClient();

        // Act
        var listResponse = await client.GetAsync($"/api/posts/{post.Id}/comments");

        // Assert
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var comments = await listResponse.Content.ReadFromJsonAsync<List<CommentResponse>>(JsonOptions);
        Assert.NotNull(comments);
        var matchedComments = comments.Where(c => c.PostId == post.Id).ToList();
        Assert.Equal(2, matchedComments.Count);
        Assert.Equal("Bình luận cùng giờ 1", matchedComments[0].Content);
        Assert.Equal("Bình luận cùng giờ 2", matchedComments[1].Content);
    }

    [Fact]
    public async Task AC4_PostComment_OnOtherUserPost_CreatesNotificationInNotificationCenterForPostAuthor()
    {
        // Arrange: Chủ bài đăng là post-owner-123
        var postRepo = _factory.Services.GetRequiredService<IPostRepository>();
        var postOwnerId = "post-owner-123";
        var post = new Post
        {
            Id = Guid.NewGuid(),
            Title = "Bài viết của post-owner-123",
            Content = "Nội dung bài viết",
            AuthorId = postOwnerId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await postRepo.AddAsync(post);

        // Người dùng bình luận là commenter-user-456 (khác chủ bài đăng)
        var commenterId = "commenter-user-456";
        var commenterName = "Trần Thị B";
        using var commenterClient = CreateAuthenticatedClient(commenterId, commenterName);

        // Act: Gửi bình luận
        var commentRes = await commenterClient.PostAsJsonAsync(
            $"/api/posts/{post.Id}/comments",
            new CreateCommentRequest("Bài viết rất hay, cảm ơn bạn đã chia sẻ!"));
        Assert.Equal(HttpStatusCode.Created, commentRes.StatusCode);
        var createdComment = await commentRes.Content.ReadFromJsonAsync<CommentResponse>(JsonOptions);
        Assert.NotNull(createdComment);

        // Assert: Chủ bài đăng (post-owner-123) nhận được thông báo trong Notification Center
        using var postOwnerClient = CreateAuthenticatedClient(postOwnerId, "Post Owner");
        var notifRes = await postOwnerClient.GetAsync("/api/notifications");
        Assert.Equal(HttpStatusCode.OK, notifRes.StatusCode);

        var notifications = await notifRes.Content.ReadFromJsonAsync<List<NotificationResponse>>(JsonOptions);
        Assert.NotNull(notifications);

        var matchedNotif = notifications.FirstOrDefault(n => n.CommentId == createdComment.Id);
        Assert.NotNull(matchedNotif);
        Assert.Equal(postOwnerId, matchedNotif.RecipientUserId);
        Assert.Equal(commenterId, matchedNotif.ActorUserId);
        Assert.Equal(commenterName, matchedNotif.ActorName);
        Assert.Equal(post.Id, matchedNotif.PostId);
        Assert.Contains(commenterName, matchedNotif.Message);
        Assert.Contains("đã bình luận", matchedNotif.Message);
    }

    [Fact]
    public async Task AC4_PostComment_OnOwnPost_DoesNotCreateNotification()
    {
        // Arrange: Chủ bài đăng tự bình luận trên bài của mình
        var postRepo = _factory.Services.GetRequiredService<IPostRepository>();
        var selfAuthorId = "self-author-789";
        var post = new Post
        {
            Id = Guid.NewGuid(),
            Title = "Bài viết tự đăng",
            Content = "Nội dung bài viết",
            AuthorId = selfAuthorId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await postRepo.AddAsync(post);

        using var selfClient = CreateAuthenticatedClient(selfAuthorId, "Chính Chủ");

        // Act
        var commentRes = await selfClient.PostAsJsonAsync(
            $"/api/posts/{post.Id}/comments",
            new CreateCommentRequest("Tự bình luận bổ sung thông tin."));
        Assert.Equal(HttpStatusCode.Created, commentRes.StatusCode);

        // Assert: Không tạo thông báo tự gửi cho chính mình
        var notifRes = await selfClient.GetAsync("/api/notifications");
        Assert.Equal(HttpStatusCode.OK, notifRes.StatusCode);
        var notifications = await notifRes.Content.ReadFromJsonAsync<List<NotificationResponse>>(JsonOptions);
        Assert.NotNull(notifications);
        Assert.DoesNotContain(notifications, n => n.PostId == post.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   \t  \n  ")]
    public async Task AC5_PostComment_EmptyOrWhitespace_ReturnsBadRequest(string content)
    {
        // Arrange
        using var client = CreateAuthenticatedClient();
        var postId = InMemoryPostRepository.DefaultPostId;
        var request = new CreateCommentRequest(content);

        // Act
        var postResponse = await client.PostAsJsonAsync($"/api/posts/{postId}/comments", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, postResponse.StatusCode);
        var error = await postResponse.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        Assert.NotNull(error);
        Assert.Contains("không được để trống hoặc chỉ chứa khoảng trắng", error.Error);
    }

    [Fact]
    public async Task AC6_PostComment_501Characters_ReturnsBadRequest()
    {
        // Arrange
        using var client = CreateAuthenticatedClient();
        var postId = InMemoryPostRepository.DefaultPostId;
        var content501 = new string('x', 501);
        var request = new CreateCommentRequest(content501);

        // Act
        var postResponse = await client.PostAsJsonAsync($"/api/posts/{postId}/comments", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, postResponse.StatusCode);
        var error = await postResponse.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        Assert.NotNull(error);
        Assert.Contains("không được vượt quá 500 ký tự", error.Error);
    }

    [Fact]
    public async Task AC7_PostComment_Exactly500Characters_ReturnsCreated()
    {
        // Arrange
        using var client = CreateAuthenticatedClient();
        var postId = InMemoryPostRepository.DefaultPostId;
        var content500 = new string('x', 500);
        var request = new CreateCommentRequest(content500);

        // Act
        var postResponse = await client.PostAsJsonAsync($"/api/posts/{postId}/comments", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
        var createdComment = await postResponse.Content.ReadFromJsonAsync<CommentResponse>(JsonOptions);
        Assert.NotNull(createdComment);
        Assert.Equal(500, createdComment.Content.Length);
    }

    [Fact]
    public async Task UnicodeRunes_CountsCodePointsCorrectly()
    {
        // Arrange: 500 emojis (mỗi emoji là 2 UTF-16 code units nhưng là 1 code point)
        using var client = CreateAuthenticatedClient();
        var postId = InMemoryPostRepository.DefaultPostId;
        var emojis500 = string.Concat(Enumerable.Repeat("🎉", 500));
        var request = new CreateCommentRequest(emojis500);

        // Act: 500 code point emojis phải được chấp nhận
        var response = await client.PostAsJsonAsync($"/api/posts/{postId}/comments", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Act: 501 code point emojis phải bị từ chối
        var emojis501 = string.Concat(Enumerable.Repeat("🎉", 501));
        var response501 = await client.PostAsJsonAsync($"/api/posts/{postId}/comments", new CreateCommentRequest(emojis501));
        Assert.Equal(HttpStatusCode.BadRequest, response501.StatusCode);
    }

    [Fact]
    public async Task NotLoggedIn_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        var postId = InMemoryPostRepository.DefaultPostId;
        var request = new CreateCommentRequest("Bình luận khi chưa đăng nhập.");

        var postResponse = await client.PostAsJsonAsync($"/api/posts/{postId}/comments", request);
        Assert.Equal(HttpStatusCode.Unauthorized, postResponse.StatusCode);
        var error = await postResponse.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        Assert.NotNull(error);
        Assert.Contains("Vui lòng đăng nhập", error.Error);
    }

    [Fact]
    public async Task NonExistentPost_ReturnsNotFound()
    {
        using var client = CreateAuthenticatedClient();
        var nonExistentPostId = Guid.NewGuid();
        var request = new CreateCommentRequest("Bình luận vào bài không tồn tại.");

        var postResponse = await client.PostAsJsonAsync($"/api/posts/{nonExistentPostId}/comments", request);
        Assert.Equal(HttpStatusCode.NotFound, postResponse.StatusCode);
        var error = await postResponse.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        Assert.NotNull(error);
        Assert.Contains("Bài đăng không tồn tại", error.Error);
    }

    [Fact]
    public async Task NotificationCenter_Unauthenticated_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/notifications");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostDeletion_CascadesDeleteCommentsAndNotifications()
    {
        var postRepo = _factory.Services.GetRequiredService<IPostRepository>();
        var commentRepo = _factory.Services.GetRequiredService<ICommentRepository>();
        var notifRepo = _factory.Services.GetRequiredService<INotificationRepository>();

        var post = new Post
        {
            Id = Guid.NewGuid(),
            Title = "Bài viết sẽ bị xóa kèm thông báo",
            Content = "Nội dung bài viết",
            AuthorId = "author-to-delete",
            CreatedAt = DateTimeOffset.UtcNow
        };
        await postRepo.AddAsync(post);

        using var client = CreateAuthenticatedClient("commenter-cascade", "Commenter");
        var commentRes = await client.PostAsJsonAsync($"/api/posts/{post.Id}/comments", new CreateCommentRequest("Bình luận sắp bị xóa"));
        Assert.Equal(HttpStatusCode.Created, commentRes.StatusCode);

        // Kiểm tra thông báo đã được tạo
        var notifsBefore = await notifRepo.GetByUserIdAsync("author-to-delete");
        Assert.NotEmpty(notifsBefore);

        // Act: Xóa bài viết
        var deleted = await postRepo.DeleteAsync(post.Id);
        Assert.True(deleted);

        // Assert: Bình luận và thông báo liên quan đều bị xóa
        var remainingComments = await commentRepo.GetByPostIdAsync(post.Id);
        Assert.Empty(remainingComments);

        var notifsAfter = await notifRepo.GetByUserIdAsync("author-to-delete");
        Assert.Empty(notifsAfter);
    }

    [Fact]
    public async Task SimpleAuthenticationHandler_BlockedInProductionEnvironment()
    {
        // Arrange: Khởi tạo app với môi trường Production
        using var prodFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
        });
        using var client = prodFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Id", "attacker-in-prod");

        var postId = InMemoryPostRepository.DefaultPostId;
        var request = new CreateCommentRequest("Bình luận thử nghiệm trong prod");

        // Act
        var response = await client.PostAsJsonAsync($"/api/posts/{postId}/comments", request);

        // Assert: SimpleAuthenticationHandler phải từ chối trong Production, trả về 401 Unauthorized
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostComment_WhenNotificationServiceFails_CommentIsStillCreatedSuccessfully()
    {
        // Arrange: Mock / ghi đè INotificationRepository bằng repo luôn ném lỗi
        using var customFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<INotificationRepository, FailingNotificationRepository>();
            });
        });

        using var client = customFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Id", "commenter-fail-notif");
        client.DefaultRequestHeaders.Add("X-User-Name", "Commenter");

        var postId = InMemoryPostRepository.DefaultPostId;
        var request = new CreateCommentRequest("Bình luận khi dịch vụ thông báo lỗi.");

        // Act: Bình luận vẫn phải thành công
        var response = await client.PostAsJsonAsync($"/api/posts/{postId}/comments", request);

        // Assert: 201 Created và bình luận được tạo
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CommentResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal("Bình luận khi dịch vụ thông báo lỗi.", created.Content);
    }

    private class FailingNotificationRepository : INotificationRepository
    {
        public Task<Notification> AddAsync(Notification notification)
        {
            throw new InvalidOperationException("Lỗi giả lập từ dịch vụ thông báo");
        }

        public Task<IReadOnlyList<Notification>> GetByUserIdAsync(string userId) =>
            Task.FromResult<IReadOnlyList<Notification>>(Array.Empty<Notification>());

        public Task DeleteByPostIdAsync(Guid postId) => Task.CompletedTask;
    }
}
