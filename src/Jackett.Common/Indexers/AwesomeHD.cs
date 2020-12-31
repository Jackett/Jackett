using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class AwesomeHD : BaseWebIndexer
    {
        private string SearchUrl => SiteLink + "searchapi.php";
        private string TorrentUrl => SiteLink + "torrents.php";
        private readonly Regex _removeYearRegex = new Regex(@" [\(\[]?(19|20)\d{2}[\)\]]?$", RegexOptions.Compiled);
        private new ConfigurationDataPasskey configData => (ConfigurationDataPasskey)base.configData;

        public AwesomeHD(IIndexerConfigurationService configService, Utils.Clients.WebClient c, Logger l, IProtectionService ps,
            ICacheService cs)
            : base(id: "awesomehd",
                   name: "Awesome-HD",
                   description: "An HD tracker",
                   link: "https://awesome-hd.me/",
                   caps: new TorznabCapabilities
                   {
                       TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId
                       },
                       MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q, MovieSearchParam.ImdbId
                       },
                       MusicSearchParams = new List<MusicSearchParam>
                       {
                           MusicSearchParam.Q
                       }
                   },
                   configService: configService,
                   client: c,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataPasskey("Note: You can find the Passkey in your profile, " +
                                                            "next to Personal information."))
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";

            AddCategoryMapping(1, TorznabCatType.MoviesHD);
            AddCategoryMapping(2, TorznabCatType.TVHD);
            AddCategoryMapping(3, TorznabCatType.Audio);
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            if (configData.Passkey.Value.Length != 32)
                throw new Exception("Invalid Passkey configured. Expected length: 32");

            var releases = await PerformQuery(new TorznabQuery());
            await ConfigureIfOK(string.Empty, releases.Any(),
                                () => throw new Exception("Could not find release from this URL."));

            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var passkey = configData.Passkey.Value;

            var qc = new NameValueCollection
            {
                {"passkey", passkey}
            };

            if (query.IsImdbQuery)
            {
                qc.Add("action", "imdbsearch");
                qc.Add("imdb", query.ImdbID);
            }
            else if (!string.IsNullOrWhiteSpace(query.GetQueryString()))
            {
                // not use query.GetQueryString(), because it includes the season
                var searchTerm = query.SearchTerm;
                // search for tv series
                if (query.Season > 0)
                    searchTerm += $": Season {query.Season:D2}";
                // remove the year, it's not supported in the api
                searchTerm = _removeYearRegex.Replace(searchTerm, "");
                qc.Add("action", "titlesearch");
                qc.Add("title", searchTerm);
            }
            else
            {
                qc.Add("action", "latestmovies");
                // the endpoint 'latestmovies' only returns movies, this hack overwrites categories to get movies even if
                // you are searching for tv series. this allows to configure the tracker in Sonarr
                query.Categories = new int[] { };
            }

            var searchUrl = SearchUrl + "?" + qc.GetQueryString();
            var results = await RequestWithCookiesAsync(searchUrl);
            if (string.IsNullOrWhiteSpace(results.ContentString))
                throw new Exception("Empty response. Please, check the Passkey.");

            try
            {
                var doc = XDocument.Parse(results.ContentString);

                var errorMsg = doc.Descendants("error").FirstOrDefault()?.Value;
                if (errorMsg?.Contains("No Results") == true)
                    return releases; // no results
                if (errorMsg != null)
                    throw new Exception(errorMsg);

                var authkey = doc.Descendants("authkey").First().Value;
                var torrents = doc.Descendants("torrent");
                foreach (var torrent in torrents)
                {
                    var torrentName = torrent.FirstValue("name").Trim();

                    // the field <type> is always Movie, so we have to guess if it's a tv series
                    var isSerie = torrentName.Contains(": Season ");
                    if (isSerie)
                        torrentName = torrentName.Replace(": Season ", " S");

                    // we have to guess if it's an audio track too
                    var isAudio = string.IsNullOrWhiteSpace((string)torrent.Element("resolution"));
                    var cat = new List<int>
                    {
                        isSerie ? TorznabCatType.TVHD.ID :
                        isAudio ? TorznabCatType.Audio.ID :
                        TorznabCatType.MoviesHD.ID
                    };

                    // if the category is not in the search categories, skip
                    if (query.Categories.Any() && !query.Categories.Intersect(cat).Any())
                        continue;

                    var title = new StringBuilder(torrentName);
                    if (!isSerie && torrent.Element("year") != null) // only for movies
                        title.Append($" {torrent.FirstValue("year")}");
                    if (torrent.Element("internal")?.Value == "1")
                        title.Append(" iNTERNAL");
                    if (torrent.Element("resolution") != null)
                        title.Append($" {torrent.FirstValue("resolution")}");
                    if (torrent.Element("media") != null)
                        title.Append($" {torrent.FirstValue("media")}");
                    if (torrent.Element("encoding") != null)
                        title.Append($" {torrent.FirstValue("encoding")}");
                    if (torrent.Element("audioformat") != null)
                        title.Append($" {torrent.FirstValue("audioformat")}");
                    if (torrent.Element("releasegroup") != null)
                        title.Append($"-{torrent.FirstValue("releasegroup")}");

                    var torrentId = torrent.FirstValue("id");
                    var groupId = torrent.FirstValue("groupid");
                    var details = new Uri($"{TorrentUrl}?id={groupId}&torrentid={torrentId}");
                    var link = new Uri($"{TorrentUrl}?action=download&id={torrentId}&authkey={authkey}&torrent_pass={passkey}");

                    var publishDate = DateTime.Parse(torrent.FirstValue("time"));
                    var size = long.Parse(torrent.FirstValue("size"));
                    var grabs = int.Parse(torrent.FirstValue("snatched"));
                    var seeders = int.Parse(torrent.FirstValue("seeders"));
                    var peers = seeders + int.Parse(torrent.FirstValue("leechers"));
                    var freeleech = double.Parse(torrent.FirstValue("freeleech"));

                    Uri poster = null;
                    string description = null;
                    if (!isAudio) // audio tracks don't have poster either description
                    {
                        // small cover only for movies
                        var smallCover = torrent.Element("smallcover");
                        var normalCover = torrent.Element("cover");
                        if (!isSerie && !string.IsNullOrWhiteSpace(smallCover?.Value) && smallCover.Value.StartsWith("https://"))
                            poster = new Uri(torrent.FirstValue("smallcover"));
                        else if (!string.IsNullOrWhiteSpace(normalCover?.Value) && normalCover.Value.StartsWith("https://"))
                            poster = new Uri(torrent.FirstValue("cover"));

                        description = torrent.Element("encodestatus") != null ?
                            $"Encode status: {torrent.FirstValue("encodestatus")}" : null;
                    }

                    var imdb = ParseUtil.GetImdbID(torrent.Element("imdb")?.Value);

                    var release = new ReleaseInfo
                    {
                        Title = title.ToString(),
                        Details = details,
                        Link = link,
                        Guid = link,
                        PublishDate = publishDate,
                        Category = cat,
                        Poster = poster,
                        Description = description,
                        Imdb = imdb,
                        Size = size,
                        Grabs = grabs,
                        Seeders = seeders,
                        Peers = peers,
                        MinimumRatio = 1,
                        MinimumSeedTime = 259200, // 72 hours
                        DownloadVolumeFactor = freeleech,
                        UploadVolumeFactor = 1
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
    }
}
