using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers.Abstract
{
    public abstract class AvistazTracker : BaseWebIndexer
    {
        private string LoginUrl => SiteLink + "auth/login";
        private string SearchUrl => SiteLink + "torrents?";
        private string IMDBSearch => SiteLink + "ajax/movies/3?term=";

        private new ConfigurationDataBasicLogin configData
        {
            get => (ConfigurationDataBasicLogin)base.configData;
            set => base.configData = value;
        }

        // hook to adjust the search term
        protected string GetSearchTerm(TorznabQuery query) => $"{query.SearchTerm} {query.GetEpisodeSearchString()}";

        public AvistazTracker(IIndexerConfigurationService configService, Utils.Clients.WebClient webClient, Logger logger, IProtectionService protectionService, string name, string desc, string link)
            : base(name: name,
                description: desc,
                link: link,
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                configService: configService,
                client: webClient,
                logger: logger,
                p: protectionService,
                configData: new ConfigurationDataBasicLogin())
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";

            TorznabCaps.SupportsImdbMovieSearch = true;

            AddCategoryMapping(1, TorznabCatType.Movies);
            AddCategoryMapping(1, TorznabCatType.MoviesForeign);
            AddCategoryMapping(1, TorznabCatType.MoviesHD);
            AddCategoryMapping(1, TorznabCatType.MoviesSD);
            AddCategoryMapping(2, TorznabCatType.TV);
            AddCategoryMapping(3, TorznabCatType.Audio);
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var loginPage = await RequestStringWithCookies(LoginUrl, string.Empty);
            var token = new Regex("<meta name=\"_token\" content=\"(.*?)\">").Match(loginPage.Content).Groups[1].ToString();
            var pairs = new Dictionary<string, string> {
                { "_token", token },
                { "email_username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "remember", "1" }
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, loginPage.Cookies, true, null, LoginUrl);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("auth/logout"), () =>
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(result.Content);
                var messageEl = dom.QuerySelector(".form-error");
                var errorMessage = messageEl.Text().Trim();
                throw new ExceptionWithConfigData(errorMessage, configData);
            });

            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var categoryMapping = MapTorznabCapsToTrackers(query).Distinct().ToList();
            var qc = new NameValueCollection
            {
                {"in", "1"},
                {"type", categoryMapping.Any() ? categoryMapping.First() : "0"} // type=0 => all categories
            };

            // imdb search
            if (!string.IsNullOrWhiteSpace(query.ImdbID))
            {
                var movieId = await GetMovieId(query.ImdbID);
                if (movieId == null)
                    return releases; // movie not found or service broken => return 0 results
                qc.Add("movie_id", movieId);
            }
            else
                qc.Add("search", GetSearchTerm(query));

            var episodeSearchUrl = SearchUrl + qc.GetQueryString();
            var response = await RequestStringWithCookiesAndRetry(episodeSearchUrl);
            if (response.IsRedirect)
            {
                // re-login
                await ApplyConfiguration(null);
                response = await RequestStringWithCookiesAndRetry(episodeSearchUrl);
            }

            try
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(response.Content);
                var rows = dom.QuerySelectorAll("table:has(thead) > tbody > tr");
                foreach (var row in rows)
                {
                    var release = new ReleaseInfo
                    {
                        MinimumRatio = 1,
                        MinimumSeedTime = 172800 // 48 hours
                    };

                    var qLink = row.QuerySelector("a.torrent-filename");
                    release.Title = qLink.Text().Trim();
                    release.Comments = new Uri(qLink.GetAttribute("href"));
                    release.Guid = release.Comments;

                    var qDownload = row.QuerySelector("a.torrent-download-icon");
                    release.Link = new Uri(qDownload.GetAttribute("href"));

                    var qBanner = row.QuerySelector("img.img-tor-poster")?.GetAttribute("data-poster-mid");
                    if (qBanner != null)
                        release.BannerUrl = new Uri(qBanner);

                    var dateStr = row.QuerySelector("td:nth-of-type(4) > span").Text().Trim();
                    release.PublishDate = DateTimeUtil.FromTimeAgo(dateStr);

                    var sizeStr = row.QuerySelector("td:nth-of-type(6) > span").Text().Trim();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    release.Seeders = ParseUtil.CoerceInt(row.QuerySelector("td:nth-of-type(7)").Text().Trim());
                    release.Peers = ParseUtil.CoerceInt(row.QuerySelector("td:nth-of-type(8)").Text().Trim()) + release.Seeders;

                    var cat = row.QuerySelectorAll("td:nth-of-type(1) i").First().GetAttribute("class")
                                            .Replace("torrent-icon", string.Empty)
                                            .Replace("fa fa-", string.Empty)
                                            .Replace("film", "1")
                                            .Replace("tv", "2")
                                            .Replace("music", "3")
                                            .Replace("text-pink", string.Empty);
                    release.Category = MapTrackerCatToNewznab(cat.Trim());

                    var grabs =row.QuerySelector("td:nth-child(9)").Text();
                    release.Grabs = ParseUtil.CoerceInt(grabs);

                    if (row.QuerySelectorAll("i.fa-star").Any())
                        release.DownloadVolumeFactor = 0;
                    else if (row.QuerySelectorAll("i.fa-star-half-o").Any())
                        release.DownloadVolumeFactor = 0.5;
                    else
                        release.DownloadVolumeFactor = 1;

                    if (row.QuerySelectorAll("i.fa-diamond").Any())
                        release.UploadVolumeFactor = 2;
                    else
                        release.UploadVolumeFactor = 1;

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(response.Content, ex);
            }
            return releases;
        }

        private async Task<string> GetMovieId(string imdbId)
        {
            try
            {
                var imdbUrl = IMDBSearch + imdbId;
                var imdbHeaders = new Dictionary<string, string> { {"X-Requested-With", "XMLHttpRequest"} };
                var imdbResponse = await RequestStringWithCookiesAndRetry(imdbUrl, null, null, imdbHeaders);
                if (imdbResponse.IsRedirect)
                {
                    // re-login
                    await ApplyConfiguration(null);
                    imdbResponse = await RequestStringWithCookiesAndRetry(imdbUrl, null, null, imdbHeaders);
                }
                var json = JsonConvert.DeserializeObject<dynamic>(imdbResponse.Content);
                return (string)((JArray)json["data"])[0]["id"];
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
