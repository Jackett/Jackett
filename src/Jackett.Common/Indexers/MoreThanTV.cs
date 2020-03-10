using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    public class MoreThanTV : BaseWebIndexer
    {
        private string LoginUrl => SiteLink + "login.php";
        private string SearchUrl => SiteLink + "ajax.php?action=browse&searchstr=";
        private string DownloadUrl => SiteLink + "torrents.php?action=download&id=";
        private string GuidUrl => SiteLink + "torrents.php?torrentid=";

        private ConfigurationDataBasicLogin ConfigData => (ConfigurationDataBasicLogin)configData;

        public MoreThanTV(IIndexerConfigurationService configService, Utils.Clients.WebClient c, Logger l, IProtectionService ps)
            : base(name: "MoreThanTV",
                description: "Private torrent tracker for TV / MOVIES, and the internal tracker for the release group DRACULA.",
                link: "https://www.morethan.tv/",
                caps: new TorznabCapabilities(
                    TorznabCatType.Movies,
                    TorznabCatType.TV,
                    TorznabCatType.Other),
                configService: configService,
                client: c,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLogin())
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";

            TorznabCaps.SupportsImdbMovieSearch = true;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string> {
                { "username", ConfigData.Username.Value },
                { "password", ConfigData.Password.Value },
                { "login", "Log in" },
                { "keeplogged", "1" }
            };

            var preRequest = await RequestStringWithCookiesAndRetry(LoginUrl, string.Empty);
            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, preRequest.Cookies, true, SearchUrl, SiteLink);

            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("status\":\"success\""), () =>
            {
                if (result.Content.Contains("Your IP address has been banned."))
                    throw new ExceptionWithConfigData("Your IP address has been banned.", ConfigData);

                var parser = new HtmlParser();
                var dom = parser.ParseDocument(result.Content);
                foreach (var element in dom.QuerySelectorAll("#loginform > table"))
                    element.Remove();
                var errorMessage = dom.QuerySelector("#loginform").TextContent.Trim().Replace("\n\t", " ");
                throw new ExceptionWithConfigData(errorMessage, ConfigData);
            });

            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            if (!string.IsNullOrWhiteSpace(query.ImdbID))
                await GetReleases(releases, query, query.GetQueryString());
            else
            {
                var searchQuery = query.GetQueryString();
                searchQuery = searchQuery.Replace("Marvels", "Marvel"); // strip 's for better results
                var newSearchQuery = Regex.Replace(searchQuery, @"(S\d{2})$", "$1*"); // If we're just seaching for a season (no episode) append an * to include all episodes of that season.
                await GetReleases(releases, query, newSearchQuery);

                // Always search for torrent groups (complete seasons) too
                var seasonMatch = new Regex(@".*\s[Ss]{1}\d{2}([Ee]{1}\d{2,3})?$").Match(searchQuery);
                if (seasonMatch.Success)
                {
                    newSearchQuery = Regex.Replace(searchQuery, @"[Ss]{1}\d{2}([Ee]{1}\d{2,3})?", $"Season {query.Season}");
                    await GetReleases(releases, query, newSearchQuery);
                }
            }

            return releases;
        }

        private string GetTorrentSearchUrl(TorznabQuery query, string searchQuery)
        {
            var qc = new NameValueCollection
            {
                { "tags_type", "1" },
                { "order_by", "time" },
                { "order_way", "desc" },
                { "group_results", "1" },
                { "action", "basic" },
                { "searchsubmit", "1" }
            };

            if (!string.IsNullOrWhiteSpace(query.ImdbID))
                qc.Add("description", query.ImdbID);
            else
                qc.Add("searchstr", searchQuery);

            if (query.Categories.Contains(TorznabCatType.Movies.ID))
                qc.Add("filter_cat[1]", "1");
            if (query.Categories.Contains(TorznabCatType.TV.ID))
                qc.Add("filter_cat[2]", "1");
            if (query.Categories.Contains(TorznabCatType.Other.ID))
                qc.Add("filter_cat[3]", "1");

            return SiteLink + "torrents.php?" + qc.GetQueryString();
        }

        private async Task GetReleases(ICollection<ReleaseInfo> releases, TorznabQuery query, string searchQuery)
        {
            var searchUrl = GetTorrentSearchUrl(query, searchQuery);
            var response = await RequestStringWithCookiesAndRetry(searchUrl);
            if (response.IsRedirect)
            {
                // re login
                await ApplyConfiguration(null);
                response = await RequestStringWithCookiesAndRetry(searchUrl);
            }

            try
            {
                var parser = new HtmlParser();
                var document = parser.ParseDocument(response.Content);
                var groups = document.QuerySelectorAll(".torrent_table > tbody > tr.group");
                var torrents = document.QuerySelectorAll(".torrent_table > tbody > tr.torrent");

                // Loop through all torrent (season) groups
                foreach (var group in groups)
                {
                    var showName = group.QuerySelector(".tp-showname a").InnerHtml.Replace("(", "").Replace(")", "").Replace(' ', '.');
                    var season = group.QuerySelector(".big_info a").InnerHtml;
                    var seasonNumber = SeasonToNumber(season);
                    if (seasonNumber != null && query.Season > 0 && seasonNumber != query.Season) // filter unwanted seasons
                        continue;
                    var seasonTag = SeasonNumberToShortSeason(seasonNumber) ?? season;

                    // Loop through all group items
                    var previousElement = group;
                    var qualityEdition = string.Empty;
                    while (true)
                    {
                        var groupItem = previousElement.NextElementSibling;

                        if (groupItem == null)
                            break;

                        if (!groupItem.ClassList[0].Equals("group_torrent") ||
                            !groupItem.ClassList[1].StartsWith("groupid_"))
                            break;

                        // Found a new edition
                        if (groupItem.ClassList[2].Equals("edition"))
                            qualityEdition = groupItem.QuerySelector(".edition_info strong").TextContent.Split('/')[1].Trim();
                        else if (groupItem.ClassList[2].StartsWith("edition_"))
                        {
                            if (qualityEdition.Equals(string.Empty))
                                break;

                            // Parse required data
                            var downloadAnchor = groupItem.QuerySelectorAll("td a").Last();
                            var qualityData = downloadAnchor.InnerHtml.Split('/');

                            switch (qualityData.Length)
                            {
                                case 0:
                                    Array.Resize(ref qualityData, 2);
                                    qualityData[0] = " ";
                                    qualityData[1] = " ";
                                    break;
                                case 1:
                                    Array.Resize(ref qualityData, 2);
                                    qualityData[1] = " ";
                                    break;
                            }

                            // Replace 4K quality tag with 2160p, so Sonarr etc. can properly parse it
                            qualityData[1] = qualityData[1].Replace("4K", "2160p");

                            // Build title
                            var title = string.Join(".", new List<string>
                            {
                                showName,
                                seasonTag,
                                qualityData[1].Trim(),
                                qualityEdition, // Audio quality should be after this one. Unobtainable at the moment.
                                $"{qualityData[0].Trim()}-MTV"
                            });

                            releases.Add(GetReleaseInfo(groupItem, downloadAnchor, title, TorznabCatType.TV.ID));
                        }
                        else
                            break;

                        previousElement = groupItem;
                    }
                }

                // Loop through all torrents
                foreach (var torrent in torrents)
                {
                    // Parse required data
                    var downloadAnchor = torrent.QuerySelector(".big_info > .group_info > a");
                    var title = downloadAnchor.TextContent;

                    int category;
                    var categories = torrent.QuerySelector(".cats_col div").ClassList;
                    if (categories.Contains("cats_tv"))
                        category = TorznabCatType.TV.ID;
                    else if (categories.Contains("cats_movies"))
                        category = TorznabCatType.Movies.ID;
                    else if (categories.Contains("cats_other"))
                        category = TorznabCatType.Other.ID;
                    else
                        throw new Exception("Couldn't find category.");

                    releases.Add(GetReleaseInfo(torrent, downloadAnchor, title, category));
                }
            }
            catch (Exception ex)
            {
                OnParseError(response.Content, ex);
            }
        }

        private ReleaseInfo GetReleaseInfo(IElement row, IElement downloadAnchor, string title, int category)
        {
            var downloadAnchorHref = downloadAnchor.Attributes["href"].Value;
            var torrentId = downloadAnchorHref.Substring(downloadAnchorHref.LastIndexOf('=') + 1);
            if (torrentId.Contains('#'))
                torrentId = torrentId.Split('#')[0];

            var qFiles = row.QuerySelector("td:nth-last-child(6)");
            var files = ParseUtil.CoerceLong(qFiles.TextContent);
            var qPublishDate = row.QuerySelector(".time.tooltip").Attributes["title"].Value;
            var publishDate = DateTime.ParseExact(qPublishDate, "MMM dd yyyy, HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToLocalTime();
            var qBanner = row.QuerySelector("div.tp-banner img")?.GetAttribute("src");
            var banner = (qBanner != null && !qBanner.Contains("/static/styles/")) ? new Uri(qBanner) : null;
            var description = row.QuerySelector("div.tags")?.TextContent.Trim();

            var torrentData = row.QuerySelectorAll(".number_column");
            if (torrentData.Length != 4) // Size (xx.xx GB[ (Max)]) Snatches (xx) Seeders (xx) Leechers (xx)
                throw new Exception($"We expected 4 torrent datas, instead we have {torrentData.Length}.");

            var size = ReleaseInfo.GetBytes(torrentData[0].TextContent);
            var grabs = int.Parse(torrentData[1].TextContent, NumberStyles.AllowThousands, CultureInfo.InvariantCulture);
            var seeders = int.Parse(torrentData[2].TextContent, NumberStyles.AllowThousands, CultureInfo.InvariantCulture);
            var leechers = int.Parse(torrentData[3].TextContent, NumberStyles.AllowThousands, CultureInfo.InvariantCulture);
            var guid = new Uri(GuidUrl + torrentId);

            return new ReleaseInfo
            {
                Title = title,
                Category = new List<int> { category }, // Who seasons movies right
                Link = new Uri(DownloadUrl + torrentId),
                PublishDate = publishDate,
                BannerUrl = banner,
                Description = description,
                Seeders = seeders,
                Peers = seeders + leechers,
                Files = files,
                Size = size,
                Grabs = grabs,
                Guid = guid,
                Comments = guid,
                DownloadVolumeFactor = 0, // ratioless tracker
                UploadVolumeFactor = 1
            };
        }

        // Changes "Season 1" to "1"
        private static int? SeasonToNumber(string season)
        {
            var seasonMatch = new Regex(@"Season (?<seasonNumber>\d{1,2})").Match(season);
            if (seasonMatch.Success)
                return int.Parse(seasonMatch.Groups["seasonNumber"].Value);

            return null;
        }

        // Changes "1" to "S01"
        private static string SeasonNumberToShortSeason(int? season)
        {
            if (season == null)
                return null;
            return $"S{season:00}";
        }
    }
}
