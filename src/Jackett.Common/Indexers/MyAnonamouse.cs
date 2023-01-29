using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class MyAnonamouse : BaseWebIndexer
    {
        private string SearchUrl => SiteLink + "tor/js/loadSearchJSONbasic.php";

        private new ConfigurationDataMyAnonamouse configData => (ConfigurationDataMyAnonamouse)base.configData;

        public MyAnonamouse(IIndexerConfigurationService configService, WebClient c, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "myanonamouse",
                   name: "MyAnonamouse",
                   description: "Friendliness, Warmth and Sharing",
                   link: "https://www.myanonamouse.net/",
                   configService: configService,
                   caps: new TorznabCapabilities
                   {
                       BookSearchParams = new List<BookSearchParam>
                       {
                           BookSearchParam.Q
                       }
                   },
                   client: c,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataMyAnonamouse())
        {
            Encoding = Encoding.UTF8;
            Language = "en-US";
            Type = "private";
            webclient.EmulateBrowser = false;

            AddCategoryMapping("13", TorznabCatType.AudioAudiobook, "AudioBooks");
            AddCategoryMapping("14", TorznabCatType.BooksEBook, "E-Books");
            AddCategoryMapping("15", TorznabCatType.AudioAudiobook, "Musicology");
            AddCategoryMapping("16", TorznabCatType.AudioAudiobook, "Radio");
            AddCategoryMapping("39", TorznabCatType.AudioAudiobook, "Audiobooks - Action/Adventure");
            AddCategoryMapping("49", TorznabCatType.AudioAudiobook, "Audiobooks - Art");
            AddCategoryMapping("50", TorznabCatType.AudioAudiobook, "Audiobooks - Biographical");
            AddCategoryMapping("83", TorznabCatType.AudioAudiobook, "Audiobooks - Business");
            AddCategoryMapping("51", TorznabCatType.AudioAudiobook, "Audiobooks - Computer/Internet");
            AddCategoryMapping("97", TorznabCatType.AudioAudiobook, "Audiobooks - Crafts");
            AddCategoryMapping("40", TorznabCatType.AudioAudiobook, "Audiobooks - Crime/Thriller");
            AddCategoryMapping("41", TorznabCatType.AudioAudiobook, "Audiobooks - Fantasy");
            AddCategoryMapping("106", TorznabCatType.AudioAudiobook, "Audiobooks - Food");
            AddCategoryMapping("42", TorznabCatType.AudioAudiobook, "Audiobooks - General Fiction");
            AddCategoryMapping("52", TorznabCatType.AudioAudiobook, "Audiobooks - General Non-Fic");
            AddCategoryMapping("98", TorznabCatType.AudioAudiobook, "Audiobooks - Historical Fiction");
            AddCategoryMapping("54", TorznabCatType.AudioAudiobook, "Audiobooks - History");
            AddCategoryMapping("55", TorznabCatType.AudioAudiobook, "Audiobooks - Home/Garden");
            AddCategoryMapping("43", TorznabCatType.AudioAudiobook, "Audiobooks - Horror");
            AddCategoryMapping("99", TorznabCatType.AudioAudiobook, "Audiobooks - Humor");
            AddCategoryMapping("84", TorznabCatType.AudioAudiobook, "Audiobooks - Instructional");
            AddCategoryMapping("44", TorznabCatType.AudioAudiobook, "Audiobooks - Juvenile");
            AddCategoryMapping("56", TorznabCatType.AudioAudiobook, "Audiobooks - Language");
            AddCategoryMapping("45", TorznabCatType.AudioAudiobook, "Audiobooks - Literary Classics");
            AddCategoryMapping("57", TorznabCatType.AudioAudiobook, "Audiobooks - Math/Science/Tech");
            AddCategoryMapping("85", TorznabCatType.AudioAudiobook, "Audiobooks - Medical");
            AddCategoryMapping("87", TorznabCatType.AudioAudiobook, "Audiobooks - Mystery");
            AddCategoryMapping("119", TorznabCatType.AudioAudiobook, "Audiobooks - Nature");
            AddCategoryMapping("88", TorznabCatType.AudioAudiobook, "Audiobooks - Philosophy");
            AddCategoryMapping("58", TorznabCatType.AudioAudiobook, "Audiobooks - Pol/Soc/Relig");
            AddCategoryMapping("59", TorznabCatType.AudioAudiobook, "Audiobooks - Recreation");
            AddCategoryMapping("46", TorznabCatType.AudioAudiobook, "Audiobooks - Romance");
            AddCategoryMapping("47", TorznabCatType.AudioAudiobook, "Audiobooks - Science Fiction");
            AddCategoryMapping("53", TorznabCatType.AudioAudiobook, "Audiobooks - Self-Help");
            AddCategoryMapping("89", TorznabCatType.AudioAudiobook, "Audiobooks - Travel/Adventure");
            AddCategoryMapping("100", TorznabCatType.AudioAudiobook, "Audiobooks - True Crime");
            AddCategoryMapping("108", TorznabCatType.AudioAudiobook, "Audiobooks - Urban Fantasy");
            AddCategoryMapping("48", TorznabCatType.AudioAudiobook, "Audiobooks - Western");
            AddCategoryMapping("111", TorznabCatType.AudioAudiobook, "Audiobooks - Young Adult");
            AddCategoryMapping("60", TorznabCatType.BooksEBook, "Ebooks - Action/Adventure");
            AddCategoryMapping("71", TorznabCatType.BooksEBook, "Ebooks - Art");
            AddCategoryMapping("72", TorznabCatType.BooksEBook, "Ebooks - Biographical");
            AddCategoryMapping("90", TorznabCatType.BooksEBook, "Ebooks - Business");
            AddCategoryMapping("61", TorznabCatType.BooksComics, "Ebooks - Comics/Graphic novels");
            AddCategoryMapping("73", TorznabCatType.BooksEBook, "Ebooks - Computer/Internet");
            AddCategoryMapping("101", TorznabCatType.BooksEBook, "Ebooks - Crafts");
            AddCategoryMapping("62", TorznabCatType.BooksEBook, "Ebooks - Crime/Thriller");
            AddCategoryMapping("63", TorznabCatType.BooksEBook, "Ebooks - Fantasy");
            AddCategoryMapping("107", TorznabCatType.BooksEBook, "Ebooks - Food");
            AddCategoryMapping("64", TorznabCatType.BooksEBook, "Ebooks - General Fiction");
            AddCategoryMapping("74", TorznabCatType.BooksEBook, "Ebooks - General Non-Fiction");
            AddCategoryMapping("102", TorznabCatType.BooksEBook, "Ebooks - Historical Fiction");
            AddCategoryMapping("76", TorznabCatType.BooksEBook, "Ebooks - History");
            AddCategoryMapping("77", TorznabCatType.BooksEBook, "Ebooks - Home/Garden");
            AddCategoryMapping("65", TorznabCatType.BooksEBook, "Ebooks - Horror");
            AddCategoryMapping("103", TorznabCatType.BooksEBook, "Ebooks - Humor");
            AddCategoryMapping("115", TorznabCatType.BooksEBook, "Ebooks - Illusion/Magic");
            AddCategoryMapping("91", TorznabCatType.BooksEBook, "Ebooks - Instructional");
            AddCategoryMapping("66", TorznabCatType.BooksEBook, "Ebooks - Juvenile");
            AddCategoryMapping("78", TorznabCatType.BooksEBook, "Ebooks - Language");
            AddCategoryMapping("67", TorznabCatType.BooksEBook, "Ebooks - Literary Classics");
            AddCategoryMapping("79", TorznabCatType.BooksMags, "Ebooks - Magazines/Newspapers");
            AddCategoryMapping("80", TorznabCatType.BooksTechnical, "Ebooks - Math/Science/Tech");
            AddCategoryMapping("92", TorznabCatType.BooksEBook, "Ebooks - Medical");
            AddCategoryMapping("118", TorznabCatType.BooksEBook, "Ebooks - Mixed Collections");
            AddCategoryMapping("94", TorznabCatType.BooksEBook, "Ebooks - Mystery");
            AddCategoryMapping("120", TorznabCatType.BooksEBook, "Ebooks - Nature");
            AddCategoryMapping("95", TorznabCatType.BooksEBook, "Ebooks - Philosophy");
            AddCategoryMapping("81", TorznabCatType.BooksEBook, "Ebooks - Pol/Soc/Relig");
            AddCategoryMapping("82", TorznabCatType.BooksEBook, "Ebooks - Recreation");
            AddCategoryMapping("68", TorznabCatType.BooksEBook, "Ebooks - Romance");
            AddCategoryMapping("69", TorznabCatType.BooksEBook, "Ebooks - Science Fiction");
            AddCategoryMapping("75", TorznabCatType.BooksEBook, "Ebooks - Self-Help");
            AddCategoryMapping("96", TorznabCatType.BooksEBook, "Ebooks - Travel/Adventure");
            AddCategoryMapping("104", TorznabCatType.BooksEBook, "Ebooks - True Crime");
            AddCategoryMapping("109", TorznabCatType.BooksEBook, "Ebooks - Urban Fantasy");
            AddCategoryMapping("70", TorznabCatType.BooksEBook, "Ebooks - Western");
            AddCategoryMapping("112", TorznabCatType.BooksEBook, "Ebooks - Young Adult");
            AddCategoryMapping("19", TorznabCatType.AudioAudiobook, "Guitar/Bass Tabs");
            AddCategoryMapping("20", TorznabCatType.AudioAudiobook, "Individual Sheet");
            AddCategoryMapping("24", TorznabCatType.AudioAudiobook, "Individual Sheet MP3");
            AddCategoryMapping("126", TorznabCatType.AudioAudiobook, "Instructional Book with Video");
            AddCategoryMapping("22", TorznabCatType.AudioAudiobook, "Instructional Media - Music");
            AddCategoryMapping("113", TorznabCatType.AudioAudiobook, "Lick Library - LTP/Jam With");
            AddCategoryMapping("114", TorznabCatType.AudioAudiobook, "Lick Library - Techniques/QL");
            AddCategoryMapping("17", TorznabCatType.AudioAudiobook, "Music - Complete Editions");
            AddCategoryMapping("26", TorznabCatType.AudioAudiobook, "Music Book");
            AddCategoryMapping("27", TorznabCatType.AudioAudiobook, "Music Book MP3");
            AddCategoryMapping("30", TorznabCatType.AudioAudiobook, "Sheet Collection");
            AddCategoryMapping("31", TorznabCatType.AudioAudiobook, "Sheet Collection MP3");
            AddCategoryMapping("127", TorznabCatType.AudioAudiobook, "Radio -  Comedy");
            AddCategoryMapping("130", TorznabCatType.AudioAudiobook, "Radio - Drama");
            AddCategoryMapping("128", TorznabCatType.AudioAudiobook, "Radio - Factual/Documentary");
            AddCategoryMapping("132", TorznabCatType.AudioAudiobook, "Radio - Reading");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            CookieHeader = "mam_id=" + configData.MamId.Value;
            try
            {
                var results = await PerformQuery(new TorznabQuery());
                if (!results.Any())
                    throw new Exception("Your man_id did not work");

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
            var limit = query.Limit > 0 ? query.Limit : 100;
            var offset = query.Offset > 0 ? query.Offset : 0;

            var qParams = new NameValueCollection
            {
                {"tor[text]", query.GetQueryString()},
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
                qParams.Add("tor[srchIn][description]", "true");

            if (configData.SearchInSeries.Value)
                qParams.Add("tor[srchIn][series]", "true");

            if (configData.SearchInFilenames.Value)
                qParams.Add("tor[srchIn][filenames]", "true");

            var catList = MapTorznabCapsToTrackers(query);
            if (catList.Any())
            {
                var index = 0;
                foreach (var cat in catList)
                {
                    qParams.Add("tor[cat][" + index + "]", cat);
                    index++;
                }
            }
            else
                qParams.Add("tor[cat][]", "0");

            var urlSearch = SearchUrl;
            if (qParams.Count > 0)
                urlSearch += $"?{qParams.GetQueryString()}";

            var response = await RequestWithCookiesAndRetryAsync(
                urlSearch,
                headers: new Dictionary<string, string>
                {
                    {"Accept", "application/json"}
                });
            if (response.ContentString.StartsWith("Error"))
                throw new Exception(response.ContentString);

            var releases = new List<ReleaseInfo>();

            try
            {
                var jsonContent = JObject.Parse(response.ContentString);
                var sitelink = new Uri(SiteLink);

                var error = jsonContent.Value<string>("error");
                if (error != null && error == "Nothing returned, out of 0")
                    return releases;

                foreach (var item in jsonContent.Value<JArray>("data"))
                {
                    var id = item.Value<long>("id");
                    var link = new Uri(sitelink, "/tor/download.php?tid=" + id);
                    var details = new Uri(sitelink, "/t/" + id);

                    var release = new ReleaseInfo
                    {
                        Guid = details,
                        Title = item.Value<string>("title").Trim(),
                        Description = item.Value<string>("description").Trim(),
                        Link = link,
                        Details = details,
                        Category = MapTrackerCatToNewznab(item.Value<string>("category")),
                        PublishDate = DateTime.ParseExact(item.Value<string>("added"), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToLocalTime(),
                        Grabs = item.Value<long>("times_completed"),
                        Files = item.Value<long>("numfiles"),
                        Seeders = item.Value<int>("seeders"),
                        Peers = item.Value<int>("seeders") + item.Value<int>("leechers"),
                        Size = ReleaseInfo.GetBytes(item.Value<string>("size")),
                        DownloadVolumeFactor = item.Value<bool>("free") ? 0 : 1,
                        UploadVolumeFactor = 1,

                        // MinimumRatio = 1, // global MR is 1.0 but torrents must be seeded for 3 days regardless of ratio
                        MinimumSeedTime = 259200 // 72 hours
                    };

                    var authorInfo = item.Value<string>("author_info");
                    if (authorInfo != null)
                        try
                        {
                            var authorInfoList = JsonConvert.DeserializeObject<Dictionary<string, string>>(authorInfo);
                            var author = authorInfoList?.Take(5).Select(v => v.Value).ToList();

                            if (author != null && author.Any())
                                release.Title += " by " + string.Join(", ", author);
                        }
                        catch (Exception)
                        {
                            // the JSON on author_info field can be malformed due to double quotes
                            logger.Warn($"{DisplayName} error parsing author_info: {authorInfo}");
                        }

                    var flags = new List<string>();

                    var langCode = item.Value<string>("lang_code");
                    if (!string.IsNullOrEmpty(langCode))
                        flags.Add(langCode);

                    var filetype = item.Value<string>("filetype");
                    if (!string.IsNullOrEmpty(filetype))
                        flags.Add(filetype.ToUpper());

                    if (flags.Count > 0)
                        release.Title += " [" + string.Join(" / ", flags) + "]";

                    if (item.Value<bool>("vip"))
                        release.Title += " [VIP]";

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
}
