using CsQuery;
using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jackett.Models.IndexerConfig;

using AngleSharp.Parser.Html;

namespace Jackett.Indexers
{
    public class HD4Free : BaseWebIndexer
    {
        private string SearchUrl { get { return SiteLink + "ajax/initial_recall.php"; } }
        private string LoginUrl { get { return SiteLink + "login.php"; } }
        private string TakeLoginUrl { get { return SiteLink + "takelogin.php"; } }

        new ConfigurationDataRecaptchaLogin configData
        {
            get { return (ConfigurationDataRecaptchaLogin)base.configData; }
            set { base.configData = value; }
        }

        public HD4Free(IIndexerConfigurationService configService, IWebClient w, Logger l, IProtectionService ps)
            : base(name: "HD4Free",
                description: "A HD trackers",
                link: "https://hd4free.xyz/",
                caps: new TorznabCapabilities(),
                configService: configService,
                client: w,
                logger: l,
                p: ps,
                configData: new ConfigurationDataRecaptchaLogin())
        {
            Encoding = Encoding.GetEncoding("UTF-8");
            Language = "en-us";
            Type = "private";

            TorznabCaps.SupportsImdbSearch = true;

            AddCategoryMapping(42, TorznabCatType.MoviesSD); // LEGi0N 480p
            AddCategoryMapping(17, TorznabCatType.MoviesHD); // LEGi0N  720p 
            AddCategoryMapping(16, TorznabCatType.MoviesHD); // LEGi0N  1080p 
            AddCategoryMapping(84, TorznabCatType.Movies3D); // LEGi0N 3D 1080p
            AddCategoryMapping(31, TorznabCatType.MoviesOther); // LEGi0N  REMUX
            AddCategoryMapping(70, TorznabCatType.MoviesBluRay); // LEGi0N BD BD25 & BD50
            AddCategoryMapping(55, TorznabCatType.Movies); // LEGi0N  Movie/TV PACKS
            AddCategoryMapping(60, TorznabCatType.Other); // shadz shack
            AddCategoryMapping(85, TorznabCatType.MoviesHD); // MarGe 720p
            AddCategoryMapping(86, TorznabCatType.MoviesHD); // MarGe 1080p
            AddCategoryMapping(73, TorznabCatType.MoviesBluRay); // GF44 BD-50
            AddCategoryMapping(74, TorznabCatType.MoviesBluRay); // GF44 BD-25
            AddCategoryMapping(88, TorznabCatType.MoviesBluRay); // taterzero BD50
            AddCategoryMapping(89, TorznabCatType.MoviesBluRay); // taterzero BD25
            AddCategoryMapping(90, TorznabCatType.Movies3D); // taterzero 3D BD
            AddCategoryMapping(39, TorznabCatType.MoviesBluRay); // Bluray REMUX
            AddCategoryMapping(38, TorznabCatType.MoviesBluRay); // Bluray 
            AddCategoryMapping(75, TorznabCatType.MoviesBluRay); // Bluray 25
            AddCategoryMapping(36, TorznabCatType.MoviesHD); // Encodes 720p
            AddCategoryMapping(35, TorznabCatType.MoviesHD); // Encodes 1080p
            AddCategoryMapping(45, TorznabCatType.Movies3D); // 1080p 3D Encodes
            AddCategoryMapping(77, TorznabCatType.MoviesHD); // WEB-DL 720p
            AddCategoryMapping(78, TorznabCatType.MoviesHD); // WEB-DL 1080p
            AddCategoryMapping(83, TorznabCatType.MoviesDVD); // DVD 5/9's
            AddCategoryMapping(47, TorznabCatType.Movies); // General x264
            AddCategoryMapping(58, TorznabCatType.Movies); // General XViD
            AddCategoryMapping(66, TorznabCatType.Movies); // x265 HEVC
            AddCategoryMapping(34, TorznabCatType.MoviesHD); // 4K
            AddCategoryMapping(61, TorznabCatType.Movies); // MOViE PACKS
            AddCategoryMapping(44, TorznabCatType.TVHD); // HDTV 720p
            AddCategoryMapping(43, TorznabCatType.TVHD); // HDTV 1080p
            AddCategoryMapping(41, TorznabCatType.TVHD); // WEB-DL 720p TV
            AddCategoryMapping(40, TorznabCatType.TVHD); // WEB-DL 1080p TV
            AddCategoryMapping(52, TorznabCatType.TVHD); // 720p TV BluRay
            AddCategoryMapping(53, TorznabCatType.TVHD); // 1080p TV BluRay
            AddCategoryMapping(62, TorznabCatType.TVHD); // HDTV Packs
            AddCategoryMapping(82, TorznabCatType.TVSD); // SDTV TV PACKS
            AddCategoryMapping(63, TorznabCatType.PC0day); // Apps Windows
            AddCategoryMapping(57, TorznabCatType.PCMac); // Appz Mac
            AddCategoryMapping(72, TorznabCatType.AudioAudiobook); // Audio Books
            AddCategoryMapping(71, TorznabCatType.Books); // Ebooks
            AddCategoryMapping(46, TorznabCatType.AudioLossless); // FLAC/Lossless
            AddCategoryMapping(81, TorznabCatType.AudioMP3); // MP3 Music
            AddCategoryMapping(87, TorznabCatType.AudioVideo); // HD MUSiC ViDEOS
            AddCategoryMapping(32, TorznabCatType.Other); // Covers And Artwork
            AddCategoryMapping(50, TorznabCatType.XXX); // Porn XXX
        }

        public override async Task<ConfigurationData> GetConfigurationForSetup()
        {
            var loginPage = await RequestStringWithCookies(LoginUrl, configData.CookieHeader.Value);
            CQ cq = loginPage.Content;
            string recaptchaSiteKey = cq.Find(".g-recaptcha").Attr("data-sitekey");
            var result = this.configData;
            result.CookieHeader.Value = loginPage.Cookies;
            result.Captcha.SiteKey = recaptchaSiteKey;
            result.Captcha.Version = "2";
            return result;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string> {
                { "returnto" , "/" },
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "g-recaptcha-response", configData.Captcha.Value },
                { "submitme", "Login" }
            };

            if (!string.IsNullOrWhiteSpace(configData.Captcha.Cookie))
            {
                // Cookie was manually supplied
                CookieHeader = configData.Captcha.Cookie;
                try
                {
                    var results = await PerformQuery(new TorznabQuery());
                    if (!results.Any())
                    {
                        throw new Exception("Your cookie did not work");
                    }

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

            var result = await RequestLoginAndFollowRedirect(TakeLoginUrl, pairs, null, true, SiteLink, LoginUrl);

            await ConfigureIfOK(result.Cookies, result.Content.Contains("logout.php"), () =>
            {
                CQ dom = result.Content;
                var messageEl = dom["table.main > tbody > tr > td > table > tbody > tr > td"];
                var errorMessage = messageEl.Text().Trim();
                if (string.IsNullOrWhiteSpace(errorMessage))
                    errorMessage = result.Content;
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            var pairs = new Dictionary<string, string>();
            var searchString = query.GetQueryString();
            var searchUrl = SearchUrl;

            pairs.Add("draw", "1");
            pairs.Add("columns[0][data]", "");
            pairs.Add("columns[0][name]", "");
            pairs.Add("columns[0][searchable]", "false");
            pairs.Add("columns[0][orderable]", "false");
            pairs.Add("columns[0][search][value]", "");
            pairs.Add("columns[0][search][regex]", "false");
            pairs.Add("columns[1][data]", "id");
            pairs.Add("columns[1][name]", "");
            pairs.Add("columns[1][searchable]", "false");
            pairs.Add("columns[1][orderable]", "true");
            pairs.Add("columns[1][search][value]", "");
            pairs.Add("columns[1][search][regex]", "false");
            pairs.Add("columns[2][data]", "cat");
            pairs.Add("columns[2][name]", "");
            pairs.Add("columns[2][searchable]", "false");
            pairs.Add("columns[2][orderable]", "true");
            pairs.Add("columns[2][search][value]", "");
            pairs.Add("columns[2][search][regex]", "false");
            pairs.Add("columns[3][data]", "name");
            pairs.Add("columns[3][name]", "");
            pairs.Add("columns[3][searchable]", "true");
            pairs.Add("columns[3][orderable]", "true");
            pairs.Add("columns[3][search][value]", "");
            pairs.Add("columns[3][search][regex]", "false");
            pairs.Add("columns[4][data]", "username");
            pairs.Add("columns[4][name]", "");
            pairs.Add("columns[4][searchable]", "true");
            pairs.Add("columns[4][orderable]", "true");
            pairs.Add("columns[4][search][value]", "");
            pairs.Add("columns[4][search][regex]", "false");
            pairs.Add("columns[5][data]", "cat-image");
            pairs.Add("columns[5][name]", "");
            pairs.Add("columns[5][searchable]", "false");
            pairs.Add("columns[5][orderable]", "true");
            pairs.Add("columns[5][search][value]", "");
            pairs.Add("columns[5][search][regex]", "false");
            pairs.Add("columns[6][data]", "cat-name");
            pairs.Add("columns[6][name]", "");
            pairs.Add("columns[6][searchable]", "false");
            pairs.Add("columns[6][orderable]", "true");
            pairs.Add("columns[6][search][value]", "");
            pairs.Add("columns[6][search][regex]", "false");
            pairs.Add("columns[7][data]", "imdbid");
            pairs.Add("columns[7][name]", "");
            pairs.Add("columns[7][searchable]", "true");
            pairs.Add("columns[7][orderable]", "true");
            pairs.Add("columns[7][search][value]", "");
            pairs.Add("columns[7][search][regex]", "false");
            pairs.Add("columns[8][data]", "genre");
            pairs.Add("columns[8][name]", "");
            pairs.Add("columns[8][searchable]", "false");
            pairs.Add("columns[8][orderable]", "true");
            pairs.Add("columns[8][search][value]", "");
            pairs.Add("columns[8][search][regex]", "false");
            pairs.Add("columns[9][data]", "added");
            pairs.Add("columns[9][name]", "");
            pairs.Add("columns[9][searchable]", "false");
            pairs.Add("columns[9][orderable]", "true");
            pairs.Add("columns[9][search][value]", "");
            pairs.Add("columns[9][search][regex]", "false");
            pairs.Add("columns[10][data]", "size");
            pairs.Add("columns[10][name]", "");
            pairs.Add("columns[10][searchable]", "false");
            pairs.Add("columns[10][orderable]", "true");
            pairs.Add("columns[10][search][value]", "");
            pairs.Add("columns[10][search][regex]", "false");
            pairs.Add("columns[11][data]", "rating");
            pairs.Add("columns[11][name]", "");
            pairs.Add("columns[11][searchable]", "false");
            pairs.Add("columns[11][orderable]", "true");
            pairs.Add("columns[11][search][value]", "");
            pairs.Add("columns[11][search][regex]", "false");
            pairs.Add("columns[12][data]", "comments");
            pairs.Add("columns[12][name]", "");
            pairs.Add("columns[12][searchable]", "false");
            pairs.Add("columns[12][orderable]", "true");
            pairs.Add("columns[12][search][value]", "");
            pairs.Add("columns[12][search][regex]", "false");
            pairs.Add("columns[13][data]", "numfiles");
            pairs.Add("columns[13][name]", "");
            pairs.Add("columns[13][searchable]", "false");
            pairs.Add("columns[13][orderable]", "true");
            pairs.Add("columns[13][search][value]", "");
            pairs.Add("columns[13][search][regex]", "false");
            pairs.Add("columns[14][data]", "seeders");
            pairs.Add("columns[14][name]", "");
            pairs.Add("columns[14][searchable]", "false");
            pairs.Add("columns[14][orderable]", "true");
            pairs.Add("columns[14][search][value]", "");
            pairs.Add("columns[14][search][regex]", "false");
            pairs.Add("columns[15][data]", "leechers");
            pairs.Add("columns[15][name]", "");
            pairs.Add("columns[15][searchable]", "false");
            pairs.Add("columns[15][orderable]", "true");
            pairs.Add("columns[15][search][value]", "");
            pairs.Add("columns[15][search][regex]", "false");
            pairs.Add("columns[16][data]", "to_go");
            pairs.Add("columns[16][name]", "");
            pairs.Add("columns[16][searchable]", "false");
            pairs.Add("columns[16][orderable]", "true");
            pairs.Add("columns[16][search][value]", "");
            pairs.Add("columns[16][search][regex]", "false");
            pairs.Add("columns[17][data]", "genre");
            pairs.Add("columns[17][name]", "");
            pairs.Add("columns[17][searchable]", "true");
            pairs.Add("columns[17][orderable]", "true");
            pairs.Add("columns[17][search][value]", "");
            pairs.Add("columns[17][search][regex]", "false");
            pairs.Add("order[0][column]", "9");
            pairs.Add("order[0][dir]", "desc");
            pairs.Add("start", "0");
            pairs.Add("length", "100");
            pairs.Add("visible", "1");
            pairs.Add("uid", "-1");
            pairs.Add("genre", "");
            
            pairs.Add("cats", string.Join(",+", MapTorznabCapsToTrackers(query)));

            if (query.ImdbID != null)
            {
                pairs.Add("search[value]", query.ImdbID);
                pairs.Add("search[regex]", "false");
            }
            else if (!string.IsNullOrWhiteSpace(searchString))
            {
                pairs.Add("search[value]", searchString);
                pairs.Add("search[regex]", "false");
            }

            var results = await PostDataWithCookiesAndRetry(searchUrl, pairs);

            try
            {
                var json = JObject.Parse(results.Content);
                foreach (var row in json["data"])
                {
                    var release = new ReleaseInfo();
                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 72 * 24 * 60 * 60;

                    var hParser = new HtmlParser();
                    var hName = hParser.Parse(row["name"].ToString());
                    var hComments = hParser.Parse(row["comments"].ToString());
                    var hNumfiles = hParser.Parse(row["numfiles"].ToString());
                    var hSeeders = hParser.Parse(row["seeders"].ToString());
                    var hLeechers = hParser.Parse(row["leechers"].ToString());

                    var hDetailsLink = hName.QuerySelector("a[href^=\"details.php?id=\"]");
                    var hCommentsLink = hComments.QuerySelector("a");
                    var hDownloadLink = hName.QuerySelector("a[title=\"Download Torrent\"]");

                    release.Title = hDetailsLink.TextContent;
                    if (query.ImdbID == null && !query.MatchQueryStringAND(release.Title))
                        continue;

                    release.Comments = new Uri(SiteLink + hCommentsLink.GetAttribute("href"));
                    release.Link = new Uri(SiteLink + hDownloadLink.GetAttribute("href"));
                    release.Guid = release.Link;

                    release.Description = row["genre"].ToString();

                    var poster = row["poster"].ToString();
                    if(!string.IsNullOrWhiteSpace(poster))
                    {
                        var posterurl = poster;
                        if (!poster.StartsWith("http"))
                            posterurl = SiteLink + poster;
                        release.BannerUrl = new Uri(posterurl);
                    }

                    release.Size = ReleaseInfo.GetBytes(row["size"].ToString());
                    var imdbId = row["imdbid"].ToString();
                    if (imdbId.StartsWith("tt"))
                        release.Imdb = ParseUtil.CoerceLong(imdbId.Substring(2));

                    var added = row["added"].ToString().Replace("<br>", " ");
                    release.PublishDate = DateTimeUtil.FromUnknown(added);

                    var catid = row["catid"].ToString();
                    release.Category = MapTrackerCatToNewznab(catid);

                    release.Seeders = ParseUtil.CoerceInt(hSeeders.QuerySelector("a").TextContent);
                    release.Peers = ParseUtil.CoerceInt(hLeechers.QuerySelector("a").TextContent) + release.Seeders;

                    release.Files = ParseUtil.CoerceInt(hNumfiles.QuerySelector("a").TextContent);

                    release.DownloadVolumeFactor = 1;
                    release.UploadVolumeFactor = 1;

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