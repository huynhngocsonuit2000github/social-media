using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SocialMedia.Api.DTOs;
using SocialMedia.Api.Repositories;
using Xunit;

namespace SocialMedia.Api.Tests;

public class PostEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public PostEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private HttpClient CreateAuthenticatedClient(string userId = "post-user-1", string userName = "Post User")
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Id", userId);
        client.DefaultRequestHeaders.Add("X-User-Name", userName);
        return client;
    }

    [Fact]
    public async Task AC5_CreatePost_1To500Characters_Returns2xxAndSavesPostSuccessfully()
    {
        // Arrange
        using var client = CreateAuthenticatedClient("author-create-post-1", "Tác Giả Một");
        var request = new CreatePostRequest("Bài viết hợp lệ dưới 500 ký tự.", "Tiêu đề bài viết");

        // Act
        var response = await client.PostAsJsonAsync("/api/posts", request);

        // Assert: Trả về HTTP status 2xx (201 Created)
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<PostResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal("Bài viết hợp lệ dưới 500 ký tự.", created.Content);
        Assert.Equal("Tiêu đề bài viết", created.Title);
        Assert.Equal("author-create-post-1", created.AuthorId);

        // Verify post is saved in repository and can be retrieved
        var postRepo = _factory.Services.GetRequiredService<IPostRepository>();
        var savedPost = await postRepo.GetByIdAsync(created.Id);
        Assert.NotNull(savedPost);
        Assert.Equal(created.Content, savedPost.Content);
    }

    [Fact]
    public async Task AC5_CreatePost_Exactly500Characters_Returns2xx()
    {
        // Arrange: 500 ký tự
        using var client = CreateAuthenticatedClient();
        var content500 = new string('x', 500);
        var request = new CreatePostRequest(content500);

        // Act
        var response = await client.PostAsJsonAsync("/api/posts", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<PostResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(500, created.Content.Length);
    }

    [Fact]
    public async Task AC6_CreatePost_501Characters_ReturnsBadRequestWithClearErrorMessage()
    {
        // Arrange: 501 ký tự
        using var client = CreateAuthenticatedClient();
        var content501 = new string('x', 501);
        var request = new CreatePostRequest(content501);

        // Act
        var response = await client.PostAsJsonAsync("/api/posts", request);

        // Assert: HTTP status 400 và thông báo lỗi rõ ràng về việc vượt quá giới hạn ký tự
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        Assert.NotNull(error);
        Assert.Contains("không được vượt quá 500 ký tự", error.Error);
    }

    [Fact]
    public async Task AC7_CreatePost_EmptyString_ReturnsBadRequestWithClearErrorMessage()
    {
        // Arrange: Chuỗi rỗng ('')
        using var client = CreateAuthenticatedClient();
        var request = new CreatePostRequest("");

        // Act
        var response = await client.PostAsJsonAsync("/api/posts", request);

        // Assert: HTTP status 400 kèm thông báo lỗi rõ ràng về việc nội dung không được để trống
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        Assert.NotNull(error);
        Assert.Contains("không được để trống", error.Error);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("   \t  \n  ")]
    public async Task AC8_CreatePost_WhitespaceOnly_ReturnsBadRequestWithClearErrorMessage(string whitespaceContent)
    {
        // Arrange: Chỉ chứa ký tự khoảng trắng
        using var client = CreateAuthenticatedClient();
        var request = new CreatePostRequest(whitespaceContent);

        // Act
        var response = await client.PostAsJsonAsync("/api/posts", request);

        // Assert: HTTP status 400 kèm thông báo lỗi rõ ràng về việc nội dung không hợp lệ hoặc không được để trống
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        Assert.NotNull(error);
        Assert.Contains("không hợp lệ hoặc không được để trống", error.Error);
    }

    [Fact]
    public async Task CreatePost_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange: Người dùng chưa đăng nhập
        using var client = _factory.CreateClient();
        var request = new CreatePostRequest("Nội dung bài viết");

        // Act
        var response = await client.PostAsJsonAsync("/api/posts", request);

        // Assert: 401 Unauthorized
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AC5_GetPosts_ValidRequest_EachPostInJsonContainsCommentCountFieldAsInteger()
    {
        // Arrange
        var postRepo = _factory.Services.GetRequiredService<IPostRepository>();
        var commentRepo = _factory.Services.GetRequiredService<ICommentRepository>();

        // Tạo 3 bài viết: 0 bình luận, 1 bình luận, 5 bình luận
        var post0 = new Models.Post
        {
            Id = Guid.NewGuid(),
            Title = "Bài viết 0 bình luận",
            Content = "Nội dung bài viết không có bình luận",
            AuthorId = "author-ac5-0",
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-30)
        };
        var post1 = new Models.Post
        {
            Id = Guid.NewGuid(),
            Title = "Bài viết 1 bình luận",
            Content = "Nội dung bài viết có đúng 1 bình luận",
            AuthorId = "author-ac5-1",
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-20)
        };
        var post5 = new Models.Post
        {
            Id = Guid.NewGuid(),
            Title = "Bài viết 5 bình luận",
            Content = "Nội dung bài viết có 5 bình luận",
            AuthorId = "author-ac5-5",
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10)
        };

        await postRepo.AddAsync(post0);
        await postRepo.AddAsync(post1);
        await postRepo.AddAsync(post5);

        // Thêm 1 bình luận cho post1
        await commentRepo.AddAsync(new Models.Comment
        {
            PostId = post1.Id,
            UserId = "user-c1",
            AuthorName = "User 1",
            Content = "Bình luận duy nhất",
            CreatedAt = DateTimeOffset.UtcNow
        });

        // Thêm 5 bình luận cho post5
        for (int i = 1; i <= 5; i++)
        {
            await commentRepo.AddAsync(new Models.Comment
            {
                PostId = post5.Id,
                UserId = $"user-c5-{i}",
                AuthorName = $"User 5-{i}",
                Content = $"Bình luận số {i}",
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        using var client = _factory.CreateClient();

        // Act: Gửi yêu cầu hợp lệ tới API danh sách bài đăng
        var response = await client.GetAsync("/api/posts");

        // Assert: Trạng thái 200 OK
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Kiểm tra phản hồi JSON thô để chứng minh trường `commentCount` có kiểu số nguyên (AC5)
        var jsonString = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(jsonString);
        var rootArray = jsonDoc.RootElement;
        Assert.Equal(JsonValueKind.Array, rootArray.ValueKind);

        // Mỗi đối tượng bài đăng trong phản hồi JSON phải chứa trường commentCount kiểu số nguyên
        foreach (var element in rootArray.EnumerateArray())
        {
            Assert.True(element.TryGetProperty("commentCount", out var commentCountProp),
                "Mỗi đối tượng bài đăng trong phản hồi JSON phải chứa trường 'commentCount'.");
            Assert.Equal(JsonValueKind.Number, commentCountProp.ValueKind);
            Assert.True(commentCountProp.TryGetInt32(out _), "Trường 'commentCount' phải là kiểu số nguyên.");
        }

        // Kiểm tra deserialize và giá trị số lượng bình luận cho từng bài viết cụ thể
        var postResponses = JsonSerializer.Deserialize<List<PostResponse>>(jsonString, JsonOptions);
        Assert.NotNull(postResponses);

        var retrieved0 = postResponses.FirstOrDefault(p => p.Id == post0.Id);
        Assert.NotNull(retrieved0);
        Assert.Equal(0, retrieved0.CommentCount);

        var retrieved1 = postResponses.FirstOrDefault(p => p.Id == post1.Id);
        Assert.NotNull(retrieved1);
        Assert.Equal(1, retrieved1.CommentCount);

        var retrieved5 = postResponses.FirstOrDefault(p => p.Id == post5.Id);
        Assert.NotNull(retrieved5);
        Assert.Equal(5, retrieved5.CommentCount);
    }

    [Fact]
    public async Task AC5_GetPostById_ValidRequest_ContainsCommentCountFieldAsInteger()
    {
        // Arrange
        var postRepo = _factory.Services.GetRequiredService<IPostRepository>();
        var commentRepo = _factory.Services.GetRequiredService<ICommentRepository>();

        var post = new Models.Post
        {
            Id = Guid.NewGuid(),
            Title = "Bài viết chi tiết",
            Content = "Nội dung bài viết",
            AuthorId = "author-single",
            CreatedAt = DateTimeOffset.UtcNow
        };
        await postRepo.AddAsync(post);

        await commentRepo.AddAsync(new Models.Comment
        {
            PostId = post.Id,
            UserId = "user-single",
            AuthorName = "User Single",
            Content = "Bình luận mẫu",
            CreatedAt = DateTimeOffset.UtcNow
        });

        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/posts/{post.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var postResponse = await response.Content.ReadFromJsonAsync<PostResponse>(JsonOptions);
        Assert.NotNull(postResponse);
        Assert.Equal(1, postResponse.CommentCount);
    }
}
