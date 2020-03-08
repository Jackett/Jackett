using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;

namespace Jackett.Common.Indexers
{
    public class HDOlimpo : BaseWebIndexer
    {
        private string LoginUrl => SiteLink + "login";
        private string LogoutUrl => SiteLink + "logout";
        private string SearchUrl => SiteLink + "torrents/buscar?page=1";
        private string CommentsUrl => SiteLink + "torrents/detalles/";
        private string DownloadUrl => SiteLink + "torrents/descargar/";
        private string BannerUrl => SiteLink + "storage/imagenes/portadas/m/";

        private new ConfigurationDataBasicLoginWithEmail configData
        {
            get => (ConfigurationDataBasicLoginWithEmail)base.configData;
            set => base.configData = value;
        }

        public HDOlimpo(IIndexerConfigurationService configService, WebClient w, Logger l, IProtectionService ps)
            : base(name: "HD-Olimpo",
                description: "HD-Olimpo is a SPANISH site for HD content",
                link: "https://hdolimpo.co/",
                caps: new TorznabCapabilities(),
                configService: configService,
                client: w,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLoginWithEmail())
        {
            Encoding = Encoding.UTF8;
            Language = "es-es";
            Type = "private";

            var premiumItem = new BoolItem() { Name = "Include Premium torrents in search results", Value = false };
            configData.AddDynamic("IncludePremium", premiumItem);

            // TODO: add subcategories
            AddCategoryMapping(1, TorznabCatType.Movies, "Movies");
            AddCategoryMapping(2, TorznabCatType.TV, "TV");
            AddCategoryMapping(3, TorznabCatType.TVDocumentary, "Documentary");
            AddCategoryMapping(4, TorznabCatType.TVAnime, "Anime");
            AddCategoryMapping(8, TorznabCatType.TVSport, "Sports");
            AddCategoryMapping(9, TorznabCatType.XXX, "XXX");
            AddCategoryMapping(10, TorznabCatType.Audio, "Music");
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            await DoLogin();

            return IndexerConfigurationStatus.RequiresTesting;
        }

        private async Task DoLogin()
        {
            var loginPage = await RequestStringWithCookies(LoginUrl, string.Empty);
            var token = new Regex("name=\"_token\" value=\"(.*?)\">").Match(loginPage.Content).Groups[1].ToString();
            var pairs = new Dictionary<string, string> {
                { "_token", token },
                { "email", configData.Email.Value },
                { "password", configData.Password.Value },
                { "remember", "on" }
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, loginPage.Cookies, true, null, LoginUrl);
            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains(LogoutUrl), () =>
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(result.Content);
                var errorMessage = dom.QuerySelector(".error").TextContent.Trim();
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var includePremium = ((BoolItem)configData.GetDynamic("IncludePremium")).Value;

            var pairs = new Dictionary<string, string>
            {
                {"freetorrent", "false"},
                {"ordenar_por", "created_at"},
                {"orden", "desc"},
                {"titulo", query.GetQueryString()}
            };

            var cats = MapTorznabCapsToTrackers(query);
            var category = cats.Count == 1 ? cats.First() : "0";
            pairs.Add("categoria", category);

            var boundary = "---------------------------" + (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds.ToString(CultureInfo.InvariantCulture).Replace(".", "");
            var bodyParts = new List<string>();

            foreach (var pair in pairs)
            {
                var part = "--" + boundary + "\r\n" +
                           "Content-Disposition: form-data; name=\"" + pair.Key + "\"\r\n" +
                           "\r\n" +
                           pair.Value;
                bodyParts.Add(part);
            }

            bodyParts.Add("--" + boundary + "--");
            var body = string.Join("\r\n", bodyParts);

            var headers = new Dictionary<string, string>
            {
                {"Content-Type", "multipart/form-data; boundary=" + boundary}
            };
            AddXsrfTokenHeader(headers, configData.CookieHeader.Value);

            var response = await PostDataWithCookies(SearchUrl, pairs, configData.CookieHeader.Value, SiteLink, headers, body);
            if (response.Content.StartsWith("<!doctype html>"))
            {
                //Cookie appears to expire after a period of time or logging in to the site via browser
                await DoLogin();

                AddXsrfTokenHeader(headers, configData.CookieHeader.Value);
                response = await PostDataWithCookies(SearchUrl, pairs, configData.CookieHeader.Value, SiteLink, headers, body);
            }

            var releases = ParseResponse(response, includePremium);

            return releases;
        }

        private static void AddXsrfTokenHeader(IDictionary<string, string> headers, string cookie)
        {
            var xsrfToken = new Regex("XSRF-TOKEN=([^;]+)").Match(cookie).Groups[1].ToString();
            xsrfToken = Uri.UnescapeDataString(xsrfToken);
            headers["X-XSRF-TOKEN"] = xsrfToken;
        }

        private List<ReleaseInfo> ParseResponse(WebClientStringResult response, bool includePremium)
        {
            var releases = new List<ReleaseInfo>();

            var torrents = CheckResponse(response);

            try
            {
                foreach (var torrent in torrents)
                {
                    var release = new ReleaseInfo();

                    release.Title = (string)torrent["titulo"] + " " + (string)torrent["titulo_extra"];

                    // for downloading "premium" torrents you need special account
                    if ((string)torrent["premium"] == "si")
                    {
                        if (includePremium)
                            release.Title += " [PREMIUM]";
                        else
                            continue;
                    }

                    release.Comments = new Uri(CommentsUrl + (string)torrent["id"]);
                    release.Guid = release.Comments;

                    release.PublishDate = DateTime.Now;
                    if (torrent["created_at"] != null)
                        release.PublishDate = DateTime.Parse((string)torrent["created_at"]);

                    if (torrent["portada"] != null)
                        release.BannerUrl = new Uri(BannerUrl + (string)(torrent["portada"]["hash"]) + "." + (string)(torrent["portada"]["ext"]));

                    release.Category = MapTrackerCatToNewznab((string)torrent["categoria"]);
                    release.Size = (long)torrent["size"];

                    release.Seeders = (int)torrent["seeders"];
                    release.Peers = release.Seeders + (int)torrent["leechers"];
                    release.Grabs = (long)torrent["snatched"];

                    release.InfoHash = (string)torrent["plain_info_hash"];
                    release.Link = new Uri(DownloadUrl + (string)torrent["id"]);

                    var files = (JArray)JsonConvert.DeserializeObject<dynamic>((string)torrent["files_list"]);
                    release.Files = files.Count;

                    release.DownloadVolumeFactor = (string)torrent["freetorrent"] == "0" ? 1 : 0;
                    release.UploadVolumeFactor = (string)torrent["doubletorrent"] == "0" ? 1 : 2;

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800; // 48 hours

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(response.Content, ex);
            }

            return releases;
        }

        private JArray CheckResponse(WebClientStringResult response)
        {
            try
            {
                var json = JsonConvert.DeserializeObject<dynamic>(response.Content);
                if (!(json is JObject) || json["torrents"] == null || !(json["torrents"]["data"] is JArray) || json["torrents"]["data"] == null)
                    throw new Exception("Server error");
                return (JArray)json["torrents"]["data"];
            }
            catch (Exception e)
            {
                logger.Error("CheckResponse() Error: ", e.Message);
                throw new Exception(response.Content);
            }
        }
    }
}
