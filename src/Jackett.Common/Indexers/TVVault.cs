using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    public class TVVault : BaseWebIndexer
    {
        private string LoginUrl => $"{SiteLink}login.php";
        private string BrowseUrl => $"{SiteLink}torrents.php";

        private new ConfigurationDataBasicLoginWithRSSAndDisplay configData
        {
            get => (ConfigurationDataBasicLoginWithRSSAndDisplay)base.configData;
            set => base.configData = value;
        }

        public TVVault(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps) : base(
            "TV-Vault", description: "A TV tracker for old shows.", link: "https://tv-vault.me/",
            caps: TorznabUtil.CreateDefaultTorznabTVCaps(), configService: configService, client: wc, logger: l, p: ps,
            configData: new ConfigurationDataBasicLoginWithRSSAndDisplay())
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";
            AddCategoryMapping(1, TorznabCatType.TV);
            AddCategoryMapping(2, TorznabCatType.Movies);
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string>
            {
                {"username", configData.Username.Value},
                {"password", configData.Password.Value},
                {"keeplogged", "1"},
                {"login", "Log+In!"}
            };
            var result = await RequestLoginAndFollowRedirectAsync(LoginUrl, pairs, null, true, null, LoginUrl, true);
            await ConfigureIfOkAsync(
                result.Cookies, result.Content?.Contains("logout.php") == true, () =>
                {
                    var errorMessage = result.Content;
                    throw new ExceptionWithConfigData(errorMessage, configData);
                });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        private string StripSearchString(string term)
        {
            // Search does not support searching with episode numbers so strip it if we have one
            // Ww AND filter the result later to archive the proper result
            term = Regex.Replace(term, @"[S|E]\d\d", string.Empty);
            return term.Trim();
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();
            var searchUrl = BrowseUrl;
            var queryCollection = new NameValueCollection
            {
                {"searchstr", StripSearchString(searchString)},
                {"order_by", "s3"},
                {"order_way", "desc"},
                {"disablegrouping", "1"}
            };
            searchUrl += $"?{queryCollection.GetQueryString()}";
            var results = await RequestStringWithCookiesAsync(searchUrl);
            try
            {
                var rowsSelector = "table.torrent_table > tbody > tr.torrent";
                var searchResultParser = new HtmlParser();
                var searchResultDocument = searchResultParser.ParseDocument(results.Content);
                var rows = searchResultDocument.QuerySelectorAll(rowsSelector);
                foreach (var row in rows)
                {
                    var release = new ReleaseInfo { MinimumRatio = 1, MinimumSeedTime = 0 };
                    var qDetailsLink = row.QuerySelector("a[href^=\"torrents.php?id=\"]");
                    var descStr = qDetailsLink.NextSibling;
                    var files = row.QuerySelector("td:nth-child(3)");
                    var added = row.QuerySelector("td:nth-child(4)");
                    var size = row.QuerySelector("td:nth-child(5)").FirstChild;
                    var grabs = row.QuerySelector("td:nth-child(6)");
                    var seeders = row.QuerySelector("td:nth-child(7)");
                    var leechers = row.QuerySelector("td:nth-child(8)");
                    var freeLeech = row.QuerySelector("strong.freeleech_normal");
                    var torrentIdParts = qDetailsLink.GetAttribute("href").Split('=');
                    var torrentId = torrentIdParts[torrentIdParts.Length - 1];
                    var dlLink = $"torrents.php?action=download&id={torrentId}";
                    release.Description = descStr.TextContent.Trim();
                    release.Title = $"{qDetailsLink.TextContent} {release.Description}";
                    release.PublishDate = DateTimeUtil.FromTimeAgo(added.TextContent);
                    release.Category = new List<int> { TvCategoryParser.ParseTvShowQuality(release.Description) };
                    release.Link = new Uri(SiteLink + dlLink);
                    release.Comments = new Uri(SiteLink + qDetailsLink.GetAttribute("href"));
                    release.Guid = release.Link;
                    release.Seeders = ParseUtil.CoerceInt(seeders.TextContent);
                    release.Peers = ParseUtil.CoerceInt(leechers.TextContent) + release.Seeders;
                    release.Size = ReleaseInfo.GetBytes(size.TextContent);
                    release.Grabs = ReleaseInfo.GetBytes(grabs.TextContent);
                    release.Files = ReleaseInfo.GetBytes(files.TextContent);
                    release.DownloadVolumeFactor = freeLeech != null ? 0 : 1;
                    release.UploadVolumeFactor = 1;
                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(results.Content, ex);
            }

            return releases;
        }
    }
}
