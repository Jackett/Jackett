using System.IO;
using Newtonsoft.Json;

namespace Jackett.Common.Helpers
{
    /// <summary>
    /// JsonTextWriter to convert XML to JSON.
    /// Makes sure that XML attributes do not have the '@' or '#' prefix in the JSON output.
    /// https://stackoverflow.com/a/43485727
    /// </summary>
    public class XmlToJsonWriter : JsonTextWriter
    {
        public XmlToJsonWriter(TextWriter textWriter) : base(textWriter)
        {
        }

        public override void WritePropertyName(string name)
        {
            if (name.StartsWith("@") || name.StartsWith("#"))
            {
                base.WritePropertyName(name.Substring(1));
            }
            else
            {
                base.WritePropertyName(name);
            }
        }
    }
}
