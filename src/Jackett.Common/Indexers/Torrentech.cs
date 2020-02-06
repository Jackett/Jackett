using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json.Linq;
using NLog;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers
{
    public class Torrentech : BaseWebIndexer
    {
        private string LoginUrl => $"{SiteLink}index.php?act=Login&CODE=01&CookieDate=1";
        private string IndexUrl => $"{SiteLink}index.php";

        private new ConfigurationDataBasicLoginWithRSSAndDisplay configData
        {
            get => (ConfigurationDataBasicLoginWithRSSAndDisplay)base.configData;
            set => base.configData = value;
        }

        public Torrentech(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps) : base(
            "Torrentech", description: "TorrenTech (TTH) is a Private Torrent Tracker for ELECTRONIC MUSIC",
            link: "https://www.torrentech.org/", caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
            configService: configService, client: wc, logger: l, p: ps,
            configData: new ConfigurationDataBasicLoginWithRSSAndDisplay())
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";
            AddCategoryMapping(1, TorznabCatType.AudioMP3);
            AddCategoryMapping(2, TorznabCatType.AudioLossless);
            AddCategoryMapping(3, TorznabCatType.AudioOther);
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string>
            {
                {"UserName", configData.Username.Value}, {"PassWord", configData.Password.Value}
            };
            var result = await RequestLoginAndFollowRedirectAsync(LoginUrl, pairs, null, true, null, LoginUrl, true);
            await ConfigureIfOkAsync(
                result.Cookies, result.Content?.Contains("Logged in as: ") == true, () =>
                {
                    var errorMessage = result.Content;
                    throw new ExceptionWithConfigData(errorMessage, configData);
                });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();
            var queryCollection = new NameValueCollection
            {
                {"act", "search"},
                {"forums", "all"},
                {"torrents", "1"},
                {"search_in", "titles"},
                {"result_type", "topics"}
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

            var searchUrl = $"{IndexUrl}?{queryCollection.GetQueryString()}";
            var results = await RequestStringWithCookiesAsync(searchUrl);
            if (results.IsRedirect && results.RedirectingTo.Contains("CODE=show"))
                results = await RequestStringWithCookiesAsync(results.RedirectingTo);
            try
            {
                var rowsSelector =
                    "div.borderwrap:has(div.maintitle) > table > tbody > tr:has(a[href*=\"index.php?showtopic=\"])";
                var searchResultParser = new HtmlParser();
                var searchResultDocument = searchResultParser.ParseDocument(results.Content);
                var rows = searchResultDocument.QuerySelectorAll(rowsSelector);
                foreach (var row in rows)
                    try
                    {
                        var release = new ReleaseInfo();
                        var statsElements = row.QuerySelector("td:nth-child(5)");
                        var stats = statsElements.TextContent.Split('·');
                        if (stats.Length != 3) // not a torrent
                            continue;
                        release.Seeders = ParseUtil.CoerceInt(stats[0]);
                        release.Peers = ParseUtil.CoerceInt(stats[1]) + release.Seeders;
                        release.Grabs = ParseUtil.CoerceInt(stats[2]);
                        release.MinimumRatio = 0.51;
                        release.MinimumSeedTime = 0;
                        var qDetailsLink = row.QuerySelector("a[onmouseover][href*=\"index.php?showtopic=\"]");
                        release.Title = qDetailsLink.TextContent;
                        release.Comments = new Uri(qDetailsLink.GetAttribute("href"));
                        release.Link = release.Comments;
                        release.Guid = release.Link;
                        release.DownloadVolumeFactor = 1;
                        release.UploadVolumeFactor = 1;
                        var id = QueryHelpers.ParseQuery(release.Comments.Query)["showtopic"].FirstOrDefault();
                        var desc = row.QuerySelector("span.desc");
                        var forange = desc.QuerySelector("font.forange");
                        if (forange != null)
                        {
                            var downloadVolumeFactor = forange.QuerySelector("i:contains(\"freeleech\")");
                            if (downloadVolumeFactor != null)
                                release.DownloadVolumeFactor = 0;
                            var uploadVolumeFactor = forange.QuerySelector("i:contains(\"x upload]\")");
                            if (uploadVolumeFactor != null)
                                release.UploadVolumeFactor = ParseUtil.CoerceDouble(
                                    uploadVolumeFactor.TextContent.Split(' ')[0].Substring(1).Replace("x", ""));
                            forange.Remove();
                        }

                        var format = desc.TextContent;
                        release.Title += $" [{format}]";
                        var preview = searchResultDocument.QuerySelector($"div#d21-tph-preview-data-{id}");
                        if (preview != null)
                        {
                            release.Description = "";
                            foreach (var e in preview.ChildNodes)
                                if (e.NodeType == NodeType.Text)
                                    release.Description += e.NodeValue;
                                else
                                    release.Description += $"{e.TextContent}\n";
                        }

                        release.Description = WebUtility.HtmlEncode(release.Description.Trim());
                        release.Description = release.Description.Replace("\n", "<br>");
                        release.Category = format.Contains("MP3")
                            ? new List<int> { TorznabCatType.AudioMP3.ID }
                            : format.Contains("AAC")
                            ? new List<int> { TorznabCatType.AudioOther.ID }
                            : format.Contains("Lossless")
                            ? new List<int> { TorznabCatType.AudioLossless.ID }
                            : new List<int> { TorznabCatType.AudioOther.ID };
                        var lastAction = row.QuerySelector("td:nth-child(9) > span").FirstChild.NodeValue;
                        release.PublishDate = DateTimeUtil.FromUnknown(lastAction, "UK");
                        releases.Add(release);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(string.Format("{0}: Error while parsing row '{1}':\n\n{2}", ID, row.OuterHtml, ex));
                    }
            }
            catch (Exception ex)
            {
                OnParseError(results.Content, ex);
            }

            return releases;
        }

        public override async Task<byte[]> Download(Uri link)
        {
            var response = await RequestStringWithCookiesAsync(link.ToString());
            var results = response.Content;
            var searchResultParser = new HtmlParser();
            var searchResultDocument = searchResultParser.ParseDocument(results);
            var downloadSelector = "a[title=\"Download attachment\"]";
            var dlUri = searchResultDocument.QuerySelector(downloadSelector);
            if (dlUri != null)
            {
                logger.Debug(string.Format("{0}: Download selector {1} matched:{2}", ID, downloadSelector, dlUri.OuterHtml));
                var href = dlUri.GetAttribute("href");
                link = new Uri(href);
            }
            else
            {
                logger.Error(string.Format("{0}: Download selector {1} didn't match:\n{2}", ID, downloadSelector, results));
                throw new Exception(string.Format("Download selector {0} didn't match", downloadSelector));
            }

            return await base.Download(link);
        }
    }
}
