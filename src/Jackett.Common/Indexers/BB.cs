using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
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
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    // To comply with the rules for this tracker, only the acronym is used and no publicly displayed URLs to the site.
    [ExcludeFromCodeCoverage]
    public class BB : BaseWebIndexer
    {
        private string BaseUrl => StringUtil.FromBase64("aHR0cHM6Ly9iYWNvbmJpdHMub3JnLw==");
        private Uri BaseUri => new Uri(BaseUrl);
        private string LoginUrl => BaseUri + "login.php";
        private string SearchUrl => BaseUri + "torrents.php?searchtags=&tags_type=0&order_by=s3&order_way=desc&disablegrouping=1&";

        private new ConfigurationDataBasicLogin configData
        {
            get => (ConfigurationDataBasicLogin)base.configData;
            set => base.configData = value;
        }

        public BB(IIndexerConfigurationService configService, WebClient w, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "bb",
                   name: "bB",
                   description: "bB is a Private Torrent Tracker for 0DAY / GENERAL",
                   link: StringUtil.FromBase64("aHR0cHM6Ly9iYWNvbmJpdHMub3JnLw=="),
                   caps: new TorznabCapabilities
                   {
                       TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                       },
                       MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q
                       },
                       MusicSearchParams = new List<MusicSearchParam>
                       {
                           MusicSearchParam.Q
                       },
                       BookSearchParams = new List<BookSearchParam>
                       {
                           BookSearchParam.Q
                       }
                   },
                   configService: configService,
                   client: w,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataBasicLogin())
        {
            Encoding = Encoding.UTF8;
            Language = "en-US";
            Type = "private";

            AddCategoryMapping(1, TorznabCatType.Audio);
            AddCategoryMapping(1, TorznabCatType.AudioMP3);
            AddCategoryMapping(1, TorznabCatType.AudioLossless);
            AddCategoryMapping(2, TorznabCatType.PC);
            AddCategoryMapping(3, TorznabCatType.BooksEBook);
            AddCategoryMapping(4, TorznabCatType.AudioAudiobook);
            AddCategoryMapping(5, TorznabCatType.Other);
            AddCategoryMapping(6, TorznabCatType.BooksMags);
            AddCategoryMapping(7, TorznabCatType.BooksComics);
            AddCategoryMapping(8, TorznabCatType.TVAnime);
            AddCategoryMapping(9, TorznabCatType.Movies);
            AddCategoryMapping(10, TorznabCatType.TVHD);
            AddCategoryMapping(10, TorznabCatType.TVSD);
            AddCategoryMapping(10, TorznabCatType.TV);
            AddCategoryMapping(11, TorznabCatType.PCGames);
            AddCategoryMapping(12, TorznabCatType.Console);
            AddCategoryMapping(13, TorznabCatType.Other);
            AddCategoryMapping(14, TorznabCatType.Other);
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "keeplogged", "1" },
                { "login", "Log In!" }
            };

            var response = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, SiteLink);
            await ConfigureIfOK(response.Cookies, response.ContentString != null && response.ContentString.Contains("logout.php"), () =>
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(response.ContentString);
                var messageEl = dom.QuerySelectorAll("#loginform");
                var messages = new List<string>();
                for (var i = 0; i < 13; i++)
                {
                    var child = messageEl[0].ChildNodes[i];
                    messages.Add(child.Text().Trim());
                }
                var message = string.Join(" ", messages);
                throw new ExceptionWithConfigData(message, configData);
            });

            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchStrings = new List<string>(new[] { query.GetQueryString() });

            if (string.IsNullOrEmpty(query.Episode) && (query.Season > 0))
                // Tracker naming rules: If query is for a whole season, "Season #" instead of "S##".
                searchStrings.Add((query.SanitizedSearchTerm + " " + string.Format("\"Season {0}\"", query.Season)).Trim());

            var categories = MapTorznabCapsToTrackers(query);
            var request_urls = new List<string>();

            foreach (var searchString in searchStrings)
            {
                var queryCollection = new NameValueCollection
                {
                    { "action", "basic" }
                };

                if (!string.IsNullOrWhiteSpace(searchString))
                {
                    queryCollection.Add("searchstr", searchString);
                }

                foreach (var cat in categories)
                {
                    queryCollection.Add("filter_cat[" + cat + "]", "1");
                }

                // remove . as not used in titles 
                request_urls.Add(SearchUrl + queryCollection.GetQueryString().Replace(".", " "));
            }

            var downloadTasksQuery = from url in request_urls select RequestWithCookiesAndRetryAsync(url);

            var responses = await Task.WhenAll(downloadTasksQuery.ToArray());

            for (var i = 0; i < searchStrings.Count(); i++)
            {
                var results = responses[i];
                // Occasionally the cookies become invalid, login again if that happens
                if (results.IsRedirect)
                {
                    await ApplyConfiguration(null);
                    results = await RequestWithCookiesAndRetryAsync(request_urls[i]);
                }
                try
                {
                    var parser = new HtmlParser();
                    var dom = parser.ParseDocument(results.ContentString);
                    var rows = dom.QuerySelectorAll("#torrent_table > tbody > tr.torrent");
                    foreach (var row in rows)
                    {
                        var release = new ReleaseInfo
                        {
                            MinimumRatio = 1,
                            MinimumSeedTime = 172800 // 48 hours
                        };

                        var catStr = row.Children[0].FirstElementChild.GetAttribute("href").Split(new[] { '[', ']' })[1];
                        release.Category = MapTrackerCatToNewznab(catStr);

                        var qDetails = row.Children[1].QuerySelector("a[title='View Torrent']");
                        release.Details = new Uri(BaseUri + qDetails.GetAttribute("href"));
                        release.Guid = release.Details;

                        var qDownload = row.Children[1].QuerySelector("a[title='Download']");
                        release.Link = new Uri(BaseUri + qDownload.GetAttribute("href"));

                        var dateStr = row.Children[3].TextContent.Trim().Replace(" and", "");
                        release.PublishDate = DateTimeUtil.FromTimeAgo(dateStr);

                        var sizeStr = row.Children[4].TextContent;
                        release.Size = ReleaseInfo.GetBytes(sizeStr);

                        release.Files = ParseUtil.CoerceInt(row.Children[2].TextContent.Trim());
                        release.Seeders = ParseUtil.CoerceInt(row.Children[7].TextContent.Trim());
                        release.Peers = ParseUtil.CoerceInt(row.Children[8].TextContent.Trim()) + release.Seeders;

                        var grabs = row.QuerySelector("td:nth-child(6)").TextContent;
                        release.Grabs = ParseUtil.CoerceInt(grabs);

                        if (row.QuerySelector("strong:contains(\"Freeleech!\")") != null)
                            release.DownloadVolumeFactor = 0;
                        else
                            release.DownloadVolumeFactor = 1;

                        release.UploadVolumeFactor = 1;

                        var title = row.QuerySelector("td:nth-child(2)");
                        foreach (var element in title.QuerySelectorAll("span, strong, div, br"))
                            element.Remove();

                        release.Title = ParseUtil.NormalizeMultiSpaces(title.TextContent.Replace(" - ]", "]"));

                        if (catStr == "10") //change "Season #" to "S##" for TV shows
                            release.Title = Regex.Replace(release.Title, @"Season (\d+)",
                                                          m => string.Format("S{0:00}", int.Parse(m.Groups[1].Value)));

                        releases.Add(release);
                    }
                }
                catch (Exception ex)
                {
                    OnParseError(results.ContentString, ex);
                }
            }
            return releases;
        }
    }
}
