using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
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
            var releases = new List<ReleaseInfo>();

            var qParams = new NameValueCollection
            {
                {"tor[text]", query.GetQueryString()},
                {"tor[srchIn][title]", "true"},
                {"tor[srchIn][author]", "true"},
                {"tor[searchType]", configData.ExcludeVip?.Value == true ? "nVIP" : "all"}, // exclude VIP torrents
                {"tor[searchIn]", "torrents"},
                {"tor[hash]", ""},
                {"tor[sortType]", "default"},
                {"tor[startNumber]", "0"},
                {"thumbnails", "1"}, // gives links for thumbnail sized versions of their posters
                //{ "posterLink", "1"}, // gives links for a full sized poster
                //{ "dlLink", "1"}, // include the url to download the torrent
                {"description", "1"} // include the description
                //{"bookmarks", "0"} // include if the item is bookmarked or not
            };

            // Exclude VIP torrents

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

            var response = await RequestWithCookiesAndRetryAsync(urlSearch);
            if (response.ContentString.StartsWith("Error"))
                throw new Exception(response.ContentString);

            try
            {
                var jsonContent = JObject.Parse(response.ContentString);
                var sitelink = new Uri(SiteLink);

                var error = jsonContent.Value<string>("error");
                if (error != null && error == "Nothing returned, out of 0")
                    return releases;

                foreach (var item in jsonContent.Value<JArray>("data"))
                {
                    //TODO shift to ReleaseInfo object initializer for consistency
                    var release = new ReleaseInfo();

                    var id = item.Value<long>("id");
                    release.Title = item.Value<string>("title");

                    release.Description = item.Value<string>("description");

                    var authorInfo = item.Value<string>("author_info");
                    string author = null;
                    if (!string.IsNullOrWhiteSpace(authorInfo))
                        try
                        {
                            authorInfo = Regex.Unescape(authorInfo);
                            var authorInfoJson = JObject.Parse(authorInfo);
                            author = authorInfoJson.First.Last.Value<string>();
                        }
                        catch (Exception)
                        {
                            // the JSON on author_info field can be malformed due to double quotes
                            logger.Warn($"{DisplayName} error parsing author_info: {authorInfo}");
                        }
                    if (author != null)
                        release.Title += " by " + author;

                    var flags = new List<string>();

                    var langCode = item.Value<string>("lang_code");
                    if (!string.IsNullOrEmpty(langCode))
                        flags.Add(langCode);

                    var filetype = item.Value<string>("filetype");
                    if (!string.IsNullOrEmpty(filetype))
                        flags.Add(filetype);

                    if (flags.Count > 0)
                        release.Title += " [" + string.Join(" / ", flags) + "]";

                    if (item.Value<int>("vip") == 1)
                        release.Title += " [VIP]";

                    var category = item.Value<string>("category");
                    release.Category = MapTrackerCatToNewznab(category);

                    release.Link = new Uri(sitelink, "/tor/download.php?tid=" + id);
                    release.Details = new Uri(sitelink, "/t/" + id);
                    release.Guid = release.Details;

                    var dateStr = item.Value<string>("added");
                    var dateTime = DateTime.ParseExact(dateStr, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    release.PublishDate = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc).ToLocalTime();

                    release.Grabs = item.Value<long>("times_completed");
                    release.Files = item.Value<long>("numfiles");
                    release.Seeders = item.Value<int>("seeders");
                    release.Peers = item.Value<int>("leechers") + release.Seeders;
                    var size = item.Value<string>("size");
                    release.Size = ReleaseInfo.GetBytes(size);

                    release.DownloadVolumeFactor = item.Value<int>("free") == 1 ? 0 : 1;
                    release.UploadVolumeFactor = 1;

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
