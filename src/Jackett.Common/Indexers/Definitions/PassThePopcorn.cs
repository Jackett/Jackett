using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jackett.Common.Extensions;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Serializer;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class PassThePopcorn : IndexerBase
    {
        public override string Id => "passthepopcorn";
        public override string Name => "PassThePopcorn";
        public override string Description => "PassThePopcorn (PTP) is a Private site for MOVIES / TV";
        // Status: https://ptp.trackerstatus.info/
        public override string SiteLink { get; protected set; } = "https://passthepopcorn.me/";
        public override string Language => "en-US";
        public override string Type => "private";

        public override bool SupportsPagination => true;

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private static string SearchUrl => "https://passthepopcorn.me/torrents.php";
        private string AuthKey { get; set; }
        private string PassKey { get; set; }

        // TODO: merge ConfigurationDataAPILoginWithUserAndPasskeyAndFilter class with with ConfigurationDataUserPasskey
        private new ConfigurationDataAPILoginWithUserAndPasskeyAndFilter configData
        {
            get => (ConfigurationDataAPILoginWithUserAndPasskeyAndFilter)base.configData;
            set => base.configData = value;
        }

        public PassThePopcorn(IIndexerConfigurationService configService, Utils.Clients.WebClient c, Logger l,
            IProtectionService ps, ICacheService cs)
            : base(configService: configService,
                   client: c,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataAPILoginWithUserAndPasskeyAndFilter(@"Enter filter options below to restrict search results.
                                                                        Separate options with a space if using more than one option.<br>Filter options available:
                                                                        <br><code>GoldenPopcorn</code><br><code>Scene</code><br><code>Checked</code><br><code>Free</code>"))
        {
            webclient.requestDelay = 4;
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                LimitsDefault = 50,
                LimitsMax = 50,
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q, MovieSearchParam.ImdbId
                }
            };

            caps.Categories.AddCategoryMapping(1, TorznabCatType.Movies, "Feature Film");
            caps.Categories.AddCategoryMapping(2, TorznabCatType.Movies, "Short Film");
            caps.Categories.AddCategoryMapping(3, TorznabCatType.TV, "Miniseries");
            caps.Categories.AddCategoryMapping(4, TorznabCatType.Movies, "Stand-up Comedy");
            caps.Categories.AddCategoryMapping(5, TorznabCatType.Movies, "Live Performance");
            caps.Categories.AddCategoryMapping(6, TorznabCatType.Movies, "Movie Collection");

            return caps;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            IsConfigured = false;
            try
            {
                var results = await PerformQuery(new TorznabQuery());
                if (!results.Any())
                {
                    throw new Exception("Testing returned no results!");
                }

                IsConfigured = true;
                SaveConfig();
            }
            catch (Exception e)
            {
                throw new ExceptionWithConfigData(e.Message, configData);
            }

            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var configGoldenPopcornOnly = configData.FilterString.Value.ToLowerInvariant().Contains("goldenpopcorn");
            var configSceneOnly = configData.FilterString.Value.ToLowerInvariant().Contains("scene");
            var configCheckedOnly = configData.FilterString.Value.ToLowerInvariant().Contains("checked");
            var configFreeOnly = configData.FilterString.Value.ToLowerInvariant().Contains("free");

            var queryCollection = new NameValueCollection
            {
                { "action", "advanced" },
                { "json", "noredirect" },
                { "grouping", "0" },
            };

            if (configFreeOnly)
            {
                queryCollection.Set("freetorrent", "1");
            }

            if (configGoldenPopcornOnly)
            {
                queryCollection.Set("scene", "2");
            }

            var searchQuery = query.ImdbID.IsNotNullOrWhiteSpace() ? query.ImdbID : query.GetQueryString();

            if (searchQuery.IsNotNullOrWhiteSpace())
            {
                queryCollection.Set("searchstr", searchQuery);
            }

            var queryCats = MapTorznabCapsToTrackers(query).Select(int.Parse).Distinct().ToList();

            if (searchQuery.IsNullOrWhiteSpace() && queryCats.Any())
            {
                queryCats.ForEach(cat => queryCollection.Set($"filter_cat[{cat}]", "1"));
            }

            if (query.Limit > 0 && query.Offset > 0)
            {
                var page = query.Offset / query.Limit + 1;
                queryCollection.Set("page", page.ToString());
            }

            var movieListSearchUrl = $"{SearchUrl}?{queryCollection.GetQueryString()}";

            var authHeaders = new Dictionary<string, string>
            {
                { "ApiUser", configData.User.Value },
                { "ApiKey", configData.Key.Value }
            };

            var indexerResponse = await RequestWithCookiesAndRetryAsync(movieListSearchUrl, headers: authHeaders);
            if (indexerResponse.IsRedirect) // untested
            {
                indexerResponse = await RequestWithCookiesAndRetryAsync(movieListSearchUrl, headers: authHeaders);
            }

            var seasonRegex = new Regex(@"\bS\d{2,3}(E\d{2,3})?\b", RegexOptions.Compiled);

            var releases = new List<ReleaseInfo>();

            try
            {
                var jsonResponse = STJson.Deserialize<PassThePopcornResponse>(indexerResponse.ContentString);

                if (jsonResponse.TotalResults == "0" ||
                    jsonResponse.TotalResults.IsNullOrWhiteSpace() ||
                    jsonResponse.Movies == null)
                {
                    return releases;
                }

                foreach (var result in jsonResponse.Movies)
                {
                    foreach (var torrent in result.Torrents)
                    {
                        if (configGoldenPopcornOnly && !torrent.GoldenPopcorn)
                        {
                            // Skip release if user only wants GoldenPopcorn
                            continue;
                        }

                        if (configSceneOnly && !torrent.Scene)
                        {
                            // Skip release if user only wants Scene
                            continue;
                        }

                        if (configCheckedOnly && !torrent.Checked)
                        {
                            // Skip release if user only wants Checked
                            continue;
                        }

                        var downloadVolumeFactor = torrent.FreeleechType?.ToUpperInvariant() switch
                        {
                            "FREELEECH" or "NEUTRAL LEECH" => 0,
                            "HALF LEECH" => 0.5,
                            _ => 1
                        };

                        if (configFreeOnly && downloadVolumeFactor != 0.0)
                        {
                            continue;
                        }

                        var id = torrent.Id;
                        var title = torrent.ReleaseName;
                        var infoUrl = GetInfoUrl(result.GroupId, id);

                        var categories = new List<int> { TorznabCatType.Movies.ID };

                        if (title != null && seasonRegex.Match(title).Success)
                        {
                            categories.Add(TorznabCatType.TV.ID);
                        }

                        var uploadVolumeFactor = torrent.FreeleechType?.ToUpperInvariant() switch
                        {
                            "NEUTRAL LEECH" => 0,
                            _ => 1
                        };

                        var release = new ReleaseInfo
                        {
                            Guid = infoUrl,
                            Title = title,
                            Year = int.Parse(result.Year),
                            Details = infoUrl,
                            Link = GetDownloadUrl(id, jsonResponse.AuthKey, jsonResponse.PassKey),
                            Category = categories,
                            Size = long.Parse(torrent.Size),
                            Grabs = int.Parse(torrent.Snatched),
                            Seeders = int.Parse(torrent.Seeders),
                            Peers = int.Parse(torrent.Leechers) + int.Parse(torrent.Seeders),
                            PublishDate = DateTime.Parse(torrent.UploadTime + " +0000", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
                            Imdb = result.ImdbId.IsNotNullOrWhiteSpace() ? int.Parse(result.ImdbId) : 0,
                            DownloadVolumeFactor = downloadVolumeFactor,
                            UploadVolumeFactor = uploadVolumeFactor,
                            MinimumRatio = 1,
                            MinimumSeedTime = 345600,
                            Genres = result.Tags?.ToList() ?? new List<string>(),
                            Poster = GetPosterUrl(result.Cover)
                        };

                        var titleTags = new List<string>();

                        if (result.Year.IsNotNullOrWhiteSpace())
                        {
                            release.Description += $"<br>\nYear: {result.Year}";
                        }

                        if (torrent.Quality.IsNotNullOrWhiteSpace())
                        {
                            release.Description += $"<br>\nQuality: {torrent.Quality}";
                        }

                        if (torrent.Resolution.IsNotNullOrWhiteSpace())
                        {
                            titleTags.Add(torrent.Resolution);
                            release.Description += $"<br>\nResolution: {torrent.Resolution}";
                        }

                        if (torrent.Source.IsNotNullOrWhiteSpace())
                        {
                            titleTags.Add(torrent.Source);
                            release.Description += $"<br>\nSource: {torrent.Source}";
                        }

                        if (torrent.Codec.IsNotNullOrWhiteSpace())
                        {
                            titleTags.Add(torrent.Codec);
                            release.Description += $"<br>\nCodec: {torrent.Codec}";
                        }

                        if (torrent.Container.IsNotNullOrWhiteSpace())
                        {
                            titleTags.Add(torrent.Container);
                            release.Description += $"<br>\nContainer: {torrent.Container}";
                        }

                        if (torrent.Scene)
                        {
                            titleTags.Add("Scene");
                            release.Description += "<br>\nScene";
                        }

                        if (torrent.Checked)
                        {
                            titleTags.Add("Checked");
                            release.Description += "<br>\nChecked";
                        }

                        if (torrent.GoldenPopcorn)
                        {
                            titleTags.Add("Golden Popcorn");
                            release.Description += "<br>\nGolden Popcorn";
                        }

                        if (torrent.RemasterTitle.IsNotNullOrWhiteSpace())
                        {
                            titleTags.Add(torrent.RemasterTitle);
                        }

                        if (configData.AddAttributesToTitle.Value && titleTags.Any())
                        {
                            release.Title += " [" + string.Join(" / ", titleTags) + "]";
                        }

                        releases.Add(release);
                    }
                }
            }
            catch (Exception ex)
            {
                OnParseError(indexerResponse.ContentString, ex);
            }

            return releases;
        }

        private Uri GetDownloadUrl(int torrentId, string authKey, string passKey)
        {
            var query = new NameValueCollection
            {
                { "action", "download" },
                { "id", torrentId.ToString() },
                { "authkey", authKey },
                { "torrent_pass", passKey }
            };

            return new UriBuilder(SiteLink)
            {
                Path = "/torrents.php",
                Query = query.GetQueryString()
            }.Uri;
        }

        private Uri GetInfoUrl(string groupId, int torrentId)
        {
            var query = new NameValueCollection
            {
                { "id", groupId },
                { "torrentid", torrentId.ToString() },
            };

            return new UriBuilder(SiteLink)
            {
                Path = "/torrents.php",
                Query = query.GetQueryString()
            }.Uri;
        }

        private static Uri GetPosterUrl(string cover)
        {
            if (cover.IsNotNullOrWhiteSpace() &&
                Uri.TryCreate(cover, UriKind.Absolute, out var posterUri) &&
                (posterUri.Scheme == Uri.UriSchemeHttp || posterUri.Scheme == Uri.UriSchemeHttps))
            {
                return posterUri;
            }

            return null;
        }
    }

    public class PassThePopcornResponse
    {
        public string TotalResults { get; set; }
        public IReadOnlyCollection<PassThePopcornMovie> Movies { get; set; }
        public string AuthKey { get; set; }
        public string PassKey { get; set; }
    }

    public class PassThePopcornMovie
    {
        public string GroupId { get; set; }
        public string Title { get; set; }
        public string Year { get; set; }
        public string ImdbId { get; set; }
        public string Cover { get; set; }
        public IReadOnlyCollection<string> Tags { get; set; }
        public IReadOnlyCollection<PassThePopcornTorrent> Torrents { get; set; }
    }

    public class PassThePopcornTorrent
    {
        public int Id { get; set; }
        public string Quality { get; set; }
        public string Source { get; set; }
        public string Container { get; set; }
        public string Codec { get; set; }
        public string Resolution { get; set; }
        public bool Scene { get; set; }
        public string Size { get; set; }
        public string UploadTime { get; set; }
        public string RemasterTitle { get; set; }
        public string Snatched { get; set; }
        public string Seeders { get; set; }
        public string Leechers { get; set; }
        public string ReleaseName { get; set; }
        public bool Checked { get; set; }
        public bool GoldenPopcorn { get; set; }
        public string FreeleechType { get; set; }
    }
}
