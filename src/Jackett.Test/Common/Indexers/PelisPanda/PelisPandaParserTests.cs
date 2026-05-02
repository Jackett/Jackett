using System;
using System.Collections.Generic;
using System.Linq;
using Jackett.Common.Indexers;
using Jackett.Common.Indexers.Definitions;
using Jackett.Common.Models;
using NUnit.Framework;

namespace Jackett.Test.Common.Indexers.PelisPanda
{
    [TestFixture]
    public class PelisPandaParserTests
    {
        private const string SiteLink = "https://pelispanda.org/";

        [Test]
        public void RequestGenerator_EmptyQuery_ReturnsEmptyChain()
        {
            var generator = new PelisPandaRequestGenerator(SiteLink);
            var chain = generator.GetSearchRequests(new TorznabQuery { SearchTerm = "" });
            Assert.That(chain.GetAllTiers().SelectMany(t => t).Count(), Is.EqualTo(0));
        }

        [Test]
        public void RequestGenerator_TermWithSeason_BuildsExpectedUrl()
        {
            var generator = new PelisPandaRequestGenerator(SiteLink);
            var chain = generator.GetSearchRequests(new TorznabQuery
            {
                SearchTerm = "stranger things",
                Season = 4
            });

            var requests = chain.GetAllTiers().SelectMany(t => t).ToList();
            Assert.That(requests.Count, Is.EqualTo(1), "Expect exactly one IndexerRequest in the chain");
            var url = requests[0].Url;
            Assert.That(url, Does.StartWith("https://pelispanda.org/wp-json/wpreact/v1/search?query="));
            Assert.That(url, Does.Contain("stranger+things+4").Or.Contain("stranger%20things%204"));
            Assert.That(url, Does.Contain("posts_per_page=500"));
            Assert.That(url, Does.Contain("page=1"));
        }

        [Test]
        public void RequestGenerator_EmptyTermWithSeason_ReturnsEmptyChain()
        {
            var generator = new PelisPandaRequestGenerator(SiteLink);
            var chain = generator.GetSearchRequests(new TorznabQuery
            {
                SearchTerm = "",
                Season = 4
            });
            Assert.That(chain.GetAllTiers().SelectMany(t => t).Count(), Is.EqualTo(0));
        }

        [Test]
        public void Parser_UnknownTypeItem_SkipsAndLogsWarn()
        {
            var (logger, target) = NewMemoryLogger();
            var web = new Jackett.Test.TestHelpers.TestWebClient();
            var parser = new PelisPandaParser(web, logger, SiteLink);

            var url = "https://pelispanda.org/wp-json/wpreact/v1/search?query=x&posts_per_page=500&page=1";
            var response = BuildResponse(url, LoadFixture("search.json"));

            var releases = parser.ParseResponse(response);

            Assert.That(target.Logs.Any(line => line.Contains("WARN") && line.Contains("documental")),
                "Expected a WARN line referencing the unknown 'documental' type");
        }

        private static IndexerResponse BuildResponse(string url, string body)
        {
            var request = new Jackett.Common.Indexers.IndexerRequest(url);
            var web = new Jackett.Common.Utils.Clients.WebResult
            {
                ContentBytes = System.Text.Encoding.UTF8.GetBytes(body),
                Status = System.Net.HttpStatusCode.OK,
                Request = new Jackett.Common.Utils.Clients.WebRequest(url)
            };
            return new IndexerResponse(request, web);
        }

        private static string LoadFixture(string name)
        {
            var path = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "",
                "Resources", "PelisPanda", name);
            return System.IO.File.ReadAllText(path);
        }

        private static (NLog.Logger logger, NLog.Targets.MemoryTarget target) NewMemoryLogger()
        {
            var target = new NLog.Targets.MemoryTarget("memtest") { Layout = "${level:uppercase=true}|${message}" };
            var config = new NLog.Config.LoggingConfiguration();
            config.AddTarget(target);
            config.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, target);
            var factory = new NLog.LogFactory { Configuration = config };
            var logger = factory.GetLogger("PelisPandaTest");
            return (logger, target);
        }

        private static Jackett.Test.TestHelpers.TestWebClient NewTestWebClient(
            params (string url, string fixture)[] mappings)
        {
            var web = new Jackett.Test.TestHelpers.TestWebClient();
            foreach (var (url, fixture) in mappings)
                web.RegisterRequestCallback(url, $"PelisPanda/{fixture}");
            return web;
        }

        private sealed class TempFixture : IDisposable
        {
            public string FixtureName { get; }
            private readonly string _path;

            public TempFixture(string prefix, string content)
            {
                FixtureName = $"PelisPanda/_tmp_{prefix}_{Guid.NewGuid():N}.json";
                _path = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "",
                    "Resources", FixtureName);
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_path));
                System.IO.File.WriteAllText(_path, content);
            }

            public void Dispose()
            {
                if (System.IO.File.Exists(_path))
                    System.IO.File.Delete(_path);
            }
        }

        [Test]
        public void Parser_Movie_ProducesOneReleasePerDownload()
        {
            var (logger, _) = NewMemoryLogger();

            var searchUrl = "https://pelispanda.org/wp-json/wpreact/v1/search?query=hero&posts_per_page=500&page=1";
            var movieUrl = "https://pelispanda.org/wp-json/wpreact/v1/movie/heroe-de-la-libertad";
            var animeUrl = "https://pelispanda.org/wp-json/wpreact/v1/anime/darwins-game";
            var serieUrl = "https://pelispanda.org/wp-json/wpreact/v1/serie/stranger-things-relatos-del-85/related";

            var web = NewTestWebClient(
                (movieUrl, "movie.json"),
                (animeUrl, "anime-empty.json"),
                (serieUrl, "serie-related.json"));

            var parser = new PelisPandaParser(web, logger, SiteLink);
            var response = BuildResponse(searchUrl, LoadFixture("search.json"));
            var releases = parser.ParseResponse(response).ToList();

            var movieReleases = releases
                .Where(r => r.Details != null &&
                            r.Details.AbsoluteUri.Contains("pelicula/heroe-de-la-libertad"))
                .ToList();

            Assert.That(movieReleases.Count, Is.EqualTo(3), "Movie should have 3 releases (3 downloads)");

            var first = movieReleases[0];
            Assert.That(first.Title, Does.Contain("Hero of Liberty"));
            Assert.That(first.Title, Does.Contain("(2024)"));
            Assert.That(first.Title, Does.Contain("1080p"));
            Assert.That(first.Title, Does.Contain("Latino"));
            Assert.That(first.Category, Does.Contain(Jackett.Common.Models.TorznabCatType.Movies.ID));
            Assert.That(first.Size, Is.EqualTo(Jackett.Common.Utils.ParseUtil.GetBytes("3.80 GB")));
            Assert.That(first.MagnetUri, Is.Not.Null);
            Assert.That(first.Link, Is.Null);
            Assert.That(first.Subs, Is.EquivalentTo(new[] { "Subtitulado" }));
            Assert.That(first.Languages, Is.EquivalentTo(new[] { "Latino" }));
            Assert.That(first.PublishDate.ToString("yyyyMMdd"), Is.EqualTo("20240105"));
            Assert.That(first.Seeders, Is.EqualTo(1));
            Assert.That(first.DownloadVolumeFactor, Is.EqualTo(0));
            Assert.That(first.UploadVolumeFactor, Is.EqualTo(1));

            var third = movieReleases[2];
            Assert.That(third.MagnetUri, Is.Null);
            Assert.That(third.Link, Is.Not.Null);
            Assert.That(third.Title, Does.Contain("2160p"));
            Assert.That(third.Languages, Is.EquivalentTo(new[] { "Castellano" }));
        }

        [Test]
        public void Parser_AnimeEmptyDownloads_NoReleases()
        {
            var (logger, _) = NewMemoryLogger();
            var searchUrl = "https://pelispanda.org/wp-json/wpreact/v1/search?query=darwin&posts_per_page=500&page=1";
            var animeUrl = "https://pelispanda.org/wp-json/wpreact/v1/anime/darwins-game";
            var movieUrl = "https://pelispanda.org/wp-json/wpreact/v1/movie/heroe-de-la-libertad";
            var serieUrl = "https://pelispanda.org/wp-json/wpreact/v1/serie/stranger-things-relatos-del-85/related";

            var web = NewTestWebClient(
                (movieUrl, "movie.json"),
                (animeUrl, "anime-empty.json"),
                (serieUrl, "serie-related.json"));

            var parser = new PelisPandaParser(web, logger, SiteLink);
            var response = BuildResponse(searchUrl, LoadFixture("search.json"));
            var releases = parser.ParseResponse(response).ToList();

            Assert.That(releases.Any(r => r.Details != null &&
                                          r.Details.AbsoluteUri.Contains("anime/darwins-game")),
                Is.False, "Anime with downloads:null must not produce releases");
        }

        [Test]
        public void Parser_SizeFallback_EstimatesFromResolution()
        {
            var (logger, _) = NewMemoryLogger();
            var searchUrl = "https://pelispanda.org/wp-json/wpreact/v1/search?query=nosize&posts_per_page=500&page=1";
            var detailUrl = "https://pelispanda.org/wp-json/wpreact/v1/movie/no-size";

            var search = @"{""results"":[{""id"":6,""slug"":""no-size"",""title"":""No Size Test"",""original_title"":""No Size Test"",""year"":2024,""type"":""pelicula""}],""total"":1,""pages"":1}";

            var web = NewTestWebClient((detailUrl, "movie-no-size.json"));
            var parser = new PelisPandaParser(web, logger, SiteLink);
            var response = BuildResponse(searchUrl, search);
            var releases = parser.ParseResponse(response).ToList();

            Assert.That(releases.Count, Is.EqualTo(1));
            Assert.That(releases[0].Size, Is.EqualTo(1L * 1024 * 1024 * 1024), "720p without size → 1GB estimate");
        }

        [Test]
        public void Parser_BadDownloadLink_SkipsRowOnly()
        {
            var (logger, target) = NewMemoryLogger();
            var searchUrl = "https://pelispanda.org/wp-json/wpreact/v1/search?query=bad&posts_per_page=500&page=1";
            var detailUrl = "https://pelispanda.org/wp-json/wpreact/v1/movie/bad-links";

            var search = @"{""results"":[{""id"":5,""slug"":""bad-links"",""title"":""Bad Links Test"",""original_title"":""Bad Links Test"",""year"":2024,""type"":""pelicula""}],""total"":1,""pages"":1}";

            var web = NewTestWebClient((detailUrl, "movie-bad-link.json"));
            var parser = new PelisPandaParser(web, logger, SiteLink);
            var response = BuildResponse(searchUrl, search);
            var releases = parser.ParseResponse(response).ToList();

            Assert.That(releases.Count, Is.EqualTo(1), "Only the valid magnet row produces a release");
            Assert.That(releases[0].MagnetUri, Is.Not.Null);
            Assert.That(target.Logs.Any(l => l.Contains("WARN") && l.Contains("malformed magnet")),
                "Expected a WARN line for the malformed magnet row");
            Assert.That(target.Logs.Any(l => l.Contains("WARN") && l.Contains("malformed download")),
                "Expected a WARN line for the malformed HTTP row");
        }

        [Test]
        public void Parser_OutOfRangeYear_FallsBackToToday()
        {
            var (logger, _) = NewMemoryLogger();
            var searchUrl = "https://pelispanda.org/wp-json/wpreact/v1/search?query=oob&posts_per_page=500&page=1";
            var detailUrl = "https://pelispanda.org/wp-json/wpreact/v1/movie/oob";

            var search = @"{""results"":[{""id"":99,""slug"":""oob"",""title"":""OOB"",""original_title"":""OOB"",""year"":-1,""type"":""pelicula""}],""total"":1,""pages"":1}";
            var detail = @"{""downloads"":[{""download_link"":""magnet:?xt=urn:btih:OOB1111111111111111111111111111111111111&dn=oob"",""quality"":""1080p"",""language"":""Latino"",""size"":""1.0 GB"",""subs"":0}]}";

            using (var fixture = new TempFixture("oob", detail))
            {
                var web = new Jackett.Test.TestHelpers.TestWebClient();
                web.RegisterRequestCallback(detailUrl, fixture.FixtureName);
                var parser = new PelisPandaParser(web, logger, SiteLink);
                var response = BuildResponse(searchUrl, search);
                var releases = parser.ParseResponse(response).ToList();

                Assert.That(releases.Count, Is.EqualTo(1));
                Assert.That(releases[0].PublishDate.Date, Is.EqualTo(System.DateTime.Today));
            }
        }

        [Test]
        public void Parser_TitleFallback_UsesSlugWhenTitlesAreNull()
        {
            var (logger, _) = NewMemoryLogger();
            var searchUrl = "https://pelispanda.org/wp-json/wpreact/v1/search?query=t&posts_per_page=500&page=1";
            var detailUrl = "https://pelispanda.org/wp-json/wpreact/v1/movie/the-fallback-slug";

            var search = @"{""results"":[{""id"":12,""slug"":""the-fallback-slug"",""title"":null,""original_title"":null,""year"":2023,""type"":""pelicula""}],""total"":1,""pages"":1}";
            var detail = @"{""downloads"":[{""download_link"":""magnet:?xt=urn:btih:TFB1111111111111111111111111111111111111&dn=tfb"",""quality"":""1080p"",""language"":""Latino"",""size"":""1.0 GB"",""subs"":0}]}";

            using (var fixture = new TempFixture("titlefallback", detail))
            {
                var web = new Jackett.Test.TestHelpers.TestWebClient();
                web.RegisterRequestCallback(detailUrl, fixture.FixtureName);
                var parser = new PelisPandaParser(web, logger, SiteLink);
                var response = BuildResponse(searchUrl, search);
                var releases = parser.ParseResponse(response).ToList();

                Assert.That(releases.Count, Is.EqualTo(1));
                Assert.That(releases[0].Title, Does.StartWith("the-fallback-slug"));
            }
        }

        [Test]
        public void Parser_Series_ProducesEpisodeReleases()
        {
            var (logger, _) = NewMemoryLogger();
            var searchUrl = "https://pelispanda.org/wp-json/wpreact/v1/search?query=stranger&posts_per_page=500&page=1";
            var serieUrl = "https://pelispanda.org/wp-json/wpreact/v1/serie/stranger-things-relatos-del-85/related";
            var movieUrl = "https://pelispanda.org/wp-json/wpreact/v1/movie/heroe-de-la-libertad";
            var animeUrl = "https://pelispanda.org/wp-json/wpreact/v1/anime/darwins-game";

            var web = NewTestWebClient(
                (movieUrl, "movie.json"),
                (animeUrl, "anime-empty.json"),
                (serieUrl, "serie-related.json"));

            var parser = new PelisPandaParser(web, logger, SiteLink);
            var response = BuildResponse(searchUrl, LoadFixture("search.json"));
            var releases = parser.ParseResponse(response).ToList();

            var episodes = releases.Where(r => r.Details != null &&
                r.Details.AbsoluteUri.Contains("serie/stranger-things-relatos-del-85")).ToList();

            Assert.That(episodes.Count, Is.EqualTo(2));
            Assert.That(episodes[0].Title, Does.Contain("S01E01"));
            Assert.That(episodes[1].Title, Does.Contain("S01E02"));
            Assert.That(episodes[0].Category, Does.Contain(Jackett.Common.Models.TorznabCatType.TV.ID));
        }

        [Test]
        public void Parser_DetailReturns429_ItemSkipped_OthersStillReturned()
        {
            var (logger, target) = NewMemoryLogger();
            var searchUrl = "https://pelispanda.org/wp-json/wpreact/v1/search?query=mix&posts_per_page=500&page=1";
            var movieUrl = "https://pelispanda.org/wp-json/wpreact/v1/movie/heroe-de-la-libertad";
            var animeUrl = "https://pelispanda.org/wp-json/wpreact/v1/anime/darwins-game";
            var serieUrl = "https://pelispanda.org/wp-json/wpreact/v1/serie/stranger-things-relatos-del-85/related";

            var web = new Jackett.Test.TestHelpers.TestWebClient();
            web.RegisterRequestCallback(movieUrl, "PelisPanda/movie.json");
            web.RegisterRequestCallback(animeUrl,
                new Jackett.Common.Utils.Clients.WebResult
                {
                    Status = (System.Net.HttpStatusCode)429,
                    ContentBytes = System.Text.Encoding.UTF8.GetBytes("rate limited")
                });
            web.RegisterRequestCallback(serieUrl, "PelisPanda/serie-related.json");

            var parser = new PelisPandaParser(web, logger, SiteLink);
            var response = BuildResponse(searchUrl, LoadFixture("search.json"));
            var releases = parser.ParseResponse(response).ToList();

            Assert.That(releases.Any(r => r.Details != null && r.Details.AbsoluteUri.Contains("pelicula/heroe-de-la-libertad")));
            Assert.That(releases.Any(r => r.Details != null && r.Details.AbsoluteUri.Contains("serie/stranger-things-relatos-del-85")));
            Assert.That(releases.Any(r => r.Details != null && r.Details.AbsoluteUri.Contains("anime/darwins-game")), Is.False);
            Assert.That(target.Logs.Any(l => l.Contains("WARN") && l.Contains("HTTP 429")));
        }

        [Test]
        public void Parser_DetailReturnsHtml_ItemSkipped()
        {
            var (logger, target) = NewMemoryLogger();
            var searchUrl = "https://pelispanda.org/wp-json/wpreact/v1/search?query=html&posts_per_page=500&page=1";
            var movieUrl = "https://pelispanda.org/wp-json/wpreact/v1/movie/heroe-de-la-libertad";
            var animeUrl = "https://pelispanda.org/wp-json/wpreact/v1/anime/darwins-game";
            var serieUrl = "https://pelispanda.org/wp-json/wpreact/v1/serie/stranger-things-relatos-del-85/related";

            var web = new Jackett.Test.TestHelpers.TestWebClient();
            web.RegisterRequestCallback(movieUrl, "PelisPanda/error-html.json");
            web.RegisterRequestCallback(animeUrl, "PelisPanda/anime-empty.json");
            web.RegisterRequestCallback(serieUrl, "PelisPanda/serie-related.json");

            var parser = new PelisPandaParser(web, logger, SiteLink);
            var response = BuildResponse(searchUrl, LoadFixture("search.json"));
            var releases = parser.ParseResponse(response).ToList();

            Assert.That(releases.Any(r => r.Details != null && r.Details.AbsoluteUri.Contains("pelicula/heroe-de-la-libertad")), Is.False,
                "HTML body should fail JSON parse and skip the movie item");
            Assert.That(releases.Any(r => r.Details != null && r.Details.AbsoluteUri.Contains("serie/stranger-things-relatos-del-85")));
            Assert.That(target.Logs.Any(l => l.Contains("WARN") && l.Contains("JSON parse")));
        }

        [Test]
        public void Parser_Dedup_SameMagnetEmittedOnce()
        {
            var (logger, _) = NewMemoryLogger();
            var searchUrl = "https://pelispanda.org/wp-json/wpreact/v1/search?query=dup&posts_per_page=500&page=1";
            var detailUrl = "https://pelispanda.org/wp-json/wpreact/v1/movie/dup";
            var search = @"{""results"":[{""id"":7,""slug"":""dup"",""title"":""Dup Test"",""original_title"":""Dup Test"",""year"":2024,""type"":""pelicula""}],""total"":1,""pages"":1}";

            var web = NewTestWebClient((detailUrl, "movie-dup.json"));
            var parser = new PelisPandaParser(web, logger, SiteLink);
            var response = BuildResponse(searchUrl, search);
            var releases = parser.ParseResponse(response).ToList();

            Assert.That(releases.Count, Is.EqualTo(1));
        }

        [Test]
        public void Parser_PreservesSearchOrder()
        {
            var (logger, _) = NewMemoryLogger();
            var searchUrl = "https://pelispanda.org/wp-json/wpreact/v1/search?query=order&posts_per_page=500&page=1";
            var movieUrl = "https://pelispanda.org/wp-json/wpreact/v1/movie/heroe-de-la-libertad";
            var animeUrl = "https://pelispanda.org/wp-json/wpreact/v1/anime/darwins-game";
            var serieUrl = "https://pelispanda.org/wp-json/wpreact/v1/serie/stranger-things-relatos-del-85/related";

            var web = NewTestWebClient(
                (movieUrl, "movie.json"),
                (animeUrl, "anime-empty.json"),
                (serieUrl, "serie-related.json"));

            var parser = new PelisPandaParser(web, logger, SiteLink);
            var response = BuildResponse(searchUrl, LoadFixture("search.json"));
            var releases = parser.ParseResponse(response).ToList();

            var detailPaths = releases.Select(r => r.Details.AbsolutePath).Distinct().ToList();
            Assert.That(detailPaths[0], Does.Contain("pelicula/heroe-de-la-libertad"));
            Assert.That(detailPaths[1], Does.Contain("serie/stranger-things-relatos-del-85"));
            Assert.That(releases.Count, Is.EqualTo(5));
        }
    }
}
