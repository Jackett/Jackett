using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using NLog;

namespace Jackett.Common.Indexers.Feeds
{
    public abstract class BaseNewznabIndexer : BaseFeedIndexer
    {
        protected BaseNewznabIndexer(string name, string link, string description, IIndexerConfigurationService configService, WebClient client, Logger logger, ConfigurationData configData, IProtectionService p, TorznabCapabilities caps = null, string downloadBase = null) : base(name, link, description, configService, client, logger, configData, p, caps, downloadBase)
        {
        }

        protected override IEnumerable<ReleaseInfo> ParseFeedForResults(string feedContent)
        {
            var doc = XDocument.Parse(feedContent);

            var results = doc.Descendants("item").Select(ResultFromFeedItem);

            return results;
        }

        protected virtual ReleaseInfo ResultFromFeedItem(XElement item)
        {
            var attributes = item.Descendants().Where(e => e.Name.LocalName == "attr");
            var release = new ReleaseInfo
            {
                Title = item.FirstValue("title"),
                Guid = new Uri(item.FirstValue("guid")),
                Link = new Uri(item.FirstValue("link")),
                Comments = new Uri(item.FirstValue("comments")),
                PublishDate = DateTime.Parse(item.FirstValue("pubDate")),
                Category = new List<int> { int.Parse(attributes.First(e => e.Attribute("name").Value == "category").Attribute("value").Value) },
                Size = ReadAttribute(attributes, "size").TryParse<long>(),
                Files = ReadAttribute(attributes, "files").TryParse<long>(),
                Description = item.FirstValue("description"),
                Seeders = ReadAttribute(attributes, "seeders").TryParse<int>(),
                Peers = ReadAttribute(attributes, "peers").TryParse<int>(),
                InfoHash = attributes.First(e => e.Attribute("name").Value == "infohash").Attribute("value").Value,
                MagnetUri = new Uri(attributes.First(e => e.Attribute("name").Value == "magneturl").Attribute("value").Value),
            };
            return release;
        }

        private string ReadAttribute(IEnumerable<XElement> attributes, string attributeName)
        {
            var attribute = attributes.FirstOrDefault(e => e.Attribute("name").Value == attributeName);
            if (attribute == null)
                return "";
            return attribute.Attribute("value").Value;
        }
    }
}
