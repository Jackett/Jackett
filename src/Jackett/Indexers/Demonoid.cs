using CsQuery;
using Jackett.Models;
using Jackett.Models.IndexerConfig;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Jackett.Indexers
{
    public class Demonoid : BaseWebIndexer
    {
        private string LoginUrl { get { return SiteLink + "account_handler.php"; } }
        private string SearchUrl { get { return SiteLink + "files/?category={0}&subcategory=All&quality=All&seeded=2&to=1&query={1}&external=2"; } }

        new ConfigurationDataBasicLogin configData
        {
            get { return (ConfigurationDataBasicLogin)base.configData; }
            set { base.configData = value; }
        }

        public Demonoid(IIndexerConfigurationService configService, IWebClient wc, Logger l, IProtectionService ps)
            : base(name: "Demonoid",
                description: "Demonoid",
                link: "https://www.demonoid.pw/",
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                configService: configService,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLogin())
        {
            Encoding = Encoding.GetEncoding("UTF-8");
            Language = "en-us";
            Type = "private";

            AddCategoryMapping(5, TorznabCatType.PC0day, "Applications");
            AddCategoryMapping(17, TorznabCatType.AudioAudiobook, "Audio Books");
            AddCategoryMapping(11, TorznabCatType.Books, "Books");
            AddCategoryMapping(10, TorznabCatType.BooksComics, "Comics");
            AddCategoryMapping(4, TorznabCatType.PCGames, "Games");
            AddCategoryMapping(9, TorznabCatType.TVAnime, "Japanese Anime");
            AddCategoryMapping(6, TorznabCatType.Other, "Miscellaneous");
            AddCategoryMapping(1, TorznabCatType.Movies, "Movies");
            AddCategoryMapping(2, TorznabCatType.Audio, "Music");
            AddCategoryMapping(13, TorznabCatType.AudioVideo, "Music Videos");
            AddCategoryMapping(8, TorznabCatType.Other, "Pictures");
            AddCategoryMapping(3, TorznabCatType.TV, "TV");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string> {
                { "nickname", configData.Username.Value },
                { "password", configData.Password.Value },
                { "returnpath", "/" },
                { "withq", "0" },
                { "Submit", "Submit" }
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, SiteLink, SiteLink);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Cookies.Contains("uid="), () =>
            {
                CQ dom = result.Content;
                string errorMessage = dom["form[id='bb_code_form']"].Parent().Find("font[class='red']").Text();
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var trackerCats = MapTorznabCapsToTrackers(query);
            var cat = (trackerCats.Count == 1 ? trackerCats.ElementAt(0) : "0");
            var episodeSearchUrl = string.Format(SearchUrl, cat, HttpUtility.UrlEncode(query.GetQueryString()));
            var results = await RequestStringWithCookiesAndRetry(episodeSearchUrl);

            
            if (results.Content.Contains("No torrents found"))
            {
                return releases;
            }

            try
            {
                CQ dom = results.Content;
                var rows = dom[".ctable_content_no_pad > table > tbody > tr"].ToArray();
                DateTime lastDateTime = default(DateTime);
                for (var i = 0; i < rows.Length; i++)
                {
                    var rowA = rows[i];
                    var rAlign = rowA.Attributes["align"];
                    if (rAlign == "right" || rAlign == "center")
                        continue;
                    if (rAlign == "left")
                    {
                        // ex: "Monday, Jun 01, 2015", "Monday, Aug 03, 2015"
                        var dateStr = rowA.Cq().Text().Trim().Replace("Added on ", "");
                        if (dateStr.ToLowerInvariant().Contains("today"))
                            lastDateTime = DateTime.Now;
                        else
                            lastDateTime = DateTime.SpecifyKind(DateTime.ParseExact(dateStr, "dddd, MMM dd, yyyy", CultureInfo.InvariantCulture), DateTimeKind.Utc).ToLocalTime();
                        continue;
                    }
                    if (rowA.ChildElements.Count() < 2)
                        continue;

                    var rowB = rows[++i];

                    var release = new ReleaseInfo();
                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800;

                    release.PublishDate = lastDateTime;

                    var catUrl = rowA.ChildElements.ElementAt(0).FirstElementChild.GetAttribute("href");
                    var catId = HttpUtility.ParseQueryString(catUrl).Get("category");
                    release.Category = MapTrackerCatToNewznab(catId);

                    var qLink = rowA.ChildElements.ElementAt(1).FirstElementChild.Cq();
                    release.Title = qLink.Text().Trim();
                    release.Description = rowB.ChildElements.ElementAt(0).Cq().Text();

                    if (release.Category != null && release.Category.Contains(TorznabCatType.Audio.ID))
                    {
                        if (release.Description.Contains("Lossless"))
                            release.Category = new List<int> { TorznabCatType.AudioLossless.ID };
                        else if (release.Description.Contains("MP3"))
                            release.Category = new List<int> { TorznabCatType.AudioMP3.ID };
                        else
                            release.Category = new List<int> { TorznabCatType.AudioOther.ID };
                    }

                    release.Comments = new Uri(SiteLink + qLink.Attr("href"));
                    release.Guid = release.Comments;

                    var qDownload = rowB.ChildElements.ElementAt(2).ChildElements.ElementAt(0).Cq();
                    release.Link = new Uri(qDownload.Attr("href"));

                    var sizeStr = rowB.ChildElements.ElementAt(3).Cq().Text();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    release.Seeders = ParseUtil.CoerceInt(rowB.ChildElements.ElementAt(6).Cq().Text());
                    release.Peers = ParseUtil.CoerceInt(rowB.ChildElements.ElementAt(6).Cq().Text()) + release.Seeders;

                    var grabs = rowB.Cq().Find("td:nth-child(6)").Text();
                    release.Grabs = ParseUtil.CoerceInt(grabs);

                    release.DownloadVolumeFactor = 0; // ratioless
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
