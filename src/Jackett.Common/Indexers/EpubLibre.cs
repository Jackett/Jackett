using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers
{
    // ReSharper disable once UnusedType.Global
    // ReSharper disable once UnusedMember.Global
    public class EpubLibre : BaseWebIndexer
    {
        private string SearchUrl => SiteLink + "catalogo/index/{0}/nuevo/todos/sin/todos/{1}/ajax";
        private string SobrecargaUrl => SiteLink + "inicio/sobrecarga";
        private const int MaxItemsPerPage = 18;
        private const int MaxSearchPageLimit = 6; // 18 items per page * 6 pages = 108
        private readonly Dictionary<string, string> _apiHeaders = new Dictionary<string, string>
        {
            {"X-Requested-With", "XMLHttpRequest"},
        };
        private readonly Dictionary<string, string> _languages = new Dictionary<string, string>
        {
            {"1", "español"},
            {"2", "catalán"},
            {"3", "euskera"},
            {"4", "gallego"},
            {"5", "inglés"},
            {"6", "francés"},
            {"7", "alemán"},
            {"8", "sueco"},
            {"9", "mandarín"},
            {"10", "italiano"},
            {"11", "portugués"},
            {"12", "esperanto"}
        };

        public EpubLibre(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps)
            : base("EpubLibre",
                   description: "Más libros, Más libres",
                   link: "https://epublibre.org/",
                   caps: new TorznabCapabilities(TorznabCatType.BooksEbook),
                   configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   configData: new ConfigurationData())
        {
            Encoding = Encoding.UTF8;
            Language = "es-es";
            Type = "public";
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            base.LoadValuesFromJson(configJson);
            var releases = await PerformQuery(new TorznabQuery());
            await ConfigureIfOK(string.Empty, releases.Any(), () =>
                                    throw new Exception("Could not find any release from this URL"));
            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var searchString = "--";
            var maxPages = 2; // we scrape only 2 pages for recent torrents
            if (!string.IsNullOrWhiteSpace(query.GetQueryString()))
            {
                searchString = Uri.EscapeUriString(query.GetQueryString());
                maxPages = MaxSearchPageLimit;
            }

            var lastPublishDate = DateTime.Now;
            for (var page = 0; page < maxPages; page++)
            {
                var searchUrl = string.Format(SearchUrl, page * MaxItemsPerPage, searchString);
                var result = await RequestStringWithCookies(searchUrl, null, null, _apiHeaders);

                try
                {
                    var json = JsonConvert.DeserializeObject<dynamic>(result.Content);
                    var parser = new HtmlParser();
                    var doc = parser.ParseDocument((string)json["contenido"]);

                    var rows = doc.QuerySelectorAll("div.span2");
                    foreach (var row in rows)
                    {
                        var title = row.QuerySelector("h2").TextContent + " - " +
                                    row.QuerySelector("h1").TextContent;
                        if (!CheckTitleMatchWords(query.GetQueryString(), title))
                            continue; // skip if it doesn't contain all words

                        var banner = new Uri(row.QuerySelector("img[id=catalog]").GetAttribute("src"));
                        var qLink = row.QuerySelector("a");
                        var comments = new Uri(qLink.GetAttribute("href"));

                        var qTooltip = parser.ParseDocument(qLink.GetAttribute("data-content"));
                        // we get the language from the last class tag => class="pull-right sprite idioma_5"
                        var languageId = qTooltip.QuerySelector("div.pull-right").GetAttribute("class").Split('_')[1];
                        title += $" [{_languages[languageId]}] [epub]";
                        var qDesc = qTooltip.QuerySelectorAll("div.row-fluid > div");
                        var description = $"Rev: {qDesc[0].TextContent} Páginas: {qDesc[1].TextContent} Puntación: {qDesc[2].TextContent} Likes: {qDesc[3].TextContent}";

                        // publish date is not available in the torrent list, but we add a relative date so we can sort
                        lastPublishDate = lastPublishDate.AddMinutes(-1);

                        var release = new ReleaseInfo
                        {
                            Title = title,
                            Comments = comments,
                            Link = comments,
                            Guid = comments,
                            PublishDate = lastPublishDate,
                            BannerUrl = banner,
                            Description = description,
                            Category = new List<int> {TorznabCatType.BooksEbook.ID},
                            Size = 5242880, // 5 MB
                            Seeders = 1,
                            Peers = 2,
                            MinimumRatio = 1,
                            MinimumSeedTime = 172800, // 48 hours
                            DownloadVolumeFactor = 0,
                            UploadVolumeFactor = 1
                        };
                        releases.Add(release);
                    }

                    if (rows.Length < MaxItemsPerPage)
                        break; // this is the last page
                }
                catch (Exception ex)
                {
                    OnParseError(result.Content, ex);
                }
            }

            return releases;
        }

        public override async Task<byte[]> Download(Uri link)
        {
            var result = await RequestStringWithCookies(link.AbsoluteUri);
            if (SobrecargaUrl.Equals(result.RedirectingTo))
                throw new Exception("El servidor se encuentra sobrecargado en estos momentos. / The server is currently overloaded.");
            try {
                var parser = new HtmlParser();
                var doc = parser.ParseDocument(result.Content);
                var magnetLink = doc.QuerySelector("a[id=en_desc]").GetAttribute("href");
                return Encoding.UTF8.GetBytes(magnetLink);
            }
            catch (Exception ex)
            {
                OnParseError(result.Content, ex);
            }
            return null;
        }

        // TODO: merge this method with query.MatchQueryStringAND
        private static bool CheckTitleMatchWords(string queryStr, string title)
        {
            // this code split the words, remove words with 2 letters or less, remove accents and lowercase
            var queryMatches = Regex.Matches(queryStr, @"\b[\w']*\b");
            var queryWords = from m in queryMatches.Cast<Match>()
                             where !string.IsNullOrEmpty(m.Value) && m.Value.Length > 2
                             select Encoding.UTF8.GetString(Encoding.GetEncoding("ISO-8859-8").GetBytes(m.Value.ToLower()));

            var titleMatches = Regex.Matches(title, @"\b[\w']*\b");
            var titleWords = from m in titleMatches.Cast<Match>()
                             where !string.IsNullOrEmpty(m.Value) && m.Value.Length > 2
                             select Encoding.UTF8.GetString(Encoding.GetEncoding("ISO-8859-8").GetBytes(m.Value.ToLower()));
            titleWords = titleWords.ToArray();

            return queryWords.All(word => titleWords.Contains(word));
        }
    }
}
