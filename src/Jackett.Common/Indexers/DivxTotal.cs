using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;

using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json.Linq;
using NLog;
using WebClient = Jackett.Common.Utils.Clients.WebClient;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;

namespace Jackett.Common.Indexers
{
    public class DivxTotal : BaseWebIndexer
    {

        private readonly int MAX_RESULTS_PER_PAGE = 15;
        private readonly int MAX_SEARCH_PAGE_LIMIT = 3;

        public DivxTotal(IIndexerConfigurationService configService, WebClient w, Logger l, IProtectionService ps)
            : base(name: "DivxTotal",
                description: "DivxTotal is a SPANISH site for Movies, TV series and Software",
                link: "https://www.divxtotal.la/",
                caps: new TorznabCapabilities(),
                configService: configService,
                client: w,
                logger: l,
                p: ps,
                configData: new ConfigurationData())
        {
            Encoding = Encoding.UTF8;
            Language = "es-es";
            Type = "public";

            var matchWords = new BoolItem() { Name = "Match words in title", Value = true };
            configData.AddDynamic("MatchWords", matchWords);

            AddCategoryMapping("peliculas", TorznabCatType.MoviesSD);
            AddCategoryMapping("peliculas-hd", TorznabCatType.MoviesSD);
            AddCategoryMapping("peliculas-3-d", TorznabCatType.MoviesHD);
            AddCategoryMapping("peliculas-dvdr", TorznabCatType.MoviesDVD);
            AddCategoryMapping("series", TorznabCatType.TVSD);
            AddCategoryMapping("programas", TorznabCatType.PC);
            AddCategoryMapping("otros", TorznabCatType.OtherMisc);
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
            var releases = new List<ReleaseInfo>();

            var queryStr = query.GetQueryString().Trim();
            var matchWords = ((BoolItem)configData.GetDynamic("MatchWords")).Value;
            matchWords = queryStr != "" && matchWords;

            var qc = new NameValueCollection();
            qc.Add("s", queryStr);

            var page = 1;
            var isLastPage = false;
            do
            {
                var url = SiteLink + "page/" + page + "/?" + qc.GetQueryString();
                var result = await RequestStringWithCookies(url);

                if (result.Status != HttpStatusCode.OK)
                    throw new ExceptionWithConfigData(result.Content, configData);

                try
                {
                    var searchResultParser = new HtmlParser();
                    var doc = searchResultParser.ParseDocument(result.Content);

                    var table = doc.QuerySelector("table.table");
                    var rows = table.QuerySelectorAll("tr");
                    isLastPage = rows.Length -1 < MAX_RESULTS_PER_PAGE; // rows includes the header
                    var isHeader = true;
                    foreach (var row in rows)
                    {
                        if (isHeader) {
                            isHeader = false;
                            continue;
                        }

                        await ParseRelease(releases, row, queryStr, query.Categories, matchWords);
                    }
                }
                catch (Exception ex)
                {
                    OnParseError(result.Content, ex);
                }

                page++; // update page number

            } while (!isLastPage && page <= MAX_SEARCH_PAGE_LIMIT);

            return releases;
        }

        public override async Task<byte[]> Download(Uri link)
        {
            // for tv series we already have the link
            var downloadUrl = link.ToString();
            // for other categories we have to do another step
            if (!downloadUrl.EndsWith(".torrent"))
            {
                var result = await RequestStringWithCookies(downloadUrl);

                if (result.Status != HttpStatusCode.OK)
                    throw new ExceptionWithConfigData(result.Content, configData);

                var searchResultParser = new HtmlParser();
                var doc = searchResultParser.ParseDocument(result.Content);

                var onclick = doc.QuerySelector("a[onclick*=\"/download/torrent.php\"]")
                    .GetAttribute("onclick");
                downloadUrl = OnclickToDownloadLink(onclick);
            }

            var content = await base.Download(new Uri(downloadUrl));
            return content;
        }

        private async Task ParseRelease(List<ReleaseInfo> releases, IElement row, string queryStr, int[] queryCats,
            bool matchWords)
        {
            var anchor = row.QuerySelector("a");
            var commentsLink = anchor.GetAttribute("href");
            var title = anchor.TextContent.Trim();
            var cat = commentsLink.Split('/')[3];
            var categories = MapTrackerCatToNewznab(cat);
            var publishStr = row.QuerySelectorAll("td")[2].TextContent.Trim();
            var publishDate = TryToParseDate(publishStr, DateTime.Now);
            var sizeStr = row.QuerySelectorAll("td")[3].TextContent.Trim();
            var size = TryToParseSize(sizeStr, 0);

            // return results only for requested categories
            if (queryCats.Any() && !queryCats.Contains(categories.First()))
                return;

            // match the words in the query with the titles
            if (matchWords && !CheckTitleMatchWords(queryStr, title))
                return;

            // parsing is different for each category
            if (cat == "series")
            {
                await ParseSeriesRelease(releases, title, commentsLink, cat, publishDate);
            } else
            {
                if (cat == "peliculas")
                    title += " [DVDRip]";
                else if (cat == "peliculas-hd")
                    title += " [HDRip]";
                else if (cat == "programas")
                    title += " [Windows]";
                GenerateRelease(releases, title, commentsLink, commentsLink, cat, publishDate, size);
            }
        }

        private async Task ParseSeriesRelease(List<ReleaseInfo> releases, string title, string commentsLink,
            string cat, DateTime publishDate)
        {
            var result = await RequestStringWithCookies(commentsLink);

            if (result.Status != HttpStatusCode.OK)
                throw new ExceptionWithConfigData(result.Content, configData);

            var searchResultParser = new HtmlParser();
            var doc = searchResultParser.ParseDocument(result.Content);

            var tables = doc.QuerySelectorAll("table.table");
            foreach (var table in tables)
            {
                var rows = table.QuerySelectorAll("tr");
                var isHeader = true;
                foreach (var row in rows)
                {
                    if (isHeader) {
                        isHeader = false;
                        continue;
                    }

                    var anchor = row.QuerySelector("a");
                    var episodeTitle = anchor.TextContent.Trim();
                    var onclick = anchor.GetAttribute("onclick");
                    var downloadLink = OnclickToDownloadLink(onclick);
                    var episodePublishStr = row.QuerySelectorAll("td")[3].TextContent.Trim();
                    var episodePublish = TryToParseDate(episodePublishStr, publishDate);

                    // clean up the title
                    episodeTitle = TryToCleanSeriesTitle(title, episodeTitle);
                    episodeTitle += " [HDTV]";

                    GenerateRelease(releases, episodeTitle, commentsLink, downloadLink, cat, episodePublish, 0);
                }
            }
        }

        private void GenerateRelease(List<ReleaseInfo> releases, string title, string commentsLink, string downloadLink,
            string cat, DateTime publishDate, long size)
        {
            var release = new ReleaseInfo();

            release.Title = title + " [Spanish]";
            release.Comments = new Uri(commentsLink);
            release.Link = new Uri(downloadLink);
            release.Guid = release.Link;

            release.Category = MapTrackerCatToNewznab(cat);
            release.PublishDate = publishDate;
            release.Size = size;

            release.Seeders = 1;
            release.Peers = 2;

            release.MinimumRatio = 0;
            release.MinimumSeedTime = 0;
            release.DownloadVolumeFactor = 0;
            release.UploadVolumeFactor = 1;

            releases.Add(release);
        }

        private string OnclickToDownloadLink(string onclick)
        {
            // onclick="post('/download/torrent.php', {u: 'aHR0cHM6Ly93d3cuZGl2eHRvdGFlbnQ='});"
            var base64EncodedData = onclick.Split('\'')[3];
            var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }

        private bool CheckTitleMatchWords(string queryStr, string title)
        {
            // this code split the words, remove words with 2 letters or less, remove accents and lowercase
            MatchCollection queryMatches = Regex.Matches(queryStr, @"\b[\w']*\b");
            var queryWords = from m in queryMatches.Cast<Match>()
                where !string.IsNullOrEmpty(m.Value) && m.Value.Length > 2
                select Encoding.UTF8.GetString(Encoding.GetEncoding("ISO-8859-8").GetBytes(m.Value.ToLower()));

            MatchCollection titleMatches = Regex.Matches(title, @"\b[\w']*\b");
            var titleWords = from m in titleMatches.Cast<Match>()
                where !string.IsNullOrEmpty(m.Value) && m.Value.Length > 2
                select Encoding.UTF8.GetString(Encoding.GetEncoding("ISO-8859-8").GetBytes(m.Value.ToLower()));
            titleWords = titleWords.ToArray();

            foreach (var word in queryWords)
            {
                if (!titleWords.Contains(word))
                    return false;
            }

            return true;
        }

        private string TryToCleanSeriesTitle(string title, string episodeTitle)
        {
            // title = Superman
            // episodeTitle = Superman1x12
            var newTitle = episodeTitle;
            try
            {
                newTitle = newTitle.Replace(title, title + " ");
                Regex r = new Regex("(([0-9]+)x([0-9]+))", RegexOptions.IgnoreCase);
                Match m = r.Match(newTitle);
                if (m.Success)
                {
                    var season = "S" + m.Groups[2].Value.PadLeft(2, '0');
                    var episode = "E" + m.Groups[3].Value.PadLeft(2, '0');
                    newTitle = newTitle.Replace(m.Groups[1].Value, season + episode);
                }
                newTitle = newTitle.Replace(" COMPLETA", "").Replace(" FINAL TEMPORADA", "");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            // return Superman S01E012
            return newTitle;
        }

        private DateTime TryToParseDate(string dateToParse, DateTime dateDefault)
        {
            var date = dateDefault;
            try
            {
                date = DateTime.ParseExact(dateToParse, "dd-MM-yyyy", CultureInfo.InvariantCulture);
            }
            catch
            {
                // ignored
            }
            return date;
        }

        private long TryToParseSize(string sizeToParse, long sizeDefault)
        {
            var size = sizeDefault;
            try
            {
                size = ReleaseInfo.GetBytes(sizeToParse);
            }
            catch
            {
                // ignored
            }
            return size;
        }
    }
}
