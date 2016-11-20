using CsQuery;
using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Jackett.Models.IndexerConfig;
using System.Collections.Specialized;

namespace Jackett.Indexers
{
    class HDCity : BaseIndexer, IIndexer
    {
        private string SearchUrl { get { return SiteLink + "index.php?page=torrents&"; } }
        private string LoginUrl { get { return SiteLink + "index.php?page=login"; } }
        private const int MAXPAGES = 10;

        new ConfigurationDataBasicLogin configData
        {
            get { return (ConfigurationDataBasicLogin)base.configData; }
            set { base.configData = value; }
        }

        public HDCity(IIndexerManagerService i, Logger l, IWebClient w, IProtectionService ps)
            : base(name: "hdcity",
                description: "hdcity  is a private torrent website with HD torrents and strict rules on their content.",
                link: "https://hdcity.li/",
                manager: i,
                client: w,
                logger: l,
                p: ps,
                configData: new ConfigurationDataBasicLogin())
        {
            TorznabCaps.Categories.Clear();

            AddCategoryMapping("12", TorznabCatType.MoviesBluRay);// Movie/Blu-Ray
            AddCategoryMapping("13", TorznabCatType.MoviesHD);//Movie/Blu-ray 1080p/i
            AddCategoryMapping("13", TorznabCatType.MoviesHD);//Movie/Blu-ray 1080p/i
            AddCategoryMapping("14", TorznabCatType.MoviesHD);//Movie/720p
            AddCategoryMapping("15", TorznabCatType.MoviesWEBDL);//Movie/1080 HDTV & WEB-DL
            AddCategoryMapping("16", TorznabCatType.MoviesWEBDL);//Movie/720 HDTV & WEB-DL
            AddCategoryMapping("17", TorznabCatType.MoviesHD); //Movies/Blu-ray Remux

            AddCategoryMapping("22", TorznabCatType.TVHD);//TV Show/1080
            AddCategoryMapping("23", TorznabCatType.TVHD);//TV Show/720
            AddCategoryMapping("24", TorznabCatType.TVHD);//TV Show/1080 HDTV & WEB-DL
            AddCategoryMapping("25", TorznabCatType.TVHD);//TV Show/720 HDTV & WEB-DL

        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);
            var loginPage = await RequestStringWithCookies(LoginUrl, string.Empty);

            var pairs = new Dictionary<string, string> {
                { "uid", configData.Username.Value },
                { "pwd", configData.Password.Value }
            };

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, loginPage.Cookies, true, null, LoginUrl);

            await ConfigureIfOK(result.Cookies, result.Content != null && result.Content.Contains("logout.php"), () =>
            {
                var errorMessage = "Couldn't login";
                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }


        protected async new Task<WebClientByteResult> RequestStringWithCookies(string url, string cookieOverride = null, string referer = null, Dictionary<string, string> headers = null)
        {
            var request = new Utils.Clients.WebRequest()
            {
                Url = url,
                Type = RequestType.GET,
                Cookies = CookieHeader,
                Referer = referer,
                Headers = headers
            };

            if (cookieOverride != null)
                request.Cookies = cookieOverride;
            WebClientByteResult result = await webclient.GetBytes(request);
            UpdateCookieHeader(result.Cookies, cookieOverride);
            return result;
        }

        protected async new Task<WebClientByteResult> RequestStringWithCookiesAndRetry(string url, string cookieOverride = null, string referer = null, Dictionary<string, string> headers = null)
        {
            Exception lastException = null;
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    return await RequestStringWithCookies(url, cookieOverride, referer, headers);
                }
                catch (Exception e)
                {
                    logger.Error(string.Format("On attempt {0} checking for results from {1}: {2}", (i + 1), DisplayName, e.Message));
                    lastException = e;
                }
                await Task.Delay(500);
            }

            throw lastException;
        }

        public async Task<CQ> getDOM(string url)
        {
            Console.WriteLine(url);
            Dictionary<String, String> headers = new Dictionary<string, string>();
            headers.Add("Content-Type", "text/html;charset=ISO-8859-1");
            WebClientByteResult results = await RequestStringWithCookiesAndRetry(url, null, null, headers);
            byte[] htmlResponseBytes = results.Content;
            CQ dom = Encoding.GetEncoding("ISO-8859-1").GetString(results.Content);
            return dom;                      
        }
        public List<ReleaseInfo> parseReleaseInfoPage(CQ dom)
        {
            var releases = new List<ReleaseInfo>();

            int rowCount = 0;
            var rows = dom["table.lista tbody:has(tr td.header a:contains('Cat.'))"].Find("tr");

            foreach (var row in rows)
            {
                CQ qRow = row.Cq();
                if (rowCount < 2) //skip 2 rows because there's an empty row & a title/sort row
                {
                    rowCount++;
                    continue;
                }

                ReleaseInfo release = new ReleaseInfo();
                String renderizedRow = row.Render();
                release.Title = qRow.Find("td:eq(1) > a:eq(0)").Text();
                if(release.Title == "")
                {
                    continue;
                }
                    release.Description = release.Title;
                //release.Imdb = imdb
                release.MinimumRatio = 1;
                release.MinimumSeedTime = 345600;



                int seeders, peers;
                if (ParseUtil.TryCoerceInt(qRow.Find("td:eq(5) a").Text(), out seeders))
                {
                    release.Seeders = seeders;
                    if (ParseUtil.TryCoerceInt(qRow.Find("td:eq(6) a").Text(), out peers))
                    {
                        release.Peers = peers + release.Seeders;
                    }
                }

                string fullSize = qRow.Find("td:eq(9)").Text();
                release.Size = ReleaseInfo.GetBytes(fullSize);

                release.Guid = new Uri(SiteLink + qRow.Find("td:eq(1) a").Attr("href"));
                release.Link = new Uri(SiteLink + qRow.Find("td:eq(2) a").Attr("href"));
                //release.Comments = new Uri(SiteLink + qRow.Find("td.mainblockcontent b a").Attr("href") + "#comments");

                release.PublishDate = DateTime.ParseExact(qRow.Find("td:eq(4)").Text(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToLocalTime();

                string categoryUrl = qRow.Find("td:eq(0) > a").Attr("href");
                string category = categoryUrl.Substring(categoryUrl.LastIndexOf("=") + 1, categoryUrl.Length - categoryUrl.LastIndexOf("=") - 1);
                release.Category = MapTrackerCatToNewznab(category);

                releases.Add(release);
            }
            return releases;
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            List<ReleaseInfo> releases = new List<ReleaseInfo>();
           
            try
            {
                var searchurls = new List<string>();
                var searchUrl = SearchUrl;// string.Format(SearchUrl, HttpUtility.UrlEncode()));
                var queryCollection = new NameValueCollection();
                var searchString = query.GetQueryString();



                List<String> categories = MapTorznabCapsToTrackers(query);
                if (categories.Count > 0)
                {
                    string categoryString = "";
                    foreach (var cat in categories)
                    {
                        categoryString += cat + ";";
                    }
                    queryCollection.Add("category", categoryString);
                }



                if (!string.IsNullOrWhiteSpace(searchString))
                {
                    queryCollection.Add("search", searchString);
                }



                queryCollection.Add("active", "1");
                queryCollection.Add("options", "0");

                string searchUrlPage1 = searchUrl + queryCollection.GetQueryString();


                int pages = 1;
                CQ page1DOM = await getDOM(searchUrlPage1);
                ParseUtil.TryCoerceInt(page1DOM["table > tbody > tr > td > form[name=change_pagepages] > select.drop_pager > option:last-child"].Text(), out pages);
                if (pages > MAXPAGES)
                {
                    pages = MAXPAGES;
                }
                //page 1
                releases.AddRange(parseReleaseInfoPage(page1DOM));
                for (int page = 2; page <= pages; page++)
                {
                    NameValueCollection queryCollectionPage = new NameValueCollection(queryCollection);
                    queryCollectionPage.Add("pages", page.ToString());
                    queryCollectionPage.Add("order", "3");
                    queryCollectionPage.Add("by", "2");
                    string searchUrlPageX = searchUrl + queryCollectionPage.GetQueryString();
                    CQ pageXDOM = await getDOM(searchUrlPageX);
                    releases.AddRange(parseReleaseInfoPage(pageXDOM));

                }
            }
            catch (Exception ex)
            {
                OnParseError(ex.ToString(), ex);
            }

            return releases;
        }
    }
}