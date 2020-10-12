using System.Collections.Generic;
using System.Linq;
using Jackett.Common.Models;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Jackett.Test.Common.Models
{
    [TestFixture]
    public class TorznabCapabilitiesTests
    {
        [Test]
        public void TestConstructors()
        {

            // TODO: re-order atrributes in TorznabCapabilities
            // TODO: tv search should be disabled by default
            // TODO: TorznabCapabilities(params TorznabCategory[] cats) should re-use TorznabCapabilities
            // TODO: initialize MusicSearchAvailable
            var torznabCaps = new TorznabCapabilities();
            Assert.True(torznabCaps.SearchAvailable);
            Assert.True(torznabCaps.TVSearchAvailable);
            Assert.False(torznabCaps.MovieSearchAvailable);
            Assert.False(torznabCaps.MusicSearchAvailable); // init
            Assert.False(torznabCaps.BookSearchAvailable);
            Assert.False(torznabCaps.SupportsImdbMovieSearch);
            Assert.False(torznabCaps.SupportsImdbTVSearch);
            Assert.False(torznabCaps.SupportsTvdbSearch);
            Assert.False(torznabCaps.SupportsTmdbMovieSearch);
            Assert.False(torznabCaps.SupportsTVRageSearch);
            Assert.AreEqual(0, torznabCaps.SupportedMusicSearchParamsList.Count);
            Assert.AreEqual(0, torznabCaps.Categories.Count);

            var cats = new TorznabCategory[0]; // empty
            torznabCaps = new TorznabCapabilities(cats);
            Assert.True(torznabCaps.SearchAvailable);
            Assert.True(torznabCaps.TVSearchAvailable);
            Assert.False(torznabCaps.MovieSearchAvailable);
            Assert.False(torznabCaps.MusicSearchAvailable); // init
            Assert.False(torznabCaps.BookSearchAvailable);
            Assert.False(torznabCaps.SupportsImdbMovieSearch);
            Assert.False(torznabCaps.SupportsImdbTVSearch);
            Assert.False(torznabCaps.SupportsTvdbSearch);
            Assert.False(torznabCaps.SupportsTmdbMovieSearch);
            Assert.False(torznabCaps.SupportsTVRageSearch);
            Assert.AreEqual(0, torznabCaps.SupportedMusicSearchParamsList.Count);
            Assert.AreEqual(0, torznabCaps.Categories.Count);

            // TODO: movies category enables Movies Search ??
            cats = new [] { TorznabCatType.MoviesSD };
            torznabCaps = new TorznabCapabilities(cats);
            Assert.AreEqual(1, torznabCaps.Categories.Count);
            Assert.AreEqual( TorznabCatType.MoviesSD, torznabCaps.Categories[0]);
            Assert.True(torznabCaps.MovieSearchAvailable);
        }

        [Test]
        public void TestTorznabCaps()
        {
            // test header
            var torznabCaps = new TorznabCapabilities();
            var xDocument = torznabCaps.GetXDocument();
            Assert.AreEqual("caps", xDocument.Root?.Name.LocalName);
            Assert.AreEqual("Jackett", xDocument.Root?.Element("server")?.Attribute("title")?.Value);
            Assert.True(xDocument.Root?.Element("searching")?.HasElements);
            Assert.False(xDocument.Root?.Element("categories")?.HasElements);

            // TODO: remove params when it's disabled. Review Torznab specs
            // test all features disabled
            torznabCaps = new TorznabCapabilities
            {
                SearchAvailable = false,
                TVSearchAvailable = false
            };
            xDocument = torznabCaps.GetXDocument();
            var xDoumentSearching = xDocument.Root?.Element("searching");
            Assert.AreEqual("no", xDoumentSearching?.Element("search")?.Attribute("available")?.Value);
            Assert.AreEqual("q", xDoumentSearching?.Element("search")?.Attribute("supportedParams")?.Value);
            Assert.AreEqual("no", xDoumentSearching?.Element("tv-search")?.Attribute("available")?.Value);
            Assert.AreEqual("q,season,ep", xDoumentSearching?.Element("tv-search")?.Attribute("supportedParams")?.Value);
            Assert.AreEqual("no", xDoumentSearching?.Element("movie-search")?.Attribute("available")?.Value);
            Assert.AreEqual("q", xDoumentSearching?.Element("movie-search")?.Attribute("supportedParams")?.Value);
            Assert.AreEqual("no", xDoumentSearching?.Element("music-search")?.Attribute("available")?.Value);
            Assert.AreEqual("", xDoumentSearching?.Element("music-search")?.Attribute("supportedParams")?.Value);
            Assert.AreEqual("no", xDoumentSearching?.Element("audio-search")?.Attribute("available")?.Value);
            Assert.AreEqual("", xDoumentSearching?.Element("audio-search")?.Attribute("supportedParams")?.Value);
            Assert.AreEqual("no", xDoumentSearching?.Element("book-search")?.Attribute("available")?.Value);
            Assert.AreEqual("q", xDoumentSearching?.Element("book-search")?.Attribute("supportedParams")?.Value);

            // TODO: validate invalid music params
            // TODO: book parameters should be configurable?
            // test all features enabled
            torznabCaps = new TorznabCapabilities
            {
                SearchAvailable = true,
                TVSearchAvailable = true,
                MovieSearchAvailable = true,
                BookSearchAvailable = true,
                SupportsImdbMovieSearch = true,
                SupportsImdbTVSearch = true,
                SupportsTvdbSearch = true,
                SupportsTmdbMovieSearch = true,
                SupportsTVRageSearch = true,
                SupportedMusicSearchParamsList = new List<string>{"q", "album", "artist", "label", "year"}
            };
            xDocument = torznabCaps.GetXDocument();
            xDoumentSearching = xDocument.Root?.Element("searching");
            Assert.AreEqual("yes", xDoumentSearching?.Element("search")?.Attribute("available")?.Value);
            Assert.AreEqual("q", xDoumentSearching?.Element("search")?.Attribute("supportedParams")?.Value);
            Assert.AreEqual("yes", xDoumentSearching?.Element("tv-search")?.Attribute("available")?.Value);
            Assert.AreEqual("q,season,ep,rid,tvdbid,imdbid", xDoumentSearching?.Element("tv-search")?.Attribute("supportedParams")?.Value);
            Assert.AreEqual("yes", xDoumentSearching?.Element("movie-search")?.Attribute("available")?.Value);
            Assert.AreEqual("q,imdbid,tmdbid", xDoumentSearching?.Element("movie-search")?.Attribute("supportedParams")?.Value);
            Assert.AreEqual("yes", xDoumentSearching?.Element("music-search")?.Attribute("available")?.Value);
            Assert.AreEqual("q,album,artist,label,year", xDoumentSearching?.Element("music-search")?.Attribute("supportedParams")?.Value);
            Assert.AreEqual("yes", xDoumentSearching?.Element("audio-search")?.Attribute("available")?.Value);
            Assert.AreEqual("q,album,artist,label,year", xDoumentSearching?.Element("audio-search")?.Attribute("supportedParams")?.Value);
            Assert.AreEqual("yes", xDoumentSearching?.Element("book-search")?.Attribute("available")?.Value);
            Assert.AreEqual("q,author,title", xDoumentSearching?.Element("book-search")?.Attribute("supportedParams")?.Value);

            // test categories
            torznabCaps = new TorznabCapabilities(new [] { TorznabCatType.MoviesSD }); // child category
            xDocument = torznabCaps.GetXDocument();
            var xDoumentCategories = xDocument.Root?.Element("categories")?.Elements("category").ToList();
            Assert.AreEqual(1, xDoumentCategories?.Count);
            Assert.AreEqual(TorznabCatType.MoviesSD.ID.ToString(), xDoumentCategories?.First().Attribute("id")?.Value);
            Assert.AreEqual(TorznabCatType.MoviesSD.Name, xDoumentCategories?.First().Attribute("name")?.Value);

            // TODO: child category is duplicated. should we add just parent and child without other subcats?
            torznabCaps = new TorznabCapabilities(new [] { TorznabCatType.Movies, TorznabCatType.MoviesSD }); // parent and child category
            xDocument = torznabCaps.GetXDocument();
            xDoumentCategories = xDocument.Root?.Element("categories")?.Elements("category").ToList();
            Assert.AreEqual(2, xDoumentCategories?.Count);
            Assert.AreEqual(TorznabCatType.Movies.ID.ToString(), xDoumentCategories?.First().Attribute("id")?.Value);
            Assert.AreEqual(TorznabCatType.Movies.Name, xDoumentCategories?.First().Attribute("name")?.Value);
            Assert.AreEqual(TorznabCatType.MoviesSD.ID.ToString(), xDoumentCategories?[1].Attribute("id")?.Value);
            Assert.AreEqual(TorznabCatType.MoviesSD.Name, xDoumentCategories?[1].Attribute("name")?.Value);
            var xDoumentSubCategories = xDoumentCategories?.First()?.Elements("subcat").ToList();
            Assert.AreEqual(9, xDoumentSubCategories?.Count);
            Assert.AreEqual(TorznabCatType.MoviesForeign.ID.ToString(), xDoumentSubCategories?.First().Attribute("id")?.Value);
            Assert.AreEqual(TorznabCatType.MoviesForeign.Name, xDoumentSubCategories?.First().Attribute("name")?.Value);

            // TODO: review Torznab spec about custom cats => https://github.com/Sonarr/Sonarr/wiki/Implementing-a-Torznab-indexer#caps-endpoint
            // TODO: orderby is not working
            torznabCaps = new TorznabCapabilities(new []
            {
                new TorznabCategory(100001, "CustomCat"),
                TorznabCatType.MoviesSD
            }); // custom category
            xDocument = torznabCaps.GetXDocument();
            xDoumentCategories = xDocument.Root?.Element("categories")?.Elements("category").ToList();
            Assert.AreEqual(2, xDoumentCategories?.Count);
            Assert.AreEqual("100001", xDoumentCategories?[0].Attribute("id")?.Value); // custom cats are first in the list
            Assert.AreEqual("CustomCat", xDoumentCategories?[0].Attribute("name")?.Value);
            Assert.AreEqual(TorznabCatType.MoviesSD.ID.ToString(), xDoumentCategories?[1].Attribute("id")?.Value);
            Assert.AreEqual(TorznabCatType.MoviesSD.Name, xDoumentCategories?[1].Attribute("name")?.Value);
        }
    }
}
