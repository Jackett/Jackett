using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
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
    [ExcludeFromCodeCoverage]
    public class TVVault : BaseWebIndexer
    {
        private string LoginUrl => SiteLink + "login.php";
        private string BrowseUrl => SiteLink + "torrents.php";

        private new ConfigurationDataBasicLoginWithRSSAndDisplay configData
        {
            get => (ConfigurationDataBasicLoginWithRSSAndDisplay)base.configData;
            set => base.configData = value;
        }

        public TVVault(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base(name: "TV-Vault",
                   description: "A TV tracker for old shows.",
                   link: "https://tv-vault.me/",
                   caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
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
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "keeplogged", "1" },
                { "login", "Log+In!" }
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, LoginUrl, true);
            await ConfigureIfOK(result.Cookies, result.Content?.Contains("logout.php") == true,
                                () => throw new ExceptionWithConfigData(result.Content, configData));
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
                { "searchstr", StripSearchString(searchString) },
                { "order_by", "s3" },
                { "order_way", "desc" },
                { "disablegrouping", "1" }
            };

            searchUrl += "?" + queryCollection.GetQueryString();

            var results = await RequestStringWithCookies(searchUrl);
            try
            {
                var RowsSelector = "table.torrent_table > tbody > tr.torrent";

                var SearchResultParser = new HtmlParser();
                var SearchResultDocument = SearchResultParser.ParseDocument(results.Content);
                var Rows = SearchResultDocument.QuerySelectorAll(RowsSelector);
                foreach (var Row in Rows)
                {
                    var qDetailsLink = Row.QuerySelector("a[href^=\"torrents.php?id=\"]");
                    var DescStr = qDetailsLink.NextSibling;
                    var Files = Row.QuerySelector("td:nth-child(3)");
                    var Added = Row.QuerySelector("td:nth-child(4)");
                    var Size = Row.QuerySelector("td:nth-child(5)").FirstChild;
                    var Grabs = Row.QuerySelector("td:nth-child(6)");
                    var Seeders = Row.QuerySelector("td:nth-child(7)");
                    var Leechers = Row.QuerySelector("td:nth-child(8)");
                    var FreeLeech = Row.QuerySelector("strong.freeleech_normal");

                    var TorrentIdParts = qDetailsLink.GetAttribute("href").Split('=');
                    var TorrentId = TorrentIdParts[TorrentIdParts.Length - 1];
                    var DLLink = "torrents.php?action=download&id=" + TorrentId.ToString();
                    var link = new Uri(SiteLink + DLLink);
                    var seeders = ParseUtil.CoerceInt(Seeders.TextContent);
                    var description = DescStr.TextContent.Trim();
                    var publishDate = DateTimeUtil.FromTimeAgo(Added.TextContent);
                    var comments = new Uri(SiteLink + qDetailsLink.GetAttribute("href"));
                    var leechers = ParseUtil.CoerceInt(Leechers.TextContent);
                    var size = ReleaseInfo.GetBytes(Size.TextContent);
                    var grabs = ParseUtil.CoerceLong(Grabs.TextContent);
                    var files = ParseUtil.CoerceLong(Files.TextContent);
                    var category = new List<int> { TvCategoryParser.ParseTvShowQuality(description) };
                    var release = new ReleaseInfo
                    {
                        MinimumRatio = 1,
                        MinimumSeedTime = 0,
                        Description = description,
                        Title = qDetailsLink.TextContent + " " + description,
                        PublishDate = publishDate,
                        Category = category,
                        Link = link,
                        Comments = comments,
                        Guid = link,
                        Seeders = seeders,
                        Peers = leechers + seeders,
                        Size = size,
                        Grabs = grabs,
                        Files = files,
                        DownloadVolumeFactor = FreeLeech != null ? 0 : 1,
                        UploadVolumeFactor = 1
                    };
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
