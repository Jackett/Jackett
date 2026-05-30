using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jackett.Common.Indexers.Definitions.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Newtonsoft.Json.Linq;
using NLog;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class SecretCinema : GazelleTracker
    {
        public override string Id => "secretcinema";
        public override string Name => "Secret Cinema";
        public override string Description => "Secret Cinema is a Private ratioless site for rare MOVIES.";
        public override string SiteLink { get; protected set; } = "https://secret-cinema.pw/";
        public override string Language => "en-US";
        public override string Type => "private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private static readonly Regex _YearRegex = new(@"(\b|[-._ ])((?:19|20)\d{2})(\b|[-._ ])", RegexOptions.Compiled);

        public SecretCinema(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
                            ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cs: cs,
                   supportsFreeleechTokens: false) // ratioless tracker
        {
        }

        private static TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q, MovieSearchParam.ImdbId
                },
                MusicSearchParams = new List<MusicSearchParam>
                {
                    MusicSearchParam.Q, MusicSearchParam.Album, MusicSearchParam.Artist, MusicSearchParam.Label, MusicSearchParam.Year
                }
            };

            caps.Categories.AddCategoryMapping(1, TorznabCatType.Movies, "Movies");
            caps.Categories.AddCategoryMapping(2, TorznabCatType.Audio, "Music");
            // cat=3 exists but it's required a refactor in Gazelle abstract to make it work
            //caps.Categories.AddCategoryMapping(3, TorznabCatType.Books, "E-Books");

            return caps;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var results = await base.PerformQuery(query);

            // results must contain search terms
            return results.Where(release => query.MatchQueryStringAND(release.Title));
        }

        protected override bool ReleaseInfoPostParse(ReleaseInfo release, JObject torrent, JObject result)
        {
            var media = torrent.GetValue("media")?.Value<string>();

            switch (media)
            {
                case "SD":
                    release.Category.Remove(TorznabCatType.Movies.ID);
                    release.Category.Add(TorznabCatType.MoviesSD.ID);
                    break;
                case "720p":
                case "1080p":
                case "2160p":
                case "4k": // not verified
                    release.Category.Remove(TorznabCatType.Movies.ID);
                    release.Category.Add(TorznabCatType.MoviesHD.ID);
                    break;
                case "DVD-R":
                    release.Category.Remove(TorznabCatType.Movies.ID);
                    release.Category.Add(TorznabCatType.MoviesDVD.ID);
                    break;
                case "BDMV":
                    release.Category.Remove(TorznabCatType.Movies.ID);
                    release.Category.Add(TorznabCatType.MoviesBluRay.ID);
                    break;
            }

            if (IsAnyMovieCategory(release.Category))
            {
                var remasterTitle = torrent.GetValue("remasterTitle")?.Value<string>()?.Trim();

                if (!string.IsNullOrWhiteSpace(remasterTitle) && _YearRegex.IsMatch(remasterTitle))
                {
                    release.Title = WebUtility.HtmlDecode(remasterTitle);
                }
                else
                {
                    var title = WebUtility.HtmlDecode(result.GetValue("groupName")?.Value<string>());

                    release.Title = $"{title} ({result.GetValue("groupYear")?.Value<string>()}) {media}".Trim();

                    if (!string.IsNullOrWhiteSpace(remasterTitle))
                    {
                        release.Title += $" / {WebUtility.HtmlDecode(remasterTitle)}";
                    }

                    // Replace media formats with standards
                    release.Title = Regex.Replace(release.Title, @"\bBDMV\b", "COMPLETE BLURAY", RegexOptions.IgnoreCase);
                    release.Title = Regex.Replace(release.Title, @"\bSD\b", "DVDRip", RegexOptions.IgnoreCase);
                }
            }

            var time = torrent.GetValue("time")?.Value<string>();

            if (!string.IsNullOrWhiteSpace(time))
            {
                release.PublishDate = DateTime.ParseExact(time + " +0200", "yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
            }

            return true;
        }

        private static bool IsAnyMovieCategory(ICollection<int> category)
        {
            return category.Contains(TorznabCatType.Movies.ID) || TorznabCatType.Movies.SubCategories.Any(subCat => category.Contains(subCat.ID));
        }
    }
}
