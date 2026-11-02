namespace SocialMedia.Api.Validators;

public static class CommentValidator
{
    public const int MaxLengthInCodePoints = 500;

    public static (bool IsValid, string? ErrorMessage) ValidateContent(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return (false, "Nội dung bình luận không được để trống hoặc chỉ chứa khoảng trắng.");
        }

        int codePointCount = content.EnumerateRunes().Count();
        if (codePointCount > MaxLengthInCodePoints)
        {
            return (false, $"Nội dung bình luận không được vượt quá {MaxLengthInCodePoints} ký tự.");
        }

        return (true, null);
    }

    public static int CountCodePoints(string? content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return 0;
        }

        return content.EnumerateRunes().Count();
    }
}
