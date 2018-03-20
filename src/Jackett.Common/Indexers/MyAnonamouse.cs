using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
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
    public class Myanonamouse : BaseWebIndexer
    {
        private string LoginUrl { get { return SiteLink + "takelogin.php"; } }
        private string SearchUrl { get { return SiteLink + "tor/js/loadSearch2.php"; } }

        private new ConfigurationDataBasicLogin configData
        {
            get { return (ConfigurationDataBasicLogin)base.configData; }
            set { base.configData = value; }
        }

        public Myanonamouse(IIndexerConfigurationService configService, WebClient c, Logger l, IProtectionService ps)
            : base(name: "MyAnonamouse",
                description: "Friendliness, Warmth and Sharing",
                link: "https://www.myanonamouse.net/",
                caps: new TorznabCapabilities(TorznabCatType.Books,
                                              TorznabCatType.AudioAudiobook,
                                              TorznabCatType.BooksComics,
                                              TorznabCatType.BooksEbook,
                                              TorznabCatType.BooksMagazines,
                                              TorznabCatType.BooksTechnical),
                configService: configService,
                client: c,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLogin())
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";

            AddCategoryMapping("61", TorznabCatType.BooksComics);
            AddCategoryMapping("91", TorznabCatType.BooksTechnical);
            AddCategoryMapping("80", TorznabCatType.BooksTechnical);
            AddCategoryMapping("79", TorznabCatType.BooksMagazines);
            AddCategoryMapping("39", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("49", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("50", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("83", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("51", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("97", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("40", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("41", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("106", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("42", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("52", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("98", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("54", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("55", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("43", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("99", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("84", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("44", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("56", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("137", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("45", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("57", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("85", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("87", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("119", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("88", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("58", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("59", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("46", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("47", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("53", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("89", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("100", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("108", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("48", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("111", TorznabCatType.AudioAudiobook);
            AddCategoryMapping("60", TorznabCatType.BooksEbook);
            AddCategoryMapping("71", TorznabCatType.BooksEbook);
            AddCategoryMapping("72", TorznabCatType.BooksEbook);
            AddCategoryMapping("90", TorznabCatType.BooksEbook);
            AddCategoryMapping("73", TorznabCatType.BooksEbook);
            AddCategoryMapping("101", TorznabCatType.BooksEbook);
            AddCategoryMapping("62", TorznabCatType.BooksEbook);
            AddCategoryMapping("63", TorznabCatType.BooksEbook);
            AddCategoryMapping("107", TorznabCatType.BooksEbook);
            AddCategoryMapping("64", TorznabCatType.BooksEbook);
            AddCategoryMapping("74", TorznabCatType.BooksEbook);
            AddCategoryMapping("102", TorznabCatType.BooksEbook);
            AddCategoryMapping("76", TorznabCatType.BooksEbook);
            AddCategoryMapping("77", TorznabCatType.BooksEbook);
            AddCategoryMapping("65", TorznabCatType.BooksEbook);
            AddCategoryMapping("103", TorznabCatType.BooksEbook);
            AddCategoryMapping("115", TorznabCatType.BooksEbook);
            AddCategoryMapping("66", TorznabCatType.BooksEbook);
            AddCategoryMapping("78", TorznabCatType.BooksEbook);
            AddCategoryMapping("138", TorznabCatType.BooksEbook);
            AddCategoryMapping("67", TorznabCatType.BooksEbook);
            AddCategoryMapping("92", TorznabCatType.BooksEbook);
            AddCategoryMapping("118", TorznabCatType.BooksEbook);
            AddCategoryMapping("94", TorznabCatType.BooksEbook);
            AddCategoryMapping("120", TorznabCatType.BooksEbook);
            AddCategoryMapping("95", TorznabCatType.BooksEbook);
            AddCategoryMapping("81", TorznabCatType.BooksEbook);
            AddCategoryMapping("82", TorznabCatType.BooksEbook);
            AddCategoryMapping("68", TorznabCatType.BooksEbook);
            AddCategoryMapping("69", TorznabCatType.BooksEbook);
            AddCategoryMapping("75", TorznabCatType.BooksEbook);
            AddCategoryMapping("96", TorznabCatType.BooksEbook);
            AddCategoryMapping("104", TorznabCatType.BooksEbook);
            AddCategoryMapping("109", TorznabCatType.BooksEbook);
            AddCategoryMapping("70", TorznabCatType.BooksEbook);
            AddCategoryMapping("112", TorznabCatType.BooksEbook);
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string> {
                { "email", configData.Username.Value },
                { "password", configData.Password.Value },
                { "returnto", "/" }
            };

            var preRequest = await RequestStringWithCookiesAndRetry(LoginUrl, string.Empty);

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, preRequest.Cookies, true, SearchUrl, SiteLink);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("Search Results"), () =>
            {
                CQ dom = result.Content;
                var errorMessage = dom["table.main table td.text"].Text().Trim().Replace("\n\t", " ");
                throw new ExceptionWithConfigData(errorMessage, configData);
            });

            return IndexerConfigurationStatus.RequiresTesting;
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

            List<string> catList = MapTorznabCapsToTrackers(query);
            if (catList.Any())
            {
                foreach (string cat in catList)
                {
                    qParams.Add("tor[cat][]", cat);
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
            if (response.Status == System.Net.HttpStatusCode.Forbidden)
            {
                // re-login
                await ApplyConfiguration(null);
                response = await RequestStringWithCookiesAndRetry(urlSearch);
            }

            try
            {
                CQ dom = response.Content;
                var rows = dom["table[class='newTorTable'] > tbody > tr[id^=\"tdr\"]"];

                foreach (IDomObject row in rows)
                {
                    CQ torrentData = row.OuterHTML;
                    CQ cells = row.Cq().Find("td");

                    string tid = torrentData.Attr("id").Substring(4);
                    var qTitle = torrentData.Find("a[class='title']").First();
                    string title = qTitle.Text().Trim();
                    var details = new Uri(SiteLink + qTitle.Attr("href"));
                    string author = torrentData.Find("a[class='author']").First().Text().Trim();
                    Uri link = new Uri(SiteLink + "tor/download.php?tid=" + tid); // DL Link is no long available directly, build it ourself
                    long files = ParseUtil.CoerceLong(cells.Elements.ElementAt(4).Cq().Find("a").Text());
                    long size = ReleaseInfo.GetBytes(cells.Elements.ElementAt(4).Cq().Text().Split('[')[1].TrimEnd(']'));
                    int seeders = ParseUtil.CoerceInt(cells.Elements.ElementAt(6).Cq().Find("p").ElementAt(0).Cq().Text());
                    int leechers = ParseUtil.CoerceInt(cells.Elements.ElementAt(6).Cq().Find("p").ElementAt(1).Cq().Text());
                    long grabs = ParseUtil.CoerceLong(cells.Elements.ElementAt(6).Cq().Find("p").ElementAt(2).Cq().Text());
                    bool freeleech = torrentData.Find("img[alt=\"freeleech\"]").Any();

                    string pubDateStr = cells.Elements.ElementAt(5).Cq().Text().Split('[')[0];
                    DateTime publishDate = DateTime.Parse(pubDateStr).ToLocalTime();

                    long category = 0;
                    string cat = torrentData.Find("a[class='newCatLink']").First().Attr("href").Remove(0, "/tor/browse.php?tor[cat][]]=".Length);
                    long.TryParse(cat, out category);

                    var release = new ReleaseInfo();

                    release.Title = String.IsNullOrEmpty(author) ? title : String.Format("{0} by {1}", title, author);
                    release.Guid = link;
                    release.Link = link;
                    release.PublishDate = publishDate;
                    release.Files = files;
                    release.Size = size;
                    release.Description = release.Title;
                    release.Seeders = seeders;
                    release.Peers = seeders + leechers;
                    release.Grabs = grabs;
                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;
                    release.Category = MapTrackerCatToNewznab(category.ToString());
                    release.Comments = details;

                    if (freeleech)
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
