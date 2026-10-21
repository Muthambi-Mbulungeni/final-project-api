using System.Text;
using Strategies.Api.Services;
using Xunit;

namespace Strategies.Api.Tests
{
    public class PasswordPolicyTests
    {
        private const string Email    = "thandeka.mokoena@example.com";
        private const string FullName = "Thandeka Mokoena";

        private static readonly PasswordPolicy Policy = new PasswordPolicy();

        [Fact]
        public void Validate_ElevenCharacters_IsRefused()
        {
            var errors = Policy.Validate("tulip garde", Email, FullName);

            Assert.Contains("Use at least 12 characters.", errors);
        }

        [Fact]
        public void Validate_TwelveCharacters_IsAccepted()
        {
            var errors = Policy.Validate("tulip garden", Email, FullName);

            Assert.Empty(errors);
        }

        [Fact]
        public void Validate_SixtyFiveCharacters_IsRefused()
        {
            var errors = Policy.Validate(new string('k', 65), Email, FullName);

            Assert.Contains("Use 64 characters or fewer.", errors);
        }

        [Fact]
        public void Validate_SixtyFourMultibyteCharactersOverSeventyTwoBytes_IsRefused()
        {
            // "é" is 2 bytes in UTF-8: 64 characters, 128 bytes. bcrypt would ignore everything past byte 72.
            var password = new string('é', 64);
            Assert.True(Encoding.UTF8.GetByteCount(password) > PasswordPolicy.MaxUtf8Bytes);

            var errors = Policy.Validate(password, Email, FullName);

            Assert.Single(errors);
            Assert.Contains("too long", errors[0]);
        }

        [Fact]
        public void Validate_WhitespaceOnly_IsRefused()
        {
            var errors = Policy.Validate("              ", Email, FullName);

            Assert.Equal(new[] { "Enter a password." }, errors);
        }

        [Fact]
        public void Validate_CommonPassword_IsRefused()
        {
            var errors = Policy.Validate("Password1234", Email, FullName);

            Assert.Contains("This password is too common. Choose something harder to guess.", errors);
        }

        [Fact]
        public void Validate_ServiceWordWithPadding_IsRefused()
        {
            var errors = Policy.Validate("Immigration2026!", Email, FullName);

            Assert.Contains("Don't base your password on your name, your email address or this service.", errors);
        }

        [Fact]
        public void Validate_PassphraseContainingServiceWordInsideAnotherWord_IsAccepted()
        {
            // "advisable" contains "visa", but plenty remains once it is removed.
            var errors = Policy.Validate("my advisable horse", Email, FullName);

            Assert.Empty(errors);
        }

        [Fact]
        public void Validate_PasswordContainingEmailLocalPart_IsRefused()
        {
            var errors = Policy.Validate("Thandeka.Mokoena99", Email, "Someone Else");

            Assert.Contains("Don't base your password on your name, your email address or this service.", errors);
        }
    }
}
