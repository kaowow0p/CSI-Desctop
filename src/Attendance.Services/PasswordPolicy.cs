namespace Attendance.Services;

public static class PasswordPolicy
{
    public const int MinLength = 8;
    public const string Description = "Не менее 8 символов, заглавные и строчные буквы, цифры.";

    /// <summary>Возвращает текст ошибки или null, если пароль подходит.</summary>
    public static string? Validate(string? password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinLength)
            return $"Пароль должен содержать не менее {MinLength} символов.";
        if (!password.Any(char.IsUpper) || !password.Any(char.IsLower))
            return "Пароль должен содержать заглавные и строчные буквы.";
        if (!password.Any(char.IsDigit))
            return "Пароль должен содержать цифры.";
        return null;
    }
}
