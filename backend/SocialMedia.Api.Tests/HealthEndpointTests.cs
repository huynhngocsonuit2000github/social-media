using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace SocialMedia.Api.Tests;

public class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetHealth_ReturnsOkAndStatusOk()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        using var response = await client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var jsonDocument = JsonDocument.Parse(content);
        var status = jsonDocument.RootElement.GetProperty("status").GetString();

        Assert.Equal("ok", status);
    }
}
