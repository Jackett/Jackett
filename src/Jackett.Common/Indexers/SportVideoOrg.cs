using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    public class SportVideoOrg : BaseCachingWebIndexer
    {
        private readonly Dictionary<string, string> _categoryMap = new Dictionary<string, string>
        {
            {"Basketball", "basketball.html"},
            {"Football", "americanfootball.html"},
            {"Baseball", "baseball.html"},
            {"Soccer", "soccer.html"},
            {"Hockey", "hockey.html"},
            {"Rugby", "rugby.html"},
            {"AFL", "afl.html"},
            {"Other", "other.html"}
        };
        private const string SEARCH_PARAM = "ALL";

        public SportVideoOrg(IIndexerConfigurationService configService, WebClient w, Logger l, IProtectionService ps)
            : base("SportsVideoOrg",
                   description: "Sports Video is a tracker containing AMERICAN FOOTBALL, BASKETBALL, BASKETBALL, GOOTBALL, HOCKEY, RUGBY, AFL & MORE",
                   link: "https://sport-video.org.ua/",
                   caps: new TorznabCapabilities(),
                   configService: configService,
                   client: w,
                   logger: l,
                   p: ps,
                   configData: new ConfigurationData())
        {
            Encoding = Encoding.GetEncoding("iso-8859-1");
            Language = "en-US";
            Type = "public";

            AddCategoryMapping(32, TorznabCatType.TVSport, "Basketball");
            AddCategoryMapping(42, TorznabCatType.TVSport, "Football");
            AddCategoryMapping(46, TorznabCatType.TVSport, "Hockey");
            AddCategoryMapping(55, TorznabCatType.TVSport, "Baseball");
            AddCategoryMapping(59, TorznabCatType.TVSport, "Soccer");
            AddCategoryMapping(45, TorznabCatType.TVSport, "Other sports");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);

            await ConfigureIfOK(string.Empty, true,
                                () => throw new Exception("Could not find releases from this URL"));

            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            bool foundCache;
            IReadOnlyList<ReleaseInfo> releases = null;
            //Try to use the cache
            lock (cache)
            {
                // Remove old cache items
                CleanCache();

                // Search in cache
                foundCache = cache.Any();
                if (foundCache)
                    releases = cache.First().Results;
            }

            if (!foundCache)
            {
                var concurrentReleases = new ConcurrentBag<ReleaseInfo>();
                await Task.WhenAll(
                    _categoryMap.Values.Select(categoryUrl => ProcessCategoryAsync(categoryUrl, concurrentReleases)));
                releases = concurrentReleases.ToList();
                //Add to the cache
                lock (cache)
                    cache.Add(new CachedQueryResult(SEARCH_PARAM, (List<ReleaseInfo>)releases));
            }

            //Return the Filtered Query
            return releases.Select(s => (ReleaseInfo)s.Clone()).Where(q => query.MatchQueryStringAND(q.Title));
        }

        private async Task ProcessCategoryAsync(string categoryUrl, ConcurrentBag<ReleaseInfo> releases)
        {
            var searchUrl = SiteLink + categoryUrl;

            var results = await RequestStringWithCookies(searchUrl);
            var resultParser = new HtmlParser();
            var searchResultDocument = resultParser.ParseDocument(results.Content);
            var rows = searchResultDocument.QuerySelectorAll("div[id^=wb_LayoutGrid]");
            var navigationRows = searchResultDocument.QuerySelectorAll("ul[id=Pagination2] li a");
            if (navigationRows?.Any() == true)
            {
                //Process the Main Page
                ProcessPage(rows, releases);

                //Loop through the Navigation
                foreach (var navigationRow in navigationRows)
                {
                    var pagedHref = navigationRow.GetAttribute("href");
                    var pagedText = navigationRow.TextContent;

                    //Skip Any Navigation Texts that are Prev, Next or an actual category
                    if (!pagedText.ToUpper().Contains("PREV") && !pagedText.ToUpper().Contains("NEXT") &&
                        !_categoryMap.ContainsValue(pagedHref.ToLower().Replace("./", string.Empty)))
                    {
                        //Update to process sub page
                        searchUrl = SiteLink + pagedHref.Replace("./", string.Empty);
                        results = await RequestStringWithCookies(searchUrl);
                        searchResultDocument = resultParser.ParseDocument(results.Content);
                        rows = searchResultDocument.QuerySelectorAll("div[id^=wb_LayoutGrid]");

                        //Process the sub page
                        ProcessPage(rows, releases);
                    }
                }
            }
        }

        /// <summary>
        /// Processes the page & parse into a release
        /// </summary>
        /// <param name="rowData"></param>
        /// <param name="releases"></param>
        private void ProcessPage(IEnumerable<IElement> rowData, ConcurrentBag<ReleaseInfo> releases)
        {
            var loopCount = 0;
            var navLoopCount = 0;
            var foundNav = false;
            var innerData = false;
            foreach (var rowLink in rowData)
            {
                //Try to Determine the Nav & Build a list
                if (foundNav == false)
                {
                    var navigationRows = rowLink.QuerySelectorAll("ul[id^=Pagination]");
                    if (navigationRows?.Any() == true)
                    {
                        foundNav = true;
                        navLoopCount = loopCount;
                    }
                }

                //Loop Through the Releases
                if ((foundNav && loopCount == (navLoopCount + 2)) || innerData)
                {
                    innerData = true;
                    var titleNode = rowLink.QuerySelectorAll("div[id^=wb_Text] strong");

                    //If we haven't found a node then skip it
                    if (titleNode?.Any() != true)
                        continue;

                    var title = titleNode.FirstOrDefault()?.TextContent;
                    var torrentNode = rowLink.QuerySelectorAll("a[href$=torrent]");
                    var torrentLink = torrentNode.FirstOrDefault()?.GetAttribute("href").Replace("./", string.Empty);

                    var link = new Uri(SiteLink + torrentLink);

                    //Try to parse the date from the title
                    DateTime releaseDate;
                    try
                    {
                        releaseDate = DateTimeUtil.FromUnknown(title);
                    }
                    catch (Exception)
                    {
                        releaseDate = DateTime.Now;
                    }

                    var release = new ReleaseInfo
                    {
                        MinimumRatio = 1,
                        MinimumSeedTime = 172800, // 48 hours
                        Title = title,
                        Category = MapTrackerCatToNewznab("32"),
                        Guid = link,
                        Comments = link,
                        PublishDate = releaseDate,
                        Size = 2147483648, // 2 GB
                        Seeders = 5,
                        Peers = 5,
                        DownloadVolumeFactor = 0,
                        UploadVolumeFactor = 1,
                        Link = link
                    };
                    releases.Add(release);
                }

                loopCount++;
            }
        }
    }
}
