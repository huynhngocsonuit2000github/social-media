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
}
