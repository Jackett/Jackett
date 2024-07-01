using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class BitHDTV : IndexerBase
    {
        public override string Id => "bithdtv";
        public override string Name => "BIT-HDTV";
        public override string Description => "BIT-HDTV - Home of High Definition";
        public override string SiteLink { get; protected set; } = "https://www.bit-hdtv.com/";
        public override Encoding Encoding => Encoding.GetEncoding("iso-8859-1");
        public override string Language => "en-US";
        public override string Type => "private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private string SearchUrl => SiteLink + "torrents.php";

        private new ConfigurationDataCookie configData => (ConfigurationDataCookie)base.configData;

        public BitHDTV(IIndexerConfigurationService configService, WebClient w, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(configService: configService,
                   client: w,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataCookie("For best results, change the 'Torrents per page' setting to 100 in your profile."))
        {
            configData.AddDynamic("freeleech", new BoolConfigurationItem("Search freeleech only") { Value = false });
            configData.AddDynamic("Account Inactivity", new DisplayInfoConfigurationItem("Account Inactivity", "Inactive accounts are disabled after 90 days for User class, after 180 days for Power User class, after 270 days for Elite User & Insane User class. This doesn't apply to Veteran User class or above. Parking your account doubles the maximum inactive time. Only the login and browsing the site is considered activity."));
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId, TvSearchParam.Genre
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q, MovieSearchParam.ImdbId, MovieSearchParam.Genre
                }
            };

            caps.Categories.AddCategoryMapping(6, TorznabCatType.AudioLossless, "HQ Audio");
            caps.Categories.AddCategoryMapping(7, TorznabCatType.Movies, "Movies");
            caps.Categories.AddCategoryMapping(8, TorznabCatType.AudioVideo, "Music Videos");
            caps.Categories.AddCategoryMapping(9, TorznabCatType.Other, "Other");
            caps.Categories.AddCategoryMapping(10, TorznabCatType.TV, "TV");
            caps.Categories.AddCategoryMapping(12, TorznabCatType.TV, "TV/Seasonpack");
            caps.Categories.AddCategoryMapping(11, TorznabCatType.XXX, "XXX");

            return caps;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            CookieHeader = configData.Cookie.Value;
            try
            {
                var results = await PerformQuery(new TorznabQuery());
                if (!results.Any())
                    throw new Exception("Found 0 results in the tracker");

                IsConfigured = true;
                SaveConfig();
                return IndexerConfigurationStatus.Completed;
            }
            catch (Exception e)
            {
                IsConfigured = false;
                throw new Exception("Your cookie did not work: " + e.Message);
            }
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var qc = new NameValueCollection
            {
                {"cat", MapTorznabCapsToTrackers(query, true).FirstIfSingleOrDefault("0")}
            };

            // free=4 green:  (DL won't be counted, and UL will be counted double.)
            // free=3 grey:   (DL will be counted as normal, and UL will be counted double.)
            // free=2 yellow: (DL won't be counted, and UL will be counted as normal.)
            // free=1 normal: (DL and UL counted as normal.)
            // free=0 (any)
            if (((BoolConfigurationItem)configData.GetDynamic("freeleech")).Value)
                qc.Add("free", "2");

            var results = new List<WebResult>();
            var search = new UriBuilder(SearchUrl);
            if (query.IsGenreQuery)
            {
                qc.Add("search", query.GetQueryString() + " " + query.Genre);
                qc.Add("options", "2"); //Search Title and Genre
                search.Query = qc.GetQueryString();
                results.Add(await RequestWithCookiesAndRetryAsync(search.ToString()));
            }
            else if (query.IsImdbQuery)
            {
                qc.Add("search", query.ImdbID);
                qc.Add("options", "4"); //Search URL field for IMDB link
                search.Query = qc.GetQueryString();
                results.Add(await RequestWithCookiesAndRetryAsync(search.ToString()));
                qc["Options"] = "1"; //Search Title and Description
                search.Query = qc.GetQueryString();
                results.Add(await RequestWithCookiesAndRetryAsync(search.ToString()));
            }
            else
            {
                //Site handles empty string on search param. No need to check for IsNullOrEmpty()
                qc.Add("search", query.GetQueryString());
                qc.Add("options", "0"); //Search Title Only
                search.Query = qc.GetQueryString();
                results.Add(await RequestWithCookiesAndRetryAsync(search.ToString()));
            }

            var parser = new HtmlParser();
            foreach (var result in results)
                try
                {
                    using var dom = parser.ParseDocument(result.ContentString);

                    var tableBody = dom.QuerySelector("#torrents-index-table > #torrents-index-table-body");
                    if (tableBody == null) // No results, so skip this search
                        continue;

                    foreach (var row in tableBody.Children)
                    {
                        var release = new ReleaseInfo();
                        var qLink = row.Children[2].QuerySelector("a");
                        release.MinimumRatio = 1;
                        release.MinimumSeedTime = 172800; // 48 hours
                        release.Title = qLink.GetAttribute("title").Replace('.', ' ');
                        var detailsLink = new Uri(qLink.GetAttribute("href"));
                        //Skip irrelevant and duplicate entries
                        if (!query.MatchQueryStringAND(release.Title) || releases.Any(r => r.Guid == detailsLink))
                            continue;

                        var genres = row.QuerySelector("font.small")?.TextContent;
                        if (!string.IsNullOrEmpty(genres))
                        {
                            genres = genres.Replace("[ ", "").Replace(" ]", "").Replace(" / ", ",").Replace(" | ", ",");
                            release.Description = genres;
                            release.Genres ??= new List<string>();
                            release.Genres = release.Genres.Union(genres.Split(',')).ToList();
                        }
                        release.Files = ParseUtil.CoerceLong(row.Children[3].TextContent);
                        release.Grabs = ParseUtil.CoerceLong(row.Children[7].TextContent);
                        release.Guid = detailsLink;
                        release.Details = release.Guid;
                        release.Link = new Uri(SiteLink + row.QuerySelector("a[href^=\"download.php\"]").GetAttribute("href"));
                        var catUrl = new Uri(SiteLink + row.Children[1].FirstElementChild.GetAttribute("href"));
                        var catQuery = HttpUtility.ParseQueryString(catUrl.Query);
                        var catNum = catQuery["cat"];
                        release.Category = MapTrackerCatToNewznab(catNum);

                        var dateString = row.Children[5].TextContent.Trim();
                        var pubDate = DateTime.ParseExact(dateString, "yyyy-MM-ddHH:mm:ss", CultureInfo.InvariantCulture);
                        release.PublishDate = DateTime.SpecifyKind(pubDate, DateTimeKind.Local);
                        var sizeStr = row.Children[6].TextContent;
                        release.Size = ParseUtil.GetBytes(sizeStr);
                        release.Seeders = ParseUtil.CoerceInt(row.Children[8].TextContent.Trim());
                        release.Peers = ParseUtil.CoerceInt(row.Children[9].TextContent.Trim()) + release.Seeders;
                        switch (row.GetAttribute("bgcolor"))
                        {
                            case "#DDDDDD": // grey
                                release.DownloadVolumeFactor = 1;
                                release.UploadVolumeFactor = 2;
                                break;
                            case "#FFFF99": // yellow
                                release.DownloadVolumeFactor = 0;
                                release.UploadVolumeFactor = 1;
                                break;
                            case "#CCFF99": // green
                                release.DownloadVolumeFactor = 0;
                                release.UploadVolumeFactor = 2;
                                break;
                            default: // normal
                                release.DownloadVolumeFactor = 1;
                                release.UploadVolumeFactor = 1;
                                break;
                        }

                        releases.Add(release);
                    }
                }
                catch (Exception ex)
                {
                    OnParseError(result.ContentString, ex);
                }

            return releases;
        }
    }
}
