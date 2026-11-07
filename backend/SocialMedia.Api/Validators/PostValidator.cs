namespace SocialMedia.Api.Validators;

public static class PostValidator
{
    public const int MaxLengthInCodePoints = 500;

    public static (bool IsValid, string? ErrorMessage) ValidateContent(string? content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return (false, "Nội dung bài đăng không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return (false, "Nội dung bài đăng không hợp lệ hoặc không được để trống.");
        }

        int codePointCount = content.EnumerateRunes().Count();
        if (codePointCount > MaxLengthInCodePoints)
        {
            return (false, $"Nội dung bài đăng không được vượt quá {MaxLengthInCodePoints} ký tự.");
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
