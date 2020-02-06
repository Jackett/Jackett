using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CsQuery;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    // To comply with the rules for this tracker, only the acronym is used and no publicly displayed URLs to the site.

    public class BB : BaseWebIndexer
    {
        private string BaseUrl => StringUtil.FromBase64("aHR0cHM6Ly9iYWNvbmJpdHMub3JnLw==");
        private Uri BaseUri => new Uri(BaseUrl);
        private string LoginUrl => $"{BaseUri}login.php";

        private string SearchUrl =>
            $"{BaseUri}torrents.php?searchtags=&tags_type=0&order_by=s3&order_way=desc&disablegrouping=1&";

        private new ConfigurationDataBasicLogin configData
        {
            get => (ConfigurationDataBasicLogin)base.configData;
            set => base.configData = value;
        }

        public BB(IIndexerConfigurationService configService, WebClient w, Logger l, IProtectionService ps) : base(
            "bB", description: "BaconBits (bB) is a Private Torrent Tracker for 0DAY / GENERAL",
            link: StringUtil.FromBase64("aHR0cHM6Ly9iYWNvbmJpdHMub3JnLw=="), caps: new TorznabCapabilities(),
            configService: configService, client: w, logger: l, p: ps, configData: new ConfigurationDataBasicLogin())
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";
            AddCategoryMapping(1, TorznabCatType.Audio);
            AddCategoryMapping(1, TorznabCatType.AudioMP3);
            AddCategoryMapping(1, TorznabCatType.AudioLossless);
            AddCategoryMapping(2, TorznabCatType.PC);
            AddCategoryMapping(3, TorznabCatType.BooksEbook);
            AddCategoryMapping(4, TorznabCatType.AudioAudiobook);
            AddCategoryMapping(7, TorznabCatType.BooksComics);
            AddCategoryMapping(8, TorznabCatType.TVAnime);
            AddCategoryMapping(9, TorznabCatType.Movies);
            AddCategoryMapping(10, TorznabCatType.TVHD);
            AddCategoryMapping(10, TorznabCatType.TVSD);
            AddCategoryMapping(10, TorznabCatType.TV);
            AddCategoryMapping(11, TorznabCatType.PCGames);
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string>
            {
                {"username", configData.Username.Value},
                {"password", configData.Password.Value},
                {"keeplogged", "1"},
                {"login", "Log In!"}
            };
            var response = await RequestLoginAndFollowRedirectAsync(LoginUrl, pairs, null, true, null, SiteLink);
            await ConfigureIfOkAsync(
                response.Cookies, response.Content?.Contains("logout.php") == true, () =>
                {
                    CQ dom = response.Content;
                    var messageEl = dom["#loginform"];
                    var messages = new List<string>();
                    for (var i = 0; i < 13; i++)
                    {
                        var child = messageEl[0].ChildNodes[i];
                        messages.Add(child.Cq().Text().Trim());
                    }

                    var message = string.Join(" ", messages);
                    throw new ExceptionWithConfigData(message, configData);
                });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchStrings = new List<string>(
                new[]
                {
                    query.GetQueryString()
                });
            if (string.IsNullOrEmpty(query.Episode) && (query.Season > 0))
                // Tracker naming rules: If query is for a whole season, "Season #" instead of "S##".
                searchStrings.Add((query.SanitizedSearchTerm + " " + string.Format("\"Season {0}\"", query.Season)).Trim());
            var categories = MapTorznabCapsToTrackers(query);
            var requestUrls = new List<string>();
            foreach (var searchString in searchStrings)
            {
                var queryCollection = new NameValueCollection { { "action", "basic" } };
                if (!string.IsNullOrWhiteSpace(searchString))
                    queryCollection.Add("searchstr", searchString);
                foreach (var cat in categories)
                    queryCollection.Add($"filter_cat[{cat}]", "1");
                requestUrls.Add(SearchUrl + queryCollection.GetQueryString());
            }

            var downloadTasksQuery = from url in requestUrls select RequestStringWithCookiesAndRetryAsync(url);
            var responses = await Task.WhenAll(downloadTasksQuery.ToArray());
            for (var i = 0; i < searchStrings.Count(); i++)
            {
                var results = responses[i];
                // Occasionally the cookies become invalid, login again if that happens
                if (results.IsRedirect)
                {
                    await ApplyConfiguration(null);
                    results = await RequestStringWithCookiesAndRetryAsync(requestUrls[i]);
                }

                try
                {
                    CQ dom = results.Content;
                    var rows = dom["#torrent_table > tbody > tr.torrent"];
                    foreach (var row in rows)
                    {
                        var qRow = row.Cq();
                        var release = new ReleaseInfo
                        {
                            MinimumRatio = 1,
                            MinimumSeedTime = 172800 // 48 hours
                        };
                        var catStr = row.ChildElements.ElementAt(0).FirstElementChild.GetAttribute("href")
                                        .Split('[', ']')[1];
                        release.Category = MapTrackerCatToNewznab(catStr);
                        var qLink = row.ChildElements.ElementAt(1).Cq().Children("a")[0].Cq();
                        var linkStr = qLink.Attr("href");
                        release.Comments = new Uri($"{BaseUrl}/{linkStr}");
                        release.Guid = release.Comments;
                        var qDownload = row.ChildElements.ElementAt(1).Cq().Find("a[title='Download']")[0].Cq();
                        release.Link = new Uri($"{BaseUrl}/{qDownload.Attr("href")}");
                        var dateStr = row.ChildElements.ElementAt(3).Cq().Text().Trim().Replace(" and", "");
                        release.PublishDate = DateTimeUtil.FromTimeAgo(dateStr);
                        var sizeStr = row.ChildElements.ElementAt(4).Cq().Text();
                        release.Size = ReleaseInfo.GetBytes(sizeStr);
                        release.Files = ParseUtil.CoerceInt(row.ChildElements.ElementAt(2).Cq().Text().Trim());
                        release.Grabs = ParseUtil.CoerceInt(row.ChildElements.ElementAt(6).Cq().Text().Trim());
                        release.Seeders = ParseUtil.CoerceInt(row.ChildElements.ElementAt(7).Cq().Text().Trim());
                        release.Peers = ParseUtil.CoerceInt(row.ChildElements.ElementAt(8).Cq().Text().Trim()) +
                                        release.Seeders;
                        var grabs = qRow.Find("td:nth-child(6)").Text();
                        release.Grabs = ParseUtil.CoerceInt(grabs);
                        release.DownloadVolumeFactor = qRow.Find("strong:contains(\"Freeleech!\")").Length >= 1 ? 0 : 1;
                        release.UploadVolumeFactor = 1;
                        var title = qRow.Find("td:nth-child(2)");
                        title.Find("span, strong, div, br").Remove();
                        release.Title = ParseUtil.NormalizeMultiSpaces(title.Text().Replace(" - ]", "]"));
                        if (catStr == "10") //change "Season #" to "S##" for TV shows
                            release.Title = Regex.Replace(
                                release.Title, @"Season (\d+)", m => string.Format("S{0:00}", int.Parse(m.Groups[1].Value)));
                        releases.Add(release);
                    }
                }
                catch (Exception ex)
                {
                    OnParseError(results.Content, ex);
                }
            }

            return releases;
        }
    }
}
