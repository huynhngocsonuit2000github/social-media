using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SocialMedia.Api.DTOs;
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
    public async Task AC1_PostComment_ValidContent_ReturnsCreatedAndCommentInList()
    {
        // Arrange
        using var client = CreateAuthenticatedClient();
        var postId = InMemoryPostRepository.DefaultPostId;
        var request = new CreateCommentRequest("Bình luận kiểm thử hợp lệ.");

        // Act
        var postResponse = await client.PostAsJsonAsync($"/api/posts/{postId}/comments", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
        var createdComment = await postResponse.Content.ReadFromJsonAsync<CommentResponse>(JsonOptions);
        Assert.NotNull(createdComment);
        Assert.Equal(postId, createdComment.PostId);
        Assert.Equal("Bình luận kiểm thử hợp lệ.", createdComment.Content);
        Assert.Equal("test-user-1", createdComment.UserId);

        // Verify comment appears in list
        var listResponse = await client.GetAsync($"/api/posts/{postId}/comments");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var comments = await listResponse.Content.ReadFromJsonAsync<List<CommentResponse>>(JsonOptions);
        Assert.NotNull(comments);
        Assert.Contains(comments, c => c.Id == createdComment.Id);
    }

    [Fact]
    public async Task AC2_PostComment_Exactly500Characters_ReturnsCreated()
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
    public async Task AC3_PostComment_501Characters_ReturnsBadRequest()
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

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   \t  \n  ")]
    public async Task AC4_PostComment_EmptyOrWhitespace_ReturnsBadRequest(string content)
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
    public async Task AC5_GetComments_MultipleComments_OrderedByCreatedAtAscending()
    {
        // Arrange: Create a new dedicated post to test ordering
        using var client = CreateAuthenticatedClient();
        var createPostResponse = await client.PostAsJsonAsync("/api/posts", new
        {
            Title = "Bài đăng kiểm tra thứ tự bình luận",
            Content = "Nội dung bài đăng",
            AuthorId = "author-1"
        });
        Assert.Equal(HttpStatusCode.Created, createPostResponse.StatusCode);
        var post = await createPostResponse.Content.ReadFromJsonAsync<PostResponse>(JsonOptions);
        Assert.NotNull(post);

        // Post 3 comments sequentially
        var res1 = await client.PostAsJsonAsync($"/api/posts/{post.Id}/comments", new CreateCommentRequest("Bình luận thứ nhất"));
        await Task.Delay(20);
        var res2 = await client.PostAsJsonAsync($"/api/posts/{post.Id}/comments", new CreateCommentRequest("Bình luận thứ hai"));
        await Task.Delay(20);
        var res3 = await client.PostAsJsonAsync($"/api/posts/{post.Id}/comments", new CreateCommentRequest("Bình luận thứ ba"));

        Assert.Equal(HttpStatusCode.Created, res1.StatusCode);
        Assert.Equal(HttpStatusCode.Created, res2.StatusCode);
        Assert.Equal(HttpStatusCode.Created, res3.StatusCode);

        // Act
        var listResponse = await client.GetAsync($"/api/posts/{post.Id}/comments");

        // Assert
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var comments = await listResponse.Content.ReadFromJsonAsync<List<CommentResponse>>(JsonOptions);
        Assert.NotNull(comments);
        Assert.True(comments.Count >= 3);

        // Verify chronological order: oldest first, newest at the end
        for (int i = 0; i < comments.Count - 1; i++)
        {
            Assert.True(comments[i].CreatedAt <= comments[i + 1].CreatedAt,
                $"Comment at {i} should be before or equal to {i + 1}");
        }

        // Specifically check our 3 comments appear in order
        var indexedComments = comments.Where(c => c.Content.StartsWith("Bình luận thứ ")).ToList();
        Assert.Equal("Bình luận thứ nhất", indexedComments[0].Content);
        Assert.Equal("Bình luận thứ hai", indexedComments[1].Content);
        Assert.Equal("Bình luận thứ ba", indexedComments[2].Content);
    }

    [Fact]
    public async Task AC7_PostComment_NotLoggedIn_ReturnsUnauthorized()
    {
        // Arrange: Client WITHOUT auth headers
        using var client = _factory.CreateClient();
        var postId = InMemoryPostRepository.DefaultPostId;
        var request = new CreateCommentRequest("Bình luận khi chưa đăng nhập.");

        // Act
        var postResponse = await client.PostAsJsonAsync($"/api/posts/{postId}/comments", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, postResponse.StatusCode);
        var error = await postResponse.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        Assert.NotNull(error);
        Assert.Contains("Vui lòng đăng nhập", error.Error);
    }

    [Fact]
    public async Task AC8_PostComment_NonExistentPost_ReturnsNotFound()
    {
        // Arrange
        using var client = CreateAuthenticatedClient();
        var nonExistentPostId = Guid.NewGuid();
        var request = new CreateCommentRequest("Bình luận vào bài không tồn tại.");

        // Act
        var postResponse = await client.PostAsJsonAsync($"/api/posts/{nonExistentPostId}/comments", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, postResponse.StatusCode);
        var error = await postResponse.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        Assert.NotNull(error);
        Assert.Contains("Bài đăng không tồn tại", error.Error);
    }

    [Fact]
    public async Task PostDeletion_CascadesDeleteComments()
    {
        // Arrange: Create a post and add a comment
        using var client = CreateAuthenticatedClient();
        var postRes = await client.PostAsJsonAsync("/api/posts", new
        {
            Title = "Bài viết sẽ bị xóa",
            Content = "Nội dung bài viết",
            AuthorId = "author-temp"
        });
        var post = await postRes.Content.ReadFromJsonAsync<PostResponse>(JsonOptions);
        Assert.NotNull(post);

        var commentRes = await client.PostAsJsonAsync($"/api/posts/{post.Id}/comments", new CreateCommentRequest("Bình luận trước khi xóa bài"));
        Assert.Equal(HttpStatusCode.Created, commentRes.StatusCode);

        // Act: Delete the post
        var deleteRes = await client.DeleteAsync($"/api/posts/{post.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteRes.StatusCode);

        // Assert: Post is gone, getting comments returns 404
        var getCommentsRes = await client.GetAsync($"/api/posts/{post.Id}/comments");
        Assert.Equal(HttpStatusCode.NotFound, getCommentsRes.StatusCode);
    }

    [Fact]
    public async Task UnicodeRunes_CountsCodePointsCorrectly()
    {
        // Arrange: 500 emojis (each is 2 UTF-16 chars)
        using var client = CreateAuthenticatedClient();
        var postId = InMemoryPostRepository.DefaultPostId;
        var emojis500 = string.Concat(Enumerable.Repeat("🎉", 500));
        var request = new CreateCommentRequest(emojis500);

        // Act
        var response = await client.PostAsJsonAsync($"/api/posts/{postId}/comments", request);

        // Assert: 500 emojis should be accepted
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Act with 501 emojis
        var emojis501 = string.Concat(Enumerable.Repeat("🎉", 501));
        var response501 = await client.PostAsJsonAsync($"/api/posts/{postId}/comments", new CreateCommentRequest(emojis501));

        // Assert: 501 emojis should be rejected
        Assert.Equal(HttpStatusCode.BadRequest, response501.StatusCode);
    }
}
