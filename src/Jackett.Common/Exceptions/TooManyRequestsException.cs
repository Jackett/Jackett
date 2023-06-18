using System;
using System.Linq;
using Jackett.Common.Utils.Clients;

namespace Jackett.Common.Exceptions
{
    public class TooManyRequestsException : Exception
    {
        public TimeSpan RetryAfter { get; private set; }

        public TooManyRequestsException(string message, TimeSpan retryWait)
            : base(message)
        {
            RetryAfter = retryWait;
        }

        public TooManyRequestsException(string message, WebResult response)
            : base(message)
        {
            if (response.Headers.TryGetValue("Retry-After", out var header) && header.FirstOrDefault() is { } retryAfter)
            {
                if (int.TryParse(retryAfter, out var seconds))
                {
                    RetryAfter = TimeSpan.FromSeconds(seconds);
                }
                else if (DateTime.TryParse(retryAfter, out var date))
                {
                    RetryAfter = date.ToUniversalTime() - DateTime.UtcNow;
                }
            }
        }
    }
}
