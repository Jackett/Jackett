using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers.Definitions
{
    [ExcludeFromCodeCoverage]
    public class LaMovie: IndexerBase
    {
        public override string Id => "lamovie";
        public override string Name => "LaMovie";
        public override string Description => "LaMovie is a semi-private site for movies and TV shows in latin spanish.";
        public override string SiteLink { get; protected set; } = "https://la.movie/";
        public override string Language => "es-419";
        public override string Type => "semi-private";
        private string LoginUrl => SiteLink + "wp-json/wpf/v1/auth/login";

        private ConfigurationDataLaMovie Configuration
        {
            get => (ConfigurationDataLaMovie)configData;
            set => configData = value;
        }

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                MovieSearchParams = new()
                {
                    MovieSearchParam.Q
                }
            };

            caps.Categories.AddCategoryMapping(1, TorznabCatType.MoviesHD);
            caps.Categories.AddCategoryMapping(2, TorznabCatType.MoviesUHD);

            return caps;
        }

        public LaMovie(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
                       ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataLaMovie())
        {
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var payload = new JObject
            {
                ["email"] = Configuration.Email.Value,
                ["password"] = Configuration.Password.Value
            }.ToString();

            var headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json",
                ["Accept"] = "application/json"
            };

            var result = await RequestWithCookiesAndRetryAsync(
                LoginUrl,
                cookieOverride: CookieHeader,
                method: RequestType.POST,
                referer: SiteLink,
                data: null,
                headers: headers,
                rawbody: payload
                );

            var json = JObject.Parse(result.ContentString);
            var token = json.SelectToken("data.token")?.ToString();

            await ConfigureIfOK(token, IsAuthorized(token), () =>
            {
                var contentString = result.ContentString;
                var json = JObject.Parse(contentString);
                var errorMessage = json.Value<string>("msg");
                throw new ExceptionWithConfigData(errorMessage, Configuration);
            });

            return IndexerConfigurationStatus.Completed;
        }

        private bool IsAuthorized(string token) => string.IsNullOrWhiteSpace(token)
            ? throw new ExceptionWithConfigData("Login response did not contain a token", Configuration) : true;

        protected new async Task ConfigureIfOK(string token, bool isLoggedin, Func<Task> onError)
        {
            if (isLoggedin)
            {
                configData.AddDynamic("authToken", new ConfigurationData.StringConfigurationItem("Auth Token") { Value = token });
                IsConfigured = true;
                SaveConfig();
            }
            else
            {
                await onError();
            }
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var searchTerm = "";
            var searchUrl = $"/wp-api/v1/search?filter=%7B%7D&postType=any&q={searchTerm}&postsPerPage=26";

            return releases;
        }

        private List<ReleaseInfo> ParseReleases(WebResult response, TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            return releases;
        }

        public override async Task<byte[]> Download(Uri link)
        {
            return null;
        }
    }
}
