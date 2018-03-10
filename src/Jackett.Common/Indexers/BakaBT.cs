using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CsQuery;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    public class BakaBT : BaseWebIndexer
    {
        public string SearchUrl { get { return SiteLink + "browse.php?only=0&incomplete=1&lossless=1&hd=1&multiaudio=1&bonus=1&reorder=1&q="; } }
        public string LoginUrl { get { return SiteLink + "login.php"; } }
        public string id = "bakabt";

        private new ConfigurationDataBasicLogin configData
        {
            get { return (ConfigurationDataBasicLogin)base.configData; }
            set { base.configData = value; }
        }

        public BakaBT(IIndexerConfigurationService configService, Utils.Clients.WebClient wc, Logger l, IProtectionService ps)
            : base(name: "BakaBT",
                description: "Anime Comunity",
                link: "https://bakabt.me/",
                caps: new TorznabCapabilities(TorznabCatType.TVAnime),
                configService: configService,
                client: wc,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLogin())
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var loginForm = await webclient.GetString(new Utils.Clients.WebRequest()
            {
                Url = LoginUrl,
                Type = RequestType.GET
            });

            var pairs = new Dictionary<string, string> {
                { "username", configData.Username.Value },
                { "password", configData.Password.Value },
                { "returnto", "/index.php" }
            };

            var response = await RequestLoginAndFollowRedirect(LoginUrl, pairs, loginForm.Cookies, true, null, SiteLink);
            var responseContent = response.Content;
            await ConfigureIfOK(response.Cookies, responseContent.Contains("<a href=\"logout.php\">Logout</a>"), () =>
                {
                    CQ dom = responseContent;
                    var messageEl = dom[".error"].First();
                    var errorMessage = messageEl.Text().Trim();
                    throw new ExceptionWithConfigData(errorMessage, configData);
                });

            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            // This tracker only deals with full seasons so chop off the episode/season number if we have it D:
            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var splitindex = query.SearchTerm.LastIndexOf(' ');
                if (splitindex > -1)
                    query.SearchTerm = query.SearchTerm.Substring(0, splitindex);
            }

            var releases = new List<ReleaseInfo>();
            var searchString = query.SanitizedSearchTerm;
            var episodeSearchUrl = SearchUrl + WebUtility.UrlEncode(searchString);
            var response = await RequestStringWithCookiesAndRetry(episodeSearchUrl);

            try
            {
                CQ dom = response.Content;
                var rows = dom[".torrents tr.torrent, .torrents tr.torrent_alt"];

                foreach (var row in rows)
                {
                    var qRow = row.Cq();
                    var qTitleLink = qRow.Find("a.title, a.alt_title").First();
                    var title = qTitleLink.Text().Trim();

                    // Insert before the release info
                    var taidx = title.IndexOf('(');
                    var tbidx = title.IndexOf('[');

                    if (taidx == -1)
                        taidx = title.Length;

                    if (tbidx == -1)
                        tbidx = title.Length;
                    var titleSplit = Math.Min(taidx, tbidx);
                    var titleSeries = title.Substring(0, titleSplit);
                    var releaseInfo = title.Substring(titleSplit);

                    // For each over each pipe deliminated name
                    foreach (var name in titleSeries.Split("|".ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
                    {
                        var release = new ReleaseInfo();

                        release.Title = (name + releaseInfo).Trim();
                        // Ensure the season is defined as this tracker only deals with full seasons
                        if (release.Title.IndexOf("Season") == -1)
                        {
                            // Insert before the release info
                            var aidx = release.Title.IndexOf('(');
                            var bidx = release.Title.IndexOf('[');

                            if (aidx == -1)
                                aidx = release.Title.Length;

                            if (bidx == -1)
                                bidx = release.Title.Length;

                            var insertPoint = Math.Min(aidx, bidx);
                            release.Title = release.Title.Substring(0, insertPoint) + "Season 1 " + release.Title.Substring(insertPoint);
                        }

                        release.Description = release.Title;
                        release.Guid = new Uri(SiteLink + qTitleLink.Attr("href"));
                        release.Comments = release.Guid;

                        release.Link = new Uri(SiteLink + qRow.Find(".peers a").First().Attr("href"));

                        release.Seeders = int.Parse(qRow.Find(".peers a").Get(0).InnerText);
                        release.Peers = release.Seeders + int.Parse(qRow.Find(".peers a").Get(1).InnerText);

                        release.MinimumRatio = 1;

                        var size = qRow.Find(".size").First().Text();
                        release.Size = ReleaseInfo.GetBytes(size);

                        //22 Jul 15
                        var dateStr = qRow.Find(".added").First().Text().Replace("'", string.Empty);
                        if (dateStr.Split(' ')[0].Length == 1)
                            dateStr = "0" + dateStr;

                        if (string.Equals(dateStr, "yesterday", StringComparison.InvariantCultureIgnoreCase))
                        {
                            release.PublishDate = DateTime.Now.AddDays(-1);
                        }
                        else if (string.Equals(dateStr, "today", StringComparison.InvariantCultureIgnoreCase))
                        {
                            release.PublishDate = DateTime.Now;
                        }
                        else
                        {
                            release.PublishDate = DateTime.ParseExact(dateStr, "dd MMM yy", CultureInfo.InvariantCulture);
                        }

                        releases.Add(release);
                    }
                }
            }
            catch (Exception ex)
            {
                OnParseError(response.Content, ex);
            }

            return releases;
        }

        public override async Task<byte[]> Download(Uri link)
        {
            var downloadPage = await RequestStringWithCookies(link.ToString());
            CQ dom = downloadPage.Content;
            var downloadLink = dom.Find(".download_link").First().Attr("href");

            if (string.IsNullOrWhiteSpace(downloadLink))
            {
                throw new Exception("Unable to find download link.");
            }

            var response = await RequestBytesWithCookies(SiteLink + downloadLink);
            return response.Content;
        }
    }
}