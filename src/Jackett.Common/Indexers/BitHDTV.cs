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

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class BitHDTV : BaseWebIndexer
    {
        private string SearchUrl => SiteLink + "torrents.php";

        private new ConfigurationDataCookie configData => (ConfigurationDataCookie)base.configData;

        public BitHDTV(IIndexerConfigurationService configService, WebClient w, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "bithdtv",
                   name: "BIT-HDTV",
                   description: "BIT-HDTV - Home of High Definition",
                   link: "https://www.bit-hdtv.com/",
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
                   client: w,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataCookie("For best results, change the 'Torrents per page' setting to 100 in your profile."))
        {
            Encoding = Encoding.GetEncoding("iso-8859-1");
            Language = "en-us";
            Type = "private";

            AddCategoryMapping(1, TorznabCatType.TVAnime, "Anime");
            AddCategoryMapping(2, TorznabCatType.MoviesBluRay, "Movies/Blu-ray");
            AddCategoryMapping(4, TorznabCatType.TVDocumentary, "Documentaries");
            AddCategoryMapping(6, TorznabCatType.AudioLossless, "HQ Audio");
            AddCategoryMapping(7, TorznabCatType.Movies, "Movies");
            AddCategoryMapping(8, TorznabCatType.AudioVideo, "Music Videos");
            AddCategoryMapping(9, TorznabCatType.Other, "Other");
            AddCategoryMapping(5, TorznabCatType.TVSport, "Sports");
            AddCategoryMapping(10, TorznabCatType.TV, "TV");
            AddCategoryMapping(12, TorznabCatType.TV, "TV/Seasonpack");
            AddCategoryMapping(11, TorznabCatType.XXX, "XXX");
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
            var results = new List<WebResult>();
            var search = new UriBuilder(SearchUrl);
            if (query.IsImdbQuery)
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
                    var dom = parser.ParseDocument(result.ContentString);
                    foreach (var child in dom.QuerySelectorAll("#needseed"))
                        child.Remove();
                    var table = dom.QuerySelector("table[align=center] + br + table > tbody");
                    if (table == null) // No results, so skip this search
                        continue;
                    foreach (var row in table.Children.Skip(1))
                    {
                        var release = new ReleaseInfo();
                        var qLink = row.Children[2].QuerySelector("a");
                        release.MinimumRatio = 1;
                        release.MinimumSeedTime = 172800; // 48 hours
                        release.Title = qLink.GetAttribute("title");
                        var detailsLink = new Uri(qLink.GetAttribute("href"));
                        //Skip irrelevant and duplicate entries
                        if (!query.MatchQueryStringAND(release.Title) || releases.Any(r => r.Guid == detailsLink))
                            continue;
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
                        release.Size = ReleaseInfo.GetBytes(sizeStr);
                        release.Seeders = ParseUtil.CoerceInt(row.Children[8].TextContent.Trim());
                        release.Peers = ParseUtil.CoerceInt(row.Children[9].TextContent.Trim()) + release.Seeders;
                        switch (row.GetAttribute("bgcolor"))
                        {
                            case "#DDDDDD":
                                release.DownloadVolumeFactor = 1;
                                release.UploadVolumeFactor = 2;
                                break;
                            case "#FFFF99":
                                release.DownloadVolumeFactor = 0;
                                release.UploadVolumeFactor = 1;
                                break;
                            case "#CCFF99":
                                release.DownloadVolumeFactor = 0;
                                release.UploadVolumeFactor = 2;
                                break;
                            default:
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
