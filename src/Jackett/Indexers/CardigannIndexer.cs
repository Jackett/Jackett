using Jackett.Utils.Clients;
using NLog;
using Jackett.Services;
using Jackett.Utils;
using Jackett.Models;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System;
using Jackett.Models.IndexerConfig;
using System.Collections.Specialized;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using static Jackett.Models.IndexerConfig.ConfigurationData;
using AngleSharp.Parser.Html;
using System.Text.RegularExpressions;
using System.Web;
using AngleSharp.Dom;
using AngleSharp.Dom.Html;

namespace Jackett.Indexers
{
    public class CardigannIndexer : BaseIndexer, IIndexer
    {
        protected IndexerDefinition Definition;
        public new string ID { get { return (Definition != null ? Definition.Site : GetIndexerID(GetType())); } }

        protected WebClientStringResult landingResult;
        protected IHtmlDocument landingResultDocument;

        new ConfigurationData configData
        {
            get { return (ConfigurationData)base.configData; }
            set { base.configData = value; }
        }

        // Cardigann yaml classes
        public class IndexerDefinition {
            public string Site { get; set; }
            public List<settingsField> Settings { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string Language { get; set; }
            public string Encoding { get; set; }
            public List<string> Links { get; set; }
            public capabilitiesBlock Caps { get; set; }
            public loginBlock Login { get; set; }
            public ratioBlock Ratio { get; set; }
            public searchBlock Search { get; set; }
            // IndexerDefinitionStats not needed/implemented
        }
        public class settingsField
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public string Label { get; set; }
        }

        public class capabilitiesBlock
        {
            public Dictionary<string, string> Categories  { get; set; }
            public Dictionary<string, List<string>> Modes { get; set; }
        }

        public class loginBlock
        {
            public string Path { get; set; }
            public string Method { get; set; }
            public string Form { get; set; }
            public Dictionary<string, string> Inputs { get; set; }
            public List<errorBlock> Error { get; set; }
            public pageTestBlock Test { get; set; }
        }

        public class errorBlock
        {
            public string Path { get; set; }
            public string Selector { get; set; }
            public selectorBlock Message { get; set; }
        }

        public class selectorBlock
        {
            public string Selector { get; set; }
            public string Text { get; set; }
            public string Attribute { get; set; }
            public string Remove { get; set; }
            public List<filterBlock> Filters { get; set; }
            public Dictionary<string, string> Case { get; set; }
        }

        public class filterBlock
        {
            public string Name { get; set; }
            public dynamic Args { get; set; }
        }

        public class pageTestBlock
        {
            public string Path { get; set; }
            public string Selector { get; set; }
        }

        public class ratioBlock : selectorBlock
        {
            public string Path { get; set; }
        }

        public class searchBlock
        {
            public string Path { get; set; }
            public Dictionary<string, string> Inputs { get; set; }
            public rowsBlock Rows { get; set; }
            public Dictionary<string, selectorBlock> Fields { get; set; }
        }

        public class rowsBlock : selectorBlock
        {
            public int After { get; set; }
            //public string Remove { get; set; } // already inherited
            public selectorBlock Dateheaders { get; set; }
        }

        public CardigannIndexer(IIndexerManagerService i, IWebClient wc, Logger l, IProtectionService ps)
            : base(manager: i,
                   client: wc,
                   logger: l,
                   p: ps)
        {
        }

        public CardigannIndexer(IIndexerManagerService i, IWebClient wc, Logger l, IProtectionService ps, string DefinitionString)
            : base(manager: i,
                   client: wc,
                   logger: l,
                   p: ps)
        {
            Init(DefinitionString);
        }

        protected void Init(string DefinitionString)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .IgnoreUnmatchedProperties()
                .Build();
            Definition = deserializer.Deserialize<IndexerDefinition>(DefinitionString);

            // Add default data if necessary
            if (Definition.Settings == null)
                Definition.Settings = new List<settingsField>();

            if (Definition.Settings.Count == 0)
            {
                Definition.Settings.Add(new settingsField { Name = "username", Label = "Username", Type = "text" });
                Definition.Settings.Add(new settingsField { Name = "password", Label = "Password", Type = "password" });
            }

            if (Definition.Encoding == null)
                Definition.Encoding = "iso-8859-1";

            if (Definition.Login != null && Definition.Login.Method == null)
                Definition.Login.Method = "form";

            // init missing mandatory attributes
            DisplayName = Definition.Name;
            DisplayDescription = Definition.Description;
            SiteLink = Definition.Links[0]; // TODO: implement alternative links
            if (!SiteLink.EndsWith("/"))
                SiteLink += "/";
            Language = Definition.Language;
            TorznabCaps = TorznabUtil.CreateDefaultTorznabTVCaps(); // TODO implement caps

            // init config Data
            configData = new ConfigurationData();
            foreach (var Setting in Definition.Settings)
            {
                configData.AddDynamic(Setting.Name, new StringItem { Name = Setting.Label });
            }

            foreach (var Category in Definition.Caps.Categories)
            {
                var cat = TorznabCatType.GetCatByName(Category.Value);
                if (cat == null)
                {
                    logger.Error(string.Format("CardigannIndexer ({0}): Can't find a category for {1}", ID, Category.Value));
                    continue;
                }
                AddCategoryMapping(Category.Key, TorznabCatType.GetCatByName(Category.Value));
                
            }
        }

        protected Dictionary<string, object> getTemplateVariablesFromConfigData()
        {
            Dictionary<string, object> variables = new Dictionary<string, object>();

            foreach (settingsField Setting in Definition.Settings)
            {
                variables[".Config."+Setting.Name] = ((StringItem)configData.GetDynamic(Setting.Name)).Value;
            }
            return variables;
        }

        // A very bad implementation of the golang template/text templating engine.
        // But it should work for most basic constucts used by Cardigann definitions.
        protected string applyGoTemplateText(string template, Dictionary<string, object> variables = null)
        {
            if (variables == null)
            {
                variables = getTemplateVariablesFromConfigData();
            }

            // handle if ... else ... expression
            Regex IfElseRegex = new Regex(@"{{if\s*(.+?)\s*}}(.*?){{\s*else\s*}}(.*?){{\s*end\s*}}");
            var IfElseRegexMatches = IfElseRegex.Match(template);

            while (IfElseRegexMatches.Success)
            {
                string conditionResult = null;

                string all = IfElseRegexMatches.Groups[0].Value;
                string condition = IfElseRegexMatches.Groups[1].Value;
                string onTrue = IfElseRegexMatches.Groups[2].Value;
                string onFalse = IfElseRegexMatches.Groups[3].Value;

                if (condition.StartsWith("."))
                {
                    string value = (string)variables[condition];
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        conditionResult = onTrue;
                    }
                    else
                    {
                        conditionResult = onFalse;
                    }
                }
                else
                {
                    throw new NotImplementedException("CardigannIndexer: Condition operation '" + condition + "' not implemented");
                }
                template = template.Replace(all, conditionResult);
                IfElseRegexMatches = IfElseRegexMatches.NextMatch();
            }

            // handle range expression
            Regex RangeRegex = new Regex(@"{{\s*range\s*(.+?)\s*}}(.*?){{\.}}(.*?){{end}}");
            var RangeRegexMatches = RangeRegex.Match(template);

            while (RangeRegexMatches.Success)
            {
                string expanded = string.Empty;

                string all = RangeRegexMatches.Groups[0].Value;
                string variable = RangeRegexMatches.Groups[1].Value;
                string prefix = RangeRegexMatches.Groups[2].Value;
                string postfix = RangeRegexMatches.Groups[3].Value;

                foreach (string value in (List<string>)variables[variable])
                {
                    expanded += prefix + value + postfix;
                }
                template = template.Replace(all, expanded);
                RangeRegexMatches = RangeRegexMatches.NextMatch();
            }

            // handle simple variables
            Regex VariablesRegEx = new Regex(@"{{\s*(\..+?)\s*}}");
            var VariablesRegExMatches = VariablesRegEx.Match(template);

            while (VariablesRegExMatches.Success)
            {
                string expanded = string.Empty;

                string all = VariablesRegExMatches.Groups[0].Value;
                string variable = VariablesRegExMatches.Groups[1].Value;

                string value = (string)variables[variable];
                template = template.Replace(all, value);
                VariablesRegExMatches = VariablesRegExMatches.NextMatch();
            }

            return template;
        }

        protected bool checkForLoginError(WebClientStringResult loginResult)
        {
            var ErrorBlocks = Definition.Login.Error;

            if (ErrorBlocks == null)
                return true; // no error

                var loginResultParser = new HtmlParser();
            var loginResultDocument = loginResultParser.Parse(loginResult.Content);
            foreach (errorBlock error in ErrorBlocks)
            {
                var selection = loginResultDocument.QuerySelector(error.Selector);
                if (selection != null)
                {
                    string errorMessage = selection.TextContent;
                    if (error.Message != null)
                    {
                        var errorSubMessage = loginResultDocument.QuerySelector(error.Message.Selector);
                        errorMessage = errorSubMessage.TextContent;
                    }
                    throw new ExceptionWithConfigData(string.Format("Login failed: {0}", errorMessage.Trim()), configData);
                }
            }
            return true; // no error
        }
        
        protected async Task<bool> DoLogin()
        {
            var Login = Definition.Login;

            if (Login == null)
                return false;

            if (Login.Method == "post")
            {
                var pairs = new Dictionary<string, string>();
                foreach (var Input in Definition.Login.Inputs)
                {
                    var value = applyGoTemplateText(Input.Value);
                    pairs.Add(Input.Key, value);
                }

                var LoginUrl = SiteLink + Login.Path;
                configData.CookieHeader.Value = null;
                var loginResult = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, SiteLink, true);
                configData.CookieHeader.Value = loginResult.Cookies;

                checkForLoginError(loginResult);
            }
            else if (Login.Method == "form")
            {
                var LoginUrl = SiteLink + Login.Path;

                var pairs = new Dictionary<string, string>();

                var CaptchaConfigItem = (RecaptchaItem)configData.GetDynamic("Captcha");

                if (CaptchaConfigItem != null)
                { 
                    if (!string.IsNullOrWhiteSpace(CaptchaConfigItem.Cookie))
                    {
                        // for remote users just set the cookie and return
                        CookieHeader = CaptchaConfigItem.Cookie;
                        return true;
                    }

                    var CloudFlareCaptchaChallenge = landingResultDocument.QuerySelector("script[src=\"/cdn-cgi/scripts/cf.challenge.js\"]");
                    if (CloudFlareCaptchaChallenge != null)
                    {
                        var CloudFlareQueryCollection = new NameValueCollection();
                        CloudFlareQueryCollection["id"] = CloudFlareCaptchaChallenge.GetAttribute("data-ray");
                    
                        CloudFlareQueryCollection["g-recaptcha-response"] = CaptchaConfigItem.Value;
                        var ClearanceUrl = resolvePath("/cdn-cgi/l/chk_captcha?" + CloudFlareQueryCollection.GetQueryString());

                        var ClearanceResult = await RequestStringWithCookies(ClearanceUrl.ToString(), null, SiteLink);

                        if (ClearanceResult.IsRedirect) // clearance successfull
                        {
                            // request real login page again
                            landingResult = await RequestStringWithCookies(LoginUrl, null, SiteLink);
                            var htmlParser = new HtmlParser();
                            landingResultDocument = htmlParser.Parse(landingResult.Content);
                        }
                        else
                        {
                            throw new ExceptionWithConfigData(string.Format("Login failed: Cloudflare clearance failed using cookies {0}: {1}", CookieHeader, ClearanceResult.Content), configData);
                        }
                    }
                    else
                    {
                        pairs.Add("g-recaptcha-response", CaptchaConfigItem.Value);
                    }
                }

                var FormSelector = Login.Form;
                if (FormSelector == null)
                    FormSelector = "form";

                // landingResultDocument might not be initiated if the login is caused by a relogin during a query
                if (landingResultDocument == null)
                {
                    await GetConfigurationForSetup();
                }

                var form = landingResultDocument.QuerySelector(FormSelector);
                if (form == null)
                {
                    throw new ExceptionWithConfigData(string.Format("Login failed: No form found on {0} using form selector {1}", LoginUrl, FormSelector), configData);
                }

                var inputs = form.QuerySelectorAll("input");
                if (inputs == null)
                {
                    throw new ExceptionWithConfigData(string.Format("Login failed: No inputs found on {0} using form selector {1}", LoginUrl, FormSelector), configData);
                }

                var submitUrl = resolvePath(form.GetAttribute("action"));
                
                foreach (var input in inputs)
                {
                    var name = input.GetAttribute("name");
                    if (name == null)
                        continue;

                    var value = input.GetAttribute("value");
                    if (value == null)
                        value = "";

                    pairs[name] = value;
                }
   
                foreach (var Input in Definition.Login.Inputs)
                {
                    var value = applyGoTemplateText(Input.Value);
                    pairs[Input.Key] = value;
                }

                // automatically solve simpleCaptchas, if used
                var simpleCaptchaPresent = landingResultDocument.QuerySelector("script[src*=\"simpleCaptcha\"]");
                if(simpleCaptchaPresent != null)
                {
                    var captchaUrl = resolvePath("simpleCaptcha.php?numImages=1");
                    var simpleCaptchaResult = await RequestStringWithCookies(captchaUrl.ToString(), null, LoginUrl);
                    var simpleCaptchaJSON = JObject.Parse(simpleCaptchaResult.Content);
                    var captchaSelection = simpleCaptchaJSON["images"][0]["hash"].ToString();
                    pairs["captchaSelection"] = captchaSelection;
                    pairs["submitme"] = "X";
                }
                
                var loginResult = await RequestLoginAndFollowRedirect(submitUrl.ToString(), pairs, configData.CookieHeader.Value, true, null, SiteLink, true);
                configData.CookieHeader.Value = loginResult.Cookies;

                checkForLoginError(loginResult);
            }
            else if (Login.Method == "cookie")
            {
                configData.CookieHeader.Value = ((StringItem)configData.GetDynamic("cookie")).Value;
            }
            else
            {
                throw new NotImplementedException("Login method " + Definition.Login.Method + " not implemented");
            }
            return true;
        }

        protected async Task<bool> TestLogin()
        {
            var Login = Definition.Login;

            if (Login == null || Login.Test == null)
                return false;

            // test if login was successful
            var LoginTestUrl = SiteLink + Login.Test.Path;
            var testResult = await RequestStringWithCookies(LoginTestUrl);

            if (testResult.IsRedirect)
            {
                throw new ExceptionWithConfigData("Login Failed, got redirected", configData);
            }

            if (Login.Test.Selector != null)
            {
                var testResultParser = new HtmlParser();
                var testResultDocument = testResultParser.Parse(testResult.Content);
                var selection = testResultDocument.QuerySelectorAll(Login.Test.Selector);
                if (selection.Length == 0)
                {
                    throw new ExceptionWithConfigData(string.Format("Login failed: Selector \"{0}\" didn't match", Login.Test.Selector), configData);
                }
            }
            return true;
        }

        protected bool CheckIfLoginIsNeeded(WebClientByteResult Result, IHtmlDocument document)
        {
            if (Result.IsRedirect)
            {
                return true;
            }

            if (Definition.Login == null || Definition.Login.Test == null)
                return false;

            if (Definition.Login.Test.Selector != null)
            {
                var selection = document.QuerySelectorAll(Definition.Login.Test.Selector);
                if (selection.Length == 0)
                {
                    return true;
                }
            }
            return false;
        }

        public override async Task<ConfigurationData> GetConfigurationForSetup()
        {
            var Login = Definition.Login;

            if (Login == null || Login.Method != "form")
                return configData;

            var LoginUrl = SiteLink + Login.Path;

            configData.CookieHeader.Value = null;
            landingResult = await RequestStringWithCookies(LoginUrl, null, SiteLink);

            var htmlParser = new HtmlParser();
            landingResultDocument = htmlParser.Parse(landingResult.Content);

            var grecaptcha = landingResultDocument.QuerySelector(".g-recaptcha");
            if (grecaptcha != null)
            {
                var CaptchaItem = new RecaptchaItem();
                CaptchaItem.Name = "Captcha";
                CaptchaItem.Version = "2";
                CaptchaItem.SiteKey = grecaptcha.GetAttribute("data-sitekey");
                if (CaptchaItem.SiteKey == null) // some sites don't store the sitekey in the .g-recaptcha div (e.g. cloudflare captcha challenge page)
                    CaptchaItem.SiteKey = landingResultDocument.QuerySelector("[data-sitekey]").GetAttribute("data-sitekey");

                configData.AddDynamic("Captcha", CaptchaItem);
            }

            return configData;
        }

        public async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            configData.LoadValuesFromJson(configJson);

            await DoLogin();
            await TestLogin();

            SaveConfig();
            IsConfigured = true;
            return IndexerConfigurationStatus.Completed;
        }

        protected string applyFilters(string Data, List<filterBlock> Filters)
        {
            if (Filters == null)
                return Data;

            foreach(filterBlock Filter in Filters)
            {
                switch (Filter.Name)
                {
                    case "querystring":
                        var param = (string)Filter.Args;
                        var qsStr = Data.Split(new char[] { '?' }, 2)[1];
                        qsStr = qsStr.Split(new char[] { '#' }, 2)[0];
                        var qs = HttpUtility.ParseQueryString(qsStr);
                        Data = qs.Get(param);
                        break;
                    case "timeparse":
                    case "dateparse":
                        var layout = (string)Filter.Args;
                        try
                        {
                            var Date = DateTimeUtil.ParseDateTimeGoLang(Data, layout);
                            Data = Date.ToString(DateTimeUtil.RFC1123ZPattern);
                        }
                        catch (Exception ex)
                        {
                            logger.Debug(ex.ToString());
                        }
                    break;
                    case "regexp":
                        var pattern = (string)Filter.Args;
                        var Regexp = new Regex(pattern);
                        var Match = Regexp.Match(Data);
                        Data = Match.Groups[1].Value;
                        break;
                    case "split":
                        var sep = (string)Filter.Args[0];
                        var pos = (string)Filter.Args[1];
                        var posInt = int.Parse(pos);
                        var strParts = Data.Split(sep[0]);
                        if (posInt < 0)
                        {
                            posInt += strParts.Length;
                        }
                        Data = strParts[posInt];
                        break;
                    case "replace":
                        var from = (string)Filter.Args[0];
                        var to = (string)Filter.Args[1];
                        Data = Data.Replace(from, to);
                        break;
                    case "trim":
                        var cutset = (string)Filter.Args;
                        Data = Data.Trim(cutset[0]);
                        break;
                    case "append":
                        var str = (string)Filter.Args;
                        Data += str;
                        break;
                    case "timeago":
                    case "fuzzytime":
                    case "reltime":
                        var timestr = (string)Filter.Args;
                        Data = DateTimeUtil.FromUnknown(timestr).ToString(DateTimeUtil.RFC1123ZPattern);
                        break;
                    default:
                        break;
                }
            }
            return Data;
        }

        protected IElement QuerySelector(IElement Element, string Selector)
        {
            // AngleSharp doesn't support the :root pseudo selector, so we check for it manually
            if (Selector.StartsWith(":root"))
            {
                Selector = Selector.Substring(5);
                while (Element.ParentElement != null)
                {
                    Element = Element.ParentElement;
                }
            }
            return Element.QuerySelector(Selector);
        }

        protected string handleSelector(selectorBlock Selector, IElement Dom)
        {
            if (Selector.Text != null)
            {
                return applyFilters(Selector.Text, Selector.Filters);
            }

            IElement selection = Dom;
            string value = null;

            if (Selector.Selector != null)
            {
                selection = QuerySelector(Dom, Selector.Selector);
                if (selection == null)
                {
                    throw new Exception(string.Format("Selector \"{0}\" didn't match {1}", Selector.Selector, Dom.OuterHtml));
                }
            }

            if (Selector.Remove != null)
            {
                foreach(var i in selection.QuerySelectorAll(Selector.Remove))
                {
                    i.Remove();
                }
            }

            if (Selector.Case != null)
            {
                foreach(var Case in Selector.Case)
                {
                    if (selection.Matches(Case.Key) || QuerySelector(selection, Case.Key) != null)
                    {
                        value = Case.Value;
                        break;
                    }
                }
                if(value == null)
                    throw new Exception(string.Format("None of the case selectors \"{0}\" matched {1}", string.Join(",", Selector.Case), selection.OuterHtml));
            }
            else if (Selector.Attribute != null)
            {
                value = selection.GetAttribute(Selector.Attribute);
            }
            else
            {
                value = selection.TextContent;
            }
                
            return applyFilters(value, Selector.Filters); ;
        }

        protected Uri resolvePath(string path)
        {
            if(path.StartsWith("http"))
            {
                return new Uri(path);
            }
            else if(path.StartsWith("/"))
            {
                var basepath = new Uri(SiteLink);
                return new Uri(basepath.Scheme+"://"+ basepath.Host + path);
            }
            else
            {
                return new Uri(SiteLink + path);
            }
            
        }

        public async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            searchBlock Search = Definition.Search;

            // init template context
            var variables = getTemplateVariablesFromConfigData();

            variables[".Query.Type"] = query.QueryType;
            variables[".Query.Q"] = query.SearchTerm;
            variables[".Query.Series"] = null;
            variables[".Query.Ep"] = query.Episode;
            variables[".Query.Season"] = query.Season;
            variables[".Query.Movie"] = null;
            variables[".Query.Year"] = null;
            variables[".Query.Limit"] = query.Limit;
            variables[".Query.Offset"] = query.Offset;
            variables[".Query.Extended"] = query.Extended;
            variables[".Query.Categories"] = query.Categories;
            variables[".Query.APIKey"] = query.ApiKey;
            variables[".Query.TVDBID"] = null;
            variables[".Query.TVRageID"] = query.RageID;
            variables[".Query.IMDBID"] = query.ImdbID;
            variables[".Query.TVMazeID"] = null;
            variables[".Query.TraktID"] = null;

            variables[".Query.Episode"] = query.GetEpisodeSearchString();
            variables[".Categories"] = MapTorznabCapsToTrackers(query);

            var KeywordTokens = new List<string>();
            var KeywordTokenKeys = new List<string> { "Q", "Series", "Movie", "Year" };
            foreach (var key in KeywordTokenKeys)
            {
                var Value = (string)variables[".Query." + key];
                if (!string.IsNullOrWhiteSpace(Value))
                    KeywordTokens.Add(Value);
            }

            if (!string.IsNullOrWhiteSpace((string)variables[".Query.Episode"]))
                KeywordTokens.Add((string)variables[".Query.Episode"]);
            variables[".Query.Keywords"] = string.Join(" ", KeywordTokens);
            variables[".Keywords"] = variables[".Query.Keywords"];

            // build search URL
            var searchUrl = SiteLink + applyGoTemplateText(Search.Path, variables) + "?";
            var queryCollection = new NameValueCollection();
            if (Search.Inputs != null)
            { 
                foreach (var Input in Search.Inputs)
                {
                    var value = applyGoTemplateText(Input.Value, variables);
                    if (Input.Key == "$raw")
                        searchUrl += value;
                    else
                        queryCollection.Add(Input.Key, value);
                }
            }
            searchUrl += "&" + queryCollection.GetQueryString();

            // send HTTP request
            var response = await RequestBytesWithCookies(searchUrl);
            var results = Encoding.GetEncoding(Definition.Encoding).GetString(response.Content);
            try
            {
                var SearchResultParser = new HtmlParser();
                var SearchResultDocument = SearchResultParser.Parse(results);

                // check if we need to login again
                var loginNeeded = CheckIfLoginIsNeeded(response, SearchResultDocument);
                if (loginNeeded)
                {
                    logger.Info(string.Format("CardigannIndexer ({0}): Relogin required", ID));
                    await DoLogin();
                    await TestLogin();
                    response = await RequestBytesWithCookies(searchUrl);
                    results = Encoding.GetEncoding(Definition.Encoding).GetString(response.Content);
                    SearchResultDocument = SearchResultParser.Parse(results);
                }

                var RowsDom = SearchResultDocument.QuerySelectorAll(Search.Rows.Selector);
                List<IElement> Rows = new List<IElement>();
                foreach (var RowDom in RowsDom)
                {
                    Rows.Add(RowDom);
                }

                // merge following rows for After selector
                var After = Definition.Search.Rows.After;
                if (After > 0)
                {
                    for (int i = 0; i < Rows.Count; i += 1)
                    {
                        var CurrentRow = Rows[i];
                        for (int j = 0; j < After; j += 1)
                        {
                            var MergeRowIndex = i + j + 1;
                            var MergeRow = Rows[MergeRowIndex];
                            List<INode> MergeNodes = new List<INode>();
                            foreach (var node in MergeRow.QuerySelectorAll("td"))
                            {
                                MergeNodes.Add(node);
                            }
                            CurrentRow.Append(MergeNodes.ToArray());
                        }
                        Rows.RemoveRange(i + 1, After);
                    }
                }

                foreach (var Row in Rows)
                {
                    try
                    {
                        var release = new ReleaseInfo();
                        release.MinimumRatio = 1;
                        release.MinimumSeedTime = 48 * 60 * 60;

                        // Parse fields
                        foreach (var Field in Search.Fields)
                        {
                            string value = handleSelector(Field.Value, Row);
                            value = ParseUtil.NormalizeSpace(value);
                            try
                            {
                                switch (Field.Key)
                                {
                                    case "download":
                                        if (value.StartsWith("magnet:"))
                                        {
                                            release.MagnetUri = new Uri(value);
                                            release.Link = release.MagnetUri;
                                        }
                                        else
                                        {
                                            release.Link = resolvePath(value);
                                        }
                                        break;
                                    case "details":
                                        var url = resolvePath(value);
                                        release.Guid = url;
                                        release.Comments = url;
                                        if (release.Guid == null)
                                            release.Guid = url;
                                        break;
                                    case "comments":
                                        var CommentsUrl = resolvePath(value);
                                        if(release.Comments == null)
                                            release.Comments = CommentsUrl;
                                        if (release.Guid == null)
                                            release.Guid = CommentsUrl;
                                        break;
                                    case "title":
                                        release.Title = value;
                                        break;
                                    case "description":
                                        release.Description = value;
                                        break;
                                    case "category":
                                        release.Category = MapTrackerCatToNewznab(value);
                                        break;
                                    case "size":
                                        release.Size = ReleaseInfo.GetBytes(value);
                                        break;
                                    case "leechers":
                                        if (release.Peers == null)
                                            release.Peers = ParseUtil.CoerceInt(value);
                                        else
                                            release.Peers += ParseUtil.CoerceInt(value);
                                        break;
                                    case "seeders":
                                        release.Seeders = ParseUtil.CoerceInt(value);
                                        if (release.Peers == null)
                                            release.Peers = release.Seeders;
                                        else
                                            release.Peers += release.Seeders;
                                        break;
                                    case "date":
                                        release.PublishDate = DateTimeUtil.FromUnknown(value);
                                        break;
                                    case "files":
                                        release.Files = ParseUtil.CoerceLong(value);
                                        break;
                                    case "grabs":
                                        release.Grabs = ParseUtil.CoerceLong(value);
                                        break;
                                    case "downloadvolumefactor":
                                        release.DownloadVolumeFactor = ParseUtil.CoerceDouble(value);
                                        break;
                                    case "uploadvolumefactor":
                                        release.UploadVolumeFactor = ParseUtil.CoerceDouble(value);
                                        break;
                                    default:
                                        break;
                                }
                            }
                            catch (Exception ex)
                            {
                                throw new Exception(string.Format("Error while parsing field={0}, selector={1}, value={2}: {3}", Field.Key, Field.Value.Selector, value, ex.Message));
                            }
                        }

                        // if DateHeaders is set go through the previous rows and look for the header selector
                        var DateHeaders = Definition.Search.Rows.Dateheaders;
                        if (DateHeaders != null)
                        {
                            var PrevRow = Row.PreviousElementSibling;
                            string value = null;
                            while (PrevRow != null)
                            {
                                try
                                {
                                    value = handleSelector(DateHeaders, PrevRow);
                                    break;
                                }
                                catch (Exception)
                                {
                                    // do nothing
                                }
                                PrevRow = PrevRow.PreviousElementSibling;
                            }
                            
                            if (value == null)
                                throw new Exception(string.Format("No date header row found for {0}", release.ToString()));

                            release.PublishDate = DateTimeUtil.FromUnknown(value);
                        }

                        releases.Add(release);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(string.Format("CardigannIndexer ({0}): Error while parsing row '{1}': {2}", ID, Row.OuterHtml, ex));
                    }
                }
            }
            catch (Exception ex)
            {
                OnParseError(results, ex);
            }

            return releases;
        }
    }
}

