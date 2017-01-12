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
using System.Text;
using System.Threading.Tasks;

using System.Collections.Specialized;
using System.Text.RegularExpressions;
using Jackett.Models.IndexerConfig.Bespoke;


namespace Jackett.Indexers
{
    class HDCity : BaseIndexer, IIndexer
    {
        private string SearchUrl { get { return SiteLink + "index.php?page=torrents&"; } }
        private string LoginUrl { get { return SiteLink + "index.php?page=login"; } }
        private const int MAXPAGES = 10;
        private Dictionary<string, string> mediaMappings = new Dictionary<string, string>();

        new ConfigurationDataHDCity configData
        {
            get { return (ConfigurationDataHDCity)base.configData; }
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
                configData: new ConfigurationDataHDCity())
        {
            TorznabCaps.Categories.Clear();

            //Movies
            addCategoryMappingByMediaType("1", TorznabCatType.MoviesHD, "movie");// Movie/ diferents types
            addCategoryMappingByMediaType("12", TorznabCatType.MoviesBluRay,"movie");// Movie/Blu-Ray
            addCategoryMappingByMediaType("13", TorznabCatType.MoviesHD, "movie");//Movie/Blu-ray 1080 rip
            addCategoryMappingByMediaType("14", TorznabCatType.MoviesHD, "movie");//Movie/720p rip
            addCategoryMappingByMediaType("15", TorznabCatType.MoviesWEBDL, "movie");//Movie/1080 HDTV & WEB-DL
            addCategoryMappingByMediaType("16", TorznabCatType.MoviesWEBDL, "movie");//Movie/720 HDTV & WEB-DL
            addCategoryMappingByMediaType("17", TorznabCatType.MoviesHD, "movie"); //Movies/Blu-ray Remux
            addCategoryMappingByMediaType("72", TorznabCatType.MoviesHD, "movie"); //Movies/Blu-ray 4K Remux
            addCategoryMappingByMediaType("73", TorznabCatType.MoviesHD, "movie"); //Movies/Blu-ray 4K Rip
            addCategoryMappingByMediaType("110", TorznabCatType.MoviesHD, "movie"); //Movies/4K HDTV & WEB-DL 

            //TVShows
            addCategoryMappingByMediaType("2", TorznabCatType.TVWEBDL, "tvshow");//TV Show/diferents types
            addCategoryMappingByMediaType("22", TorznabCatType.TVHD,"tvshow");//TV Show/Blu-ray 1080 rip
            addCategoryMappingByMediaType("23", TorznabCatType.TVHD, "tvshow");//TV Show/Blu-ray 720 rip
            addCategoryMappingByMediaType("24", TorznabCatType.TVWEBDL, "tvshow");//TV Show/1080 HDTV & WEB-DL
            addCategoryMappingByMediaType("25", TorznabCatType.TVWEBDL, "tvshow");//TV Show/720 HDTV & WEB-DL
            addCategoryMappingByMediaType("76", TorznabCatType.TVHD, "tvshow");//TV Show/4K BDRemux
            addCategoryMappingByMediaType("77", TorznabCatType.TVHD, "tvshow");//TV Show/4K Blu-ray Rip
            addCategoryMappingByMediaType("111", TorznabCatType.TVHD, "tvshow");//TV Show/4K WEB-DL

            //Anime
            addCategoryMappingByMediaType("3", TorznabCatType.TVAnime, "tvshow");//Anime/ diferents types
            addCategoryMappingByMediaType("28", TorznabCatType.TVAnime, "tvshow");//Anime/Blu-ray 1080 rip
            addCategoryMappingByMediaType("29", TorznabCatType.TVAnime, "tvshow");//Anime/Blu-ray 720 rip
            addCategoryMappingByMediaType("32", TorznabCatType.TVAnime, "tvshow");//Anime/Blu-ray Remux
            addCategoryMappingByMediaType("107", TorznabCatType.TVAnime, "tvshow");//Anime/HDTV 4K

            //XXX
            addCategoryMappingByMediaType("7", TorznabCatType.XXX, "xxx");//XXX/diferent type
            addCategoryMappingByMediaType("49", TorznabCatType.XXX, "xxx");//XXX/Blu-ray 1080 rip
            addCategoryMappingByMediaType("52", TorznabCatType.XXX, "xxx");//XXX/Blu-ray 720 rip
            addCategoryMappingByMediaType("56", TorznabCatType.XXX, "xxx");//XXX/HDTV 1080
            addCategoryMappingByMediaType("60", TorznabCatType.XXX, "xxx");//XXX/HDTV 720
            addCategoryMappingByMediaType("105", TorznabCatType.XXX, "xxx");//XXX/4K Blu-Ray Remux 
            addCategoryMappingByMediaType("106", TorznabCatType.XXX, "xxx");//XXX/4K Blu-Ray Rip
            addCategoryMappingByMediaType("115", TorznabCatType.XXX, "xxx");//XXX/4K HDTV & WEB-DL

            //Animation
            addCategoryMappingByMediaType("9", TorznabCatType.TVAnime, "tvshow");//Animation/diferents types
            addCategoryMappingByMediaType("41", TorznabCatType.TVAnime, "tvshow");//Animation/Blu-ray 1080
            addCategoryMappingByMediaType("42", TorznabCatType.TVAnime, "tvshow");//Animation/Blu-ray 720
            addCategoryMappingByMediaType("43", TorznabCatType.TVAnime, "tvshow");//Animation/WEB-DL 1080
            addCategoryMappingByMediaType("44", TorznabCatType.TVAnime, "tvshow");//Animation/WEB-DL 720
            addCategoryMappingByMediaType("63", TorznabCatType.TVAnime, "tvshow");//Animation/Blu-Ray Remux
            addCategoryMappingByMediaType("81", TorznabCatType.TVAnime, "tvshow");//Animation/Blu-Ray 4K Remux
            addCategoryMappingByMediaType("82", TorznabCatType.TVAnime, "tvshow");//Animation/Blu-Ray 4K Rip
            addCategoryMappingByMediaType("108", TorznabCatType.TVAnime, "tvshow");//Animation/4K HDTV WEB-DL

            //Show Animation
            addCategoryMappingByMediaType("10", TorznabCatType.TVAnime, "tvshow");//Animation/diferent type
            addCategoryMappingByMediaType("30", TorznabCatType.TVAnime, "tvshow");//Animation/Blu-ray 1080
            addCategoryMappingByMediaType("31", TorznabCatType.TVAnime, "tvshow");//Animation/Blu-ray 720
            addCategoryMappingByMediaType("33", TorznabCatType.TVAnime, "tvshow");//Animation/WEB-DL 1080
            addCategoryMappingByMediaType("35", TorznabCatType.TVAnime, "tvshow");//Animation/WEB-DL 720
            addCategoryMappingByMediaType("85", TorznabCatType.TVAnime, "tvshow");//Animation/Blu-Ray 4K Remux
            addCategoryMappingByMediaType("86", TorznabCatType.TVAnime, "tvshow");//Animation/Blu-Ray 4K Rip
            addCategoryMappingByMediaType("109", TorznabCatType.TVAnime, "tvshow");//Animation/4K HDTV WEB-DL
        }



        public void addCategoryMappingByMediaType(string HDCityID, TorznabCategory torznabCategory,String mediaType)
        {
            mediaMappings.Add(HDCityID, mediaType);
            AddCategoryMapping(HDCityID, torznabCategory);

        }

        /**public void imdbSearch(string title)
        {
            String encodedTitle = HttpUtility.UrlEncode(title);
            string url = String.Format("https://v2.sg.media-imdb.com/suggests/{0}/{1}.json",encodedTitle.Substring(0,1).ToLower(), encodedTitle);
            System.Net.WebRequest request = System.Net.WebRequest.Create(url);
            WebResponse response = request.GetResponse();

            using (Stream stream = response.GetResponseStream())
            {
                JavaScriptSerializer js = new JavaScriptSerializer();

                string responseString = new StreamReader(stream).ReadToEnd();

                String variable = "imdb&" + title.Replace(" ", "_");
                String jsonResponse = responseString.Substring(variable.Length + 1);
                jsonResponse = jsonResponse.Substring(0, jsonResponse.Length - 1);

                var obj = js.Deserialize<dynamic>(jsonResponse);
                var dd = obj["d"];
                if (dd != null && dd.Length>0)
                {
                    foreach(var di in dd) { 
                        Console.WriteLine(di["l"]);
                        Console.WriteLine(di["id"]);
                        Console.WriteLine(di["q"]);
                    }
                }


            }
        }**/

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
                if (rowCount < 1) //skip 2 rows because there's an empty row & a title/sort row
                {
                    rowCount++;
                    continue;
                }

                ReleaseInfo release = new ReleaseInfo();
                 String renderizedRow = row.Render();
                string categoryUrl = qRow.Find("td:eq(0) > a").Attr("href");
                if (categoryUrl==null || categoryUrl == "")
                {
                    continue;
                }
                string category = categoryUrl.Substring(categoryUrl.LastIndexOf("=") + 1, categoryUrl.Length - categoryUrl.LastIndexOf("=") - 1);
                release.Category = MapTrackerCatToNewznab(category);
                string title = qRow.Find("td:eq(1) > a:eq(0)").Text();

                if (!mediaMappings.ContainsKey(category))
                {
                    Console.WriteLine(String.Format("Category {0} is not matched on result {1}, contact the developer!", category, title));
                    continue;
                }
                string mediaType = mediaMappings[category];


                //Check filter
                if (mediaType == "tvshow" && configData.TVShowsFilter.Value != "")
                {
                    if (!checkRegularExpressionMatching(title, configData.TVShowsFilter.Value)){
                        Console.WriteLine(String.Format("{0} not match {1}. DISCARTED", title.ToLower(), configData.TVShowsFilter.Value));
                        continue;
                    }
                } else 
                //Check filter
                if (mediaType == "movie" && configData.MoviesFilter.Value != "")
                {
                    if (!checkRegularExpressionMatching(title, configData.MoviesFilter.Value)){
                        Console.WriteLine(String.Format("{0} not match {1}. DISCARTED", title.ToLower(), configData.MoviesFilter.Value));
                        continue;
                    }
                }


                if (configData.TVShowEnglishMode.Value && mediaType.Equals("tvshow"))
                {
                    title = title.ToLower().Replace("ª", "").Replace("°","").Replace("[pack]","").Trim();
                    


                    Regex qariRegex = new Regex("((temporada|temp|t)( *)(?<season>[0-9]{1,3})|(?<season>[0-9]{1,3})( *)(temporada|temp))(( *)[xe]( *)(?<episode>[0-9]{1,4}))?");
                    MatchCollection mc = qariRegex.Matches(title);
                    //We are finding tv shows
                    if (mc.Count > 0 && mc[0].Success)
                    {
                        string prefix = title.Substring(0, mc[0].Index);
                        string suffix = title.Substring(mc[0].Index + mc[0].Length);
                        int season = int.Parse(mc[0].Groups["season"].Value);
                        //Find only season
                        if (mc[0].Groups["episode"].Value.Equals(""))
                        {
                            title = string.Format("{0} S{1:00} {2}", prefix, season, suffix);
                        }
                        else
                        {
                            int episode = int.Parse(mc[0].Groups["episode"].Value);
                            title = string.Format("{0} S{1:00}E{2:00} {3}", prefix, season, episode, suffix);
                        }
                    }
                }

                release.Title = title;
                if (release.Title == "")
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
                release.Comments = new Uri(SiteLink + qRow.Find("td:eq(1) > a:eq(0)").Attr("href"));


                release.PublishDate = DateTime.ParseExact(qRow.Find("td:eq(4)").Text(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToLocalTime();

               

                releases.Add(release);
            }
            return releases;
        }

        private bool checkRegularExpressionMatching(string stringToCheck, string regularExpression)
        {
            Regex qariRegex = new Regex(regularExpression,RegexOptions.IgnoreCase);
            MatchCollection mc = qariRegex.Matches(stringToCheck.ToLower());
            return mc.Count > 0 && mc[0].Success;
        }
           

        public async Task<IEnumerable<ReleaseInfo>> performSimpleSearch(TorznabQuery query,NameValueCollection queryParams,String searchString)
        {
            NameValueCollection queryCollection = new NameValueCollection(queryParams);
            List<ReleaseInfo> releases = new List<ReleaseInfo>();

            if (!string.IsNullOrWhiteSpace(searchString))
            {               
                queryCollection.Add("search", searchString);
            }

            string searchUrlPage1 = SearchUrl + queryCollection.GetQueryString();
            int pages = 0;
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
                string searchUrlPageX = SearchUrl + queryCollectionPage.GetQueryString();
                CQ pageXDOM = await getDOM(searchUrlPageX);
                releases.AddRange(parseReleaseInfoPage(pageXDOM));

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
                /**if (configData.TranslateMediaNamesToEnglish.Value)
                {
                    //imdbSearch(searchString);
                }**/


                List<String> categories = MapTorznabCapsToTrackers(query);
                /**if (categories.Count > 0)
                {
                    string categoryString = "";
                    foreach (var cat in categories)
                    {
                        categoryString += cat + ";";
                    }
                    queryCollection.Add("category", categoryString);
                }**/
                queryCollection.Add("category", "0");
                queryCollection.Add("active", "1");
                queryCollection.Add("options", "0");

                releases.AddRange(await performSimpleSearch(query, queryCollection, searchString));

                if (configData.TVShowEnglishMode.Value)
                {                
                    //Regex qariRegex = new Regex("(?<tvshow>(.*)) S(?<season>([0-9]{1,3}))(E[0-9](?<episode>([0-9]{1,4})))? ?(?<suffix>(.*))",RegexOptions.IgnoreCase);
                    Regex qariRegex = new Regex("(?<tvshow>(.*)) S(?<season>([0-9]{1,3}))(E(?<episode>([0-9]{1,4})))? ?(?<suffix>(.*))", RegexOptions.IgnoreCase);
                    MatchCollection mc = qariRegex.Matches(searchString);
                    //We are finding tv shows
                    if (mc.Count > 0 && mc[0].Success)
                    {
                        string substring = searchString.Substring(mc[0].Index, mc[0].Length);
                        string tvshow = mc[0].Groups["tvshow"].Value;
                        int season = int.Parse(mc[0].Groups["season"].Value);
                        string suffix = mc[0].Groups["suffix"].Value;
                        //Find only season
                        if (mc[0].Groups["episode"].Value.Equals(""))
                        {
                            releases.AddRange(await performSimpleSearch(query, queryCollection, string.Format("{0} temp {1} {2}", tvshow, season, suffix)));
                            releases.AddRange(await performSimpleSearch(query, queryCollection, string.Format("{0} T{1} {2}", tvshow, season, suffix)));
                            if (releases.Count == 0)
                            {
                                releases.AddRange(await performSimpleSearch(query, queryCollection, string.Format("{0} {1}", tvshow, season)));
                            }
                        }
                        //Find episodes
                        else
                        {
                            int episode = int.Parse(mc[0].Groups["episode"].Value);
                            releases.AddRange(await performSimpleSearch(query, queryCollection, string.Format("{0} {1}x{2:00} {3}", tvshow, season, episode, suffix)));
                        }
                    }
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