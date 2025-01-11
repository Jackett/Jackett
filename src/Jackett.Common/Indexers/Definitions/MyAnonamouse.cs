using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jackett.Common.Extensions;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class MyAnonamouse : IndexerBase
    {
        public override string Id => "myanonamouse";
        public override string Name => "MyAnonamouse";
        public override string Description => "Friendliness, Warmth and Sharing";
        public override string SiteLink { get; protected set; } = "https://www.myanonamouse.net/";
        public override string Language => "en-US";
        public override string Type => "private";

        public override bool SupportsPagination => true;

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private string SearchUrl => SiteLink + "tor/js/loadSearchJSONbasic.php";

        private static readonly Regex _SanitizeSearchQueryRegex = new Regex("[^\\w]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private new ConfigurationDataMyAnonamouse configData => (ConfigurationDataMyAnonamouse)base.configData;

        public MyAnonamouse(IIndexerConfigurationService configService, WebClient c, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(configService: configService,
                   client: c,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataMyAnonamouse())
        {
            webclient.EmulateBrowser = false;
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                BookSearchParams = new List<BookSearchParam>
                {
                    BookSearchParam.Q
                }
            };

            caps.Categories.AddCategoryMapping("13", TorznabCatType.AudioAudiobook, "AudioBooks");
            caps.Categories.AddCategoryMapping("14", TorznabCatType.BooksEBook, "E-Books");
            caps.Categories.AddCategoryMapping("15", TorznabCatType.AudioAudiobook, "Musicology");
            caps.Categories.AddCategoryMapping("16", TorznabCatType.AudioAudiobook, "Radio");
            caps.Categories.AddCategoryMapping("39", TorznabCatType.AudioAudiobook, "Audiobooks - Action/Adventure");
            caps.Categories.AddCategoryMapping("49", TorznabCatType.AudioAudiobook, "Audiobooks - Art");
            caps.Categories.AddCategoryMapping("50", TorznabCatType.AudioAudiobook, "Audiobooks - Biographical");
            caps.Categories.AddCategoryMapping("83", TorznabCatType.AudioAudiobook, "Audiobooks - Business");
            caps.Categories.AddCategoryMapping("51", TorznabCatType.AudioAudiobook, "Audiobooks - Computer/Internet");
            caps.Categories.AddCategoryMapping("97", TorznabCatType.AudioAudiobook, "Audiobooks - Crafts");
            caps.Categories.AddCategoryMapping("40", TorznabCatType.AudioAudiobook, "Audiobooks - Crime/Thriller");
            caps.Categories.AddCategoryMapping("41", TorznabCatType.AudioAudiobook, "Audiobooks - Fantasy");
            caps.Categories.AddCategoryMapping("106", TorznabCatType.AudioAudiobook, "Audiobooks - Food");
            caps.Categories.AddCategoryMapping("42", TorznabCatType.AudioAudiobook, "Audiobooks - General Fiction");
            caps.Categories.AddCategoryMapping("52", TorznabCatType.AudioAudiobook, "Audiobooks - General Non-Fic");
            caps.Categories.AddCategoryMapping("98", TorznabCatType.AudioAudiobook, "Audiobooks - Historical Fiction");
            caps.Categories.AddCategoryMapping("54", TorznabCatType.AudioAudiobook, "Audiobooks - History");
            caps.Categories.AddCategoryMapping("55", TorznabCatType.AudioAudiobook, "Audiobooks - Home/Garden");
            caps.Categories.AddCategoryMapping("43", TorznabCatType.AudioAudiobook, "Audiobooks - Horror");
            caps.Categories.AddCategoryMapping("99", TorznabCatType.AudioAudiobook, "Audiobooks - Humor");
            caps.Categories.AddCategoryMapping("84", TorznabCatType.AudioAudiobook, "Audiobooks - Instructional");
            caps.Categories.AddCategoryMapping("44", TorznabCatType.AudioAudiobook, "Audiobooks - Juvenile");
            caps.Categories.AddCategoryMapping("56", TorznabCatType.AudioAudiobook, "Audiobooks - Language");
            caps.Categories.AddCategoryMapping("45", TorznabCatType.AudioAudiobook, "Audiobooks - Literary Classics");
            caps.Categories.AddCategoryMapping("57", TorznabCatType.AudioAudiobook, "Audiobooks - Math/Science/Tech");
            caps.Categories.AddCategoryMapping("85", TorznabCatType.AudioAudiobook, "Audiobooks - Medical");
            caps.Categories.AddCategoryMapping("87", TorznabCatType.AudioAudiobook, "Audiobooks - Mystery");
            caps.Categories.AddCategoryMapping("119", TorznabCatType.AudioAudiobook, "Audiobooks - Nature");
            caps.Categories.AddCategoryMapping("88", TorznabCatType.AudioAudiobook, "Audiobooks - Philosophy");
            caps.Categories.AddCategoryMapping("58", TorznabCatType.AudioAudiobook, "Audiobooks - Pol/Soc/Relig");
            caps.Categories.AddCategoryMapping("59", TorznabCatType.AudioAudiobook, "Audiobooks - Recreation");
            caps.Categories.AddCategoryMapping("46", TorznabCatType.AudioAudiobook, "Audiobooks - Romance");
            caps.Categories.AddCategoryMapping("47", TorznabCatType.AudioAudiobook, "Audiobooks - Science Fiction");
            caps.Categories.AddCategoryMapping("53", TorznabCatType.AudioAudiobook, "Audiobooks - Self-Help");
            caps.Categories.AddCategoryMapping("89", TorznabCatType.AudioAudiobook, "Audiobooks - Travel/Adventure");
            caps.Categories.AddCategoryMapping("100", TorznabCatType.AudioAudiobook, "Audiobooks - True Crime");
            caps.Categories.AddCategoryMapping("108", TorznabCatType.AudioAudiobook, "Audiobooks - Urban Fantasy");
            caps.Categories.AddCategoryMapping("48", TorznabCatType.AudioAudiobook, "Audiobooks - Western");
            caps.Categories.AddCategoryMapping("111", TorznabCatType.AudioAudiobook, "Audiobooks - Young Adult");
            caps.Categories.AddCategoryMapping("60", TorznabCatType.BooksEBook, "Ebooks - Action/Adventure");
            caps.Categories.AddCategoryMapping("71", TorznabCatType.BooksEBook, "Ebooks - Art");
            caps.Categories.AddCategoryMapping("72", TorznabCatType.BooksEBook, "Ebooks - Biographical");
            caps.Categories.AddCategoryMapping("90", TorznabCatType.BooksEBook, "Ebooks - Business");
            caps.Categories.AddCategoryMapping("61", TorznabCatType.BooksComics, "Ebooks - Comics/Graphic novels");
            caps.Categories.AddCategoryMapping("73", TorznabCatType.BooksEBook, "Ebooks - Computer/Internet");
            caps.Categories.AddCategoryMapping("101", TorznabCatType.BooksEBook, "Ebooks - Crafts");
            caps.Categories.AddCategoryMapping("62", TorznabCatType.BooksEBook, "Ebooks - Crime/Thriller");
            caps.Categories.AddCategoryMapping("63", TorznabCatType.BooksEBook, "Ebooks - Fantasy");
            caps.Categories.AddCategoryMapping("107", TorznabCatType.BooksEBook, "Ebooks - Food");
            caps.Categories.AddCategoryMapping("64", TorznabCatType.BooksEBook, "Ebooks - General Fiction");
            caps.Categories.AddCategoryMapping("74", TorznabCatType.BooksEBook, "Ebooks - General Non-Fiction");
            caps.Categories.AddCategoryMapping("102", TorznabCatType.BooksEBook, "Ebooks - Historical Fiction");
            caps.Categories.AddCategoryMapping("76", TorznabCatType.BooksEBook, "Ebooks - History");
            caps.Categories.AddCategoryMapping("77", TorznabCatType.BooksEBook, "Ebooks - Home/Garden");
            caps.Categories.AddCategoryMapping("65", TorznabCatType.BooksEBook, "Ebooks - Horror");
            caps.Categories.AddCategoryMapping("103", TorznabCatType.BooksEBook, "Ebooks - Humor");
            caps.Categories.AddCategoryMapping("115", TorznabCatType.BooksEBook, "Ebooks - Illusion/Magic");
            caps.Categories.AddCategoryMapping("91", TorznabCatType.BooksEBook, "Ebooks - Instructional");
            caps.Categories.AddCategoryMapping("66", TorznabCatType.BooksEBook, "Ebooks - Juvenile");
            caps.Categories.AddCategoryMapping("78", TorznabCatType.BooksEBook, "Ebooks - Language");
            caps.Categories.AddCategoryMapping("67", TorznabCatType.BooksEBook, "Ebooks - Literary Classics");
            caps.Categories.AddCategoryMapping("79", TorznabCatType.BooksMags, "Ebooks - Magazines/Newspapers");
            caps.Categories.AddCategoryMapping("80", TorznabCatType.BooksTechnical, "Ebooks - Math/Science/Tech");
            caps.Categories.AddCategoryMapping("92", TorznabCatType.BooksEBook, "Ebooks - Medical");
            caps.Categories.AddCategoryMapping("118", TorznabCatType.BooksEBook, "Ebooks - Mixed Collections");
            caps.Categories.AddCategoryMapping("94", TorznabCatType.BooksEBook, "Ebooks - Mystery");
            caps.Categories.AddCategoryMapping("120", TorznabCatType.BooksEBook, "Ebooks - Nature");
            caps.Categories.AddCategoryMapping("95", TorznabCatType.BooksEBook, "Ebooks - Philosophy");
            caps.Categories.AddCategoryMapping("81", TorznabCatType.BooksEBook, "Ebooks - Pol/Soc/Relig");
            caps.Categories.AddCategoryMapping("82", TorznabCatType.BooksEBook, "Ebooks - Recreation");
            caps.Categories.AddCategoryMapping("68", TorznabCatType.BooksEBook, "Ebooks - Romance");
            caps.Categories.AddCategoryMapping("69", TorznabCatType.BooksEBook, "Ebooks - Science Fiction");
            caps.Categories.AddCategoryMapping("75", TorznabCatType.BooksEBook, "Ebooks - Self-Help");
            caps.Categories.AddCategoryMapping("96", TorznabCatType.BooksEBook, "Ebooks - Travel/Adventure");
            caps.Categories.AddCategoryMapping("104", TorznabCatType.BooksEBook, "Ebooks - True Crime");
            caps.Categories.AddCategoryMapping("109", TorznabCatType.BooksEBook, "Ebooks - Urban Fantasy");
            caps.Categories.AddCategoryMapping("70", TorznabCatType.BooksEBook, "Ebooks - Western");
            caps.Categories.AddCategoryMapping("112", TorznabCatType.BooksEBook, "Ebooks - Young Adult");
            caps.Categories.AddCategoryMapping("19", TorznabCatType.AudioAudiobook, "Guitar/Bass Tabs");
            caps.Categories.AddCategoryMapping("20", TorznabCatType.AudioAudiobook, "Individual Sheet");
            caps.Categories.AddCategoryMapping("24", TorznabCatType.AudioAudiobook, "Individual Sheet MP3");
            caps.Categories.AddCategoryMapping("126", TorznabCatType.AudioAudiobook, "Instructional Book with Video");
            caps.Categories.AddCategoryMapping("22", TorznabCatType.AudioAudiobook, "Instructional Media - Music");
            caps.Categories.AddCategoryMapping("113", TorznabCatType.AudioAudiobook, "Lick Library - LTP/Jam With");
            caps.Categories.AddCategoryMapping("114", TorznabCatType.AudioAudiobook, "Lick Library - Techniques/QL");
            caps.Categories.AddCategoryMapping("17", TorznabCatType.AudioAudiobook, "Music - Complete Editions");
            caps.Categories.AddCategoryMapping("26", TorznabCatType.AudioAudiobook, "Music Book");
            caps.Categories.AddCategoryMapping("27", TorznabCatType.AudioAudiobook, "Music Book MP3");
            caps.Categories.AddCategoryMapping("30", TorznabCatType.AudioAudiobook, "Sheet Collection");
            caps.Categories.AddCategoryMapping("31", TorznabCatType.AudioAudiobook, "Sheet Collection MP3");
            caps.Categories.AddCategoryMapping("127", TorznabCatType.AudioAudiobook, "Radio -  Comedy");
            caps.Categories.AddCategoryMapping("130", TorznabCatType.AudioAudiobook, "Radio - Drama");
            caps.Categories.AddCategoryMapping("128", TorznabCatType.AudioAudiobook, "Radio - Factual/Documentary");
            caps.Categories.AddCategoryMapping("132", TorznabCatType.AudioAudiobook, "Radio - Reading");

            return caps;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            CookieHeader = "mam_id=" + configData.MamId.Value;
            try
            {
                var results = await PerformQuery(new TorznabQuery());
                if (!results.Any())
                {
                    throw new Exception("Your man_id did not work");
                }

                IsConfigured = true;
                SaveConfig();
                return IndexerConfigurationStatus.Completed;
            }
            catch (Exception e)
            {
                IsConfigured = false;
                throw new Exception("Your man_id did not work: " + e.Message);
            }
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var term = _SanitizeSearchQueryRegex.Replace(query.GetQueryString(), " ").Trim();

            if (query.SearchTerm.IsNotNullOrWhiteSpace() && term.IsNullOrWhiteSpace())
            {
                logger.Debug("Search term is empty after being sanitized, stopping search. Initial search term: '{0}'", query.SearchTerm);

                return releases;
            }

            var limit = query.Limit > 0 ? query.Limit : 100;
            var offset = query.Offset > 0 ? query.Offset : 0;

            var parameters = new NameValueCollection
            {
                {"tor[text]", term},
                {"tor[searchType]", configData.SearchType.Value},
                {"tor[srchIn][title]", "true"},
                {"tor[srchIn][author]", "true"},
                {"tor[srchIn][narrator]", "true"},
                {"tor[searchIn]", "torrents"},
                {"tor[sortType]", "default"},
                {"tor[perpage]", limit.ToString()},
                {"tor[startNumber]", offset.ToString()},
                {"thumbnails", "1"}, // gives links for thumbnail sized versions of their posters
                {"description", "1"} // include the description
            };

            if (configData.SearchInDescription.Value)
            {
                parameters.Add("tor[srchIn][description]", "true");
            }

            if (configData.SearchInSeries.Value)
            {
                parameters.Add("tor[srchIn][series]", "true");
            }

            if (configData.SearchInFilenames.Value)
            {
                parameters.Add("tor[srchIn][filenames]", "true");
            }

            if (configData.SearchLanguages.Values is { Length: > 0 })
            {
                foreach (var (language, index) in configData.SearchLanguages.Values.Select((value, index) => (value, index)))
                {
                    parameters.Set($"tor[browse_lang][{index}]", language);
                }
            }

            var catList = MapTorznabCapsToTrackers(query).Distinct().ToList();

            if (catList.Any())
            {
                foreach (var (category, index) in catList.Select((value, index) => (value, index)))
                {
                    parameters.Set($"tor[cat][{index}]", category);
                }
            }
            else
            {
                parameters.Add("tor[cat][]", "0");
            }

            var urlSearch = SearchUrl;
            if (parameters.Count > 0)
            {
                urlSearch += $"?{parameters.GetQueryString()}";
            }

            var response = await RequestWithCookiesAndRetryAsync(
                urlSearch,
                headers: new Dictionary<string, string>
                {
                    {"Accept", "application/json"}
                });

            if (response.ContentString.StartsWith("Error"))
            {
                throw new Exception(response.ContentString);
            }

            try
            {
                var sitelink = new Uri(SiteLink);

                var jsonResponse = JsonConvert.DeserializeObject<MyAnonamouseResponse>(response.ContentString);

                var error = jsonResponse.Error;
                if (error.IsNotNullOrWhiteSpace() && error.StartsWithIgnoreCase("Nothing returned, out of"))
                {
                    return releases;
                }

                if (jsonResponse.Data == null)
                {
                    throw new Exception($"Unexpected response content from indexer request: {jsonResponse.Message ?? "Check the logs for more information."}");
                }

                foreach (var item in jsonResponse.Data)
                {
                    var id = item.Id;
                    var link = new Uri(sitelink, $"/tor/download.php?tid={id}");
                    var details = new Uri(sitelink, $"/t/{id}");

                    var isFreeLeech = item.Free || item.PersonalFreeLeech;

                    var release = new ReleaseInfo
                    {
                        Guid = details,
                        Title = item.Title.Trim(),
                        Description = item.Description.Trim(),
                        Link = link,
                        Details = details,
                        Category = MapTrackerCatToNewznab(item.Category),
                        PublishDate = DateTime.ParseExact(item.Added, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToLocalTime(),
                        Grabs = item.Grabs,
                        Files = item.NumFiles,
                        Seeders = item.Seeders,
                        Peers = item.Seeders + item.Leechers,
                        Size = ParseUtil.GetBytes(item.Size),
                        DownloadVolumeFactor = isFreeLeech ? 0 : 1,
                        UploadVolumeFactor = 1,

                        // MinimumRatio = 1, // global MR is 1.0 but torrents must be seeded for 3 days regardless of ratio
                        MinimumSeedTime = 259200 // 72 hours
                    };

                    var authorInfo = item.AuthorInfo;
                    if (authorInfo != null)
                    {
                        try
                        {
                            var authorInfoList = JsonConvert.DeserializeObject<Dictionary<string, string>>(authorInfo);
                            var author = authorInfoList?.Take(5).Select(v => v.Value).ToList();

                            if (author != null && author.Any())
                            {
                                release.Title += " by " + string.Join(", ", author);
                            }
                        }
                        catch (Exception)
                        {
                            // the JSON on author_info field can be malformed due to double quotes
                            logger.Warn($"{Name} error parsing author_info: {authorInfo}");
                        }
                    }

                    var flags = new List<string>();

                    var langCode = item.LanguageCode;
                    if (!string.IsNullOrEmpty(langCode))
                    {
                        flags.Add(langCode);
                    }

                    var filetype = item.Filetype;
                    if (!string.IsNullOrEmpty(filetype))
                    {
                        flags.Add(filetype.ToUpper());
                    }

                    if (flags.Count > 0)
                    {
                        release.Title += " [" + string.Join(" / ", flags) + "]";
                    }

                    if (item.Vip)
                    {
                        release.Title += " [VIP]";
                    }

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(response.ContentString, ex);
            }

            return releases;
        }
    }

    public class MyAnonamouseResponse
    {
        public string Error { get; set; }
        public IReadOnlyCollection<MyAnonamouseTorrent> Data { get; set; }
        public string Message { get; set; }
    }

    public class MyAnonamouseTorrent
    {
        public int Id { get; set; }
        public string Title { get; set; }
        [JsonProperty(PropertyName = "author_info")]
        public string AuthorInfo { get; set; }
        public string Description { get; set; }
        [JsonProperty(PropertyName = "lang_code")]
        public string LanguageCode { get; set; }
        public string Filetype { get; set; }
        public bool Vip { get; set; }
        public bool Free { get; set; }
        [JsonProperty(PropertyName = "personal_freeleech")]
        public bool PersonalFreeLeech { get; set; }
        [JsonProperty(PropertyName = "fl_vip")]
        public bool FreeVip { get; set; }
        public string Category { get; set; }
        public string Added { get; set; }
        [JsonProperty(PropertyName = "times_completed")]
        public int Grabs { get; set; }
        public int Seeders { get; set; }
        public int Leechers { get; set; }
        public int NumFiles { get; set; }
        public string Size { get; set; }
    }
}
