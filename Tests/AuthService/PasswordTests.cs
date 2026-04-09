using NUnit.Framework;

namespace FreshMart.Tests.AuthService;

// Password validation logic
public static class PasswordHelper
{
    public static bool IsStrongPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6) return false;
        bool hasUpper = password.Any(char.IsUpper);
        bool hasLower = password.Any(char.IsLower);
        bool hasDigit = password.Any(char.IsDigit);
        return hasUpper && hasLower && hasDigit;
    }

    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        try { var a = new System.Net.Mail.MailAddress(email); return a.Address == email; }
        catch { return false; }
    }

    public static string NormalizeEmail(string email) => email.Trim().ToLower();
}

[TestFixture]
public class PasswordTests
{
    [Test]
    public void IsStrongPassword_ValidPassword_ReturnsTrue()
    {
        Assert.That(PasswordHelper.IsStrongPassword("Admin@123"), Is.True);
    }

    [Test]
    public void IsStrongPassword_TooShort_ReturnsFalse()
    {
        Assert.That(PasswordHelper.IsStrongPassword("Ab1"), Is.False);
    }

    [Test]
    public void IsStrongPassword_NoUppercase_ReturnsFalse()
    {
        Assert.That(PasswordHelper.IsStrongPassword("admin123"), Is.False);
    }

    [Test]
    public void IsStrongPassword_NoLowercase_ReturnsFalse()
    {
        Assert.That(PasswordHelper.IsStrongPassword("ADMIN123"), Is.False);
    }

    [Test]
    public void IsStrongPassword_NoDigit_ReturnsFalse()
    {
        Assert.That(PasswordHelper.IsStrongPassword("AdminPass"), Is.False);
    }

    [Test]
    public void IsStrongPassword_EmptyString_ReturnsFalse()
    {
        Assert.That(PasswordHelper.IsStrongPassword(""), Is.False);
    }

    [Test]
    public void IsValidEmail_ValidEmail_ReturnsTrue()
    {
        Assert.That(PasswordHelper.IsValidEmail("aryandalal081@gmail.com"), Is.True);
    }

    [Test]
    public void IsValidEmail_MissingAt_ReturnsFalse()
    {
        Assert.That(PasswordHelper.IsValidEmail("invalidemail.com"), Is.False);
    }

    [Test]
    public void IsValidEmail_Empty_ReturnsFalse()
    {
        Assert.That(PasswordHelper.IsValidEmail(""), Is.False);
    }

    [Test]
    public void NormalizeEmail_TrimsAndLowers()
    {
        Assert.That(PasswordHelper.NormalizeEmail("  ARYAN@Gmail.COM  "), Is.EqualTo("aryan@gmail.com"));
    }

    [Test]
    public void NormalizeEmail_AlreadyNormalized_Unchanged()
    {
        Assert.That(PasswordHelper.NormalizeEmail("test@test.com"), Is.EqualTo("test@test.com"));
    }
}
