using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Strategies.Api.Services
{
    /// <summary>
    /// Password rules in the style of NIST SP 800-63B: a length range, a blocklist of common
    /// passwords and a check for words tied to this service or user. No composition rules
    /// (uppercase, digits, symbols) and spaces are allowed.
    /// </summary>
    public class PasswordPolicy : IPasswordPolicy
    {
        public const int MinLength = 12;
        public const int MaxLength = 64;

        // bcrypt silently ignores everything past 72 bytes.
        public const int MaxUtf8Bytes = 72;

        // A context word is refused when fewer than this many characters remain once it is removed.
        private const int MinRemainingAfterContextWord = 8;

        private static readonly string[] ServiceWords =
        {
            "strategies", "nexus", "immigration", "migration", "visa", "southafrica"
        };

        // Most common passwords from public breach lists, plus common 12+ character patterns
        // (most short entries are already refused by the length rule). Compared case-insensitively.
        private static readonly HashSet<string> CommonPasswords = new(StringComparer.OrdinalIgnoreCase)
        {
            "123456", "password", "123456789", "12345678", "12345", "qwerty", "1234567", "111111",
            "1234567890", "123123", "abc123", "1234", "password1", "iloveyou", "1q2w3e4r", "000000",
            "qwerty123", "zaq12wsx", "dragon", "sunshine", "princess", "letmein", "654321", "monkey",
            "27653", "1qaz2wsx", "123321", "qwertyuiop", "superman", "asdfghjkl", "trustno1", "football",
            "baseball", "welcome", "admin", "login", "starwars", "master", "hello", "freedom",
            "whatever", "qazwsx", "michael", "shadow", "jessica", "charlie", "jennifer", "hunter",
            "killer", "soccer", "batman", "andrew", "tigger", "daniel", "thomas", "jordan",
            "harley", "ranger", "buster", "pepper", "ginger", "joshua", "summer", "ashley",
            "mustang", "access", "flower", "computer", "cheese", "internet", "matrix", "secret",
            "passw0rd", "p@ssw0rd", "p@ssword", "password!", "password12", "password123", "password1!",
            "welcome1", "welcome123", "letmein1", "admin123", "administrator", "root", "toor", "changeme",
            "default", "guest", "test", "test123", "testing", "qwe123", "qweqwe", "asdasd",
            "zxcvbnm", "zxcvbn", "asdf1234", "abcd1234", "abcdef", "abcdefg", "abcdefgh", "11111111",
            "00000000", "88888888", "987654321", "7777777", "666666", "555555", "121212", "112233",
            "159753", "147258369", "789456123", "123qwe", "1q2w3e", "1qaz2wsx3edc", "q1w2e3r4",
            "q1w2e3r4t5", "q1w2e3r4t5y6", "1q2w3e4r5t", "1q2w3e4r5t6y", "qwerty1", "qwerty12", "qwerty1234",
            "iloveyou1", "iloveyou12", "lovely", "loveme", "babygirl", "angel", "princess1", "sunshine1",
            "football1", "baseball1", "monkey1", "dragon1", "shadow1", "superman1", "michael1", "jordan23",
            "chocolate", "butterfly", "liverpool", "chelsea", "arsenal", "manchester", "samsung", "google",
            "apple", "iphone", "pokemon", "minecraft", "fortnite", "naruto", "spiderman", "blink182",
            "aaaaaa", "aaaaaaaa", "qqqqqq", "zzzzzz", "solo", "mypassword", "yourpassword", "newpassword",
            "letmein123", "trustno1234", "secret123", "pass123", "pass1234", "passpass", "passwordpassword",
            "password1234", "password12345", "password123!", "password2024", "password2025", "password2026",
            "123456789012", "1234567890123", "12345678910", "1234567891011", "123456123456", "123123123123",
            "111111111111", "000000000000", "aaaaaaaaaaaa", "abcdefghijkl", "abcdefghijklm", "qwertyuiop12",
            "qwertyuiop123", "qwerty123456", "qwertyuiopasdfghjkl", "asdfghjkl123", "zxcvbnm12345",
            "1qazxsw23edc", "zaq12wsxcde3", "zaq1zaq1", "qazwsxedc", "qazwsxedcrfv", "iloveyou1234",
            "welcome12345", "letmein12345", "changeme1234", "football1234", "baseball1234", "monkey123456",
            "dragon123456", "sunshine1234", "princess1234", "superman1234", "administrator1", "correcthorsebatterystaple"
        };

        public IReadOnlyList<string> Validate(string? password, string? email, string? fullName)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(password))
            {
                errors.Add("Enter a password.");
                return errors;
            }

            // Counted in code points, so an emoji or accented letter counts as one (matches the web).
            var length = CountCharacters(password);
            if (length < MinLength)
                errors.Add($"Use at least {MinLength} characters.");
            else if (length > MaxLength)
                errors.Add($"Use {MaxLength} characters or fewer.");
            else if (Encoding.UTF8.GetByteCount(password) > MaxUtf8Bytes)
                errors.Add("This password is too long. Use fewer accented letters, symbols or emoji.");

            if (errors.Count > 0)
                return errors;

            if (CommonPasswords.Contains(password))
                errors.Add("This password is too common. Choose something harder to guess.");

            if (IsBuiltOnContextWord(password, email, fullName))
                errors.Add("Don't base your password on your name, your email address or this service.");

            return errors;
        }

        private static bool IsBuiltOnContextWord(string password, string? email, string? fullName)
        {
            var lowered = password.ToLowerInvariant();

            foreach (var word in ContextWords(email, fullName))
            {
                if (!lowered.Contains(word, StringComparison.Ordinal))
                    continue;

                var remaining = lowered.Replace(word, string.Empty, StringComparison.Ordinal);
                if (CountCharacters(remaining) < MinRemainingAfterContextWord)
                    return true;
            }

            return false;
        }

        private static IEnumerable<string> ContextWords(string? email, string? fullName)
        {
            foreach (var word in ServiceWords)
                yield return word;

            var at = email?.IndexOf('@') ?? -1;
            if (at > 0)
            {
                var localPart = email!.Substring(0, at).Trim().ToLowerInvariant();
                if (localPart.Count(char.IsLetter) >= 3)
                    yield return localPart;
            }

            if (!string.IsNullOrWhiteSpace(fullName))
            {
                var nameParts = Regex
                    .Split(fullName.ToLowerInvariant(), @"[^\p{L}]+")
                    .Where(part => part.Length >= 3);

                foreach (var part in nameParts)
                    yield return part;
            }
        }

        private static int CountCharacters(string value) => value.EnumerateRunes().Count();
    }
}
