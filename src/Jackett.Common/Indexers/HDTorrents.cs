using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Text;
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
    public class HDTorrents : BaseWebIndexer
    {
        private string SearchUrl { get { return SiteLink + "torrents.php?"; } }
        private string LoginUrl { get { return SiteLink + "login.php"; } }
        private const int MAXPAGES = 3;
        public override string[] AlternativeSiteLinks { get; protected set; } = new string[] { "https://hdts.ru/", "https://hd-torrents.org/", "https://hd-torrents.net/", "https://hd-torrents.me/" };

        private new ConfigurationDataBasicLogin configData
        {
            get { return (ConfigurationDataBasicLogin)base.configData; }
            set { base.configData = value; }
        }

        public HDTorrents(IIndexerConfigurationService configService, WebClient w, Logger l, IProtectionService ps)
            : base(name: "HD-Torrents",
                description: "HD-Torrents is a private torrent website with HD torrents and strict rules on their content.",
                link: "https://hdts.ru/",// Of the accessible domains the .ru seems the most reliable.  https://hdts.ru | https://hd-torrents.org | https://hd-torrents.net | https://hd-torrents.me
                configService: configService,
                client: w,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLogin())
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";

            TorznabCaps.SupportsImdbSearch = true;

            TorznabCaps.Categories.Clear();

            // Movie
            AddCategoryMapping("70", TorznabCatType.MoviesUHD, "Movie/UHD/Blu-Ray");
            AddCategoryMapping("1", TorznabCatType.MoviesHD, "Movie/Blu-Ray");
            AddCategoryMapping("71", TorznabCatType.MoviesUHD, "Movie/UHD/Remux");
            AddCategoryMapping("2", TorznabCatType.MoviesHD, "Movie/Remux");
            AddCategoryMapping("5", TorznabCatType.MoviesHD, "Movie/1080p/i");
            AddCategoryMapping("3", TorznabCatType.MoviesHD, "Movie/720p");
            AddCategoryMapping("64", TorznabCatType.MoviesUHD, "Movie/2160p");
            AddCategoryMapping("63", TorznabCatType.Audio, "Movie/Audio Track");
            // TV Show

            AddCategoryMapping("72", TorznabCatType.TVUHD, "TV Show/UHD/Blu-ray");
            AddCategoryMapping("59", TorznabCatType.TVHD, "TV Show/Blu-ray");
            AddCategoryMapping("73", TorznabCatType.TVUHD, "TV Show/UHD/Remux");
            AddCategoryMapping("60", TorznabCatType.TVHD, "TV Show/Remux");
            AddCategoryMapping("30", TorznabCatType.TVHD, "TV Show/1080p/i");
            AddCategoryMapping("38", TorznabCatType.TVHD, "TV Show/720p");
            AddCategoryMapping("65", TorznabCatType.TVUHD, "TV Show/2160p");
            // Music
            AddCategoryMapping("44", TorznabCatType.Audio, "Music/Album");
            AddCategoryMapping("61", TorznabCatType.AudioVideo, "Music/Blu-Ray");
            AddCategoryMapping("62", TorznabCatType.AudioVideo, "Music/Remux");
            AddCategoryMapping("57", TorznabCatType.AudioVideo, "Music/1080p/i");
            AddCategoryMapping("45", TorznabCatType.AudioVideo, "Music/720p");
            AddCategoryMapping("66", TorznabCatType.AudioVideo, "Music/2160p");
            // XXX
            AddCategoryMapping("58", TorznabCatType.XXX, "XXX/Blu-ray");
            AddCategoryMapping("74", TorznabCatType.XXX, "XXX/UHD/Blu-ray");
            AddCategoryMapping("48", TorznabCatType.XXX, "XXX/1080p/i");
            AddCategoryMapping("47", TorznabCatType.XXX, "XXX/720p");
            AddCategoryMapping("67", TorznabCatType.XXX, "XXX/2160p");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var loginPage = await RequestStringWithCookies(LoginUrl, string.Empty);

            var pairs = new Dictionary<string, string> {
                { "uid", configData.Username.Value },
                { "pwd", configData.Password.Value }
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, loginPage.Cookies, true, null, LoginUrl);

            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("If your browser doesn't have javascript enabled"), () =>
            {
                var errorMessage = "Couldn't login";
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchurls = new List<string>();
            var searchUrl = SearchUrl;// string.Format(SearchUrl, HttpUtility.UrlEncode()));
            var queryCollection = new NameValueCollection();
            var searchString = query.GetQueryString();

            foreach (var cat in MapTorznabCapsToTrackers(query))
            {
                searchUrl += "category%5B%5D=" + cat + "&";
            }

            if (query.ImdbID != null)
            {
                queryCollection.Add("search", query.ImdbID);
            }
            else if (!string.IsNullOrWhiteSpace(searchString))
            {
                queryCollection.Add("search", searchString);
            }

            queryCollection.Add("active", "0");
            queryCollection.Add("options", "0");

            searchUrl += queryCollection.GetQueryString().Replace("(", "%28").Replace(")", "%29"); // maually url encode brackets to prevent "hacking" detection

            var results = await RequestStringWithCookiesAndRetry(searchUrl);
            try
            {
                CQ dom = results.Content;
                ReleaseInfo release;

                var rows = dom[".mainblockcontenttt > tbody > tr:has(a[href^=\"details.php?id=\"])"];
                foreach (var row in rows)
                {
                    CQ qRow = row.Cq();

                    release = new ReleaseInfo();

                    release.Title = qRow.Find("td.mainblockcontent b a").Text();
                    release.Description = qRow.Find("td:nth-child(3) > span").Text();

                    if (0 != qRow.Find("td.mainblockcontent u").Length)
                    {
                        var imdbStr = qRow.Find("td.mainblockcontent u").Parent().First().Attr("href").Replace("http://www.imdb.com/title/tt", "").Replace("/", "");
                        long imdb;
                        if (ParseUtil.TryCoerceLong(imdbStr, out imdb))
                        {
                            release.Imdb = imdb;
                        }
                    }

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;

                    // Sometimes the uploader column is missing
                    int seeders, peers;
                    if (ParseUtil.TryCoerceInt(qRow.Find("td:nth-last-child(3)").Text(), out seeders))
                    {
                        release.Seeders = seeders;
                        if (ParseUtil.TryCoerceInt(qRow.Find("td:nth-last-child(2)").Text(), out peers))
                        {
                            release.Peers = peers + release.Seeders;
                        }
                    }

                    release.Grabs = ParseUtil.CoerceLong(qRow.Find("td:nth-last-child(1)").Text());

                    string fullSize = qRow.Find("td.mainblockcontent").Get(6).InnerText;
                    release.Size = ReleaseInfo.GetBytes(fullSize);

                    release.Guid = new Uri(SiteLink + qRow.Find("td.mainblockcontent b a").Attr("href"));
                    release.Link = new Uri(SiteLink + qRow.Find("td.mainblockcontent").Get(3).FirstChild.GetAttribute("href"));
                    release.Comments = new Uri(SiteLink + qRow.Find("td.mainblockcontent b a").Attr("href"));

                    string[] dateSplit = qRow.Find("td.mainblockcontent").Get(5).InnerHTML.Split(',');
                    string dateString = dateSplit[1].Substring(0, dateSplit[1].IndexOf('>')).Trim();
                    release.PublishDate = DateTime.ParseExact(dateString, "dd MMM yyyy HH:mm:ss zz00", CultureInfo.InvariantCulture).ToLocalTime();

                    string category = qRow.Find("td:eq(0) a").Attr("href").Replace("torrents.php?category=", "");
                    release.Category = MapTrackerCatToNewznab(category);

                    release.UploadVolumeFactor = 1;

                    if (qRow.Find("img[alt=\"Free Torrent\"]").Length >= 1)
                    {
                        release.DownloadVolumeFactor = 0;
                        release.UploadVolumeFactor = 0;
                    }
                    else if (qRow.Find("img[alt=\"Silver Torrent\"]").Length >= 1)
                        release.DownloadVolumeFactor = 0.5;
                    else if (qRow.Find("img[alt=\"Bronze Torrent\"]").Length >= 1)
                        release.DownloadVolumeFactor = 0.75;
                    else if (qRow.Find("img[alt=\"Blue Torrent\"]").Length >= 1)
                        release.DownloadVolumeFactor = 0.25;
                    else
                        release.DownloadVolumeFactor = 1;

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
