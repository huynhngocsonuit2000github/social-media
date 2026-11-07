using SocialMedia.Api.Validators;
using Xunit;

namespace SocialMedia.Api.Tests;

public class PostValidatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ValidateContent_EmptyOrNull_ReturnsInvalid(string? content)
    {
        // Act
        var (isValid, errorMessage) = PostValidator.ValidateContent(content);

        // Assert
        Assert.False(isValid);
        Assert.Equal("Nội dung bài đăng không được để trống.", errorMessage);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("   \t  \n  ")]
    public void ValidateContent_WhitespaceOnly_ReturnsInvalid(string? content)
    {
        // Act
        var (isValid, errorMessage) = PostValidator.ValidateContent(content);

        // Assert
        Assert.False(isValid);
        Assert.Equal("Nội dung bài đăng không hợp lệ hoặc không được để trống.", errorMessage);
    }

    [Fact]
    public void ValidateContent_SingleCharacter_ReturnsValid()
    {
        // Arrange
        var content = "A";

        // Act
        var (isValid, errorMessage) = PostValidator.ValidateContent(content);

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void ValidateContent_Exactly500Characters_ReturnsValid()
    {
        // Arrange
        var content = new string('p', 500);

        // Act
        var (isValid, errorMessage) = PostValidator.ValidateContent(content);

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
        Assert.Equal(500, PostValidator.CountCodePoints(content));
    }

    [Fact]
    public void ValidateContent_501Characters_ReturnsInvalid()
    {
        // Arrange
        var content = new string('p', 501);

        // Act
        var (isValid, errorMessage) = PostValidator.ValidateContent(content);

        // Assert
        Assert.False(isValid);
        Assert.Equal("Nội dung bài đăng không được vượt quá 500 ký tự.", errorMessage);
    }

    [Fact]
    public void ValidateContent_UnicodeEmojis500CodePoints_ReturnsValid()
    {
        // Arrange: "🚀" là 2 UTF-16 code units nhưng là 1 Unicode code point
        var content = string.Concat(Enumerable.Repeat("🚀", 500));

        // Act
        var (isValid, errorMessage) = PostValidator.ValidateContent(content);

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
        Assert.Equal(500, PostValidator.CountCodePoints(content));
    }

    [Fact]
    public void ValidateContent_UnicodeEmojis501CodePoints_ReturnsInvalid()
    {
        // Arrange: 501 emojis
        var content = string.Concat(Enumerable.Repeat("🚀", 501));

        // Act
        var (isValid, errorMessage) = PostValidator.ValidateContent(content);

        // Assert
        Assert.False(isValid);
        Assert.Equal("Nội dung bài đăng không được vượt quá 500 ký tự.", errorMessage);
        Assert.Equal(501, PostValidator.CountCodePoints(content));
    }

    [Fact]
    public void ValidateContent_VietnameseText_ReturnsValid()
    {
        // Arrange
        var content = "Hôm nay thời tiết thật đẹp, một ngày tuyệt vời để chia sẻ điều mới!";

        // Act
        var (isValid, errorMessage) = PostValidator.ValidateContent(content);

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
    }
}
