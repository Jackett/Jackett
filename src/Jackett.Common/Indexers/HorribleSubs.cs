using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
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

        public override string[] LegacySiteLinks { get; protected set; } = {
            "http://horriblesubs.info/"
        };

        public HorribleSubs(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base("Horrible Subs",
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

            await ConfigureIfOK(string.Empty, releases.Any(),
                                () => throw new Exception("Could not find releases from this URL"));

            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.GetQueryString();

            if (string.IsNullOrWhiteSpace(searchString))
                return await PerformLatestQuery();

            // ignore ' (e.g. search for america's Next Top Model)
            searchString = searchString.Replace("'", "");
            var queryCollection = new NameValueCollection
            {
                {"method", "search"},
                {"value", searchString}
            };

            var searchUrl = ApiEndpoint + "?" + queryCollection.GetQueryString();
            var response = await RequestStringWithCookiesAndRetry(searchUrl, string.Empty);

            try
            {
                if (response.Content.Contains("Nothing was found"))
                    return releases;

                var parser = new HtmlParser();
                var dom = parser.ParseDocument(response.Content);
                var resultLinks = dom.QuerySelectorAll("ul > li > a");
                var uniqueShowLinks = new HashSet<string>();
                foreach (var resultLink in resultLinks)
                {
                    var href = SiteLink + resultLink.GetAttribute("href").TrimStart('/'); // = https://horriblesubs.info/shows/boruto-naruto-next-generations#71
                    var showUrl = href.Split('#').First();
                    uniqueShowLinks.Add(showUrl);
                }
                foreach (var showLink in uniqueShowLinks)
                {
                    var showReleases = await GetReleases(showLink, false);
                    releases.AddRange(showReleases);
                }
            }
            catch (Exception ex)
            {
                OnParseError(response.Content, ex);
            }

            return releases;
        }

        private async Task<IEnumerable<ReleaseInfo>> PerformLatestQuery()
        {
            var releases = new List<ReleaseInfo>();
            var queryCollection = new NameValueCollection
            {
                { "method", "getlatest" }
            };

            var searchUrl = ApiEndpoint + "?" + queryCollection.GetQueryString();
            var response = await RequestStringWithCookiesAndRetry(searchUrl, string.Empty);

            try
            {
                if (response.Content.Contains("Nothing was found"))
                    return releases;

                var parser = new HtmlParser();
                var dom = parser.ParseDocument(response.Content);
                var latestresults = dom.QuerySelectorAll("ul > li > a");
                foreach (var resultLink in latestresults)
                {
                    // href = https://horriblesubs.info/shows/boruto-naruto-next-generations#71
                    var href = SiteLink + resultLink.GetAttribute("href").TrimStart('/');
                    var hrefParts = href.Split('#');
                    var episodeNumber = hrefParts.Last(); // episodeNumber = 71
                    var showUrl = hrefParts.First();
                    var showReleases = await GetReleases(showUrl, true, episodeNumber);
                    releases.AddRange(showReleases);
                }
            }
            catch (Exception ex)
            {
                OnParseError(response.Content, ex);
            }

            return releases;
        }

        private async Task<IEnumerable<ReleaseInfo>> GetReleases(string resultUrl, bool latestOnly, string titleContains = null)
        {
            var releases = new List<ReleaseInfo>();
            var parser = new HtmlParser();

            var response = await RequestStringWithCookiesAndRetry(resultUrl, string.Empty);
            await FollowIfRedirect(response);

            try
            {
                var match = Regex.Match(response.Content, "(var hs_showid = )([0-9]*)(;)", RegexOptions.IgnoreCase);
                if (match.Success == false)
                    return releases;

                var showId = int.Parse(match.Groups[2].Value);

                var apiUrls = new [] {
                    ApiEndpoint + "?method=getshows&type=batch&showid=" + showId, // https://horriblesubs.info/api.php?method=getshows&type=batch&showid=1194
                    ApiEndpoint + "?method=getshows&type=show&showid=" + showId // https://horriblesubs.info/api.php?method=getshows&type=show&showid=869
                };

                var rows = new List<IElement>();
                foreach (var apiUrl in apiUrls)
                {
                    var nextId = 0;
                    while (true)
                    {
                        var showApiResponse = await RequestStringWithCookiesAndRetry(apiUrl + "&nextid=" + nextId, string.Empty);
                        var showApiDom = parser.ParseDocument(showApiResponse.Content);
                        var releaseRowResults = showApiDom.QuerySelectorAll("div.rls-info-container");
                        rows.AddRange(releaseRowResults);
                        nextId++;

                        if (releaseRowResults.Length == 0 || latestOnly)
                            break;
                    }
                }

                foreach (var row in rows)
                {
                    var dateStr = row.QuerySelector(".rls-date").TextContent.Trim();

                    var qTitle = row.QuerySelector("a");
                    var title = qTitle.TextContent;
                    title = title.Replace("SD720p1080p", "");
                    title = title.Replace(dateStr, "");

                    if (!string.IsNullOrWhiteSpace(titleContains) && !title.Contains(titleContains))
                        continue;

                    // Ensure fansub group name is present in the title
                    // This is needed for things like configuring tag restrictions in Sonarr
                    if (!title.Contains("[HorribleSubs]"))
                        title = "[HorribleSubs] " + title;

                    var episode = qTitle.QuerySelector("strong")?.TextContent;
                    var comments = new Uri(resultUrl + (episode != null ? "#" + episode : ""));

                    var publishDate = dateStr switch
                    {
                        "Today" => DateTime.Today,
                        "Yesterday" => DateTime.Today.AddDays(-1),
                        _ => DateTime.SpecifyKind(
                                         DateTime.ParseExact(dateStr, "MM/dd/yy", CultureInfo.InvariantCulture),
                                         DateTimeKind.Utc)
                                     .ToLocalTime()
                    };

                    var p480 = row.QuerySelector(".link-480p");
                    if (p480 != null) // size = 400 MB
                        releases.Add(MakeRelease(p480, $"{title} [480p]", 419430400, comments, publishDate));

                    var p720 = row.QuerySelector(".link-720p");
                    if (p720 != null) // size 700 MB
                        releases.Add(MakeRelease(p720, $"{title} [720p]", 734003200, comments, publishDate));

                    var p1080 = row.QuerySelector(".link-1080p");
                    if (p1080 != null) // size 1.4 GB
                        releases.Add(MakeRelease(p1080, $"{title} [1080p]", 1503238553, comments, publishDate));
                }
            }
            catch (Exception ex)
            {
                OnParseError(response.Content, ex);
            }
            return releases;
        }

        private ReleaseInfo MakeRelease(IElement releaseSelector, string title, long size, Uri comments,
                                        DateTime publishDate)
        {
            Uri link = null;
            Uri magnet = null;
            Uri guid = null;
            if (releaseSelector.QuerySelector(".hs-magnet-link > a") != null)
            {
                magnet = new Uri(releaseSelector.QuerySelector(".hs-magnet-link > a").GetAttribute("href"));
                guid = magnet;
            }
            if (releaseSelector.QuerySelector(".hs-torrent-link > a") != null)
            {
                link = new Uri(releaseSelector.QuerySelector(".hs-torrent-link > a").GetAttribute("href"));
                guid = link;
            }

            var release = new ReleaseInfo
            {
                Title = title,
                Link = link,
                MagnetUri = magnet,
                Guid = guid,
                Comments = comments,
                PublishDate = publishDate,
                Files = 1,
                Category = new List<int> { TorznabCatType.TVAnime.ID },
                Size = size,
                Seeders = 1,
                Peers = 2,
                MinimumRatio = 1,
                MinimumSeedTime = 172800, // 48 hours
                DownloadVolumeFactor = 0,
                UploadVolumeFactor = 1
            };
            return release;
        }
    }
}
