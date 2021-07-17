using System;
using System.Collections.Generic;
using System.Linq;
using Jackett.Common.Utils.Clients;

namespace Jackett.Performance.Utils
{
    public sealed class WebRequestEqualityComparer : IEqualityComparer<WebRequest>
    {
        public static readonly WebRequestEqualityComparer Default = new WebRequestEqualityComparer();
        private static readonly PostDataEqualityComparer _PostDataEqualityComparer = new PostDataEqualityComparer();

        public bool Equals(WebRequest x, WebRequest y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (ReferenceEquals(x, null))
                return false;
            if (ReferenceEquals(y, null))
                return false;
            if (x.GetType() != y.GetType())
                return false;
            if (!string.Equals(x.Url, y.Url, StringComparison.InvariantCulture))
                return false;
            if (x.Type != y.Type)
                return false;
            if (!string.Equals(x.RawBody, y.RawBody, StringComparison.InvariantCulture))
                return false;
            return _PostDataEqualityComparer.Equals(x.PostData, y.PostData);
        }

        public int GetHashCode(WebRequest obj)
        {
            unchecked
            {
                var hashCode = (obj.Url != null ? StringComparer.InvariantCulture.GetHashCode(obj.Url) : 0);
                hashCode = (hashCode * 397) ^ (int)obj.Type;
                hashCode = (hashCode * 397) ^ (obj.RawBody != null ? StringComparer.InvariantCulture.GetHashCode(obj.RawBody) : 0);
                hashCode = (hashCode * 397) ^ (obj.PostData != null ? _PostDataEqualityComparer.GetHashCode(obj.PostData) : 0);
                return hashCode;
            }
        }
        public class PostDataEqualityComparer : IEqualityComparer<IEnumerable<KeyValuePair<string, string>>>, IEqualityComparer<KeyValuePair<string, string>>
        {
            public bool Equals(IEnumerable<KeyValuePair<string, string>> x, IEnumerable<KeyValuePair<string, string>> y)
            {
                if (ReferenceEquals(x, y))
                    return true;
                if (ReferenceEquals(x, null))
                    return false;
                if (ReferenceEquals(y, null))
                    return false;
                if (x.GetType() != y.GetType())
                    return false;
                return Enumerable.SequenceEqual(x, y, this);
            }

            public int GetHashCode(IEnumerable<KeyValuePair<string, string>> obj)
            {
                unchecked
                {
                    return obj.Aggregate(17, (current, pair) => (current * 397) ^ _PostDataEqualityComparer.GetHashCode(pair));
                }
            }

            public bool Equals(KeyValuePair<string, string> x, KeyValuePair<string, string> y)
            {
                return string.Equals(x.Key, y.Key, StringComparison.InvariantCulture) && string.Equals(x.Value, y.Value, StringComparison.InvariantCulture);
            }

            public int GetHashCode(KeyValuePair<string, string> obj)
            {
                unchecked
                {
                    var hashCode = StringComparer.InvariantCulture.GetHashCode(obj.Key);
                    return (hashCode * 397) ^ StringComparer.InvariantCulture.GetHashCode(obj.Value);
                }
            }


        }
    }
}
