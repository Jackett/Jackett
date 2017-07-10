using AutoMapper;
using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Jackett.Indexers;

namespace Jackett.Controllers
{
    [AllowAnonymous]
    [JackettAPINoCache]
    public class PotatoController : ApiController
    {
        private IIndexerManagerService indexerService;
        private Logger logger;
        private IServerService serverService;
        private ICacheService cacheService;
        private IWebClient webClient;

        public static int[] MOVIE_CATS
        {
            get
            {
                var torznabQuery = new TorznabQuery()
                {
                    Categories = new int[1] { TorznabCatType.Movies.ID },
                };

                torznabQuery.ExpandCatsToSubCats();
                return torznabQuery.Categories;
            }
        }

        public PotatoController(IIndexerManagerService i, Logger l, IServerService s, ICacheService c, IWebClient w)
        {
            indexerService = i;
            logger = l;
            serverService = s;
            cacheService = c;
            webClient = w;
        }

        [HttpGet]
        public async Task<HttpResponseMessage> Call(string indexerID, [FromUri]TorrentPotatoRequest request)
        {
            var indexer = indexerService.GetIndexer(indexerID);

            var allowBadApiDueToDebug = false;
#if DEBUG
            allowBadApiDueToDebug = Debugger.IsAttached;
#endif

            if (!allowBadApiDueToDebug && !string.Equals(request.passkey, serverService.Config.APIKey, StringComparison.InvariantCultureIgnoreCase))
            {
                logger.Warn(string.Format("A request from {0} was made with an incorrect API key.", Request.GetOwinContext().Request.RemoteIpAddress));
                return Request.CreateResponse(HttpStatusCode.Forbidden, "Incorrect API key");
            }

            if (!indexer.IsConfigured)
            {
                logger.Warn(string.Format("Rejected a request to {0} which is unconfigured.", indexer.DisplayName));
                return Request.CreateResponse(HttpStatusCode.Forbidden, "This indexer is not configured.");
            }

            if (!indexer.TorznabCaps.Categories.Select(c => c.ID).Any(i => MOVIE_CATS.Contains(i)))
            {
                logger.Warn(string.Format("Rejected a request to {0} which does not support searching for movies.", indexer.DisplayName));
                return Request.CreateResponse(HttpStatusCode.Forbidden, "This indexer does not support movies.");
            }

            var year = 0;

            var omdbApiKey = serverService.Config.OmdbApiKey;
            if (!request.imdbid.IsNullOrEmptyOrWhitespace() && !omdbApiKey.IsNullOrEmptyOrWhitespace())
            {
                // We are searching by IMDB id so look up the name
                var resolver = new OmdbResolver(webClient, omdbApiKey.ToNonNull());
                var movie = await resolver.MovieForId(request.imdbid.ToNonNull());
                request.search = movie.Title;
                year = ParseUtil.CoerceInt(movie.Year);
            }

            var torznabQuery = new TorznabQuery()
            {
                ApiKey = request.passkey,
                Categories = MOVIE_CATS,
                SearchTerm = request.search,
                ImdbID = request.imdbid,
                QueryType = "TorrentPotato"
            };

            IEnumerable<ReleaseInfo> releases = new List<ReleaseInfo>();

            if (!string.IsNullOrWhiteSpace(torznabQuery.SanitizedSearchTerm))
                releases = await indexer.ResultsForQuery(torznabQuery);

            // Cache non query results
            if (string.IsNullOrEmpty(torznabQuery.SanitizedSearchTerm))
            {
                cacheService.CacheRssResults(indexer, releases);
            }

            var serverUrl = string.Format("{0}://{1}:{2}{3}", Request.RequestUri.Scheme, Request.RequestUri.Host, Request.RequestUri.Port, serverService.BasePath());
            var potatoResponse = new TorrentPotatoResponse();

            releases = TorznabUtil.FilterResultsToTitle(releases, torznabQuery.SanitizedSearchTerm, year);
            releases = TorznabUtil.FilterResultsToImdb(releases, request.imdbid);

            foreach (var r in releases)
            {
                var release = Mapper.Map<ReleaseInfo>(r);
                release.Link = serverService.ConvertToProxyLink(release.Link, serverUrl, indexerID, "dl", release.Title + ".torrent");

                // Only accept torrent links, magnet is not supported
                // This seems to be no longer the case, allowing magnet URIs for now
                if (release.Link != null || release.MagnetUri != null)
                {
                    potatoResponse.results.Add(new TorrentPotatoResponseItem()
                    {
                        release_name = release.Title + "[" + indexer.DisplayName + "]", // Suffix the indexer so we can see which tracker we are using in CPS as it just says torrentpotato >.>
                        torrent_id = release.Guid.ToString(),
                        details_url = release.Comments.ToString(),
                        download_url = (release.Link != null ? release.Link.ToString() : release.MagnetUri.ToString()),
                        imdb_id = release.Imdb.HasValue ? "tt" + release.Imdb : null,
                        freeleech = (release.DownloadVolumeFactor == 0 ? true : false),
                        type = "movie",
                        size = (long)release.Size / (1024 * 1024), // This is in MB
                        leechers = (int)release.Peers - (int)release.Seeders,
                        seeders = (int)release.Seeders,
                        publish_date = r.PublishDate == DateTime.MinValue ? null : release.PublishDate.ToUniversalTime().ToString("s")
                    });
                }
            }

            // Log info
            if (string.IsNullOrWhiteSpace(torznabQuery.SanitizedSearchTerm))
            {
                logger.Info(string.Format("Found {0} torrentpotato releases from {1}", releases.Count(), indexer.DisplayName));
            }
            else
            {
                logger.Info(string.Format("Found {0} torrentpotato releases from {1} for: {2}", releases.Count(), indexer.DisplayName, torznabQuery.GetQueryString()));
            }

            // Force the return as Json
            return new HttpResponseMessage()
            {
                Content = new JsonContent(potatoResponse)
            };
        }
    }
}
