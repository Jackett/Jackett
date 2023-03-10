using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json.Linq;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;
using WebClient = Jackett.Common.Utils.Clients.WebClient;



namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class ShareWood : IndexerBase
    {
        public override string Id => "sharewoodapi";
        public override string Name => "Sharewood API";
        public override string Description => "Sharewood is a Semi-Private FRENCH Torrent Tracker for GENERAL";
        public override string SiteLink { get; protected set; } = "https://www.sharewood.tv/";
        public override string Language => "fr-FR";
        public override string Type => "semi-private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private readonly Dictionary<string, string> _apiHeaders = new Dictionary<string, string>
        {
            {"Accept", "application/json"},
            {"Content-Type", "application/json"}
        };
        // API DOC: https://github.com/Jackett/Jackett/issues/10269
        private string SearchUrl => SiteLink + "api/" + configData.Passkey.Value;
        private new ConfigurationDataPasskey configData => (ConfigurationDataPasskey)base.configData;

        public ShareWood(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps, ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataPasskey())
        {
            // requestDelay for API Limit (1 request per 2 seconds)
            webclient.requestDelay = 2.1;

            var FreeLeechOnly = new BoolConfigurationItem("Search freeleech only");
            configData.AddDynamic("freeleechonly", FreeLeechOnly);

            var ReplaceMulti = new BoolConfigurationItem("Replace MULTi by another language in release name");
            configData.AddDynamic("replacemulti", ReplaceMulti);

            // Configure the language select option for MULTI
            var languageSelect = new SingleSelectConfigurationItem("Replace MULTi by this language", new Dictionary<string, string>
            {
                {"FRENCH", "FRENCH"},
                {"MULTi FRENCH", "MULTi FRENCH"},
                {"ENGLISH", "ENGLISH"},
                {"MULTi ENGLISH", "MULTi ENGLISH" },
                {"VOSTFR", "VOSTFR"},
                {"MULTi VOSTFR", "MULTi VOSTFR"}
            })
            { Value = "FRENCH" };
            ;
            configData.AddDynamic("languageid", languageSelect);

            var ReplaceVostfr = new BoolConfigurationItem("Replace VOSTFR and SUBFRENCH with ENGLISH");
            configData.AddDynamic("replacevostfr", ReplaceVostfr);

            EnableConfigurableRetryAttempts();
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
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
                },
                SupportsRawSearch = true
            };

            //CATEGORIES
            //caps.Categories.AddCategoryMapping(1, TorznabCatType.Movies, "Vidéos");
            //caps.Categories.AddCategoryMapping(1, TorznabCatType.TV, "Vidéos");
            //caps.Categories.AddCategoryMapping(2, TorznabCatType.Audio, "Audio");
            //caps.Categories.AddCategoryMapping(3, TorznabCatType.PC, "Application");
            //caps.Categories.AddCategoryMapping(4, TorznabCatType.Books, "Ebooks");
            //caps.Categories.AddCategoryMapping(5, TorznabCatType.PCGames, "Jeu-Vidéo");
            //caps.Categories.AddCategoryMapping(6, TorznabCatType.OtherMisc, "Formation");
            //caps.Categories.AddCategoryMapping(7, TorznabCatType.XXX, "XXX");

            //SUBCATEGORIES
            caps.Categories.AddCategoryMapping(9, TorznabCatType.Movies, "Films");
            caps.Categories.AddCategoryMapping(10, TorznabCatType.TV, "Série");
            caps.Categories.AddCategoryMapping(11, TorznabCatType.MoviesOther, "Film Animation");
            caps.Categories.AddCategoryMapping(12, TorznabCatType.TVAnime, "Série Animation");
            caps.Categories.AddCategoryMapping(13, TorznabCatType.TVDocumentary, "Documentaire");
            caps.Categories.AddCategoryMapping(14, TorznabCatType.TVOther, "Emission TV");
            caps.Categories.AddCategoryMapping(15, TorznabCatType.TVOther, "Spectacle/Concert");
            caps.Categories.AddCategoryMapping(16, TorznabCatType.TVSport, "Sport");
            caps.Categories.AddCategoryMapping(17, TorznabCatType.AudioOther, "Karaoké Vidéo");
            caps.Categories.AddCategoryMapping(18, TorznabCatType.AudioOther, "Karaoké");
            caps.Categories.AddCategoryMapping(20, TorznabCatType.Audio, "Musique");
            caps.Categories.AddCategoryMapping(21, TorznabCatType.AudioOther, "Podcast");
            caps.Categories.AddCategoryMapping(22, TorznabCatType.Audio, "Sample");
            caps.Categories.AddCategoryMapping(23, TorznabCatType.AudioAudiobook, "Ebook Audio");
            caps.Categories.AddCategoryMapping(24, TorznabCatType.Books, "BD");
            caps.Categories.AddCategoryMapping(25, TorznabCatType.BooksComics, "Comic");
            caps.Categories.AddCategoryMapping(26, TorznabCatType.BooksOther, "Manga");
            caps.Categories.AddCategoryMapping(27, TorznabCatType.Books, "Livre");
            caps.Categories.AddCategoryMapping(28, TorznabCatType.BooksMags, "Presse");
            caps.Categories.AddCategoryMapping(29, TorznabCatType.Audio, "Application Linux");
            caps.Categories.AddCategoryMapping(30, TorznabCatType.PC, "Application Window");
            caps.Categories.AddCategoryMapping(31, TorznabCatType.PCMac, "Application Mac");
            caps.Categories.AddCategoryMapping(34, TorznabCatType.PCMobileiOS, "Application Smartphone/Tablette");
            caps.Categories.AddCategoryMapping(34, TorznabCatType.PCMobileAndroid, "Application Smartphone/Tablette");
            caps.Categories.AddCategoryMapping(35, TorznabCatType.Other, "GPS");
            caps.Categories.AddCategoryMapping(36, TorznabCatType.Audio, "Jeux Linux");
            caps.Categories.AddCategoryMapping(37, TorznabCatType.PCGames, "Jeux Windows");
            caps.Categories.AddCategoryMapping(39, TorznabCatType.ConsoleNDS, "Jeux Nintendo");
            caps.Categories.AddCategoryMapping(39, TorznabCatType.ConsoleWii, "Jeux Nintendo");
            caps.Categories.AddCategoryMapping(39, TorznabCatType.ConsoleWiiware, "Jeux Nintendo");
            caps.Categories.AddCategoryMapping(39, TorznabCatType.Console3DS, "Jeux Nintendo");
            caps.Categories.AddCategoryMapping(39, TorznabCatType.ConsoleWiiU, "Jeux Nintendo");
            caps.Categories.AddCategoryMapping(41, TorznabCatType.PCMobileAndroid, "PC/Mobile-Android");
            caps.Categories.AddCategoryMapping(42, TorznabCatType.PCGames, "Jeux Microsoft");
            caps.Categories.AddCategoryMapping(44, TorznabCatType.XXX, "XXX Films");
            caps.Categories.AddCategoryMapping(45, TorznabCatType.XXXOther, "XXX Hentai");
            caps.Categories.AddCategoryMapping(47, TorznabCatType.XXXImageSet, "XXX Images");
            caps.Categories.AddCategoryMapping(48, TorznabCatType.XXXOther, "XXX Jeu-Vidéo");
            caps.Categories.AddCategoryMapping(50, TorznabCatType.OtherMisc, "Formation Logiciels");
            caps.Categories.AddCategoryMapping(49, TorznabCatType.OtherMisc, "Formations Vidéos");
            caps.Categories.AddCategoryMapping(51, TorznabCatType.XXXOther, "XXX Ebooks");
            caps.Categories.AddCategoryMapping(52, TorznabCatType.AudioVideo, "Vidéos-Clips");
            caps.Categories.AddCategoryMapping(51, TorznabCatType.XXXOther, "XXX Ebooks");
            caps.Categories.AddCategoryMapping(51, TorznabCatType.XXXOther, "XXX Ebooks");

            return caps;
        }

        private string MultiRename(string term, string replacement)
        {
            replacement = " " + replacement + " ";
            term = Regex.Replace(term, @"(?i)\b(MULTI(?!.*(?:FRENCH|ENGLISH|VOSTFR)))\b", replacement);
            return term;
        }

        private string VostfrRename(string term, string replacement)
        {
            term = Regex.Replace(term, @"(?i)\b(vostfr|subfrench)\b", replacement);
            return term;
        }

        private bool GetFreeLeech => ((BoolConfigurationItem)configData.GetDynamic("freeleechonly")).Value;
        private bool GetReplaceMulti => ((BoolConfigurationItem)configData.GetDynamic("replacemulti")).Value;
        private string GetLang => ((SingleSelectConfigurationItem)configData.GetDynamic("languageid")).Value;
        private bool GetReplaceVostfr => ((BoolConfigurationItem)configData.GetDynamic("replacevostfr")).Value;
        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            if (configData.Passkey.Value.Length != 32)
                throw new Exception("Invalid Passkey configured. Expected length: 32");

            var releases = await PerformQuery(new TorznabQuery());

            await ConfigureIfOK(string.Empty, releases.Any(),
                                () => throw new Exception("Could not find releases."));

            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {

            var releases = new List<ReleaseInfo>();
            var Mapcats = MapTorznabCapsToTrackers(query);

            if (Mapcats.Count == 0)
            { // NO CATEGORIES ==> RSS SEARCH
                Mapcats.Add("1000");
            }
            foreach (var cats in Mapcats)
            {

                var searchUrl = SearchUrl;

                var qc = new List<KeyValuePair<string, string>> // NameValueCollection don't support cat[]=19&cat[]=6
                {
                    {"limit", "25"}
                };

                if (Convert.ToInt32(cats) != 1000)
                {
                    qc.Add("subcategory", cats);
                }

                if (query.GetQueryString() != "")
                {
                    qc.Add("name", query.GetQueryString());
                    searchUrl = searchUrl + "/search";
                }
                else
                {
                    searchUrl = searchUrl + "/last-torrents";
                }

                searchUrl = searchUrl + "?" + qc.GetQueryString();

                var response = await RequestWithCookiesAsync(searchUrl);
                if (response.Status == HttpStatusCode.Unauthorized)
                {
                    response = await RequestWithCookiesAsync(searchUrl);
                }
                else if (response.Status != HttpStatusCode.OK)
                    throw new Exception($"Unknown error in search: {response.ContentString}");

                try
                {
                    var rows = JArray.Parse(response.ContentString);
                    foreach (var row in rows)
                    {
                        var id = row.Value<string>("id");
                        var link = new Uri($"{SearchUrl}/{id}/download");
                        var urlStr = row.Value<string>("slug");
                        var details = new Uri($"{SiteLink}torrents/{urlStr}.{id}");
                        DateTime publishDate = DateTime.Parse(row.Value<string>("created_at"), CultureInfo.InvariantCulture);
                        var cat = row.Value<string>("subcategory_id");

                        if (Convert.ToInt32(cats) != 1000)
                        {
                            if (Convert.ToInt32(cats) < 8) //USE CATEGORIES
                            {
                                cat = row.Value<string>("category_id");
                            }
                            else //USE SUBCATEGORIES
                            {
                                cat = row.Value<string>("subcategory_id");
                            }
                        }

                        long dlVolumeFactor = 1;
                        long ulVolumeFactor = 1;
                        if (row.Value<bool>("free") == true)
                        {
                            dlVolumeFactor = 0;
                        }
                        if (row.Value<bool>("doubleup") == true)
                        {
                            ulVolumeFactor = 2;
                        }
                        var sizeString = row.Value<string>("size");
                        var title = row.Value<string>("name");

                        //SPECIAL CASES

                        if (GetFreeLeech == true && dlVolumeFactor == 1)
                            continue;

                        if (GetReplaceMulti == true)
                        {
                            title = MultiRename(title, GetLang);
                        }

                        if (GetReplaceVostfr == true)
                        {
                            title = VostfrRename(title, "ENGLISH");
                        }

                        var release = new ReleaseInfo
                        {

                            Title = title,
                            Link = link,
                            Details = details,
                            Guid = details,
                            Category = MapTrackerCatToNewznab(cat),
                            PublishDate = publishDate,
                            Size = ParseUtil.GetBytes(sizeString),
                            Grabs = row.Value<long>("times_completed"),
                            Seeders = row.Value<long>("seeders"),
                            Peers = row.Value<long>("leechers") + row.Value<long>("seeders"),
                            DownloadVolumeFactor = dlVolumeFactor,
                            UploadVolumeFactor = ulVolumeFactor,
                            MinimumRatio = 1,
                            MinimumSeedTime = 259200 // 72 hours
                        };

                        releases.Add(release);
                    }
                }
                catch (Exception ex)
                {
                    OnParseError(response.ContentString, ex);
                }
            }
            return releases;
        }

        public override async Task<byte[]> Download(Uri link)
        {
            var response = await RequestWithCookiesAsync(link.ToString());
            if (response.Status == HttpStatusCode.Unauthorized)
            {
                response = await RequestWithCookiesAsync(link.ToString());
            }
            else if (response.Status != HttpStatusCode.OK)
                throw new Exception($"Unknown error in download: {response.ContentBytes}");
            return response.ContentBytes;
        }
    }
}
