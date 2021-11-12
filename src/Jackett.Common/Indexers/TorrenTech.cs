using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class TorrenTech : BaseWebIndexer
    {
        private string LoginUrl => SiteLink + "index.php?act=Login&CODE=01&CookieDate=1";
        private string IndexUrl => SiteLink + "index.php";

        private new ConfigurationDataBasicLoginWithRSSAndDisplay configData
        {
            get => (ConfigurationDataBasicLoginWithRSSAndDisplay)base.configData;
            set => base.configData = value;
        }

        public TorrenTech(IIndexerConfigurationService configService, Utils.Clients.WebClient wc, Logger l,
            IProtectionService ps, ICacheService cs)
            : base(id: "torrentech",
                   name: "Torrentech",
                   description: "Torrentech (TTH) is a Private Torrent Tracker for ELECTRONIC MUSIC",
                   link: "https://www.torrentech.org/",
                   caps: new TorznabCapabilities
                   {
                       MusicSearchParams = new List<MusicSearchParam>
                       {
                           MusicSearchParam.Q
                       }
                   },
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataBasicLoginWithRSSAndDisplay())
        {
            Encoding = Encoding.UTF8;
            Language = "en-US";
            Type = "private";

            wc.AddTrustedCertificate(new Uri(SiteLink).Host, "22E3C9896A1207EFF97599FE12B9DBB2AF8EC0CA");

            AddCategoryMapping(1, TorznabCatType.AudioMP3);
            AddCategoryMapping(2, TorznabCatType.AudioLossless);
            AddCategoryMapping(3, TorznabCatType.AudioOther);
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string>
            {
                { "UserName", configData.Username.Value },
                { "PassWord", configData.Password.Value }
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, LoginUrl, true);
            await ConfigureIfOK(result.Cookies, result.ContentString != null && result.ContentString.Contains("Logged in as: "), () =>
            {
                var errorMessage = result.ContentString;
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();

            WebResult results = null;
            var queryCollection = new NameValueCollection
            {
                { "act", "search" },
                { "forums", "all" },
                { "torrents", "1" },
                { "search_in", "titles" },
                { "result_type", "topics" }
            };

            // if the search string is empty use the getnew view
            if (string.IsNullOrWhiteSpace(searchString))
            {
                queryCollection.Add("CODE", "getnew");
                queryCollection.Add("active", "1");
            }
            else // use the normal search
            {
                searchString = searchString.Replace("-", " ");
                queryCollection.Add("CODE", "01");
                queryCollection.Add("keywords", searchString);
            }

            var searchUrl = IndexUrl + "?" + queryCollection.GetQueryString();
            results = await RequestWithCookiesAsync(searchUrl);
            if (results.IsRedirect && results.RedirectingTo.Contains("CODE=show"))
            {
                results = await RequestWithCookiesAsync(results.RedirectingTo);
            }
            try
            {
                var RowsSelector = "div.borderwrap:has(div.maintitle) > table > tbody > tr:has(a[href*=\"index.php?showtopic=\"])";

                var SearchResultParser = new HtmlParser();
                var SearchResultDocument = SearchResultParser.ParseDocument(results.ContentString);
                var Rows = SearchResultDocument.QuerySelectorAll(RowsSelector);
                foreach (var Row in Rows)
                {
                    try
                    {
                        //TODO refactor to initializer
                        var release = new ReleaseInfo();

                        var StatsElements = Row.QuerySelector("td:nth-child(5)");
                        var stats = StatsElements.TextContent.Split('·');
                        if (stats.Length != 3) // not a torrent
                            continue;

                        release.Seeders = ParseUtil.CoerceInt(stats[0]);
                        release.Peers = ParseUtil.CoerceInt(stats[1]) + release.Seeders;
                        release.Grabs = ParseUtil.CoerceInt(stats[2]);

                        release.MinimumRatio = 0.51;
                        release.MinimumSeedTime = 0;

                        var qDetailsLink = Row.QuerySelector("a[onmouseover][href*=\"index.php?showtopic=\"]");
                        release.Title = qDetailsLink.TextContent;
                        release.Details = new Uri(qDetailsLink.GetAttribute("href"));
                        release.Link = release.Details;
                        release.Guid = release.Link;

                        release.DownloadVolumeFactor = 1;
                        release.UploadVolumeFactor = 1;

                        var id = QueryHelpers.ParseQuery(release.Details.Query)["showtopic"].FirstOrDefault();

                        var desc = Row.QuerySelector("span.desc");
                        var forange = desc.QuerySelector("font.forange");
                        if (forange != null)
                        {
                            var DownloadVolumeFactor = forange.QuerySelector("i:contains(\"freeleech\")");
                            if (DownloadVolumeFactor != null)
                                release.DownloadVolumeFactor = 0;

                            var UploadVolumeFactor = forange.QuerySelector("i:contains(\"x upload]\")");
                            if (UploadVolumeFactor != null)
                                release.UploadVolumeFactor = ParseUtil.CoerceDouble(UploadVolumeFactor.TextContent.Split(' ')[0].Substring(1).Replace("x", ""));
                            forange.Remove();
                        }
                        var format = desc.TextContent;
                        release.Title += " [" + format + "]";

                        var preview = SearchResultDocument.QuerySelector("div#d21-tph-preview-data-" + id);
                        if (preview != null)
                        {
                            release.Description = "";
                            foreach (var e in preview.ChildNodes)
                            {
                                if (e.NodeType == NodeType.Text)
                                    release.Description += e.NodeValue;
                                else
                                    release.Description += e.TextContent + "\n";
                            }
                        }
                        release.Description = WebUtility.HtmlEncode(release.Description.Trim());
                        release.Description = release.Description.Replace("\n", "<br>");

                        if (format.Contains("MP3"))
                            release.Category = new List<int> { TorznabCatType.AudioMP3.ID };
                        else if (format.Contains("AAC"))
                            release.Category = new List<int> { TorznabCatType.AudioOther.ID };
                        else if (format.Contains("Lossless"))
                            release.Category = new List<int> { TorznabCatType.AudioLossless.ID };
                        else
                            release.Category = new List<int> { TorznabCatType.AudioOther.ID };

                        var lastAction = Row.QuerySelector("td:nth-child(9) > span").FirstChild.NodeValue;
                        release.PublishDate = DateTimeUtil.FromUnknown(lastAction, "UK");

                        releases.Add(release);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(string.Format("{0}: Error while parsing row '{1}':\n\n{2}", Id, Row.OuterHtml, ex));
                    }
                }
            }
            catch (Exception ex)
            {
                OnParseError(results.ContentString, ex);
            }

            return releases;
        }

        public override async Task<byte[]> Download(Uri link)
        {
            var response = await RequestWithCookiesAsync(link.ToString());
            var results = response.ContentString;
            var SearchResultParser = new HtmlParser();
            var SearchResultDocument = SearchResultParser.ParseDocument(results);
            var downloadSelector = "a[title=\"Download attachment\"]";
            var DlUri = SearchResultDocument.QuerySelector(downloadSelector);
            if (DlUri != null)
            {
                logger.Debug(string.Format("{0}: Download selector {1} matched:{2}", Id, downloadSelector, DlUri.OuterHtml));
                var href = DlUri.GetAttribute("href");
                link = new Uri(href);
            }
            else
            {
                logger.Error(string.Format("{0}: Download selector {1} didn't match:\n{2}", Id, downloadSelector, results));
                throw new Exception(string.Format("Download selector {0} didn't match", downloadSelector));
            }
            return await base.Download(link);
        }
    }
}
