using System.Linq;
using System.Xml.Linq;
using Jackett.Common.Extensions;
using Newtonsoft.Json.Linq;

namespace Jackett.Common.Helpers
{
    public static class XmlToJsonConverter
    {
        /// <summary>
        /// Converts an XML element to a JSON object.
        /// Attributes are stored in an @attributes object.
        /// </summary>
        public static JToken XmlToJson(XElement element)
        {
            var obj = new JObject();

            // Build @attributes object from element attributes
            if (element.Attributes().Any())
            {
                var attributes = element.Attributes()
                    .ToDictionary(
                        attribute => GetName(attribute.Name, attribute.Parent?.Document),
                        attribute => (JToken)attribute.Value
                    );
                obj.Add("@attributes", JObject.FromObject(attributes));
            }

            foreach (var childElement in element.Elements())
            {
                var childObj = XmlToJson(childElement);
                var childName = GetName(childElement.Name, element.Document);

                // If the child name already exists, convert it to an array
                if (obj.ContainsKey(childName))
                {
                    if (obj[childName] is JArray existingArray)
                    {
                        existingArray.Add(childObj);
                    }
                    else
                    {
                        obj[childName] = new JArray(obj[childName]!, childObj);
                    }
                }
                else
                {
                    obj.Add(childName, childObj);
                }
            }

            if (!element.Elements().Any() && !element.Value.IsNullOrWhiteSpace())
            {
                return element.Value;
            }

            return obj;
        }

        private static string GetName(XName name, XDocument document)
        {
            if (document == null)
            {
                return name.LocalName;
            }

            var prefix = document.Root?.GetPrefixOfNamespace(name.Namespace);
            return prefix != null ? $"{prefix}:{name.LocalName}" : name.LocalName;
        }
    }
}
