using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Xml.Linq;

namespace Jackett.Common.Models
{
    public class ResultPage
    {
        private static readonly XNamespace atomNs = "http://www.w3.org/2005/Atom";
        private static readonly XNamespace torznabNs = "http://torznab.com/schemas/2015/feed";

        public ChannelInfo ChannelInfo { get; private set; }
        public IEnumerable<ReleaseInfo> Releases { get; set; }

        public ResultPage(ChannelInfo channelInfo)
        {
            ChannelInfo = channelInfo;
            Releases = new List<ReleaseInfo>();
        }

        private string xmlDateFormat(DateTime dt)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            //Sat, 14 Mar 2015 17:10:42 -0400
            var f = string.Format(@"{0:ddd, dd MMM yyyy HH:mm:ss }{1}", dt, string.Format("{0:zzz}", dt).Replace(":", ""));
            return f;
        }

        private XElement getTorznabElement(string name, object value) => value == null ? null : new XElement(torznabNs + "attr", new XAttribute("name", name), new XAttribute("value", value));

        public string ToXml(Uri selfAtom)
        {
            // IMPORTANT: We can't use Uri.ToString(), because it generates URLs without URL encode (links with unicode
            // characters are broken). We must use Uri.AbsoluteUri instead that handles encoding correctly
            var xdoc = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement("rss",
                    new XAttribute("version", "1.0"),
                    new XAttribute(XNamespace.Xmlns + "atom", atomNs.NamespaceName),
                    new XAttribute(XNamespace.Xmlns + "torznab", torznabNs.NamespaceName),
                    new XElement("channel",
                        new XElement(atomNs + "link",
                            new XAttribute("href", selfAtom.AbsoluteUri),
                            new XAttribute("rel", "self"),
                            new XAttribute("type", "application/rss+xml")
                        ),
                        new XElement("title", ChannelInfo.Title),
                        new XElement("description", ChannelInfo.Description),
                        new XElement("link", ChannelInfo.Link.AbsoluteUri),
                        new XElement("language", ChannelInfo.Language),
                        new XElement("category", ChannelInfo.Category),
                        new XElement("image",
                            new XElement("url", ChannelInfo.ImageUrl.AbsoluteUri),
                            new XElement("title", ChannelInfo.ImageTitle),
                            new XElement("link", ChannelInfo.ImageLink.AbsoluteUri),
                            new XElement("description", ChannelInfo.ImageDescription)
                        ),
                        from r in Releases
                        select new XElement("item",
                            new XElement("title", r.Title),
                            new XElement("guid", r.Guid.AbsoluteUri),  // GUID and (Link or Magnet) are mandatory
                            new XElement("jackettindexer", new XAttribute("id", r.Origin.Id), r.Origin.DisplayName),
                            r.Comments == null ? null : new XElement("comments", r.Comments.AbsoluteUri),
                            r.PublishDate == DateTime.MinValue ? new XElement("pubDate", xmlDateFormat(DateTime.Now)) : new XElement("pubDate", xmlDateFormat(r.PublishDate)),
                            r.Size == null ? null : new XElement("size", r.Size),
                            r.Files == null ? null : new XElement("files", r.Files),
                            r.Grabs == null ? null : new XElement("grabs", r.Grabs),
                            new XElement("description", r.Description),
                            new XElement("link", r.Link?.AbsoluteUri ?? r.MagnetUri.AbsoluteUri),
                            r.Category == null ? null : from c in r.Category select new XElement("category", c),
                            new XElement(
                                "enclosure",
                                new XAttribute("url", r.Link?.AbsoluteUri ?? r.MagnetUri.AbsoluteUri),
                                r.Size == null ? null : new XAttribute("length", r.Size),
                                new XAttribute("type", "application/x-bittorrent")
                            ),
                            r.Category == null ? null : from c in r.Category select getTorznabElement("category", c),
                            getTorznabElement("magneturl", r.MagnetUri?.AbsoluteUri),
                            getTorznabElement("rageid", r.RageID),
                            getTorznabElement("thetvdb", r.TVDBId),
                            getTorznabElement("imdb", r.Imdb == null ? null : ((long)r.Imdb).ToString("D7")),
                            getTorznabElement("seeders", r.Seeders),
                            getTorznabElement("peers", r.Peers),
                            getTorznabElement("infohash", r.InfoHash),
                            getTorznabElement("minimumratio", r.MinimumRatio),
                            getTorznabElement("minimumseedtime", r.MinimumSeedTime),
                            getTorznabElement("downloadvolumefactor", r.DownloadVolumeFactor),
                            getTorznabElement("uploadvolumefactor", r.UploadVolumeFactor)
                        )
                    )
                )
            );

            return xdoc.Declaration.ToString() + Environment.NewLine + xdoc.ToString();
        }
    }
}
