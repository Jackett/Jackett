using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
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
    public class AwesomeHD : BaseWebIndexer
    {
        private string SearchUrl => SiteLink + "searchapi.php";
        private string TorrentUrl => SiteLink + "torrents.php";
        private new ConfigurationDataPasskey configData => (ConfigurationDataPasskey)base.configData;

        public AwesomeHD(IIndexerConfigurationService configService, Utils.Clients.WebClient c, Logger l, IProtectionService ps)
            : base("Awesome-HD",
                description: "An HD tracker",
                link: "https://awesome-hd.me/",
                caps: new TorznabCapabilities(),
                configService: configService,
                client: c,
                logger: l,
                p: ps,
                configData: new ConfigurationDataPasskey("Note: You can find the Passkey in your profile, next to Personal information."))
        {
            Encoding = Encoding.UTF8;
            Language = "en-us";
            Type = "private";

            TorznabCaps.SupportsImdbMovieSearch = true;
            TorznabCaps.SupportsImdbTVSearch = true;

            AddCategoryMapping(1, TorznabCatType.MoviesHD);
            AddCategoryMapping(2, TorznabCatType.TVHD);
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
                var searchTerm = query.SearchTerm; // not use query.GetQueryString(), because it includes the season
                if (query.Season > 0) // search for tv series
                    searchTerm += $": Season {query.Season:D2}";

                qc.Add("action", "titlesearch");
                qc.Add("title", searchTerm);
            }
            else
            {
                qc.Add("action", "latestmovies");
                // the endpoint 'latestmovies' only returns movies, this hack overwrites categories to get movies even if
                // you are searching for tv series. this allows to configure the tracker in Sonarr
                query.Categories = new int[] {};
            }

            var searchUrl = SearchUrl + "?" + qc.GetQueryString();
            var results = await RequestStringWithCookies(searchUrl);
            if (string.IsNullOrWhiteSpace(results.Content))
                throw new Exception("Empty response. Please, check the Passkey.");

            try
            {
                var doc = XDocument.Parse(results.Content);

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

                    // if the category is not in the search categories, skip
                    var cat = new List<int> {isSerie ? TorznabCatType.TVHD.ID : TorznabCatType.MoviesHD.ID};
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
                    var comments = new Uri($"{TorrentUrl}?id={groupId}&torrentid={torrentId}");
                    var link = new Uri($"{TorrentUrl}?action=download&id={torrentId}&authkey={authkey}&torrent_pass={passkey}");

                    var publishDate = DateTime.Parse(torrent.FirstValue("time"));
                    var size = long.Parse(torrent.FirstValue("size"));
                    var grabs = int.Parse(torrent.FirstValue("snatched"));
                    var seeders = int.Parse(torrent.FirstValue("seeders"));
                    var peers = seeders + int.Parse(torrent.FirstValue("leechers"));
                    var freeleech = double.Parse(torrent.FirstValue("freeleech"));

                    Uri banner = null;
                    // small cover only for movies
                    if (!isSerie && !string.IsNullOrWhiteSpace(torrent.Element("smallcover")?.Value))
                        banner = new Uri(torrent.FirstValue("smallcover"));
                    else if (!string.IsNullOrWhiteSpace(torrent.Element("cover")?.Value))
                        banner = new Uri(torrent.FirstValue("cover"));

                    var description = torrent.Element("encodestatus") != null ?
                        $"Encode status: {torrent.FirstValue("encodestatus")}" : null;

                    var imdb = ParseUtil.GetImdbID(torrent.Element("imdb")?.Value);

                    var release = new ReleaseInfo
                    {
                        Title = title.ToString(),
                        Comments = comments,
                        Link = link,
                        Guid = link,
                        PublishDate = publishDate,
                        Category = cat,
                        BannerUrl = banner,
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
                OnParseError(results.Content, ex);
            }

            return releases;
        }
    }
}
