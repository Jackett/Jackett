using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
    // This tracker is based on GazelleTracker but we can't use the API/abstract because there are some
    // missing features. https://github.com/Jackett/Jackett/issues/8508
    [ExcludeFromCodeCoverage]
    public class TVVault : BaseWebIndexer
    {
        private string LoginUrl => SiteLink + "login.php";
        private string BrowseUrl => SiteLink + "torrents.php";

        private new ConfigurationDataBasicLogin configData => (ConfigurationDataBasicLogin)base.configData;

        public TVVault(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "tvvault",
                   name: "TV-Vault",
                   description: "A TV tracker for old shows",
                   link: "https://tv-vault.me/",
                   caps: new TorznabCapabilities
                   {
                       TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId
                       },
                       MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q, MovieSearchParam.ImdbId
                       }
                   },
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataBasicLogin())
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
            await ConfigureIfOK(result.Cookies, result.ContentString?.Contains("logout.php") == true, () =>
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(result.ContentString);
                var errorMessage = dom.QuerySelector("form#loginform").TextContent.Trim();
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var qc = new NameValueCollection
            {
                { "order_by", "s3" },
                { "order_way", "desc" },
                { "disablegrouping", "1" }
            };

            if (query.IsImdbQuery)
            {
                qc.Add("action", "advanced");
                qc.Add("imdbid", query.ImdbID);
            }
            else
                qc.Add("searchstr", StripSearchString(query.GetQueryString()));

            var searchUrl = BrowseUrl + "?" + qc.GetQueryString();
            var results = await RequestWithCookiesAsync(searchUrl);
            try
            {
                var seasonRegEx = new Regex(@$"Season\s+0*{query.Season}[^\d]", RegexOptions.IgnoreCase);

                var parser = new HtmlParser();
                var doc = parser.ParseDocument(results.ContentString);
                var rows = doc.QuerySelectorAll("table.torrent_table > tbody > tr.torrent");
                foreach (var row in rows)
                {
                    var qDetailsLink = row.QuerySelector("a[href^=\"torrents.php?id=\"]");
                    var title = qDetailsLink.TextContent;
                    // if it's a season search, we filter results. the trailing space is to match regex
                    if (query.Season > 0 && !seasonRegEx.Match($"{title} ").Success)
                        continue;

                    var description = qDetailsLink.NextSibling.TextContent.Trim();
                    title += " " + description;
                    var details = new Uri(SiteLink + qDetailsLink.GetAttribute("href"));
                    var torrentId = qDetailsLink.GetAttribute("href").Split('=').Last();
                    var link = new Uri(SiteLink + "torrents.php?action=download&id=" + torrentId);

                    var files = ParseUtil.CoerceLong(row.QuerySelector("td:nth-child(3)").TextContent);
                    var publishDate = DateTimeUtil.FromTimeAgo(row.QuerySelector("td:nth-child(4)").TextContent);
                    var size = ReleaseInfo.GetBytes(row.QuerySelector("td:nth-child(5)").FirstChild.TextContent);
                    var grabs = ParseUtil.CoerceLong(row.QuerySelector("td:nth-child(6)").TextContent);
                    var seeders = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(7)").TextContent);
                    var leechers = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(8)").TextContent);

                    var dlVolumeFactor = row.QuerySelector("strong.freeleech_normal") != null ? 0 : 1;

                    var category = new List<int> { TvCategoryParser.ParseTvShowQuality(description) };

                    var release = new ReleaseInfo
                    {
                        MinimumRatio = 1,
                        MinimumSeedTime = 0,
                        Description = description,
                        Title = title,
                        PublishDate = publishDate,
                        Category = category,
                        Link = link,
                        Details = details,
                        Guid = link,
                        Seeders = seeders,
                        Peers = leechers + seeders,
                        Size = size,
                        Grabs = grabs,
                        Files = files,
                        DownloadVolumeFactor = dlVolumeFactor,
                        UploadVolumeFactor = 1
                    };
                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(results.ContentString, ex);
            }

            return releases;
        }

        private string StripSearchString(string term)
        {
            // Search does not support searching with episode numbers so strip it if we have one
            // Ww AND filter the result later to archive the proper result
            term = Regex.Replace(term, @"[S|E]\d\d", string.Empty);
            return term.Trim();
        }

    }
}
