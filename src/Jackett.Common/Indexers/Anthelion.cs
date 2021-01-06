using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
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
    // missing features. https://github.com/Jackett/Jackett/issues/7923
    [ExcludeFromCodeCoverage]
    public class Anthelion : BaseWebIndexer
    {
        private string LoginUrl => SiteLink + "login.php";
        private string BrowseUrl => SiteLink + "torrents.php";

        private new ConfigurationDataBasicLogin configData => (ConfigurationDataBasicLogin)base.configData;

        public override string[] LegacySiteLinks { get; protected set; } = {
            "https://tehconnection.me/"
        };

        public Anthelion(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "anthelion",
                   name: "Anthelion", // old name: TehConnection.me
                   description: "A movies tracker",
                   link: "https://anthelion.me/",
                   caps: new TorznabCapabilities {
                       TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                       },
                       MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q
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

            AddCategoryMapping("1", TorznabCatType.Movies, "Film/Feature");
            AddCategoryMapping("2", TorznabCatType.Movies, "Film/Short");
            AddCategoryMapping("3", TorznabCatType.TV, "TV/Miniseries");
            AddCategoryMapping("4", TorznabCatType.Other, "Other");
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

            // TODO: IMDB search is available but it requires to parse the details page
            var qc = new NameValueCollection
            {
                { "order_by", "time" },
                { "order_way", "desc" },
                { "action", "basic" },
                { "searchsubmit", "1" },
                { "searchstr", query.IsImdbQuery ? query.ImdbID : query.GetQueryString() }
            };

            var catList = MapTorznabCapsToTrackers(query);
            foreach (var cat in catList)
                qc.Add($"filter_cat[{cat}]", "1");

            var searchUrl = BrowseUrl + "?" + qc.GetQueryString();
            var results = await RequestWithCookiesAsync(searchUrl);
            try
            {
                var parser = new HtmlParser();
                var doc = parser.ParseDocument(results.ContentString);
                var rows = doc.QuerySelectorAll("table.torrent_table > tbody > tr.torrent");
                foreach (var row in rows)
                {
                    var qDetailsLink = row.QuerySelector("a.torrent_name");
                    var year = qDetailsLink.NextSibling.TextContent.Replace("[", "").Replace("]", "").Trim();
                    var tags = row.QuerySelector("div.torrent_info").FirstChild.TextContent.Replace(" / ", " ").Trim();
                    var title = $"{qDetailsLink.TextContent} {year} {tags}";
                    var description = row.QuerySelector("div.tags").TextContent.Trim();
                    var details = new Uri(SiteLink + qDetailsLink.GetAttribute("href"));
                    var torrentId = qDetailsLink.GetAttribute("href").Split('=').Last();
                    var link = new Uri(SiteLink + "torrents.php?action=download&id=" + torrentId);
                    var posterStr = qDetailsLink.GetAttribute("data-cover");
                    var poster = !string.IsNullOrWhiteSpace(posterStr) ? new Uri(qDetailsLink.GetAttribute("data-cover")) : null;

                    var files = ParseUtil.CoerceLong(row.QuerySelector("td:nth-child(3)").TextContent);
                    var publishDate = DateTimeUtil.FromTimeAgo(row.QuerySelector("td:nth-child(4)").TextContent);
                    var size = ReleaseInfo.GetBytes(row.QuerySelector("td:nth-child(5)").FirstChild.TextContent);
                    var grabs = ParseUtil.CoerceLong(row.QuerySelector("td:nth-child(6)").TextContent);
                    var seeders = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(7)").TextContent);
                    var leechers = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(8)").TextContent);

                    var dlVolumeFactor = row.QuerySelector("strong.tl_free") != null ? 0 : 1;

                    var cat = row.QuerySelector("td.cats_col > div").GetAttribute("class").Replace("tooltip cats_", "");
                    var category = new List<int> {
                        cat switch
                        {
                            "featurefilm" => TorznabCatType.Movies.ID,
                            "shortfilm" => TorznabCatType.Movies.ID,
                            "miniseries" => TorznabCatType.TV.ID,
                            "other" => TorznabCatType.Other.ID,
                            _ => throw new Exception($"Unknown category: {cat}")
                        }
                    };

                    // TODO: TMDb is also available
                    var qImdb = row.QuerySelector("a[href^=\"https://www.imdb.com\"]");
                    var imdb = qImdb != null ? ParseUtil.GetImdbID(qImdb.GetAttribute("href").Split('/').Last()) : null;

                    var release = new ReleaseInfo
                    {
                        MinimumRatio = 1,
                        MinimumSeedTime = 259200,
                        Description = description,
                        Title = title,
                        PublishDate = publishDate,
                        Category = category,
                        Link = link,
                        Details = details,
                        Guid = link,
                        Imdb = imdb,
                        Poster = poster,
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
    }
}
