using Newtonsoft.Json.Serialization;

namespace Jackett.Common.Helpers
{
    public class LowerCaseNamingStrategy : NamingStrategy
    {
        protected override string ResolvePropertyName(string name) => name.ToLowerInvariant();
    }
}
