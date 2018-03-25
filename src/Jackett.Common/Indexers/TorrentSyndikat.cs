using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
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
    public class TorrentSyndikat : BaseWebIndexer
    {
        private string SearchUrl { get { return SiteLink + "browse.php"; } }
        private string LoginUrl { get { return SiteLink + "eing2.php"; } }
        private string CaptchaUrl { get { return SiteLink + "simpleCaptcha.php?numImages=1"; } }
        private TimeZoneInfo germanyTz;

        private new ConfigurationDataBasicLoginWithRSSAndDisplay configData
        {
            get { return (ConfigurationDataBasicLoginWithRSSAndDisplay)base.configData; }
            set { base.configData = value; }
        }

        public TorrentSyndikat(IIndexerConfigurationService configService, WebClient w, Logger l, IProtectionService ps)
            : base(name: "Torrent-Syndikat",
                description: "A German general tracker",
                link: "https://torrent-syndikat.org/",
                caps: new TorznabCapabilities(),
                configService: configService,
                client: w,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLoginWithRSSAndDisplay())
        {
            Encoding = Encoding.UTF8;
            Language = "de-de";
            Type = "private";

            TorznabCaps.SupportsImdbSearch = true;

            this.configData.DisplayText.Value = "Only the results from the first search result page are shown, adjust your profile settings to show the maximum.";
            this.configData.DisplayText.Name = "Notice";

            AddCategoryMapping(2, TorznabCatType.PC, "Apps / Windows");
            AddCategoryMapping(13, TorznabCatType.PC, "Apps / Linux");
            AddCategoryMapping(4, TorznabCatType.PCMac, "Apps / Mac");
            AddCategoryMapping(6, TorznabCatType.PC, "Apps / Misc");

            AddCategoryMapping(12, TorznabCatType.PCGames, "Spiele / PC");
            AddCategoryMapping(8, TorznabCatType.ConsolePSP, "Spiele / PSX/PSP");
            AddCategoryMapping(7, TorznabCatType.ConsoleWii, "Spiele / Wii");
            AddCategoryMapping(32, TorznabCatType.ConsoleXbox, "Spiele / XBOX");
            AddCategoryMapping(41, TorznabCatType.ConsoleOther, "Spiele / Misc");

            AddCategoryMapping(22, TorznabCatType.Movies3D, "Filme / 3D");
            AddCategoryMapping(3, TorznabCatType.MoviesBluRay, "Filme / BluRay");
            AddCategoryMapping(11, TorznabCatType.MoviesOther, "Filme / REMUX");
            AddCategoryMapping(42, TorznabCatType.MoviesUHD, "Filme / 2160p");
            AddCategoryMapping(9, TorznabCatType.MoviesHD, "Filme / 1080p");
            AddCategoryMapping(20, TorznabCatType.MoviesHD, "Filme / 720p");
            AddCategoryMapping(21, TorznabCatType.MoviesDVD, "Filme / DVD");
            AddCategoryMapping(10, TorznabCatType.MoviesSD, "Filme / SD");
            AddCategoryMapping(31, TorznabCatType.MoviesOther, "Filme / Anime");

            AddCategoryMapping(43, TorznabCatType.TVUHD, "TV / Serien/UHD");
            AddCategoryMapping(16, TorznabCatType.TVHD, "TV / Serien/HD");
            AddCategoryMapping(15, TorznabCatType.TVSD, "TV / Serien/SD");
            AddCategoryMapping(44, TorznabCatType.TVUHD, "TV / Packs/UHD");
            AddCategoryMapping(23, TorznabCatType.TVHD, "TV / Packs/HD");
            AddCategoryMapping(27, TorznabCatType.TVSD, "TV / Packs/SD");
            AddCategoryMapping(28, TorznabCatType.TVDocumentary, "TV / Dokus/SD");
            AddCategoryMapping(29, TorznabCatType.TVDocumentary, "TV / Dokus/HD");
            AddCategoryMapping(30, TorznabCatType.TVSport, "TV / Sport");
            AddCategoryMapping(40, TorznabCatType.TVAnime, "TV / Anime");

            AddCategoryMapping(24, TorznabCatType.AudioLossless, "Audio / FLAC");
            AddCategoryMapping(25, TorznabCatType.AudioMP3, "Audio / MP3");
            AddCategoryMapping(35, TorznabCatType.AudioOther, "Audio / Other");
            AddCategoryMapping(26, TorznabCatType.Audio, "Audio / Packs");
            AddCategoryMapping(18, TorznabCatType.AudioAudiobook, "Audio / aBooks");
            AddCategoryMapping(33, TorznabCatType.AudioVideo, "Audio / Videos");

            AddCategoryMapping(17, TorznabCatType.Books, "Misc / eBooks");
            AddCategoryMapping(5, TorznabCatType.PCPhoneOther, "Misc / Mobile");
            AddCategoryMapping(39, TorznabCatType.Other, "Misc / Bildung");

            AddCategoryMapping(36, TorznabCatType.TVFOREIGN, "Englisch / TV");
            AddCategoryMapping(37, TorznabCatType.MoviesForeign, "Englisch / Filme");
            AddCategoryMapping(47, TorznabCatType.Books, "Englisch / eBooks");
            AddCategoryMapping(48, TorznabCatType.Other, "Englisch / Bildung");
            AddCategoryMapping(49, TorznabCatType.TVSport, "Englisch / Sport");

            TimeZoneInfo.TransitionTime startTransition = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 3, 0, 0), 3, 5, DayOfWeek.Sunday);
            TimeZoneInfo.TransitionTime endTransition = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 4, 0, 0), 10, 5, DayOfWeek.Sunday);
            TimeSpan delta = new TimeSpan(1, 0, 0);
            TimeZoneInfo.AdjustmentRule adjustment = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(1999, 10, 1), DateTime.MaxValue.Date, delta, startTransition, endTransition);
            TimeZoneInfo.AdjustmentRule[] adjustments = { adjustment };
            germanyTz = TimeZoneInfo.CreateCustomTimeZone("W. Europe Standard Time", new TimeSpan(1, 0, 0), "(GMT+01:00) W. Europe Standard Time", "W. Europe Standard Time", "W. Europe DST Time", adjustments);
    }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            CookieHeader = "";

            var result1 = await RequestStringWithCookies(CaptchaUrl);
            var json1 = JObject.Parse(result1.Content);
            var captchaSelection = json1["images"][0]["hash"];

            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "captchaSelection", (string)captchaSelection },
                { "submitme", "X" }
            };

            var result2 = await RequestLoginAndFollowRedirect(LoginUrl, pairs, result1.Cookies, true, null, null, true);

            await ConfigureIfOK(result2.Cookies, result2.Content.Contains("/logout.php"), () =>
            {
                var errorMessage = result2.Content;
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            var searchString = query.GetQueryString();
            var searchUrl = SearchUrl;
            var queryCollection = new NameValueCollection();

            queryCollection.Add("incldead", "1");
            queryCollection.Add("rel_type", "0"); // Alle

            if(query.ImdbID != null)
            {
                queryCollection.Add("searchin", "imdb");
                queryCollection.Add("search", query.ImdbID);
            } else {
                queryCollection.Add("searchin", "title");
                
                if (!string.IsNullOrWhiteSpace(searchString))
                {
                    // use AND+wildcard operator to avoid getting to many useless results
                    var searchStringArray = Regex.Split(searchString.Trim(), "[ _.-]+", RegexOptions.Compiled).ToList();
                    searchStringArray = searchStringArray.Where(x => x.Length >= 3).ToList(); //  remove words with less than 3 characters
                    searchStringArray = searchStringArray.Where(x => !new string[] { "der", "die", "das", "the" }.Contains(x.ToLower())).ToList(); //  remove words with less than 3 characters
                    searchStringArray = searchStringArray.Select(x => "+" + x + "*").ToList(); // add AND operators+wildcards
                    var searchStringFinal = String.Join(" ", searchStringArray);
                    queryCollection.Add("search", searchStringFinal);
            }

            }
            foreach (var cat in MapTorznabCapsToTrackers(query))
            {
                queryCollection.Add("c" + cat, "1");
            }

            searchUrl += "?" + queryCollection.GetQueryString();

            var results = await RequestStringWithCookiesAndRetry(searchUrl);

            if (results.IsRedirect)
            {
                await ApplyConfiguration(null);
                results = await RequestStringWithCookiesAndRetry(searchUrl);
            }

            try
            {
                CQ dom = results.Content;
                var rows = dom["table.torrent_table > tbody > tr"];
                var globalFreeleech = dom.Find("legend:contains(\"Freeleech\")+ul > li > b:contains(\"Freeleech\")").Any();
                foreach (var row in rows.Skip(1))
                {
                    var release = new ReleaseInfo();
                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 96 * 60 * 60;

                    var qRow = row.Cq();

                    var catStr = row.ChildElements.ElementAt(0).FirstElementChild.GetAttribute("href").Split('=')[1];
                    release.Category = MapTrackerCatToNewznab(catStr);

                    var qLink = row.ChildElements.ElementAt(2).FirstElementChild.Cq();
                    release.Link = new Uri(SiteLink + qLink.Attr("href"));
                    var torrentId = qLink.Attr("href").Split('=').Last();

                    var descCol = row.ChildElements.ElementAt(1);
                    var qCommentLink = descCol.FirstElementChild.Cq();
                    var torrentTag = descCol.Cq().Find("span.torrent-tag");
                    var torrentTags = torrentTag.Elements.Select(x => x.InnerHTML).ToList();
                    release.Title = qCommentLink.Attr("title");
                    release.Description = String.Join(", ", torrentTags);
                    release.Comments = new Uri(SiteLink + qCommentLink.Attr("href").Replace("&hit=1", ""));
                    release.Guid = release.Comments;

                    var torrent_details = descCol.Cq().Find(".torrent_details").Get(0);
                    var rawDateStr = torrent_details.ChildNodes.ElementAt(torrent_details.ChildNodes.Length - 3).Cq().Text();
                    var dateStr = rawDateStr.Trim().Replace("von", "").Trim();
                    DateTime dateGerman;
                    if (dateStr.StartsWith("Heute "))
                        dateGerman = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Unspecified) + TimeSpan.Parse(dateStr.Split(' ')[1]);
                    else if (dateStr.StartsWith("Gestern "))
                        dateGerman = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Unspecified) + TimeSpan.Parse(dateStr.Split(' ')[1]) - TimeSpan.FromDays(1);
                    else
                        dateGerman = DateTime.SpecifyKind(DateTime.ParseExact(dateStr, "dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture), DateTimeKind.Unspecified);

                    DateTime pubDateUtc = TimeZoneInfo.ConvertTimeToUtc(dateGerman, germanyTz);
                    release.PublishDate = pubDateUtc.ToLocalTime();

                    var imdbLink = descCol.Cq().Find("a[href*=\"&searchin=imdb\"]");
                    if (imdbLink.Any())
                        release.Imdb = ParseUtil.GetLongFromString(imdbLink.Attr("href"));

                    var sizeStr = row.ChildElements.ElementAt(5).Cq().Text();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    release.Seeders = ParseUtil.CoerceInt(row.ChildElements.ElementAt(7).Cq().Text());
                    release.Peers = ParseUtil.CoerceInt(row.ChildElements.ElementAt(8).Cq().Text()) + release.Seeders;

                    var grabs = qRow.Find("td:nth-child(7)").Text();
                    release.Grabs = ParseUtil.CoerceInt(grabs);

                    if (globalFreeleech)
                        release.DownloadVolumeFactor = 0;
                    else if (qRow.Find("span.torrent-tag-free").Length >= 1)
                        release.DownloadVolumeFactor = 0;
                    else
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
