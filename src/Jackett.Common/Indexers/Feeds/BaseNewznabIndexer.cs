using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
    [ExcludeFromCodeCoverage]
    public abstract class BaseNewznabIndexer : BaseFeedIndexer
    {
        protected BaseNewznabIndexer(IIndexerConfigurationService configService, WebClient client, Logger logger,
                                     ConfigurationData configData, IProtectionService p, ICacheService cs,
                                     string downloadBase = null)
            : base(configService: configService,
                   client: client,
                   logger: logger,
                   p: p,
                   cs: cs,
                   configData: configData,
                   downloadBase: downloadBase)
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
            long? size = null;
            if (long.TryParse(ReadAttribute(attributes, "size"), out var longVal))
                size = (long?)longVal;
            else if (long.TryParse(item.FirstValue("size"), out longVal))
                size = (long?)longVal;
            long? files = null;
            if (long.TryParse(ReadAttribute(attributes, "files"), out longVal))
                files = (long?)longVal;
            else if (long.TryParse(item.FirstValue("files"), out longVal))
                files = (long?)longVal;
            long? grabs = null;
            if (item.Descendants("grabs").Any())
                grabs = long.TryParse(item.FirstValue("grabs"), out longVal) ? (long?)longVal : null;
            var seeders = int.TryParse(ReadAttribute(attributes, "seeders"), out var intVal) ? (int?)intVal : null;
            var peers = int.TryParse(ReadAttribute(attributes, "peers"), out intVal) ? (int?)intVal : null;
            double? downloadvolumefactor = double.TryParse(ReadAttribute(attributes, "downloadvolumefactor"), out var doubleVal) ? (double?)doubleVal : null;
            double? uploadvolumefactor = double.TryParse(ReadAttribute(attributes, "uploadvolumefactor"), out doubleVal) ? (double?)doubleVal : null;
            var magnet = ReadAttribute(attributes, "magneturl");
            var magneturi = !string.IsNullOrEmpty(magnet) ? new Uri(magnet) : null;
            var categories = item.Descendants().Where(e => e.Name == "category" && int.TryParse(e.Value, out var categoryid));
            List<int> categoryids = null;
            if (categories.Any())
                categoryids = new List<int> { int.Parse(categories.Last(e => !string.IsNullOrEmpty(e.Value)).Value) };
            else
                categoryids = new List<int> { int.Parse(attributes.First(e => e.Attribute("name").Value == "category").Attribute("value").Value) };
            var imdb = long.TryParse(ReadAttribute(attributes, "imdb"), out longVal) ? (long?)longVal : null;
            var imdbId = ReadAttribute(attributes, "imdbid");
            if (imdb == null && imdbId.StartsWith("tt"))
                imdb = long.TryParse(imdbId.Substring(2), out longVal) ? (long?)longVal : null;
            var rageId = long.TryParse(ReadAttribute(attributes, "rageid"), out longVal) ? (long?)longVal : null;
            var tvdbId = long.TryParse(ReadAttribute(attributes, "tvdbid"), out longVal) ? (long?)longVal : null;
            var tvMazeid = long.TryParse(ReadAttribute(attributes, "tvmazeid"), out longVal) ? (long?)longVal : null;

            var release = new ReleaseInfo
            {
                Title = item.FirstValue("title"),
                Guid = new Uri(item.FirstValue("guid")),
                Link = new Uri(item.FirstValue("link")),
                Details = new Uri(item.FirstValue("comments")),
                PublishDate = DateTime.Parse(item.FirstValue("pubDate")),
                Category = categoryids,
                Size = size,
                Files = files,
                Description = item.FirstValue("description"),
                Grabs = grabs,
                Seeders = seeders,
                Peers = peers,
                InfoHash = attributes.First(e => e.Attribute("name").Value == "infohash").Attribute("value").Value,
                DownloadVolumeFactor = downloadvolumefactor,
                UploadVolumeFactor = uploadvolumefactor,
                Imdb = imdb,
                RageID = rageId,
                TVDBId = tvdbId,
                TVMazeId = tvMazeid
            };
            if (magneturi != null)
                release.MagnetUri = magneturi;
            return release;
        }

        protected string ReadAttribute(IEnumerable<XElement> attributes, string attributeName)
        {
            var attribute = attributes.FirstOrDefault(e => e.Attribute("name").Value == attributeName);
            if (attribute == null)
                return "";
            return attribute.Attribute("value").Value;
        }
    }
}
