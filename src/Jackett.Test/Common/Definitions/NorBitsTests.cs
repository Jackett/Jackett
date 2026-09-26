using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Jackett.Common.Indexers.Definitions;
using Jackett.Common.Models;
using Jackett.Common.Utils.Clients;
using Jackett.Test.TestHelpers;
using NLog;
using NUnit.Framework;
using WebClient = Jackett.Common.Utils.Clients.WebClient;
using WebRequest = Jackett.Common.Utils.Clients.WebRequest;

namespace Jackett.Test.Common.Definitions
{
    [TestFixture]
    public class NorBitsTests
    {
        [Test]
        public async Task ImdbTvSearchWithDiacriticsSendsOneTrackerSearchAsync()
        {
            var webClient = new ScriptedWebClient();
            webClient.QueueSearchResponse(AuthenticatedPage(TorrentRow("Requested.Show.S02", "tt1234567", 1)));
            webClient.QueueSearchResponse(AuthenticatedPage(TorrentRow("Requested.Show.S02", "tt1234567", 1)));
            var indexer = CreateIndexer(webClient);
            var query = ImdbQuery();
            query.SearchTerm = "Éxample Show";

            await indexer.ResultsForQuery(query, false);

            var trackerSearches = webClient.Requests
                .Where(request => request.Url.Contains("imdbsearch="))
                .Select(request => request.Url)
                .ToList();

            Assert.That(trackerSearches, Is.EqualTo(new[]
            {
                "https://norbits.net/browse.php?imdbsearch=tt1234567&search=S02&incldead=1&fullsearch=0&scenerelease=0"
            }));
        }

        [Test]
        public async Task ImdbTvSearchReturnsOnlyRowsForRequestedImdbAsync()
        {
            var webClient = new ScriptedWebClient();
            webClient.QueueSearchResponse(AuthenticatedPage(
                TorrentRow("Requested.Show.S02", "tt1234567", 1),
                TorrentRow("Different.Show.S02", "tt7654321", 2),
                TorrentRow("Missing.Imdb.Show.S02", null, 3)));
            var indexer = CreateIndexer(webClient);

            var result = await indexer.ResultsForQuery(ImdbQuery(), false);

            Assert.That(
                result.Releases.Select(release => release.Title),
                Is.EqualTo(new[] { "Requested.Show.S02" }));
        }

        private static NorBits CreateIndexer(ScriptedWebClient webClient) =>
            new NorBits(
                null,
                webClient,
                LogManager.GetCurrentClassLogger(),
                null,
                new TestCacheService());

        private static TorznabQuery ImdbQuery() => new TorznabQuery
        {
            QueryType = "tvsearch",
            ImdbID = "tt1234567",
            Season = 2,
            Cache = false
        };

        private static string AuthenticatedPage(params string[] rows) =>
            $@"<html><body><a href=""logout.php"">Logout</a>
                <table id=""torrentTable""><tbody><tr><th>Header</th></tr>{string.Join(string.Empty, rows)}</tbody></table>
                </body></html>";

        private static string TorrentRow(string title, string imdbId, int id)
        {
            var imdbLink = imdbId == null
                ? string.Empty
                : $@"<a href=""https://imdb.com/title/{imdbId}"">IMDb</a>";

            return $@"<tr>
                <td><a href=""?main_cat[]=2"">TV</a></td>
                <td><a href=""download.php?id={id}"">Download</a><a href=""details.php?id={id}"" title=""{WebUtility.HtmlEncode(title)}"">Details</a>{imdbLink}</td>
                <td><a>1</a></td><td></td><td>2026-08-0912:00:00</td><td></td>
                <td>1 GB</td><td>1</td><td>2</td><td>3</td>
                </tr>";
        }

        private sealed class ScriptedWebClient : WebClient
        {
            private readonly Queue<WebResult> _searchResponses = new Queue<WebResult>();

            public ScriptedWebClient()
                : base(null, null, null, new Jackett.Common.Models.Config.ServerConfig(null))
            {
            }

            public List<WebRequest> Requests { get; } = new List<WebRequest>();

            public void QueueSearchResponse(string content) =>
                _searchResponses.Enqueue(new WebResult
                {
                    ContentString = content,
                    Status = HttpStatusCode.OK
                });

            public override Task<WebResult> GetResultAsync(WebRequest request)
            {
                Requests.Add(request);

                if (!request.Url.Contains("imdbsearch="))
                {
                    return Task.FromResult(new WebResult
                    {
                        ContentString = AuthenticatedPage(),
                        Request = request,
                        Status = HttpStatusCode.OK
                    });
                }

                if (_searchResponses.Count == 0)
                {
                    throw new InvalidOperationException($"No scripted response for {request.Url}");
                }

                var response = _searchResponses.Dequeue();
                response.Request = request;
                return Task.FromResult(response);
            }
        }
    }
}
