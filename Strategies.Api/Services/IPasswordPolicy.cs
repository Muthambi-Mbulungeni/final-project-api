using System.Collections.Generic;

namespace Strategies.Api.Services
{
    public interface IPasswordPolicy
    {
        /// <summary>
        /// Checks a new password and returns plain-English errors the web can show.
        /// An empty list means the password is acceptable.
        /// </summary>
        IReadOnlyList<string> Validate(string? password, string? email, string? fullName);
    }
}
