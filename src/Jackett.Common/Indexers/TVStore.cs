using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class TVStore : BaseWebIndexer
    {
        private readonly Dictionary<int, long> _imdbLookup = new Dictionary<int, long>(); // _imdbLookup[internalId] = imdbId

        private readonly Dictionary<long, int>
            _internalLookup = new Dictionary<long, int>(); // _internalLookup[imdbId] = internalId

        private readonly Regex _seriesInfoMatch = new Regex(
            @"catl\[\d+\]=(?<seriesID>\d+).*catIM\[\k<seriesID>]='(?<ImdbId>\d+)'", RegexOptions.Compiled);

        private readonly Regex _seriesInfoSearchRegex = new Regex(
            @"S(?<season>\d{1,3})(?:E(?<episode>\d{1,3}))?$", RegexOptions.IgnoreCase);

        public TVStore(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps) :
            base(id: "tvstore",
                 name: "TV Store",
                 description: "TV Store is a HUNGARIAN Private Torrent Tracker for TV",
                 link: "https://tvstore.me/",
                 caps: new TorznabCapabilities
                 {
                     TvSearchParams = new List<TvSearchParam>
                     {
                         TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId
                     },
                     MovieSearchParams = new List<MovieSearchParam>
                     {
                         MovieSearchParam.Q, MovieSearchParam.ImdbId
                     }
                 },
                 configService: configService,
                 client: wc,
                 logger: l,
                 p: ps,
                 configData: new ConfigurationDataTVstore())
        {
            Encoding = Encoding.UTF8;
            Language = "hu-hu";
            Type = "private";

            AddCategoryMapping(1, TorznabCatType.TV);
            AddCategoryMapping(2, TorznabCatType.TVHD);
            AddCategoryMapping(3, TorznabCatType.TVSD);
        }

        private string LoginUrl => SiteLink + "takelogin.php";
        private string LoginPageUrl => SiteLink + "login.php?returnto=%2F";
        private string SearchUrl => SiteLink + "torrent/br_process.php";
        private string DownloadUrl => SiteLink + "torrent/download.php";
        private string BrowseUrl => SiteLink + "torrent/browse.php";

        private new ConfigurationDataTVstore configData => (ConfigurationDataTVstore)base.configData;

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            var loginPage = await RequestWithCookiesAsync(LoginPageUrl, string.Empty);
            var pairs = new Dictionary<string, string>
            {
                {"username", configData.Username.Value},
                {"password", configData.Password.Value},
                {"back", "%2F"},
                {"logout", "1"}
            };
            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, loginPage.Cookies, true, referer: SiteLink);
            await ConfigureIfOK(
                result.Cookies, result.ContentString?.Contains("Főoldal") == true,
                () => throw new ExceptionWithConfigData("Error while trying to login.", configData));
            return IndexerConfigurationStatus.RequiresTesting;
        }

        /// <summary>
        ///     Calculate the Upload Factor for the torrents
        /// </summary>
        /// <returns>The calculated factor</returns>
        /// <param name="dateTime">Date time.</param>
        /// <param name="isSeasonPack">Determine if torrent type is season pack or single episode</param>
        private static double UploadFactorCalculator(DateTime dateTime, bool isSeasonPack)
        {
            var dd = (DateTime.Now - dateTime).Days;
            /* In case of season Packs */
            if (isSeasonPack)
            {
                if (dd >= 90)
                    return 4;
                if (dd >= 30)
                    return 2;
                if (dd >= 14)
                    return 1.5;
            }
            else /* In case of single episodes */
            {
                if (dd >= 60)
                    return 2;
                if (dd >= 30)
                    return 1.5;
            }

            return 1;
        }

        /// <summary>
        ///     Parses the torrents from the content
        /// </summary>
        /// <returns>The parsed torrents.</returns>
        /// <param name="results">The result of the query</param>
        /// <param name="alreadyFound">Number of the already found torrents.(used for limit)</param>
        /// <param name="limit">The limit to the number of torrents to download </param>
        /// <param name="previouslyParsedOnPage">Current position in parsed results</param>
        private async Task<List<ReleaseInfo>> ParseTorrentsAsync(WebResult results, int alreadyFound, int limit,
                                                                 int previouslyParsedOnPage)
        {
            var releases = new List<ReleaseInfo>();
            var queryParams = new NameValueCollection
            {
                {"func", "getToggle"},
                {"w", "F"},
                {"pg", "0"}
            };
            try
            {
                /* Content Looks like this
                 * 2\15\2\1\1727\207244\1x08 \[WebDL-720p - Eng - AJP69]\gb\2018-03-09 08:11:53\akció, kaland, sci-fi \0\0\1\191170047\1\0\Anonymous\50\0\0\\0\4\0\174\0\
                 * 1\ 0\0\1\1727\207243\1x08 \[WebDL-1080p - Eng - AJP69]\gb\2018-03-09 08:11:49\akció, kaland, sci-fi \0\0\1\305729738\1\0\Anonymous\50\0\0\\0\8\0\102\0\0\0\0\1\\\
                 * First 3 items per page are total results, results per page, and results this page
                 * There is also a tail of ~4 items after the results for some reason. Looks like \1\\\
                 */
                var parameters = results.ContentString.Split('\\');
                var torrentsThisPage = int.Parse(parameters[2]);
                var maxTorrents = Math.Min(torrentsThisPage, limit - alreadyFound);
                var rows = parameters.Skip(3) //Skip pages info
                                     .Select((str, index) => (index, str)) //Index each string for grouping
                                     .GroupBy(n => n.index / 27) // each torrent is divided into 27 parts
                                     .Skip(previouslyParsedOnPage).Take(maxTorrents)// only parse the rows we want
                                                                                    //Convert above query into a List<string>(27) in prep for parsing
                                     .Select(entry => entry.Select(item => item.str).ToList());
                foreach (var row in rows)
                {
                    var torrentId = row[(int)TorrentParts.TorrentId];
                    var downloadLink = new Uri(DownloadUrl + "?id=" + torrentId);
                    var imdbId = _imdbLookup.TryGetValue(int.Parse(row[(int)TorrentParts.InternalId]), out var imdb)
                        ? (long?)imdb
                        : null;
                    var files = int.Parse(row[(int)TorrentParts.Files]);
                    var size = long.Parse(row[(int)TorrentParts.SizeBytes]);
                    var seeders = int.Parse(row[(int)TorrentParts.Seeders]);
                    var leechers = int.Parse(row[(int)TorrentParts.Leechers]);
                    var grabs = int.Parse(row[(int)TorrentParts.Grabs]);
                    var publishDate = DateTime.Parse(row[(int)TorrentParts.PublishDate]);
                    var isSeasonPack = row[(int)TorrentParts.EpisodeInfo].Contains("évad");
                    queryParams["id"] = torrentId;
                    queryParams["now"] = DateTimeUtil.DateTimeToUnixTimestamp(DateTime.UtcNow)
                                                     .ToString(CultureInfo.InvariantCulture);
                    var filesList = (await RequestWithCookiesAndRetryAsync(SearchUrl + "?" + queryParams.GetQueryString()))
                        .ContentString;
                    var firstFileName = filesList.Split(
                        new[]
                        {
                            @"\\"
                        }, StringSplitOptions.None)[1];
                    // Delete the file extension. Many first files are either mkv or nfo.
                    // Cannot confirm these are the only extensions, so generic remove all 3 char extensions at end of section.
                    firstFileName = Regex.Replace(firstFileName, @"\.\w{3}$", string.Empty);
                    if (isSeasonPack)
                        firstFileName = Regex.Replace(
                            firstFileName, @"(?<=S\d+)E\d{2,3}", string.Empty, RegexOptions.IgnoreCase);
                    var category = new[]
                    {
                        TvCategoryParser.ParseTvShowQuality(firstFileName)
                    };
                    var release = new ReleaseInfo
                    {
                        Title = firstFileName,
                        Link = downloadLink,
                        Guid = downloadLink,
                        PublishDate = publishDate,
                        Files = files,
                        Size = size,
                        Category = category,
                        Seeders = seeders,
                        Peers = leechers + seeders,
                        Grabs = grabs,
                        MinimumRatio = 1,
                        MinimumSeedTime = 172800, // 48 hours
                        DownloadVolumeFactor = 1,
                        UploadVolumeFactor = UploadFactorCalculator(publishDate, isSeasonPack),
                        Imdb = imdbId
                    };
                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(results.ContentString, ex);
            }

            return releases;
        }

        /// <summary>
        ///     Map internally used series info to its corresponding IMDB number.
        ///     Saves this data into 2 dictionaries for easy lookup from one value to the other
        /// </summary>
        private async Task PopulateImdbMapAsync()
        {
            var result = await RequestWithCookiesAndRetryAsync(BrowseUrl);
            foreach (Match match in _seriesInfoMatch.Matches(result.ContentString))
            {
                var internalId = int.Parse(match.Groups["seriesID"].Value);
                var imdbId = long.Parse(match.Groups["ImdbId"].Value);
                _imdbLookup[internalId] = imdbId;
                _internalLookup[imdbId] = internalId;
            }
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            if (!_imdbLookup.Any())
                await PopulateImdbMapAsync();
            var queryParams = new NameValueCollection
            {
                {"now", DateTimeUtil.DateTimeToUnixTimestamp(DateTime.UtcNow).ToString(CultureInfo.InvariantCulture)},
                {"p", "1"}
            };
            if (query.Limit == 0)
                query.Limit = 100;
            if (query.IsImdbQuery)
            {
                if (!string.IsNullOrEmpty(query.ImdbIDShort) && _internalLookup.TryGetValue(
                    long.Parse(query.ImdbIDShort), out var internalId))
                    queryParams.Add("g", internalId.ToString());
                else
                    return Enumerable.Empty<ReleaseInfo>();
            }
            else
            {
                queryParams.Add("g", "0");
                if (!string.IsNullOrWhiteSpace(query.SearchTerm))
                {
                    var searchString = query.SanitizedSearchTerm;
                    if (query.Season == 0 && string.IsNullOrWhiteSpace(query.Episode))
                    {
                        //Jackett doesn't check for lowercase s00e00 so do it here.
                        var searchMatch = _seriesInfoSearchRegex.Match(searchString);
                        if (searchMatch.Success)
                        {
                            query.Season = int.Parse(searchMatch.Groups["season"].Value);
                            query.Episode = searchMatch.Groups["episode"].Success
                                ? $"{int.Parse(searchMatch.Groups["episode"].Value):00}"
                                : null;
                            query.SearchTerm = searchString.Remove(searchMatch.Index, searchMatch.Length).Trim(); // strip SnnEnn
                        }
                    }
                }
                else if (query.IsTest)
                    query.Limit = 20;

                // Search string must be converted to Base64
                var plainTextBytes = Encoding.UTF8.GetBytes(query.SanitizedSearchTerm);
                queryParams.Add("c", Convert.ToBase64String(plainTextBytes));
            }

            if (query.Season != 0)
            {
                queryParams.Add("s", query.Season.ToString());
                if (!string.IsNullOrWhiteSpace(query.Episode))
                    queryParams.Add("e", query.Episode);
            }

            var results = await RequestWithCookiesAndRetryAsync(SearchUrl + "?" + queryParams.GetQueryString());
            // Parse page Information from result
            var content = results.ContentString;
            var splits = content.Split('\\');
            var totalFound = int.Parse(splits[0]);
            var torrentPerPage = int.Parse(splits[1]);
            if (totalFound == 0 || query.Offset > totalFound)
                return Enumerable.Empty<ReleaseInfo>();
            var startPage = query.Offset / torrentPerPage + 1;
            var previouslyParsedOnPage = query.Offset % torrentPerPage;
            var pages = totalFound / torrentPerPage + 1;
            // First page content is already ready
            if (startPage == 1)
            {
                releases.AddRange(await ParseTorrentsAsync(results, releases.Count, query.Limit, previouslyParsedOnPage));
                previouslyParsedOnPage = 0;
                startPage++;
            }

            for (var page = startPage; page <= pages && releases.Count < query.Limit; page++)
            {
                queryParams["page"] = page.ToString();
                results = await RequestWithCookiesAndRetryAsync(SearchUrl + "?" + queryParams.GetQueryString());
                releases.AddRange(await ParseTorrentsAsync(results, releases.Count, query.Limit, previouslyParsedOnPage));
                previouslyParsedOnPage = 0;
            }

            return releases;
        }

        private enum TorrentParts
        {
            InternalId = 1,
            TorrentId = 2,
            EpisodeInfo = 3,
            PublishDate = 6,
            Files = 10,
            SizeBytes = 11,
            Seeders = 20,
            Leechers = 21,
            Grabs = 22
        }
    }
}
