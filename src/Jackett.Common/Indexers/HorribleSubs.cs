using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using System.Text.RegularExpressions;
using AngleSharp.Parser.Html;

namespace Jackett.Common.Indexers
{
    class HorribleSubs : BaseWebIndexer
    {
        private string ApiEndpoint { get { return SiteLink + "api.php"; } }

        public override string[] LegacySiteLinks { get; protected set; } = new string[] {
            "http://horriblesubs.info/"
        };

        public HorribleSubs(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
    : base(name: "Horrible Subs",
        description: "HorribleSubs - So bad yet so good",
        link: "https://horriblesubs.info/",
        caps: new TorznabCapabilities(TorznabCatType.TVAnime),
        configService: configService,
        client: wc,
        logger: l,
        p: ps,
        configData: new ConfigurationData())
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "public";
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);
            var releases = await PerformQuery(new TorznabQuery());

            await ConfigureIfOK(string.Empty, releases.Count() > 0, () =>
            {
                throw new Exception("Could not find releases from this URL");
            });

            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            return await PerformQuery(query, 0);
        }

        private async Task<IEnumerable<ReleaseInfo>> PerformLatestQuery(TorznabQuery query, int attempts)
        {
            var ResultParser = new HtmlParser();
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();
            var queryCollection = new NameValueCollection();

            queryCollection.Add("method", "getlatest");

            var searchUrl = ApiEndpoint + "?" + queryCollection.GetQueryString();
            var response = await RequestStringWithCookiesAndRetry(searchUrl, string.Empty);

            try
            {
                if (response.Content.Contains("Nothing was found"))
                {
                    return releases.ToArray();
                }

                var dom = ResultParser.Parse(response.Content);
                var latestresults = dom.QuerySelectorAll("ul > li > a");
                foreach (var row in latestresults)
                {
                    var href = SiteLink + row.Attributes["href"].Value.Substring(1);
                    var showrels = await GetRelease(href);
                    releases.AddRange(showrels);
                }

            }
            catch (Exception ex)
            {
                OnParseError(response.Content, ex);
            }

            return releases;
        }

        private async Task<IEnumerable<ReleaseInfo>> GetRelease(string ResultURL)
        {
            var releases = new List<ReleaseInfo>();
            var ResultParser = new HtmlParser();
            try
            {
                var episodeno = ResultURL.Substring(ResultURL.LastIndexOf("#") + 1); // = 71
                ResultURL = ResultURL.Replace("#" + episodeno, ""); // = https://horriblesubs.info/shows/boruto-naruto-next-generations

                var showPageResponse = await RequestStringWithCookiesAndRetry(ResultURL, string.Empty);
                await FollowIfRedirect(showPageResponse);

                Match match = Regex.Match(showPageResponse.Content, "(var hs_showid = )([0-9]*)(;)", RegexOptions.IgnoreCase);
                if (match.Success == false)
                {
                    return releases;
                }

                int ShowID = int.Parse(match.Groups[2].Value);

                string showAPIURL = ApiEndpoint + "?method=getshows&type=show&showid=" + ShowID; //https://horriblesubs.info/api.php?method=getshows&type=show&showid=869
                var showAPIResponse = await RequestStringWithCookiesAndRetry(showAPIURL, string.Empty);


                var showAPIdom = ResultParser.Parse(showAPIResponse.Content);
                var releaserows = showAPIdom.QuerySelectorAll("div.rls-info-container");

                foreach (var releaserow in releaserows)
                {
                    string dateStr = releaserow.QuerySelector(".rls-date").TextContent.Trim();
                    string title = releaserow.FirstChild.TextContent;
                    title = title.Replace("SD720p1080p", "");
                    title = title.Replace(dateStr, "");

                    if (title.Contains(episodeno) == false)
                    {
                        continue;
                    }

                    DateTime releasedate;
                    if (dateStr == "Today")
                    {
                        releasedate = DateTime.Today;
                    }
                    else if (dateStr == "Yesterday")
                    {
                        releasedate = DateTime.Today.AddDays(-1);
                    }
                    else
                    {
                        releasedate = DateTime.SpecifyKind(DateTime.ParseExact(dateStr, "MM/dd/yy", CultureInfo.InvariantCulture), DateTimeKind.Utc).ToLocalTime();
                    }
                    
                    var p480 = releaserow.QuerySelector(".link-480p");

                    if (p480 != null)
                    {
                        var release = new ReleaseInfo();
                        release.Title = string.Format("{0} [480p]", title);
                        release.PublishDate = releasedate;
                        release.Link = new Uri(p480.QuerySelector(".hs-torrent-link > a").GetAttribute("href"));
                        release.MagnetUri = new Uri(p480.QuerySelector(".hs-magnet-link > a").GetAttribute("href"));
                        release.Files = 1;
                        release.Category = new List<int> { TorznabCatType.TVAnime.ID };
                        release.Size = 524288000;
                        release.Seeders = 999;
                        release.Peers = 1998;
                        release.DownloadVolumeFactor = 0;
                        release.UploadVolumeFactor = 1;
                        releases.Add(release);
                    }

                    var p720 = releaserow.QuerySelector(".link-720p");

                    if (p720 != null)
                    {
                        var release = new ReleaseInfo();
                        release.Title = string.Format("{0} [720p]", title);
                        release.PublishDate = releasedate;
                        release.Link = new Uri(p720.QuerySelector(".hs-torrent-link > a").GetAttribute("href"));
                        release.MagnetUri = new Uri(p720.QuerySelector(".hs-magnet-link > a").GetAttribute("href"));
                        release.Files = 1;
                        release.Category = new List<int> { TorznabCatType.TVAnime.ID };
                        release.Size = 524288000;
                        release.Seeders = 999;
                        release.Peers = 1998;
                        release.DownloadVolumeFactor = 0;
                        release.UploadVolumeFactor = 1;
                        releases.Add(release);
                    }

                    var p1080 = releaserow.QuerySelector(".link-1080p");

                    if (p1080 != null)
                    {
                        var release = new ReleaseInfo();
                        release.Title = string.Format("{0} [1080p]", title);
                        release.PublishDate = releasedate;
                        release.Link = new Uri(p1080.QuerySelector(".hs-torrent-link > a").GetAttribute("href"));
                        release.MagnetUri = new Uri(p1080.QuerySelector(".hs-magnet-link > a").GetAttribute("href"));
                        release.Files = 1;
                        release.Category = new List<int> { TorznabCatType.TVAnime.ID };
                        release.Size = 524288000;
                        release.Seeders = 999;
                        release.Peers = 1998;
                        release.DownloadVolumeFactor = 0;
                        release.UploadVolumeFactor = 1;
                        releases.Add(release);
                    }

                }
            }
            catch (Exception ex)
            {
                OnParseError("", ex);
            }
            return releases;
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query, int attempts)
        {
            var ResultParser = new HtmlParser();
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();
            var queryCollection = new NameValueCollection();


            if (string.IsNullOrWhiteSpace(searchString))
            {
                return await PerformLatestQuery(query, attempts);
            }
            else
            {
                queryCollection.Add("method", "search");

                searchString = searchString.Replace("'", ""); // ignore ' (e.g. search for america's Next Top Model)
                queryCollection.Add("value", searchString);
            }

            var searchUrl = ApiEndpoint + "?" + queryCollection.GetQueryString();
            var response = await RequestStringWithCookiesAndRetry(searchUrl, string.Empty);

            try
            {
                if (response.Content.Contains("Nothing was found"))
                {
                    return releases.ToArray();
                }
                var dom = ResultParser.Parse(response.Content);
                var showlinks = dom.QuerySelectorAll("ul > li > a");
                foreach (var showlink in showlinks)
                {
                    var href = SiteLink + showlink.Attributes["href"].Value.Substring(1); // = https://horriblesubs.info/shows/boruto-naruto-next-generations#71

                    var showrels = await GetRelease(href);
                    releases.AddRange(showrels);


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
