using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    internal class HorribleSubs : BaseWebIndexer
    {
        private string ApiEndpoint => SiteLink + "api.php";

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

            await ConfigureIfOK(string.Empty, releases.Any(), () =>
            {
                throw new Exception("Could not find releases from this URL");
            });

            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var ResultParser = new HtmlParser();
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();
            var queryCollection = new NameValueCollection();


            if (string.IsNullOrWhiteSpace(searchString))
            {
                return await PerformLatestQuery(query);
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
                var dom = ResultParser.ParseDocument(response.Content);
                var resultLinks = dom.QuerySelectorAll("ul > li > a");
                var uniqueShowLinks = new HashSet<string>();
                foreach (var resultLink in resultLinks)
                {
                    var href = SiteLink + resultLink.Attributes["href"].Value.Substring(1); // = https://horriblesubs.info/shows/boruto-naruto-next-generations#71
                    var showUrl = href.Substring(0, href.LastIndexOf("#"));
                    uniqueShowLinks.Add(showUrl);
                }
                foreach (var showLink in uniqueShowLinks)
                {
                    var showReleases = await GetReleases(showLink, latestOnly: false);
                    releases.AddRange(showReleases);
                }
            }
            catch (Exception ex)
            {
                OnParseError(response.Content, ex);
            }

            return releases;
        }

        private async Task<IEnumerable<ReleaseInfo>> PerformLatestQuery(TorznabQuery query)
        {
            var ResultParser = new HtmlParser();
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();
            var queryCollection = new NameValueCollection
            {
                { "method", "getlatest" }
            };

            var searchUrl = ApiEndpoint + "?" + queryCollection.GetQueryString();
            var response = await RequestStringWithCookiesAndRetry(searchUrl, string.Empty);

            try
            {
                if (response.Content.Contains("Nothing was found"))
                {
                    return releases.ToArray();
                }

                var dom = ResultParser.ParseDocument(response.Content);
                var latestresults = dom.QuerySelectorAll("ul > li > a");
                foreach (var resultLink in latestresults)
                {
                    var href = SiteLink + resultLink.Attributes["href"].Value.Substring(1); // = https://horriblesubs.info/shows/boruto-naruto-next-generations#71
                    var episodeNumber = href.Substring(href.LastIndexOf("#") + 1); // = 71
                    var showUrl = href.Substring(0, href.LastIndexOf("#"));
                    var showReleases = await GetReleases(showUrl, latestOnly: true, titleContains: episodeNumber);
                    releases.AddRange(showReleases);
                }
            }
            catch (Exception ex)
            {
                OnParseError(response.Content, ex);
            }

            return releases;
        }

        private async Task<IEnumerable<ReleaseInfo>> GetReleases(string ResultURL, bool latestOnly, string titleContains = null)
        {
            var releases = new List<ReleaseInfo>();
            var ResultParser = new HtmlParser();
            try
            {
                var showPageResponse = await RequestStringWithCookiesAndRetry(ResultURL, string.Empty);
                await FollowIfRedirect(showPageResponse);

                var match = Regex.Match(showPageResponse.Content, "(var hs_showid = )([0-9]*)(;)", RegexOptions.IgnoreCase);
                if (match.Success == false)
                {
                    return releases;
                }

                var ShowID = int.Parse(match.Groups[2].Value);

                var apiUrls = new string[] {
                    ApiEndpoint + "?method=getshows&type=batch&showid=" + ShowID, //https://horriblesubs.info/api.php?method=getshows&type=batch&showid=1194
                    ApiEndpoint + "?method=getshows&type=show&showid=" + ShowID //https://horriblesubs.info/api.php?method=getshows&type=show&showid=869
                };

                var releaserows = new List<AngleSharp.Dom.IElement>();
                foreach (var apiUrl in apiUrls)
                {
                    var nextId = 0;
                    while (true)
                    {
                        var showAPIResponse = await RequestStringWithCookiesAndRetry(apiUrl + "&nextid=" + nextId, string.Empty);
                        var showAPIdom = ResultParser.ParseDocument(showAPIResponse.Content);
                        var releaseRowResults = showAPIdom.QuerySelectorAll("div.rls-info-container");
                        releaserows.AddRange(releaseRowResults);
                        nextId++;

                        if (releaseRowResults.Length == 0 || latestOnly)
                        {
                            break;
                        }
                    }
                }

                foreach (var releaserow in releaserows)
                {
                    var dateStr = releaserow.QuerySelector(".rls-date").TextContent.Trim();
                    var title = releaserow.FirstChild.TextContent;
                    title = title.Replace("SD720p1080p", "");
                    title = title.Replace(dateStr, "");

                    if (!string.IsNullOrWhiteSpace(titleContains) && !title.Contains(titleContains))
                    {
                        continue;
                    }

                    // Ensure fansub group name is present in the title
                    // This is needed for things like configuring tag restrictions in Sonarr
                    if (!title.Contains("[HorribleSubs]"))
                    {
                        title = "[HorribleSubs] " + title;
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
                        var release = new ReleaseInfo
                        {
                            PublishDate = releasedate,
                            Files = 1,
                            Category = new List<int> { TorznabCatType.TVAnime.ID },
                            Size = 524288000,
                            Seeders = 1,
                            Peers = 2,
                            MinimumRatio = 1,
                            MinimumSeedTime = 172800, // 48 hours
                            DownloadVolumeFactor = 0,
                            UploadVolumeFactor = 1
                        };
                        release.Title = string.Format("{0} [480p]", title);
                        if (p480.QuerySelector(".hs-torrent-link > a") != null)
                        {
                            release.Link = new Uri(p480.QuerySelector(".hs-torrent-link > a").GetAttribute("href"));
                            release.Comments = new Uri(release.Link.AbsoluteUri.Replace("/torrent", string.Empty));
                            release.Guid = release.Link;
                        }
                        if (p480.QuerySelector(".hs-magnet-link > a") != null)
                        {
                            release.MagnetUri = new Uri(p480.QuerySelector(".hs-magnet-link > a").GetAttribute("href"));
                            release.Guid = release.MagnetUri;
                        }
                        releases.Add(release);
                    }

                    var p720 = releaserow.QuerySelector(".link-720p");

                    if (p720 != null)
                    {
                        var release = new ReleaseInfo
                        {
                            PublishDate = releasedate,
                            Files = 1,
                            Category = new List<int> { TorznabCatType.TVAnime.ID },
                            Size = 524288000,
                            Seeders = 1,
                            Peers = 2,
                            MinimumRatio = 1,
                            MinimumSeedTime = 172800, // 48 hours
                            DownloadVolumeFactor = 0,
                            UploadVolumeFactor = 1
                        };
                        release.Title = string.Format("{0} [720p]", title);
                        if (p720.QuerySelector(".hs-torrent-link > a") != null)
                        {
                            release.Link = new Uri(p720.QuerySelector(".hs-torrent-link > a").GetAttribute("href"));
                            release.Comments = new Uri(release.Link.AbsoluteUri.Replace("/torrent", string.Empty));
                            release.Guid = release.Link;
                        }
                        if (p720.QuerySelector(".hs-magnet-link > a") != null)
                        {
                            release.MagnetUri = new Uri(p720.QuerySelector(".hs-magnet-link > a").GetAttribute("href"));
                            release.Guid = release.MagnetUri;
                        }
                        releases.Add(release);
                    }

                    var p1080 = releaserow.QuerySelector(".link-1080p");

                    if (p1080 != null)
                    {
                        var release = new ReleaseInfo
                        {
                            PublishDate = releasedate,
                            Files = 1,
                            Category = new List<int> { TorznabCatType.TVAnime.ID },
                            Size = 524288000,
                            Seeders = 1,
                            Peers = 2,
                            MinimumRatio = 1,
                            MinimumSeedTime = 172800, // 48 hours
                            DownloadVolumeFactor = 0,
                            UploadVolumeFactor = 1
                        };
                        release.Title = string.Format("{0} [1080p]", title);
                        if (p1080.QuerySelector(".hs-torrent-link > a") != null)
                        {
                            release.Link = new Uri(p1080.QuerySelector(".hs-torrent-link > a").GetAttribute("href"));
                            release.Comments = new Uri(release.Link.AbsoluteUri.Replace("/torrent", string.Empty));
                            release.Guid = release.Link;
                        }
                        if (p1080.QuerySelector(".hs-magnet-link > a") != null)
                        {
                            release.MagnetUri = new Uri(p1080.QuerySelector(".hs-magnet-link > a").GetAttribute("href"));
                            release.Guid = release.MagnetUri;
                        }
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
    }
}
