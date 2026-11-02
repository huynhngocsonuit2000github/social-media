using SocialMedia.Api.Validators;
using Xunit;

namespace SocialMedia.Api.Tests;

public class CommentValidatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   \t  \n  ")]
    public void ValidateContent_EmptyOrWhitespace_ReturnsInvalid(string? content)
    {
        // Act
        var (isValid, errorMessage) = CommentValidator.ValidateContent(content);

        // Assert
        Assert.False(isValid);
        Assert.Equal("Nội dung bình luận không được để trống hoặc chỉ chứa khoảng trắng.", errorMessage);
    }

    [Fact]
    public void ValidateContent_SingleCharacter_ReturnsValid()
    {
        // Arrange
        var content = "A";

        // Act
        var (isValid, errorMessage) = CommentValidator.ValidateContent(content);

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void ValidateContent_Exactly500Characters_ReturnsValid()
    {
        // Arrange
        var content = new string('a', 500);

        // Act
        var (isValid, errorMessage) = CommentValidator.ValidateContent(content);

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
        Assert.Equal(500, CommentValidator.CountCodePoints(content));
    }

    [Fact]
    public void ValidateContent_501Characters_ReturnsInvalid()
    {
        // Arrange
        var content = new string('a', 501);

        // Act
        var (isValid, errorMessage) = CommentValidator.ValidateContent(content);

        // Assert
        Assert.False(isValid);
        Assert.Equal("Nội dung bình luận không được vượt quá 500 ký tự.", errorMessage);
    }

    [Fact]
    public void ValidateContent_UnicodeEmojis500CodePoints_ReturnsValid()
    {
        // Arrange: "😀" is 2 UTF-16 code units (surrogate pair) but 1 Unicode code point
        var content = string.Concat(Enumerable.Repeat("😀", 500));

        // Act
        var (isValid, errorMessage) = CommentValidator.ValidateContent(content);

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
        Assert.Equal(500, CommentValidator.CountCodePoints(content));
    }

    [Fact]
    public void ValidateContent_UnicodeEmojis501CodePoints_ReturnsInvalid()
    {
        // Arrange: 501 emojis
        var content = string.Concat(Enumerable.Repeat("😀", 501));

        // Act
        var (isValid, errorMessage) = CommentValidator.ValidateContent(content);

        // Assert
        Assert.False(isValid);
        Assert.Equal("Nội dung bình luận không được vượt quá 500 ký tự.", errorMessage);
        Assert.Equal(501, CommentValidator.CountCodePoints(content));
    }

    [Fact]
    public void ValidateContent_VietnameseText_ReturnsValid()
    {
        // Arrange
        var content = "Bài viết rất hay và ý nghĩa! Chúc bạn ngày mới tốt lành.";

        // Act
        var (isValid, errorMessage) = CommentValidator.ValidateContent(content);

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
    }
}
