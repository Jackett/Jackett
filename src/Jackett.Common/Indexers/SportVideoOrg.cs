using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    public class SportVideoOrg : BaseCachingWebIndexer
    {
        private new ConfigurationData configData
        {
            get => base.configData;
            set => base.configData = value;
        }

        private Dictionary<string, string> CategoryMap = new Dictionary<string, string>();
        private const string SEARCH_PARAM = "ALL";

        public SportVideoOrg(IIndexerConfigurationService configService, Utils.Clients.WebClient w, Logger l, IProtectionService ps)
            : base(
                name: "SportsVideoOrg",
                description: "Sports Video is a tracker containing AMERICAN FOOTBALL, BASKETBALL, BASKETBALL, GOOTBALL, HOCKEY, RUGBY, AFL & MORE",
                link: "https://sport-video.org.ua/",
                caps: new TorznabCapabilities
                {

                },
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

            CategoryMap.Add("Basketball", "basketball.html");
            CategoryMap.Add("Football", "americanfootball.html");
            CategoryMap.Add("Baseball", "baseball.html");
            CategoryMap.Add("Soccer", "soccer.html");
            CategoryMap.Add("Hockey", "hockey.html");
            CategoryMap.Add("Rugby", "rugby.html");
            CategoryMap.Add("AFL", "afl.html");
            CategoryMap.Add("Other", "other.html");
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
            var searchString = query.GetQueryString();
            var keywordSearch = !string.IsNullOrWhiteSpace(searchString);
            var releases = new List<ReleaseInfo>();

            //Try to use the cache
            lock (cache)
            {
                // Remove old cache items
                CleanCache();

                // Search in cache
                var cachedResult = cache.Where(i => i.Query == SEARCH_PARAM).FirstOrDefault();
                if (cachedResult != null)
                    return cachedResult.Results.Select(s => (ReleaseInfo)s.Clone()).ToArray().Where(q => q.Title.ToUpper().Contains(searchString.ToUpper()));
            }

            ConcurrentBag<ReleaseInfo> concurrentReleases = new ConcurrentBag<ReleaseInfo>();
            List<Task> categoryTasks = new List<Task>();
            foreach (var category in CategoryMap)
            {
                categoryTasks.Add(ProcessCategoryAsync(category.Key, category.Value, concurrentReleases));
            }

            await Task.WhenAll(categoryTasks.ToArray());
            releases = concurrentReleases.ToList();

            //Add to the cache
            lock (cache)
            {
                cache.Add(new CachedQueryResult(SEARCH_PARAM, releases));
            }

            //Return the Filtered Query
            return releases.Select(s => (ReleaseInfo)s.Clone()).ToArray().Where(q => q.Title.ToUpper().Contains(searchString.ToUpper()));
        }

        private async Task ProcessCategoryAsync(string categoryName, string categoryUrl, ConcurrentBag<ReleaseInfo> releases)
        {
            var searchUrl = SiteLink + categoryUrl;
            List<string> pagedNavLinks = new List<string>();

            var results = await RequestStringWithCookies(searchUrl);
            var resultParser = new HtmlParser();
            var searchResultDocument = resultParser.ParseDocument(results.Content);

            var rowSelector = "div[id^=wb_LayoutGrid]";
            var rows = searchResultDocument.QuerySelectorAll(rowSelector);

            var navigationSelector = "ul[id=Pagination2] li a";
            var navigationRows = searchResultDocument.QuerySelectorAll(navigationSelector);
            if (navigationRows != null && navigationRows.Any())
            {
                //Process the Main Page
                ProcessPage(rows, releases);

                //Loop through the Navigation
                foreach (var navigationRow in navigationRows)
                {
                    var pagedHref = navigationRow.Attributes["href"].Value;
                    var pagedText = navigationRow.TextContent;

                    //Skip Any Navigation Texts that are Prev, Next or an actual category
                    if (pagedText.ToUpper().Contains("PREV") || pagedText.ToUpper().Contains("NEXT") || CategoryMap.ContainsValue(pagedHref.ToLower().Replace("./", string.Empty)))
                    {
                        continue;
                    }
                    else
                    {
                        //Update to process sub page
                        searchUrl = SiteLink + pagedHref.Replace("./", string.Empty);
                        results = await RequestStringWithCookies(searchUrl);
                        searchResultDocument = resultParser.ParseDocument(results.Content);
                        rows = searchResultDocument.QuerySelectorAll(rowSelector);

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
        private void ProcessPage(IHtmlCollection<IElement> rowData, ConcurrentBag<ReleaseInfo> releases)
        {
            int loopCount = 0;
            int navLoopCount = 0;
            bool foundNav = false;
            bool innerData = false;
            foreach (var rowLink in rowData)
            {
                //Try to Determine the Nav & Build a list
                if (foundNav == false)
                {
                    var navigationSelector = "ul[id^=Pagination]";
                    var navigationRows = rowLink.QuerySelectorAll(navigationSelector);
                    if (navigationRows != null && navigationRows.Any())
                    {
                        foundNav = true;
                        navLoopCount = loopCount;
                    }
                }

                //Loop Through the Releases
                if ((foundNav && loopCount == (navLoopCount + 2)) || innerData)
                {
                    innerData = true;
                    var titleSelector = "div[id^=wb_Text] strong";
                    var titleNode = rowLink.QuerySelectorAll(titleSelector);

                    //If we haven't found a node then skip it
                    if (titleNode == null || titleNode.Any() == false)
                        continue;

                    var title = titleNode.FirstOrDefault().TextContent;

                    var torrentSelect = "a[href$=torrent]";
                    var torrentNode = rowLink.QuerySelectorAll(torrentSelect);
                    var torrent = torrentNode.FirstOrDefault().Attributes["href"].Value;

                    var link = new Uri(SiteLink + torrent.Replace("./", string.Empty));

                    //Try to parse the date from the title
                    var releaseDate = DateTime.Now;
                    try
                    {
                        releaseDate = DateTimeUtil.FromUnknown(title);
                    }
                    catch (Exception ex)
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
