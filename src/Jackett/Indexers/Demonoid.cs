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
    public class Demonoid : BaseIndexer, IIndexer
    {
        private string LoginUrl { get { return SiteLink + "account_handler.php"; } }
        private string SearchUrl { get { return SiteLink + "files/?category={0}&subcategory=All&quality=All&seeded=0&to=1&query={1}"; } }

        new ConfigurationDataBasicLogin configData
        {
            get { return (ConfigurationDataBasicLogin)base.configData; }
            set { base.configData = value; }
        }

        public Demonoid(IIndexerManagerService i, Logger l, IWebClient wc, IProtectionService ps)
            : base(name: "Demonoid",
                description: "Demonoid",
                link: "http://www.dnoid.me/",
                caps: TorznabUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLogin())
        {
            AddCategoryMapping(3, TorznabCatType.TV);
            AddCategoryMapping(3, TorznabCatType.TVSD);
            AddCategoryMapping(3, TorznabCatType.TVHD);
            AddCategoryMapping(1, TorznabCatType.Movies);
        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);
            var pairs = new Dictionary<string, string> {
                { "nickname", configData.Username.Value },
                { "password", configData.Password.Value },
                { "returnpath", "/" },
                { "withq", "0" },
                { "Submit", "Submit" }
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, SiteLink);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("user_control_panel.php"), () =>
            {
                CQ dom = result.Content;
                string errorMessage = dom["form[id='bb_code_form']"].Parent().Find("font[class='red']").Text();
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
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

                    var qLink = rowA.ChildElements.ElementAt(1).FirstElementChild.Cq();
                    release.Title = qLink.Text().Trim();
                    release.Description = release.Title;

                    release.Comments = new Uri(SiteLink + qLink.Attr("href"));
                    release.Guid = release.Comments;

                    var qDownload = rowB.ChildElements.ElementAt(2).ChildElements.ElementAt(0).Cq();
                    release.Link = new Uri(SiteLink + qDownload.Attr("href"));

                    var sizeStr = rowB.ChildElements.ElementAt(3).Cq().Text();
                    release.Size = ReleaseInfo.GetBytes(sizeStr);

                    release.Seeders = ParseUtil.CoerceInt(rowB.ChildElements.ElementAt(6).Cq().Text());
                    release.Peers = ParseUtil.CoerceInt(rowB.ChildElements.ElementAt(6).Cq().Text()) + release.Seeders;

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
