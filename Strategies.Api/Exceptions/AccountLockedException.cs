using System;

namespace Strategies.Api.Exceptions
{
    public class AccountLockedException : Exception
    {
        public DateTime? LockoutEnd { get; }

        public AccountLockedException(string message, DateTime? lockoutEnd = null) : base(message)
        {
            LockoutEnd = lockoutEnd;
        }
    }
}
