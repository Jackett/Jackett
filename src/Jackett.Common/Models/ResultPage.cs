using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;

namespace Jackett.Common.Models
{
    public class ResultPage
    {
        private static readonly XNamespace _AtomNs = "http://www.w3.org/2005/Atom";
        private static readonly XNamespace _TorznabNs = "http://torznab.com/schemas/2015/feed";

        // filters control characters but allows only properly-formed surrogate sequences
        // https://stackoverflow.com/a/961504
        private static readonly Regex _InvalidXmlChars = new Regex(
            @"(?<![\uD800-\uDBFF])[\uDC00-\uDFFF]|[\uD800-\uDBFF](?![\uDC00-\uDFFF])|[\x00-\x08\x0B\x0C\x0E-\x1F\x7F-\x9F\uFEFF\uFFFE\uFFFF]",
            RegexOptions.Compiled);

        private ChannelInfo ChannelInfo { get; }
        public IEnumerable<ReleaseInfo> Releases { get; set; }

        public ResultPage(ChannelInfo channelInfo)
        {
            ChannelInfo = channelInfo;
            Releases = new List<ReleaseInfo>();
        }

        /// <summary>
        /// removes any unusual unicode characters that can't be encoded into XML (eg 0x1A)
        /// </summary>
        private static string RemoveInvalidXMLChars(string text)
        {
            if (text == null)
                return null;
            return _InvalidXmlChars.Replace(text, "");
        }

        private static string XmlDateFormat(DateTime dt)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            //Sat, 14 Mar 2015 17:10:42 -0400
            return $"{dt:ddd, dd MMM yyyy HH:mm:ss} " + $"{dt:zzz}".Replace(":", "");
        }

        private static XElement GetTorznabElement(string name, object value)
        {
            if (value == null)
                return null;
            return new XElement(_TorznabNs + "attr", new XAttribute("name", name), new XAttribute("value", value));
        }

        public string ToXml(Uri selfAtom)
        {
            // IMPORTANT: We can't use Uri.ToString(), because it generates URLs without URL encode (links with unicode
            // characters are broken). We must use Uri.AbsoluteUri instead that handles encoding correctly
            var xdoc = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement("rss",
                    new XAttribute("version", "2.0"),
                    new XAttribute(XNamespace.Xmlns + "atom", _AtomNs.NamespaceName),
                    new XAttribute(XNamespace.Xmlns + "torznab", _TorznabNs.NamespaceName),
                    new XElement("channel",
                        new XElement(_AtomNs + "link",
                            new XAttribute("href", selfAtom.AbsoluteUri),
                            new XAttribute("rel", "self"),
                            new XAttribute("type", "application/rss+xml")
                        ),
                        new XElement("title", ChannelInfo.Title),
                        new XElement("description", ChannelInfo.Description),
                        new XElement("link", ChannelInfo.Link.AbsoluteUri),
                        new XElement("language", ChannelInfo.Language),
                        new XElement("category", ChannelInfo.Category),
                        from r in Releases
                        select new XElement("item",
                            new XElement("title", RemoveInvalidXMLChars(r.Title)),
                            new XElement("guid", r.Guid.AbsoluteUri),  // GUID and (Link or Magnet) are mandatory
                            new XElement("jackettindexer", new XAttribute("id", r.Origin.Id), r.Origin.DisplayName),
                            r.Details == null ? null : new XElement("comments", r.Details.AbsoluteUri),
                            r.PublishDate == DateTime.MinValue ? new XElement("pubDate", XmlDateFormat(DateTime.Now)) : new XElement("pubDate", XmlDateFormat(r.PublishDate)),
                            r.Size == null ? null : new XElement("size", r.Size),
                            r.Files == null ? null : new XElement("files", r.Files),
                            r.Grabs == null ? null : new XElement("grabs", r.Grabs),
                            new XElement("description", RemoveInvalidXMLChars(r.Description)),
                            new XElement("link", r.Link?.AbsoluteUri ?? r.MagnetUri.AbsoluteUri),
                            r.Category == null ? null : from c in r.Category select new XElement("category", c),
                            new XElement(
                                "enclosure",
                                new XAttribute("url", r.Link?.AbsoluteUri ?? r.MagnetUri.AbsoluteUri),
                                r.Size == null ? null : new XAttribute("length", r.Size),
                                new XAttribute("type", "application/x-bittorrent")
                            ),
                            r.Category == null ? null : from c in r.Category select GetTorznabElement("category", c),
                            GetTorznabElement("imdb", r.Imdb?.ToString("D7")),
                            GetTorznabElement("imdbid", r.Imdb != null ? "tt" + r.Imdb?.ToString("D7") : null),
                            GetTorznabElement("rageid", r.RageID),
                            GetTorznabElement("tvdbid", r.TVDBId),
                            GetTorznabElement("tmdbid", r.TMDb),
                            GetTorznabElement("author", RemoveInvalidXMLChars(r.Author)),
                            GetTorznabElement("booktitle", RemoveInvalidXMLChars(r.BookTitle)),
                            GetTorznabElement("seeders", r.Seeders),
                            GetTorznabElement("peers", r.Peers),
                            GetTorznabElement("magneturl", r.MagnetUri?.AbsoluteUri),
                            GetTorznabElement("infohash", RemoveInvalidXMLChars(r.InfoHash)),
                            GetTorznabElement("minimumratio", r.MinimumRatio),
                            GetTorznabElement("minimumseedtime", r.MinimumSeedTime),
                            GetTorznabElement("downloadvolumefactor", r.DownloadVolumeFactor),
                            GetTorznabElement("uploadvolumefactor", r.UploadVolumeFactor),
                            GetTorznabElement("coverurl", r.Poster?.AbsoluteUri)
                        )
                    )
                )
            );

            return xdoc.Declaration + Environment.NewLine + xdoc;
        }
    }
}
