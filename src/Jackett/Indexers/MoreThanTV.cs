using Jackett.Models;
using Jackett.Services;
using Jackett.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using AngleSharp.Dom;
using AngleSharp.Parser.Html;
using CsQuery;
using Jackett.Models.IndexerConfig;
using Jackett.Utils;

namespace Jackett.Indexers
{
    public class MoreThanTV : BaseIndexer, IIndexer
    {
        private string LoginUrl => SiteLink + "login.php";
        private string SearchUrl => SiteLink + "ajax.php?action=browse&searchstr=";
        private string DownloadUrl => SiteLink + "torrents.php?action=download&id=";
        private string GuidUrl => SiteLink + "torrents.php?torrentid=";

        private ConfigurationDataBasicLogin ConfigData => (ConfigurationDataBasicLogin) configData;

        public MoreThanTV(IIndexerManagerService i, IWebClient c, Logger l, IProtectionService ps)
            : base(name: "MoreThanTV",
                description: "ROMANIAN Private Torrent Tracker for TV / MOVIES, and the internal tracker for the release group DRACULA.",
                link: "https://www.morethan.tv/",
                caps: new TorznabCapabilities(TorznabCatType.TV,
                                              TorznabCatType.Movies),
                manager: i,
                client: c,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLogin())
        {
        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            ConfigData.LoadValuesFromJson(configJson);
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
                CQ dom = result.Content;
                dom["#loginform > table"].Remove();
                var errorMessage = dom["#loginform"].Text().Trim().Replace("\n\t", " ");
                throw new ExceptionWithConfigData(errorMessage, ConfigData);
            });

            return IndexerConfigurationStatus.RequiresTesting;
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var isTv = Array.IndexOf(query.Categories, TorznabCatType.TV.ID) > -1;
            var releases = new List<ReleaseInfo>();
            var searchQuery = query.GetQueryString();

            await GetReleases(releases, query, searchQuery);

            // Search for torrent groups
            if (isTv)
            {
                var seasonMatch = new Regex(@".*\s[Ss]{1}\d{2}").Match(query.GetQueryString());
                if (seasonMatch.Success)
                {
                    var newSearchQuery = Regex.Replace(searchQuery, @"[Ss]{1}\d{2}", $"Season {query.Season}");

                    await GetReleases(releases, query, newSearchQuery);
                }
            }

            return releases;
        }

        private string GetTorrentSearchUrl(int[] categories, string searchQuery)
        {
            var extra = "";

            if (Array.IndexOf(categories, TorznabCatType.Movies.ID) > -1)
                extra += "&filter_cat%5B1%5D=1";

            if (Array.IndexOf(categories, TorznabCatType.TV.ID) > -1)
                extra += "&filter_cat%5B2%5D=1";

            return SiteLink + $"torrents.php?searchstr={HttpUtility.UrlEncode(searchQuery)}&tags_type=1&order_by=time&order_way=desc&group_results=1{extra}&action=basic&searchsubmit=1";
        }

        private async Task GetReleases(ICollection<ReleaseInfo> releases, TorznabQuery query, string searchQuery)
        {
            var searchUrl = GetTorrentSearchUrl(query.Categories, searchQuery);
            var response = await RequestStringWithCookiesAndRetry(searchUrl);

            try
            {
                var parser = new HtmlParser();
                var document = parser.Parse(response.Content);
                var groups = document.QuerySelectorAll(".torrent_table > tbody > tr.group");
                var torrents = document.QuerySelectorAll(".torrent_table > tbody > tr.torrent");

                // Loop through all torrent (season) groups
                foreach (var group in groups)
                {
                    var showName = group.QuerySelector(".tp-showname a").InnerHtml.Replace("(", "").Replace(")", "").Replace(' ', '.');
                    var season = group.QuerySelector(".big_info a").InnerHtml;

                    // Loop through all group items
                    var previousElement = group;
                    var qualityEdition = string.Empty;
                    while (true)
                    {
                        var groupItem = previousElement.NextElementSibling;

                        if (groupItem == null) break;

                        if (!groupItem.ClassList[0].Equals("group_torrent") ||
                            !groupItem.ClassList[1].StartsWith("groupid_")) break;

                        // Found a new edition
                        if (groupItem.ClassList[2].Equals("edition"))
                        {
                            qualityEdition = groupItem.QuerySelector(".edition_info strong").TextContent.Split('/')[1].Trim();
                        }
                        else if (groupItem.ClassList[2].StartsWith("edition_"))
                        {
                            if (qualityEdition.Equals(string.Empty)) break;

                            // Parse required data
                            var downloadAnchor = groupItem.QuerySelectorAll("td a").Last();
                            var qualityData = downloadAnchor.InnerHtml.Split('/');

                            if (qualityData.Length < 2)
                                throw new Exception($"We expected 2 or more quality datas, instead we have {qualityData.Length}.");

                            // Build title
                            var title = string.Join(".", new List<string>
                            {
                                showName,
                                SeasonToShortSeason(season),
                                qualityData[1].Trim(),
                                qualityEdition, // Audio quality should be after this one. Unobtainable at the moment.
                                $"{qualityData[0].Trim()}-MTV"
                            });

                            releases.Add(GetReleaseInfo(groupItem, downloadAnchor, title, TorznabCatType.TV.ID));
                        }
                        else
                        {
                            break;
                        }

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
                    {
                        category = TorznabCatType.TV.ID;
                    }
                    else if (categories.Contains("cats_movies"))
                    {
                        category = TorznabCatType.Movies.ID;
                    }
                    else
                    {
                        throw new Exception("Couldn't find category.");
                    }

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
            // Parse required data
            var downloadAnchorHref = downloadAnchor.Attributes["href"].Value;
            var torrentId = downloadAnchorHref.Substring(downloadAnchorHref.LastIndexOf('=') + 1);
            var qFiles = row.QuerySelector("td:nth-last-child(6)");
            var files = ParseUtil.CoerceLong(qFiles.TextContent);
            var publishDate = DateTime.ParseExact(row.QuerySelector(".time.tooltip").Attributes["title"].Value, "MMM dd yyyy, HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToLocalTime();
            var torrentData = row.QuerySelectorAll(".number_column"); // Size (xx.xx GB[ (Max)]) Snatches (xx) Seeders (xx) Leechers (xx)

            if (torrentData.Length != 4)
                throw new Exception($"We expected 4 torrent datas, instead we have {torrentData.Length}.");

            if (torrentId.Contains('#'))
                torrentId = torrentId.Split('#')[0];

            var size = ParseSizeToBytes(torrentData[0].TextContent);
            var grabs = int.Parse(torrentData[1].TextContent);
            var seeders = int.Parse(torrentData[2].TextContent);
            var guid = new Uri(GuidUrl + torrentId);

            // Build releaseinfo
            return new ReleaseInfo
            {
                Title = title,
                Description = title,
                Category = category, // Who seasons movies right
                Link = new Uri(DownloadUrl + torrentId),
                PublishDate = publishDate,
                Seeders = seeders,
                Peers = seeders,
                Files = files,
                Size = size,
                Grabs = grabs,
                Guid = guid,
                Comments = guid,
                DownloadVolumeFactor = 0, // ratioless tracker
                UploadVolumeFactor = 1
            };
        }

        // Changes "Season 1" to "S01"
        private static string SeasonToShortSeason(string season)
        {
            var seasonMatch = new Regex(@"Season (?<seasonNumber>\d{1,2})").Match(season);
            if (seasonMatch.Success)
            {
                season = $"S{int.Parse(seasonMatch.Groups["seasonNumber"].Value):00}";
            }

            return season;
        }

        // Changes "xx.xx GB/MB" to bytes
        private static long ParseSizeToBytes(string strSize)
        {
            var sizeParts = strSize.Split(' ');
            if (sizeParts.Length != 2)
                throw new Exception($"We expected 2 size parts, instead we have {sizeParts.Length}.");

            var size = double.Parse(sizeParts[0]);

            switch (sizeParts[1].Trim())
            {
                case "GB":
                    size = size*1000*1000*1000;
                    break;
                case "MB":
                    size = size*1000*1000;
                    break;
                default:
                    throw new Exception($"Unknown size type {sizeParts[1].Trim()}.");
            }

            return (long) Math.Ceiling(size);
        }

    }
}
