using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    public class Myanonamouse : BaseWebIndexer
    {
        private string LoginUrl { get { return SiteLink + "takelogin.php"; } }
        private string SearchUrl { get { return SiteLink + "tor/js/loadSearchJSONbasic.php"; } }

        private new ConfigurationDataMyAnonamouse configData
        {
            get { return (ConfigurationDataMyAnonamouse)base.configData; }
            set { base.configData = value; }
        }

        public Myanonamouse(IIndexerConfigurationService configService, WebClient c, Logger l, IProtectionService ps)
            : base(name: "MyAnonamouse",
                description: "Friendliness, Warmth and Sharing",
                link: "https://www.myanonamouse.net/",
                configService: configService,
                caps: new TorznabCapabilities(),
                client: c,
                logger: l,
                p: ps,
                configData: new ConfigurationDataMyAnonamouse())
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";
            webclient.EmulateBrowser = false;

            AddCategoryMapping("13", TorznabCatType.AudioAudiobook, "AudioBooks");
            AddCategoryMapping("14", TorznabCatType.BooksEbook, "E-Books");
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
            AddCategoryMapping("60", TorznabCatType.BooksEbook, "Ebooks - Action/Adventure");
            AddCategoryMapping("71", TorznabCatType.BooksEbook, "Ebooks - Art");
            AddCategoryMapping("72", TorznabCatType.BooksEbook, "Ebooks - Biographical");
            AddCategoryMapping("90", TorznabCatType.BooksEbook, "Ebooks - Business");
            AddCategoryMapping("61", TorznabCatType.BooksComics, "Ebooks - Comics/Graphic novels");
            AddCategoryMapping("73", TorznabCatType.BooksEbook, "Ebooks - Computer/Internet");
            AddCategoryMapping("101", TorznabCatType.BooksEbook, "Ebooks - Crafts");
            AddCategoryMapping("62", TorznabCatType.BooksEbook, "Ebooks - Crime/Thriller");
            AddCategoryMapping("63", TorznabCatType.BooksEbook, "Ebooks - Fantasy");
            AddCategoryMapping("107", TorznabCatType.BooksEbook, "Ebooks - Food");
            AddCategoryMapping("64", TorznabCatType.BooksEbook, "Ebooks - General Fiction");
            AddCategoryMapping("74", TorznabCatType.BooksEbook, "Ebooks - General Non-Fiction");
            AddCategoryMapping("102", TorznabCatType.BooksEbook, "Ebooks - Historical Fiction");
            AddCategoryMapping("76", TorznabCatType.BooksEbook, "Ebooks - History");
            AddCategoryMapping("77", TorznabCatType.BooksEbook, "Ebooks - Home/Garden");
            AddCategoryMapping("65", TorznabCatType.BooksEbook, "Ebooks - Horror");
            AddCategoryMapping("103", TorznabCatType.BooksEbook, "Ebooks - Humor");
            AddCategoryMapping("115", TorznabCatType.BooksEbook, "Ebooks - Illusion/Magic");
            AddCategoryMapping("91", TorznabCatType.BooksEbook, "Ebooks - Instructional");
            AddCategoryMapping("66", TorznabCatType.BooksEbook, "Ebooks - Juvenile");
            AddCategoryMapping("78", TorznabCatType.BooksEbook, "Ebooks - Language");
            AddCategoryMapping("67", TorznabCatType.BooksEbook, "Ebooks - Literary Classics");
            AddCategoryMapping("79", TorznabCatType.BooksMagazines, "Ebooks - Magazines/Newspapers");
            AddCategoryMapping("80", TorznabCatType.BooksTechnical, "Ebooks - Math/Science/Tech");
            AddCategoryMapping("92", TorznabCatType.BooksEbook, "Ebooks - Medical");
            AddCategoryMapping("118", TorznabCatType.BooksEbook, "Ebooks - Mixed Collections");
            AddCategoryMapping("94", TorznabCatType.BooksEbook, "Ebooks - Mystery");
            AddCategoryMapping("120", TorznabCatType.BooksEbook, "Ebooks - Nature");
            AddCategoryMapping("95", TorznabCatType.BooksEbook, "Ebooks - Philosophy");
            AddCategoryMapping("81", TorznabCatType.BooksEbook, "Ebooks - Pol/Soc/Relig");
            AddCategoryMapping("82", TorznabCatType.BooksEbook, "Ebooks - Recreation");
            AddCategoryMapping("68", TorznabCatType.BooksEbook, "Ebooks - Romance");
            AddCategoryMapping("69", TorznabCatType.BooksEbook, "Ebooks - Science Fiction");
            AddCategoryMapping("75", TorznabCatType.BooksEbook, "Ebooks - Self-Help");
            AddCategoryMapping("96", TorznabCatType.BooksEbook, "Ebooks - Travel/Adventure");
            AddCategoryMapping("104", TorznabCatType.BooksEbook, "Ebooks - True Crime");
            AddCategoryMapping("109", TorznabCatType.BooksEbook, "Ebooks - Urban Fantasy");
            AddCategoryMapping("70", TorznabCatType.BooksEbook, "Ebooks - Western");
            AddCategoryMapping("112", TorznabCatType.BooksEbook, "Ebooks - Young Adult");
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

            // TODO: implement captcha
            CookieHeader = "mam_id=" + configData.MamId.Value;
            try
            {
                var results = await PerformQuery(new TorznabQuery());
                if (results.Count() == 0)
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

            NameValueCollection qParams = new NameValueCollection();
            qParams.Add("tor[text]", query.GetQueryString());
            qParams.Add("tor[srchIn][title]", "true");
            qParams.Add("tor[srchIn][author]", "true");
            qParams.Add("tor[searchType]", "all");
            qParams.Add("tor[searchIn]", "torrents");
            qParams.Add("tor[hash]", "");
            qParams.Add("tor[sortType]", "default");
            qParams.Add("tor[startNumber]", "0");

            qParams.Add("thumbnails", "1"); // gives links for thumbnail sized versions of their posters
            //qParams.Add("posterLink", "1"); // gives links for a full sized poster
            //qParams.Add("dlLink", "1"); // include the url to download the torrent
            qParams.Add("description", "1"); // include the description
            //qParams.Add("bookmarks", "0"); // include if the item is bookmarked or not

            List<string> catList = MapTorznabCapsToTrackers(query);
            if (catList.Any())
            {
                int index = 0;
                foreach (string cat in catList)
                {
                    qParams.Add("tor[cat]["+index+"]", cat);
                    index++;
                }
            }
            else
            {
                qParams.Add("tor[cat][]", "0");
            }

            string urlSearch = SearchUrl;
            if (qParams.Count > 0)
            {
                urlSearch += $"?{qParams.GetQueryString()}";
            }

            var response = await RequestStringWithCookiesAndRetry(urlSearch);
            if (response.Content.StartsWith("Error"))
            {
                throw new Exception(response.Content);
            }

            try
            {
                var jsonContent = JObject.Parse(response.Content);
                var sitelink = new Uri(SiteLink);

                var error = jsonContent.Value<string>("error");
                if(error != null)
                {
                    if (error == "Nothing returned, out of 0")
                        return releases;
                }

                foreach (var item in jsonContent.Value<JArray>("data"))
                {
                    var release = new ReleaseInfo();
                        
                    var id = item.Value<long>("id");
                    release.Title = item.Value<string>("title");

                    release.Description = item.Value<string>("description");

                    var author_info = item.Value<string>("author_info");
                    string author = null;
                    if (!string.IsNullOrWhiteSpace(author_info))
                    {
                        author_info = Regex.Unescape(author_info);
                        var author_info_json = JObject.Parse(author_info);
                        author = author_info_json.First.Last.Value<string>();
                    }
                    if (author != null)
                        release.Title += " by " + author;

                    var flags = new List<string>();

                    var lang_code = item.Value<string>("lang_code");
                    if (!string.IsNullOrEmpty(lang_code))
                        flags.Add(lang_code);

                    var filetype = item.Value<string>("filetype");
                    if (!string.IsNullOrEmpty(filetype))
                        flags.Add(filetype);

                    if (flags.Count > 0)
                        release.Title += " [" + string.Join(" / ", flags) + "]";

                    var category = item.Value<string>("category");
                    release.Category = MapTrackerCatToNewznab(category);

                    release.Link = new Uri(sitelink, "/tor/download.php?tid=" + id);
                    release.Comments = new Uri(sitelink, "/t/" + id);
                    release.Guid = release.Comments;

                    var dateStr = item.Value<string>("added");
                    var dateTime = DateTime.ParseExact(dateStr, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    release.PublishDate = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc).ToLocalTime();

                    release.Grabs = item.Value<long>("times_completed");
                    release.Files = item.Value<long>("numfiles");
                    release.Seeders = item.Value<int>("seeders");
                    release.Peers = item.Value<int>("leechers") + release.Seeders;
                    var size = item.Value<string>("size");
                    release.Size = ReleaseInfo.GetBytes(size);
                    var free = item.Value<int>("free");
                    
                    if (free == 1)
                        release.DownloadVolumeFactor = 0;
                    else
                        release.DownloadVolumeFactor = 1;
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
    }
}
