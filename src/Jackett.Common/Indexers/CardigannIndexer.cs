using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Jackett.Common.Helpers;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json.Linq;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Common.Indexers
{
    public class CardigannIndexer : BaseWebIndexer
    {
        protected IndexerDefinition Definition;
        public override string ID => (Definition != null ? Definition.Site : GetIndexerID(GetType()));

        protected WebClientStringResult landingResult;
        protected IHtmlDocument landingResultDocument;

        protected List<string> DefaultCategories = new List<string>();

        private new ConfigurationData configData
        {
            get => base.configData;
            set => base.configData = value;
        }

        protected readonly string[] OptionalFileds =
        {
            "imdb",
            "rageid",
            "tvdbid",
            "banner"
        };

        public CardigannIndexer(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
                                IndexerDefinition definition) : base(configService, wc, l, ps)
        {
            Definition = definition;

            // Add default data if necessary
            if (definition.Settings == null)
                definition.Settings = new List<settingsField>
                {
                    new settingsField {Name = "username", Label = "Username", Type = "text"},
                    new settingsField {Name = "password", Label = "Password", Type = "password"}
                };
            if (definition.Encoding == null)
                definition.Encoding = "UTF-8";
            if (definition.Login != null && definition.Login.Method == null)
                definition.Login.Method = "form";
            if (definition.Search.Paths == null)
                definition.Search.Paths = new List<searchPathBlock>();

            // convert definitions with a single search Path to a Paths entry
            if (definition.Search.Path != null)
            {
                var legacySearchPath = new searchPathBlock { Path = definition.Search.Path, Inheritinputs = true };
                definition.Search.Paths.Add(legacySearchPath);
            }

            // init missing mandatory attributes
            DisplayName = definition.Name;
            DisplayDescription = definition.Description;
            if (definition.Links.Count > 1)
                AlternativeSiteLinks = definition.Links.ToArray();
            DefaultSiteLink = definition.Links[0];
            if (definition.Legacylinks != null)
                LegacySiteLinks = definition.Legacylinks.ToArray();
            Encoding = Encoding.GetEncoding(definition.Encoding);
            if (!DefaultSiteLink.EndsWith("/"))
                DefaultSiteLink += "/";
            Language = definition.Language;
            Type = definition.Type;
            TorznabCaps = new TorznabCapabilities
            {
                SupportsImdbMovieSearch = definition
                                          .Caps.Modes.Where(c => c.Key == "movie-search" && c.Value.Contains("imdbid"))
                                          .Any()
            };
            if (definition.Caps.Modes.ContainsKey("music-search"))
                TorznabCaps.SupportedMusicSearchParamsList = definition.Caps.Modes["music-search"];

            // init config Data
            configData = new ConfigurationData();
            foreach (var setting in definition.Settings)
            {
                Item item;
                if (setting.Type != null)
                    switch (setting.Type)
                    {
                        case "checkbox":
                            item = new BoolItem { Value = false };
                            if (setting.Default != null && setting.Default == "true")
                                ((BoolItem)item).Value = true;
                            break;
                        case "password":
                        case "text":
                            item = new StringItem { Value = setting.Default };
                            break;
                        case "select":
                            if (setting.Options == null)
                                throw new Exception("Options must be given for the 'select' type.");
                            item = new SelectItem(setting.Options) { Value = setting.Default };
                            break;
                        case "info":
                            item = new DisplayItem(setting.Default);
                            break;
                        default:
                            throw new Exception($"Invalid setting type '{setting.Type}' specified.");
                    }
                else
                {
                    item = new StringItem { Value = setting.Default };
                    ;
                }

                item.Name = setting.Label;
                if (item.Name == null)
                    item.Name = setting.Name;
                configData.AddDynamic(setting.Name, item);
            }

            if (definition.Caps.Categories != null)
                foreach (var category in definition.Caps.Categories)
                {
                    var cat = TorznabCatType.GetCatByName(category.Value);
                    if (cat == null)
                    {
                        logger.Error(
                            string.Format(
                                "CardigannIndexer ({0}): invalid Torznab category for id {1}: {2}", ID, category.Key,
                                category.Value));
                        continue;
                    }

                    AddCategoryMapping(category.Key, cat);
                }

            if (definition.Caps.Categorymappings != null)
                foreach (var categorymapping in definition.Caps.Categorymappings)
                {
                    TorznabCategory torznabCat = null;
                    if (categorymapping.cat != null)
                    {
                        torznabCat = TorznabCatType.GetCatByName(categorymapping.cat);
                        if (torznabCat == null)
                        {
                            logger.Error(
                                string.Format(
                                    "CardigannIndexer ({0}): invalid Torznab category for id {1}: {2}", ID,
                                    categorymapping.id, categorymapping.cat));
                            continue;
                        }
                    }

                    AddCategoryMapping(categorymapping.id, torznabCat, categorymapping.desc);
                    if (categorymapping.Default)
                        DefaultCategories.Add(categorymapping.id);
                }

            LoadValuesFromJson(null);
        }

        public override void LoadValuesFromJson(JToken jsonConfig, bool useProtectionService = false)
        {
            base.LoadValuesFromJson(jsonConfig, useProtectionService);

            // add self signed cert to trusted certs
            if (Definition.Certificates != null)
                foreach (var certificateHash in Definition.Certificates)
                    webclient.AddTrustedCertificate(new Uri(SiteLink).Host, certificateHash);
        }

        protected Dictionary<string, object> getTemplateVariablesFromConfigData()
        {
            var variables = new Dictionary<string, object> { [".Config.sitelink"] = SiteLink };
            foreach (var setting in Definition.Settings)
            {
                string value;
                var item = configData.GetDynamic(setting.Name);
                value = item.GetType() == typeof(BoolItem)
                    ? ((BoolItem)item).Value ? "true" : ""
                    : item.GetType() == typeof(SelectItem) ? ((SelectItem)item).Value : ((StringItem)item).Value;
                variables[$".Config.{setting.Name}"] = value;
            }

            return variables;
        }

        // A very bad implementation of the golang template/text templating engine.
        // But it should work for most basic constucts used by Cardigann definitions.
        protected delegate string TemplateTextModifier(string str);

        protected string applyGoTemplateText(string template, Dictionary<string, object> variables = null,
                                             TemplateTextModifier modifier = null)
        {
            if (variables == null)
                variables = getTemplateVariablesFromConfigData();

            // handle re_replace expression
            // Example: {{ re_replace .Query.Keywords "[^a-zA-Z0-9]+" "%" }}
            var reReplaceRegex = new Regex(@"{{\s*re_replace\s+(\..+?)\s+""(.*?)""\s+""(.*?)""\s*}}");
            var reReplaceRegexMatches = reReplaceRegex.Match(template);
            while (reReplaceRegexMatches.Success)
            {
                var all = reReplaceRegexMatches.Groups[0].Value;
                var variable = reReplaceRegexMatches.Groups[1].Value;
                var regexp = reReplaceRegexMatches.Groups[2].Value;
                var newvalue = reReplaceRegexMatches.Groups[3].Value;
                var replaceRegex = new Regex(regexp);
                var input = (string)variables[variable];
                var expanded = replaceRegex.Replace(input, newvalue);
                if (modifier != null)
                    expanded = modifier(expanded);
                template = template.Replace(all, expanded);
                reReplaceRegexMatches = reReplaceRegexMatches.NextMatch();
            }

            // handle join expression
            // Example: {{ join .Categories "," }}
            var joinRegex = new Regex(@"{{\s*join\s+(\..+?)\s+""(.*?)""\s*}}");
            var joinMatches = joinRegex.Match(template);
            while (joinMatches.Success)
            {
                var all = joinMatches.Groups[0].Value;
                var variable = joinMatches.Groups[1].Value;
                var delimiter = joinMatches.Groups[2].Value;
                var input = (ICollection<string>)variables[variable];
                var expanded = string.Join(delimiter, input);
                if (modifier != null)
                    expanded = modifier(expanded);
                template = template.Replace(all, expanded);
                joinMatches = joinMatches.NextMatch();
            }

            // handle or, and functions
            var andOrRegex = new Regex(@"(and|or)\s+\((\..+?)\)\s+\((\..+?)\)(\s+\((\..+?)\)){0,1}");
            var andOrRegexMatches = andOrRegex.Match(template);
            while (andOrRegexMatches.Success)
            {
                var functionResult = "";
                var all = andOrRegexMatches.Groups[0].Value;
                var op = andOrRegexMatches.Groups[1].Value;
                var first = andOrRegexMatches.Groups[2].Value;
                var second = andOrRegexMatches.Groups[3].Value;
                var third = "";
                if (andOrRegexMatches.Groups.Count > 5)
                    third = andOrRegexMatches.Groups[5].Value;
                var value = variables[first];
                if (op == "and")
                {
                    functionResult = second;
                    if (value == null || (value is string && string.IsNullOrWhiteSpace((string)value)))
                        functionResult = first;
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(third))
                        {
                            functionResult = third;
                            value = variables[second];
                            if (value == null || (value is string && string.IsNullOrWhiteSpace((string)value)))
                                functionResult = second;
                        }
                    }
                }

                if (op == "or")
                {
                    functionResult = first;
                    if (value == null || (value is string && string.IsNullOrWhiteSpace((string)value)))
                    {
                        functionResult = second;
                        if (!string.IsNullOrWhiteSpace(third))
                        {
                            value = variables[second];
                            if (value == null || (value is string && string.IsNullOrWhiteSpace((string)value)))
                                functionResult = third;
                        }
                    }
                }

                template = template.Replace(all, functionResult);
                andOrRegexMatches = andOrRegexMatches.NextMatch();
            }

            // handle if ... else ... expression
            var ifElseRegex = new Regex(@"{{\s*if\s*(.+?)\s*}}(.*?){{\s*else\s*}}(.*?){{\s*end\s*}}");
            var ifElseRegexMatches = ifElseRegex.Match(template);
            while (ifElseRegexMatches.Success)
            {
                var all = ifElseRegexMatches.Groups[0].Value;
                var condition = ifElseRegexMatches.Groups[1].Value;
                var onTrue = ifElseRegexMatches.Groups[2].Value;
                var onFalse = ifElseRegexMatches.Groups[3].Value;
                string conditionResult;
                if (condition.StartsWith("."))
                {
                    var value = variables[condition];
                    bool conditionResultState;
                    if (value == null)
                        conditionResultState = false;
                    else if (value is string)
                        conditionResultState = !string.IsNullOrWhiteSpace((string)value);
                    else if (value is ICollection)
                        conditionResultState = ((ICollection)value).Count > 0;
                    else
                        throw new Exception(
                            string.Format("Unexpceted type for variable {0}: {1}", condition, value.GetType()));
                    conditionResult = conditionResultState ? onTrue : onFalse;
                }
                else
                    throw new NotImplementedException($"CardigannIndexer: Condition operation '{condition}' not implemented");

                template = template.Replace(all, conditionResult);
                ifElseRegexMatches = ifElseRegexMatches.NextMatch();
            }

            // handle range expression
            var rangeRegex = new Regex(@"{{\s*range\s*(.+?)\s*}}(.*?){{\.}}(.*?){{end}}");
            var rangeRegexMatches = rangeRegex.Match(template);
            while (rangeRegexMatches.Success)
            {
                var expanded = string.Empty;
                var all = rangeRegexMatches.Groups[0].Value;
                var variable = rangeRegexMatches.Groups[1].Value;
                var prefix = rangeRegexMatches.Groups[2].Value;
                var postfix = rangeRegexMatches.Groups[3].Value;
                foreach (var value in (ICollection<string>)variables[variable])
                {
                    var newvalue = value;
                    if (modifier != null)
                        newvalue = modifier(newvalue);
                    expanded += prefix + newvalue + postfix;
                }

                template = template.Replace(all, expanded);
                rangeRegexMatches = rangeRegexMatches.NextMatch();
            }

            // handle simple variables
            var variablesRegEx = new Regex(@"{{\s*(\..+?)\s*}}");
            var variablesRegExMatches = variablesRegEx.Match(template);
            while (variablesRegExMatches.Success)
            {
                var all = variablesRegExMatches.Groups[0].Value;
                var variable = variablesRegExMatches.Groups[1].Value;
                var value = (string)variables[variable];
                if (modifier != null)
                    value = modifier(value);
                template = template.Replace(all, value);
                variablesRegExMatches = variablesRegExMatches.NextMatch();
            }

            return template;
        }

        protected bool checkForError(WebClientStringResult loginResult, IList<errorBlock> errorBlocks)
        {
            if (loginResult.Status == HttpStatusCode.Unauthorized) // e.g. used by YGGtorrent
                throw new ExceptionWithConfigData("401 Unauthorized, check your credentials", configData);
            if (errorBlocks == null)
                return true; // no error
            var resultParser = new HtmlParser();
            var resultDocument = resultParser.ParseDocument(loginResult.Content);
            foreach (var error in errorBlocks)
            {
                var selection = resultDocument.QuerySelector(error.Selector);
                if (selection != null)
                {
                    var errorMessage = selection.TextContent;
                    if (error.Message != null)
                        errorMessage = handleSelector(error.Message, resultDocument.FirstElementChild);
                    throw new ExceptionWithConfigData(string.Format("Error: {0}", errorMessage.Trim()), configData);
                }
            }

            return true; // no error
        }

        protected async Task<bool> DoLoginAsync()
        {
            var login = Definition.Login;
            if (login == null)
                return true;
            if (login.Method == "post")
            {
                var pairs = new Dictionary<string, string>();
                foreach (var input in Definition.Login.Inputs)
                {
                    var value = applyGoTemplateText(input.Value);
                    pairs.Add(input.Key, value);
                }

                var loginUrl = resolvePath(login.Path).ToString();
                configData.CookieHeader.Value = null;
                var loginResult = await RequestLoginAndFollowRedirectAsync(loginUrl, pairs, null, true, null, SiteLink, true);
                configData.CookieHeader.Value = loginResult.Cookies;
                checkForError(loginResult, Definition.Login.Error);
            }
            else if (login.Method == "form")
            {
                var loginUrl = resolvePath(login.Path).ToString();
                var queryCollection = new NameValueCollection();
                var pairs = new Dictionary<string, string>();
                var captchaConfigItem = (RecaptchaItem)configData.GetDynamic("Captcha");
                if (captchaConfigItem != null)
                {
                    if (!string.IsNullOrWhiteSpace(captchaConfigItem.Cookie))
                    {
                        // for remote users just set the cookie and return
                        CookieHeader = captchaConfigItem.Cookie;
                        return true;
                    }

                    var cloudFlareCaptchaChallenge =
                        landingResultDocument.QuerySelector("script[src=\"/cdn-cgi/scripts/cf.challenge.js\"]");
                    if (cloudFlareCaptchaChallenge != null)
                    {
                        var cloudFlareQueryCollection = new NameValueCollection
                        {
                            ["id"] = cloudFlareCaptchaChallenge.GetAttribute("data-ray"),
                            ["g-recaptcha-response"] = captchaConfigItem.Value
                        };
                        var clearanceUrl = resolvePath(
                            $"/cdn-cgi/l/chk_captcha?{cloudFlareQueryCollection.GetQueryString()}");
                        var clearanceResult = await RequestStringWithCookiesAsync(clearanceUrl.ToString(), null, SiteLink);
                        if (clearanceResult.IsRedirect) // clearance successfull
                        {
                            // request real login page again
                            landingResult = await RequestStringWithCookiesAsync(loginUrl, null, SiteLink);
                            var htmlParser = new HtmlParser();
                            landingResultDocument = htmlParser.ParseDocument(landingResult.Content);
                        }
                        else
                            throw new ExceptionWithConfigData(
                                string.Format(
                                    "Login failed: Cloudflare clearance failed using cookies {0}: {1}", CookieHeader,
                                    clearanceResult.Content), configData);
                    }
                    else
                        pairs.Add("g-recaptcha-response", captchaConfigItem.Value);
                }

                var formSelector = login.Form;
                if (formSelector == null)
                    formSelector = "form";

                // landingResultDocument might not be initiated if the login is caused by a relogin during a query
                if (landingResultDocument == null)
                {
                    var configurationResult = await GetConfigurationForSetupAsync(true);
                    if (configurationResult == null) // got captcha
                        return false;
                }

                var form = landingResultDocument.QuerySelector(formSelector);
                if (form == null)
                    throw new ExceptionWithConfigData(
                        string.Format("Login failed: No form found on {0} using form selector {1}", loginUrl, formSelector),
                        configData);
                var inputs = form.QuerySelectorAll("input");
                if (inputs == null)
                    throw new ExceptionWithConfigData(
                        string.Format(
                            "Login failed: No inputs found on {0} using form selector {1}", loginUrl, formSelector),
                        configData);
                var submitUrlstr = form.GetAttribute("action");
                if (login.Submitpath != null)
                    submitUrlstr = login.Submitpath;
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

                foreach (var input in Definition.Login.Inputs)
                {
                    var value = applyGoTemplateText(input.Value);
                    var inputKey = input.Key;
                    if (login.Selectors)
                    {
                        var inputElement = landingResultDocument.QuerySelector(input.Key);
                        if (inputElement == null)
                            throw new ExceptionWithConfigData(
                                string.Format("Login failed: No input found using selector {0}", input.Key), configData);
                        inputKey = inputElement.GetAttribute("name");
                    }

                    pairs[inputKey] = value;
                }

                // selector inputs
                if (login.Selectorinputs != null)
                    foreach (var selectorinput in login.Selectorinputs)
                    {
                        string value = null;
                        try
                        {
                            value = handleSelector(selectorinput.Value, landingResultDocument.FirstElementChild);
                            pairs[selectorinput.Key] = value;
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(
                                string.Format(
                                    "Error while parsing selector input={0}, selector={1}, value={2}: {3}",
                                    selectorinput.Key, selectorinput.Value.Selector, value, ex.Message));
                        }
                    }

                // getselector inputs
                if (login.Getselectorinputs != null)
                    foreach (var selectorinput in login.Getselectorinputs)
                    {
                        string value = null;
                        try
                        {
                            value = handleSelector(selectorinput.Value, landingResultDocument.FirstElementChild);
                            queryCollection[selectorinput.Key] = value;
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(
                                string.Format(
                                    "Error while parsing get selector input={0}, selector={1}, value={2}: {3}",
                                    selectorinput.Key, selectorinput.Value.Selector, value, ex.Message));
                        }
                    }

                if (queryCollection.Count > 0)
                    submitUrlstr += $"?{queryCollection.GetQueryString()}";
                var submitUrl = resolvePath(submitUrlstr, new Uri(loginUrl));

                // automatically solve simpleCaptchas, if used
                var simpleCaptchaPresent = landingResultDocument.QuerySelector("script[src*=\"simpleCaptcha\"]");
                if (simpleCaptchaPresent != null)
                {
                    var captchaUrl = resolvePath("simpleCaptcha.php?numImages=1");
                    var simpleCaptchaResult = await RequestStringWithCookiesAsync(captchaUrl.ToString(), null, loginUrl);
                    var simpleCaptchaJson = JObject.Parse(simpleCaptchaResult.Content);
                    var captchaSelection = simpleCaptchaJson["images"][0]["hash"].ToString();
                    pairs["captchaSelection"] = captchaSelection;
                    pairs["submitme"] = "X";
                }

                if (login.Captcha != null)
                {
                    var captcha = login.Captcha;
                    if (captcha.Type == "image")
                    {
                        var captchaText = (StringItem)configData.GetDynamic("CaptchaText");
                        if (captchaText != null)
                        {
                            var input = captcha.Input;
                            if (login.Selectors)
                            {
                                var inputElement = landingResultDocument.QuerySelector(captcha.Input);
                                if (inputElement == null)
                                    throw new ExceptionWithConfigData(
                                        string.Format("Login failed: No captcha input found using {0}", captcha.Input),
                                        configData);
                                input = inputElement.GetAttribute("name");
                            }

                            pairs[input] = captchaText.Value;
                        }
                    }

                    if (captcha.Type == "text")
                    {
                        var captchaAnswer = (StringItem)configData.GetDynamic("CaptchaAnswer");
                        if (captchaAnswer != null)
                        {
                            var input = captcha.Input;
                            if (login.Selectors)
                            {
                                var inputElement = landingResultDocument.QuerySelector(captcha.Input);
                                if (inputElement == null)
                                    throw new ExceptionWithConfigData(
                                        string.Format("Login failed: No captcha input found using {0}", captcha.Input),
                                        configData);
                                input = inputElement.GetAttribute("name");
                            }

                            pairs[input] = captchaAnswer.Value;
                        }
                    }
                }

                // clear landingResults/Document, otherwise we might use an old version for a new relogin (if GetConfigurationForSetup() wasn't called before)
                landingResult = null;
                landingResultDocument = null;
                var enctype = form.GetAttribute("enctype");
                WebClientStringResult loginResult;
                if (enctype == "multipart/form-data")
                {
                    var headers = new Dictionary<string, string>();
                    var boundary =
                        $"---------------------------{(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds.ToString().Replace(".", "")}";
                    var bodyParts = new List<string>();
                    foreach (var pair in pairs)
                    {
                        var part =
                            $"--{boundary}\r\nContent-Disposition: form-data; name=\"{pair.Key}\"\r\n\r\n{pair.Value}";
                        bodyParts.Add(part);
                    }

                    bodyParts.Add($"--{boundary}--");
                    headers.Add("Content-Type", $"multipart/form-data; boundary={boundary}");
                    var body = string.Join("\r\n", bodyParts);
                    loginResult = await PostDataWithCookiesAsync(
                        submitUrl.ToString(), pairs, configData.CookieHeader.Value, SiteLink, headers, body);
                }
                else
                    loginResult = await RequestLoginAndFollowRedirectAsync(
                        submitUrl.ToString(), pairs, configData.CookieHeader.Value, true, null, loginUrl, true);

                configData.CookieHeader.Value = loginResult.Cookies;
                checkForError(loginResult, Definition.Login.Error);
            }
            else if (login.Method == "cookie")
                configData.CookieHeader.Value = ((StringItem)configData.GetDynamic("cookie")).Value;
            else if (login.Method == "get")
            {
                var queryCollection = new NameValueCollection();
                foreach (var input in Definition.Login.Inputs)
                {
                    var value = applyGoTemplateText(input.Value);
                    queryCollection.Add(input.Key, value);
                }

                var loginUrl = resolvePath($"{login.Path}?{queryCollection.GetQueryString()}").ToString();
                configData.CookieHeader.Value = null;
                var loginResult = await RequestStringWithCookiesAsync(loginUrl, null, SiteLink);
                configData.CookieHeader.Value = loginResult.Cookies;
                checkForError(loginResult, Definition.Login.Error);
            }
            else if (login.Method == "oneurl")
            {
                var oneUrl = applyGoTemplateText(Definition.Login.Inputs["oneurl"]);
                var loginUrl = resolvePath(login.Path + oneUrl).ToString();
                configData.CookieHeader.Value = null;
                var loginResult = await RequestStringWithCookiesAsync(loginUrl, null, SiteLink);
                configData.CookieHeader.Value = loginResult.Cookies;
                checkForError(loginResult, Definition.Login.Error);
            }
            else
                throw new NotImplementedException($"Login method {Definition.Login.Method} not implemented");

            logger.Debug(string.Format("CardigannIndexer ({0}): Cookies after login: {1}", ID, CookieHeader));
            return true;
        }

        protected string getRedirectDomainHint(string requestUrl, string redirectUrl)
        {
            if (requestUrl.StartsWith(SiteLink) && !redirectUrl.StartsWith(SiteLink))
            {
                var uri = new Uri(redirectUrl);
                return $"{uri.Scheme}://{uri.Host}/";
            }

            return null;
        }

        protected string getRedirectDomainHint(WebClientByteResult result) =>
            getRedirectDomainHint(result.Request.Url, result.RedirectingTo);

        protected string getRedirectDomainHint(WebClientStringResult result) =>
            getRedirectDomainHint(result.Request.Url, result.RedirectingTo);

        protected async Task<bool> TestLoginAsync()
        {
            var login = Definition.Login;
            if (login?.Test == null)
                return false;

            // test if login was successful
            var loginTestUrl = resolvePath(login.Test.Path).ToString();
            var testResult = await RequestStringWithCookiesAsync(loginTestUrl);
            if (testResult.IsRedirect)
            {
                var errormessage = "Login Failed, got redirected.";
                var domainHint = getRedirectDomainHint(testResult);
                if (domainHint != null)
                {
                    errormessage += $" Try changing the indexer URL to {domainHint}.";
                    if (Definition.Followredirect)
                    {
                        configData.SiteLink.Value = domainHint;
                        SiteLink = configData.SiteLink.Value;
                        SaveConfig();
                        errormessage += " Updated site link, please try again.";
                    }
                }

                throw new ExceptionWithConfigData(errormessage, configData);
            }

            if (login.Test.Selector != null)
            {
                var testResultParser = new HtmlParser();
                var testResultDocument = testResultParser.ParseDocument(testResult.Content);
                var selection = testResultDocument.QuerySelectorAll(login.Test.Selector);
                if (selection.Length == 0)
                    throw new ExceptionWithConfigData(
                        string.Format("Login failed: Selector \"{0}\" didn't match", login.Test.Selector), configData);
            }

            return true;
        }

        protected bool CheckIfLoginIsNeeded(WebClientStringResult result, IHtmlDocument document)
        {
            if (result.IsRedirect)
            {
                var domainHint = getRedirectDomainHint(result);
                if (domainHint != null)
                {
                    var errormessage = $"Got redirected to another domain. Try changing the indexer URL to {domainHint}.";
                    if (Definition.Followredirect)
                    {
                        configData.SiteLink.Value = domainHint;
                        SiteLink = configData.SiteLink.Value;
                        SaveConfig();
                        errormessage += " Updated site link, please try again.";
                    }

                    throw new ExceptionWithConfigData(errormessage, configData);
                }

                return true;
            }

            if (Definition.Login?.Test == null)
                return false;
            if (Definition.Login.Test.Selector != null)
            {
                var selection = document.QuerySelectorAll(Definition.Login.Test.Selector);
                if (selection.Length == 0)
                    return true;
            }

            return false;
        }

        public override async Task<ConfigurationData> GetConfigurationForSetup()
        {
            try
            {
                return await GetConfigurationForSetupAsync(false);
            }
            catch (Exception e)
            {
                logger.Error($"Exception in GetConfigurationForSetup ({ID}): {e}");
                return configData;
            }
        }

        public async Task<ConfigurationData> GetConfigurationForSetupAsync(bool automaticlogin)
        {
            var login = Definition.Login;
            if (login == null || login.Method != "form")
                return configData;
            var loginUrl = resolvePath(login.Path);
            configData.CookieHeader.Value = null;
            if (login.Cookies != null)
                configData.CookieHeader.Value = string.Join("; ", login.Cookies);
            landingResult = await RequestStringWithCookiesAsync(loginUrl.AbsoluteUri, null, SiteLink);
            var htmlParser = new HtmlParser();
            landingResultDocument = htmlParser.ParseDocument(landingResult.Content);
            var hasCaptcha = false;
            var cloudFlareCaptchaScript = landingResultDocument.QuerySelector("script[src*=\"/recaptcha/api.js\"]");
            var cloudFlareCaptchaGroup = landingResultDocument.QuerySelector("#recaptca_group");
            var cloudFlareCaptchaDisplay = true;
            if (cloudFlareCaptchaGroup != null)
            {
                var cloudFlareCaptchaGroupStyle = cloudFlareCaptchaGroup.GetAttribute("style");
                if (cloudFlareCaptchaGroupStyle != null)
                    cloudFlareCaptchaDisplay = !cloudFlareCaptchaGroupStyle.Contains("display:none;");
            }

            var grecaptcha = landingResultDocument.QuerySelector(".g-recaptcha");
            if (cloudFlareCaptchaScript != null && grecaptcha != null && cloudFlareCaptchaDisplay)
            {
                hasCaptcha = true;
                var captchaItem = new RecaptchaItem
                {
                    Name = "Captcha",
                    Version = "2",
                    SiteKey = grecaptcha.GetAttribute("data-sitekey")
                };
                if (captchaItem.SiteKey == null
                    ) // some sites don't store the sitekey in the .g-recaptcha div (e.g. cloudflare captcha challenge page)
                    captchaItem.SiteKey = landingResultDocument.QuerySelector("[data-sitekey]").GetAttribute("data-sitekey");
                configData.AddDynamic("Captcha", captchaItem);
            }

            if (login.Captcha != null)
            {
                var captcha = login.Captcha;
                if (captcha.Type == "image")
                {
                    var captchaElement = landingResultDocument.QuerySelector(captcha.Selector);
                    if (captchaElement != null)
                    {
                        hasCaptcha = true;
                        var captchaUrl = resolvePath(captchaElement.GetAttribute("src"), loginUrl);
                        var captchaImageData = await RequestBytesWithCookiesAsync(
                            captchaUrl.ToString(), landingResult.Cookies, RequestType.Get, loginUrl.AbsoluteUri);
                        var captchaImage = new ImageItem { Name = "Captcha Image" };
                        var captchaText = new StringItem { Name = "Captcha Text" };
                        captchaImage.Value = captchaImageData.Content;
                        configData.AddDynamic("CaptchaImage", captchaImage);
                        configData.AddDynamic("CaptchaText", captchaText);
                    }
                    else
                        logger.Debug(string.Format("CardigannIndexer ({0}): No captcha image found", ID));
                }
                else if (captcha.Type == "text")
                {
                    var captchaElement = landingResultDocument.QuerySelector(captcha.Selector);
                    if (captchaElement != null)
                    {
                        hasCaptcha = true;
                        var captchaChallenge = new DisplayItem(captchaElement.TextContent) { Name = "Captcha Challenge" };
                        var captchaAnswer = new StringItem { Name = "Captcha Answer" };
                        configData.AddDynamic("CaptchaChallenge", captchaChallenge);
                        configData.AddDynamic("CaptchaAnswer", captchaAnswer);
                    }
                    else
                        logger.Debug(string.Format("CardigannIndexer ({0}): No captcha image found", ID));
                }
                else
                    throw new NotImplementedException(
                        string.Format("Captcha type \"{0}\" is not implemented", captcha.Type));
            }

            if (hasCaptcha && automaticlogin)
            {
                configData.LastError.Value = "Got captcha during automatic login, please reconfigure manually";
                logger.Error(string.Format("CardigannIndexer ({0}): Found captcha during automatic login, aborting", ID));
                return null;
            }

            return configData;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);
            await DoLoginAsync();
            await TestLoginAsync();
            IsConfigured = true;
            SaveConfig();
            return IndexerConfigurationStatus.Completed;
        }

        protected string applyFilters(string data, List<filterBlock> filters, Dictionary<string, object> variables = null)
        {
            if (filters == null)
                return data;
            foreach (var filter in filters)
                switch (filter.Name)
                {
                    case "querystring":
                        var param = (string)filter.Args;
                        data = ParseUtil.GetArgumentFromQueryString(data, param);
                        break;
                    case "timeparse":
                    case "dateparse":
                        var layout = (string)filter.Args;
                        try
                        {
                            var date = DateTimeUtil.ParseDateTimeGoLang(data, layout);
                            data = date.ToString(DateTimeUtil.RFC1123ZPattern);
                        }
                        catch (FormatException ex)
                        {
                            logger.Debug(ex.Message);
                        }

                        break;
                    case "regexp":
                        var pattern = (string)filter.Args;
                        var regexp = new Regex(pattern);
                        var match = regexp.Match(data);
                        data = match.Groups[1].Value;
                        break;
                    case "re_replace":
                        var regexpreplacePattern = (string)filter.Args[0];
                        var regexpreplaceReplacement = (string)filter.Args[1];
                        regexpreplaceReplacement = applyGoTemplateText(regexpreplaceReplacement, variables);
                        var regexpreplaceRegex = new Regex(regexpreplacePattern);
                        data = regexpreplaceRegex.Replace(data, regexpreplaceReplacement);
                        break;
                    case "split":
                        var sep = (string)filter.Args[0];
                        var pos = (string)filter.Args[1];
                        var posInt = int.Parse(pos);
                        var strParts = data.Split(sep[0]);
                        if (posInt < 0)
                            posInt += strParts.Length;
                        data = strParts[posInt];
                        break;
                    case "replace":
                        var from = (string)filter.Args[0];
                        var to = (string)filter.Args[1];
                        to = applyGoTemplateText(to, variables);
                        data = data.Replace(from, to);
                        break;
                    case "trim":
                        var cutset = (string)filter.Args;
                        data = cutset != null ? data.Trim(cutset[0]) : data.Trim();
                        break;
                    case "prepend":
                        var prependstr = (string)filter.Args;
                        data = applyGoTemplateText(prependstr, variables) + data;
                        break;
                    case "append":
                        var str = (string)filter.Args;
                        data += applyGoTemplateText(str, variables);
                        break;
                    case "tolower":
                        data = data.ToLower();
                        break;
                    case "toupper":
                        data = data.ToUpper();
                        break;
                    case "urldecode":
                        data = WebUtilityHelpers.UrlDecode(data, Encoding);
                        break;
                    case "urlencode":
                        data = WebUtilityHelpers.UrlEncode(data, Encoding);
                        break;
                    case "timeago":
                    case "reltime":
                        data = DateTimeUtil.FromTimeAgo(data).ToString(DateTimeUtil.RFC1123ZPattern);
                        break;
                    case "fuzzytime":
                        data = DateTimeUtil.FromUnknown(data).ToString(DateTimeUtil.RFC1123ZPattern);
                        break;
                    case "validfilename":
                        data = StringUtil.MakeValidFileName(data, '_', false);
                        break;
                    case "diacritics":
                        var diacriticsOp = (string)filter.Args;
                        if (diacriticsOp == "replace")
                        {
                            // Should replace diacritics charcaters with their base character
                            // It's not perfect, e.g. "ŠĐĆŽ - šđčćž" becomes "SĐCZ-sđccz"
                            var stFormD = data.Normalize(NormalizationForm.FormD);
                            var len = stFormD.Length;
                            var sb = new StringBuilder();
                            for (var i = 0; i < len; i++)
                            {
                                var uc = CharUnicodeInfo.GetUnicodeCategory(stFormD[i]);
                                if (uc != UnicodeCategory.NonSpacingMark)
                                    sb.Append(stFormD[i]);
                            }

                            data = (sb.ToString().Normalize(NormalizationForm.FormC));
                        }
                        else
                            throw new Exception("unsupported diacritics filter argument");

                        break;
                    case "jsonjoinarray":
                        var jsonjoinarrayJsonPath = (string)filter.Args[0];
                        var jsonjoinarraySeparator = (string)filter.Args[1];
                        var jsonjoinarrayO = JObject.Parse(data);
                        var jsonjoinarrayOResult = jsonjoinarrayO.SelectToken(jsonjoinarrayJsonPath);
                        var jsonjoinarrayOResultStrings = jsonjoinarrayOResult.Select(j => j.ToString());
                        data = string.Join(jsonjoinarraySeparator, jsonjoinarrayOResultStrings);
                        break;
                    case "hexdump":
                        // this is mainly for debugging invisible special char related issues
                        var hexData = string.Join("", data.Select(c => $"{c}({((int)c):X2})"));
                        logger.Debug(string.Format("CardigannIndexer ({0}): strdump: {1}", ID, hexData));
                        break;
                    case "strdump":
                        // for debugging
                        var debugData = data.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\xA0", "\\xA0");
                        logger.Debug(string.Format("CardigannIndexer ({0}): strdump: {1}", ID, debugData));
                        break;
                }

            return data;
        }

        protected IElement QuerySelector(IElement element, string selector)
        {
            // AngleSharp doesn't support the :root pseudo selector, so we check for it manually
            if (selector.StartsWith(":root"))
            {
                selector = selector.Substring(5);
                while (element.ParentElement != null)
                    element = element.ParentElement;
            }

            return element.QuerySelector(selector);
        }

        protected string handleSelector(selectorBlock selector, IElement dom, Dictionary<string, object> variables = null)
        {
            if (selector.Text != null)
                return applyFilters(applyGoTemplateText(selector.Text, variables), selector.Filters, variables);
            var selection = dom;
            string value = null;
            if (selector.Selector != null)
            {
                selection = dom.Matches(selector.Selector) ? dom : QuerySelector(dom, selector.Selector);
                if (selection == null)
                    throw new Exception(
                        string.Format("Selector \"{0}\" didn't match {1}", selector.Selector, dom.ToHtmlPretty()));
            }

            if (selector.Remove != null)
                foreach (var i in selection.QuerySelectorAll(selector.Remove))
                    i.Remove();
            if (selector.Case != null)
            {
                foreach (var @case in selector.Case)
                    if (selection.Matches(@case.Key) || QuerySelector(selection, @case.Key) != null)
                    {
                        value = @case.Value;
                        break;
                    }

                if (value == null)
                    throw new Exception(
                        string.Format(
                            "None of the case selectors \"{0}\" matched {1}", string.Join(",", selector.Case),
                            selection.ToHtmlPretty()));
            }
            else if (selector.Attribute != null)
            {
                value = selection.GetAttribute(selector.Attribute);
                if (value == null)
                    throw new Exception(
                        string.Format(
                            "Attribute \"{0}\" is not set for element {1}", selector.Attribute, selection.ToHtmlPretty()));
            }
            else
                value = selection.TextContent;

            return applyFilters(ParseUtil.NormalizeSpace(value), selector.Filters, variables);
        }

        protected Uri resolvePath(string path, Uri currentUrl = null)
        {
            if (currentUrl == null)
                currentUrl = new Uri(SiteLink);
            return new Uri(currentUrl, path);
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var search = Definition.Search;

            // init template context
            var variables = getTemplateVariablesFromConfigData();
            variables[".Query.Type"] = query.QueryType;
            variables[".Query.Q"] = query.SearchTerm;
            variables[".Query.Series"] = null;
            variables[".Query.Ep"] = query.Episode;
            variables[".Query.Season"] = query.Season;
            variables[".Query.Movie"] = null;
            variables[".Query.Year"] = query.Year.ToString();
            variables[".Query.Limit"] = query.Limit.ToString();
            variables[".Query.Offset"] = query.Offset.ToString();
            variables[".Query.Extended"] = query.Extended.ToString();
            variables[".Query.Categories"] = query.Categories;
            variables[".Query.APIKey"] = query.ApiKey;
            variables[".Query.TVDBID"] = null;
            variables[".Query.TVRageID"] = query.RageID;
            variables[".Query.IMDBID"] = query.ImdbID;
            variables[".Query.IMDBIDShort"] = query.ImdbIDShort;
            variables[".Query.TVMazeID"] = null;
            variables[".Query.TraktID"] = null;
            variables[".Query.Album"] = query.Album;
            variables[".Query.Artist"] = query.Artist;
            variables[".Query.Label"] = query.Label;
            variables[".Query.Track"] = query.Track;
            //variables[".Query.Genre"] = query.Genre ?? new List<string>();
            variables[".Query.Episode"] = query.GetEpisodeSearchString();
            var mappedCategories = MapTorznabCapsToTrackers(query);
            if (mappedCategories.Count == 0)
                mappedCategories = DefaultCategories;
            variables[".Categories"] = mappedCategories;
            var keywordTokens = new List<string>();
            var keywordTokenKeys = new List<string> { "Q", "Series", "Movie", "Year" };
            foreach (var key in keywordTokenKeys)
            {
                var value = (string)variables[$".Query.{key}"];
                if (!string.IsNullOrWhiteSpace(value))
                    keywordTokens.Add(value);
            }

            if (!string.IsNullOrWhiteSpace((string)variables[".Query.Episode"]))
                keywordTokens.Add((string)variables[".Query.Episode"]);
            variables[".Query.Keywords"] = string.Join(" ", keywordTokens);
            variables[".Keywords"] = applyFilters((string)variables[".Query.Keywords"], search.Keywordsfilters);

            // TODO: prepare queries first and then send them parallel 
            var searchPaths = search.Paths;
            foreach (var searchPath in searchPaths)
            {
                // skip path if categories don't match
                if (searchPath.Categories != null && mappedCategories.Count > 0)
                {
                    var invertMatch = (searchPath.Categories[0] == "!");
                    var hasIntersect = mappedCategories.Intersect(searchPath.Categories).Any();
                    if (invertMatch)
                        hasIntersect = !hasIntersect;
                    if (!hasIntersect)
                        continue;
                }

                // build search URL
                // HttpUtility.UrlPathEncode seems to only encode spaces, we use UrlEncode and replace + with %20 as a workaround
                var searchUrl = resolvePath(
                    applyGoTemplateText(searchPath.Path, variables, WebUtility.UrlEncode).Replace("+", "%20")).AbsoluteUri;
                var queryCollection = new List<KeyValuePair<string, string>>();
                var method = RequestType.Get;
                if (string.Equals(searchPath.Method, "post", StringComparison.OrdinalIgnoreCase))
                    method = RequestType.Post;
                var inputsList = new List<Dictionary<string, string>>();
                if (searchPath.Inheritinputs)
                    inputsList.Add(search.Inputs);
                inputsList.Add(searchPath.Inputs);
                foreach (var inputs in inputsList)
                    if (inputs != null)
                        foreach (var input in inputs)
                            if (input.Key == "$raw")
                            {
                                var rawStr = applyGoTemplateText(input.Value, variables, WebUtility.UrlEncode);
                                foreach (var part in rawStr.Split('&'))
                                {
                                    var parts = part.Split(
                                        new[]
                                        {
                                            '='
                                        }, 2);
                                    var key = parts[0];
                                    if (key.Length == 0)
                                        continue;
                                    var value = "";
                                    if (parts.Count() == 2)
                                        value = parts[1];
                                    queryCollection.Add(key, value);
                                }
                            }
                            else
                                queryCollection.Add(input.Key, applyGoTemplateText(input.Value, variables));

                if (method == RequestType.Get)
                    if (queryCollection.Count > 0)
                        searchUrl += $"?{queryCollection.GetQueryString(Encoding)}";
                var searchUrlUri = new Uri(searchUrl);

                // send HTTP request
                WebClientStringResult response = null;
                Dictionary<string, string> headers = null;
                if (search.Headers != null)
                {
                    // FIXME: fix jackett header handling (allow it to specifiy the same header multipe times)
                    headers = new Dictionary<string, string>();
                    foreach (var header in search.Headers)
                        headers.Add(header.Key, header.Value[0]);
                }

                response = method == RequestType.Post
                    ? await PostDataWithCookiesAsync(searchUrl, queryCollection, null, null, headers)
                    : await RequestStringWithCookiesAsync(searchUrl, null, null, headers);
                if (response.IsRedirect && searchPath.Followredirect)
                    await FollowIfRedirectAsync(response);
                var results = response.Content;
                try
                {
                    var searchResultParser = new HtmlParser();
                    var searchResultDocument = searchResultParser.ParseDocument(results);

                    // check if we need to login again
                    var loginNeeded = CheckIfLoginIsNeeded(response, searchResultDocument);
                    if (loginNeeded)
                    {
                        logger.Info(string.Format("CardigannIndexer ({0}): Relogin required", ID));
                        var loginResult = await DoLoginAsync();
                        if (!loginResult)
                            throw new Exception("Relogin failed");
                        await TestLoginAsync();
                        response = method == RequestType.Post ? await PostDataWithCookiesAsync(searchUrl, queryCollection) : await RequestStringWithCookiesAsync(searchUrl);
                        if (response.IsRedirect && searchPath.Followredirect)
                            await FollowIfRedirectAsync(response);
                        results = response.Content;
                        searchResultDocument = searchResultParser.ParseDocument(results);
                    }

                    checkForError(response, Definition.Search.Error);
                    if (search.Preprocessingfilters != null)
                    {
                        results = applyFilters(results, search.Preprocessingfilters, variables);
                        searchResultDocument = searchResultParser.ParseDocument(results);
                        logger.Debug(
                            string.Format("CardigannIndexer ({0}): result after preprocessingfilters: {1}", ID, results));
                    }

                    var rowsSelector = applyGoTemplateText(search.Rows.Selector, variables);
                    var rowsDom = searchResultDocument.QuerySelectorAll(rowsSelector);
                    var rows = new List<IElement>();
                    foreach (var rowDom in rowsDom)
                        rows.Add(rowDom);

                    // merge following rows for After selector
                    var after = Definition.Search.Rows.After;
                    if (after > 0)
                        for (var i = 0; i < rows.Count; i += 1)
                        {
                            var currentRow = rows[i];
                            for (var j = 0; j < after; j += 1)
                            {
                                var mergeRowIndex = i + j + 1;
                                var mergeRow = rows[mergeRowIndex];
                                var mergeNodes = new List<INode>();
                                foreach (var node in mergeRow.ChildNodes)
                                    mergeNodes.Add(node);
                                currentRow.Append(mergeNodes.ToArray());
                            }

                            rows.RemoveRange(i + 1, after);
                        }

                    foreach (var row in rows)
                        try
                        {
                            var release = new ReleaseInfo
                            {
                                MinimumRatio = 1,
                                MinimumSeedTime = 172800 // 48 hours
                            };

                            // Parse fields
                            foreach (var field in search.Fields)
                            {
                                var fieldParts = field.Key.Split('|');
                                var fieldName = fieldParts[0];
                                var fieldModifiers = new List<string>();
                                for (var i = 1; i < fieldParts.Length; i++)
                                    fieldModifiers.Add(fieldParts[i]);
                                string value = null;
                                var variablesKey = $".Result.{fieldName}";
                                try
                                {
                                    value = handleSelector(field.Value, row, variables);
                                    switch (fieldName)
                                    {
                                        case "download":
                                            if (string.IsNullOrEmpty(value))
                                            {
                                                value = null;
                                                release.Link = null;
                                                break;
                                            }

                                            if (value.StartsWith("magnet:"))
                                            {
                                                release.MagnetUri = new Uri(value);
                                                //release.Link = release.MagnetUri;
                                                value = release.MagnetUri.ToString();
                                            }
                                            else
                                            {
                                                release.Link = resolvePath(value, searchUrlUri);
                                                value = release.Link.ToString();
                                            }

                                            break;
                                        case "magnet":
                                            var magnetUri = new Uri(value);
                                            release.MagnetUri = magnetUri;
                                            value = magnetUri.ToString();
                                            if (release.Guid == null)
                                                release.Guid = magnetUri;
                                            break;
                                        case "details":
                                            var url = resolvePath(value, searchUrlUri);
                                            release.Guid = url;
                                            release.Comments = url;
                                            if (release.Guid == null)
                                                release.Guid = url;
                                            value = url.ToString();
                                            break;
                                        case "comments":
                                            var commentsUrl = resolvePath(value, searchUrlUri);
                                            if (release.Comments == null)
                                                release.Comments = commentsUrl;
                                            if (release.Guid == null)
                                                release.Guid = commentsUrl;
                                            value = commentsUrl.ToString();
                                            break;
                                        case "title":
                                            if (fieldModifiers.Contains("append"))
                                                release.Title += value;
                                            else
                                                release.Title = value;
                                            value = release.Title;
                                            break;
                                        case "description":
                                            if (fieldModifiers.Contains("append"))
                                                release.Description += value;
                                            else
                                                release.Description = value;
                                            value = release.Description;
                                            break;
                                        case "category":
                                            var cats = MapTrackerCatToNewznab(value);
                                            if (release.Category == null)
                                                release.Category = cats;
                                            else
                                                foreach (var cat in cats)
                                                    if (!release.Category.Contains(cat))
                                                        release.Category.Add(cat);
                                            value = release.Category.ToString();
                                            break;
                                        case "size":
                                            release.Size = ReleaseInfo.GetBytes(value);
                                            value = release.Size.ToString();
                                            break;
                                        case "leechers":
                                            var leechers = ParseUtil.CoerceInt(value);
                                            if (release.Peers == null)
                                                release.Peers = leechers;
                                            else
                                                release.Peers += leechers;
                                            value = leechers.ToString();
                                            break;
                                        case "seeders":
                                            release.Seeders = ParseUtil.CoerceInt(value);
                                            if (release.Peers == null)
                                                release.Peers = release.Seeders;
                                            else
                                                release.Peers += release.Seeders;
                                            value = release.Seeders.ToString();
                                            break;
                                        case "date":
                                            release.PublishDate = DateTimeUtil.FromUnknown(value);
                                            value = release.PublishDate.ToString(DateTimeUtil.RFC1123ZPattern);
                                            break;
                                        case "files":
                                            release.Files = ParseUtil.CoerceLong(value);
                                            value = release.Files.ToString();
                                            break;
                                        case "grabs":
                                            release.Grabs = ParseUtil.CoerceLong(value);
                                            value = release.Grabs.ToString();
                                            break;
                                        case "downloadvolumefactor":
                                            release.DownloadVolumeFactor = ParseUtil.CoerceDouble(value);
                                            value = release.DownloadVolumeFactor.ToString();
                                            break;
                                        case "uploadvolumefactor":
                                            release.UploadVolumeFactor = ParseUtil.CoerceDouble(value);
                                            value = release.UploadVolumeFactor.ToString();
                                            break;
                                        case "minimumratio":
                                            release.MinimumRatio = ParseUtil.CoerceDouble(value);
                                            value = release.MinimumRatio.ToString();
                                            break;
                                        case "minimumseedtime":
                                            release.MinimumSeedTime = ParseUtil.CoerceLong(value);
                                            value = release.MinimumSeedTime.ToString();
                                            break;
                                        case "imdb":
                                            release.Imdb = ParseUtil.GetLongFromString(value);
                                            value = release.Imdb.ToString();
                                            break;
                                        case "rageid":
                                            var rageIdRegEx = new Regex(@"(\d+)", RegexOptions.Compiled);
                                            var rageIdMatch = rageIdRegEx.Match(value);
                                            var rageId = rageIdMatch.Groups[1].Value;
                                            release.RageID = ParseUtil.CoerceLong(rageId);
                                            value = release.RageID.ToString();
                                            break;
                                        case "tvdbid":
                                            var tvdbIdRegEx = new Regex(@"(\d+)", RegexOptions.Compiled);
                                            var tvdbIdMatch = tvdbIdRegEx.Match(value);
                                            var tvdbId = tvdbIdMatch.Groups[1].Value;
                                            release.TVDBId = ParseUtil.CoerceLong(tvdbId);
                                            value = release.TVDBId.ToString();
                                            break;
                                        case "banner":
                                            if (!string.IsNullOrWhiteSpace(value))
                                            {
                                                var bannerurl = resolvePath(value, searchUrlUri);
                                                release.BannerUrl = bannerurl;
                                            }

                                            value = release.BannerUrl.ToString();
                                            break;
                                    }

                                    variables[variablesKey] = value;
                                }
                                catch (Exception ex)
                                {
                                    if (!variables.ContainsKey(variablesKey))
                                        variables[variablesKey] = null;
                                    if (OptionalFileds.Contains(field.Key) || fieldModifiers.Contains("optional") ||
                                        field.Value.Optional)
                                        continue;
                                    throw new Exception(
                                        string.Format(
                                            "Error while parsing field={0}, selector={1}, value={2}: {3}", field.Key,
                                            field.Value.Selector, value ?? "<null>", ex.Message));
                                }
                            }

                            var filters = Definition.Search.Rows.Filters;
                            var skipRelease = false;
                            if (filters != null)
                                foreach (var filter in filters)
                                    switch (filter.Name)
                                    {
                                        case "andmatch":
                                            var characterLimit = -1;
                                            if (filter.Args != null)
                                                characterLimit = int.Parse(filter.Args);
                                            if (query.ImdbID != null && TorznabCaps.SupportsImdbMovieSearch)
                                                break; // skip andmatch filter for imdb searches
                                            if (!query.MatchQueryStringAND(release.Title, characterLimit))
                                            {
                                                logger.Debug(
                                                    string.Format(
                                                        "CardigannIndexer ({0}): skipping {1} (andmatch filter)", ID,
                                                        release.Title));
                                                skipRelease = true;
                                            }

                                            break;
                                        case "strdump":
                                            // for debugging
                                            logger.Debug(
                                                string.Format(
                                                    "CardigannIndexer ({0}): row strdump: {1}", ID, row.ToHtmlPretty()));
                                            break;
                                        default:
                                            logger.Error(
                                                string.Format(
                                                    "CardigannIndexer ({0}): Unsupported rows filter: {1}", ID,
                                                    filter.Name));
                                            break;
                                    }

                            if (skipRelease)
                                continue;

                            // if DateHeaders is set go through the previous rows and look for the header selector
                            var dateHeaders = Definition.Search.Rows.Dateheaders;
                            if (release.PublishDate == DateTime.MinValue && dateHeaders != null)
                            {
                                var prevRow = row.PreviousElementSibling;
                                string value = null;
                                if (prevRow == null) // continue with parent
                                {
                                    var parent = row.ParentElement;
                                    if (parent != null)
                                        prevRow = parent.PreviousElementSibling;
                                }

                                while (prevRow != null)
                                {
                                    var curRow = prevRow;
                                    logger.Debug(prevRow.OuterHtml);
                                    try
                                    {
                                        value = handleSelector(dateHeaders, curRow);
                                        break;
                                    }
                                    catch (Exception)
                                    {
                                        // do nothing
                                    }

                                    prevRow = curRow.PreviousElementSibling;
                                    if (prevRow == null) // continue with parent
                                    {
                                        var parent = curRow.ParentElement;
                                        if (parent != null)
                                            prevRow = parent.PreviousElementSibling;
                                    }
                                }

                                if (value == null && dateHeaders.Optional == false)
                                    throw new Exception(string.Format("No date header row found for {0}", release));
                                if (value != null)
                                    release.PublishDate = DateTimeUtil.FromUnknown(value);
                            }

                            releases.Add(release);
                        }
                        catch (Exception ex)
                        {
                            logger.Error(
                                string.Format(
                                    "CardigannIndexer ({0}): Error while parsing row '{1}':\n\n{2}", ID, row.ToHtmlPretty(),
                                    ex));
                        }
                }
                catch (Exception ex)
                {
                    OnParseError(results, ex);
                }
            }

            if (query.Limit > 0)
                releases = releases.Take(query.Limit).ToList();
            return releases;
        }

        protected async Task<WebClientByteResult> HandleRequestAsync(requestBlock request,
                                                                Dictionary<string, object> variables = null,
                                                                string referer = null)
        {
            var requestLinkStr = resolvePath(applyGoTemplateText(request.Path, variables)).ToString();
            Dictionary<string, string> pairs = null;
            var queryCollection = new NameValueCollection();
            var method = RequestType.Get;
            if (string.Equals(request.Method, "post", StringComparison.OrdinalIgnoreCase))
            {
                method = RequestType.Post;
                pairs = new Dictionary<string, string>();
            }

            foreach (var input in request.Inputs)
            {
                var value = applyGoTemplateText(input.Value, variables);
                if (method == RequestType.Get)
                    queryCollection.Add(input.Key, value);
                else if (method == RequestType.Post)
                    pairs.Add(input.Key, value);
            }

            if (queryCollection.Count > 0)
            {
                if (!requestLinkStr.Contains("?"))
                    requestLinkStr += $"?{queryCollection.GetQueryString(Encoding).Substring(1)}";
                else
                    requestLinkStr += queryCollection.GetQueryString(Encoding);
            }

            var response = await RequestBytesWithCookiesAndRetryAsync(requestLinkStr, null, method, referer, pairs);
            logger.Debug(
                $"CardigannIndexer ({ID}): handleRequest() remote server returned {response.Status.ToString()}{(response.IsRedirect ? $" => {response.RedirectingTo}" : "")}");
            return response;
        }

        protected IDictionary<string, object> AddTemplateVariablesFromUri(IDictionary<string, object> variables, Uri uri,
                                                                          string prefix = "")
        {
            variables[$"{prefix}.AbsoluteUri"] = uri.AbsoluteUri;
            variables[$"{prefix}.AbsolutePath"] = uri.AbsolutePath;
            variables[$"{prefix}.Scheme"] = uri.Scheme;
            variables[$"{prefix}.Host"] = uri.Host;
            variables[$"{prefix}.Port"] = uri.Port.ToString();
            variables[$"{prefix}.PathAndQuery"] = uri.PathAndQuery;
            variables[$"{prefix}.Query"] = uri.Query;
            var queryString = QueryHelpers.ParseQuery(uri.Query);
            foreach (var key in queryString.Keys)
                //If we have supplied the same query string multiple time, just take the first.
                variables[$"{prefix}.Query.{key}"] = queryString[key].First();
            return variables;
        }

        public override async Task<byte[]> Download(Uri link)
        {
            var method = RequestType.Get;
            if (Definition.Download != null)
            {
                var download = Definition.Download;
                var variables = getTemplateVariablesFromConfigData();
                AddTemplateVariablesFromUri(variables, link, ".DownloadUri");
                if (download.Before != null)
                {
                    var beforeresult = await HandleRequestAsync(download.Before, variables, link.ToString());
                }

                if (download.Method != null)
                    if (download.Method == "post")
                        method = RequestType.Post;
                if (download.Selector != null)
                {
                    var selector = applyGoTemplateText(download.Selector, variables);
                    var response = await RequestStringWithCookiesAsync(link.ToString());
                    if (response.IsRedirect)
                        response = await RequestStringWithCookiesAsync(response.RedirectingTo);
                    var results = response.Content;
                    var searchResultParser = new HtmlParser();
                    var searchResultDocument = searchResultParser.ParseDocument(results);
                    var downloadElement = searchResultDocument.QuerySelector(selector);
                    if (downloadElement != null)
                    {
                        logger.Debug(
                            string.Format(
                                "CardigannIndexer ({0}): Download selector {1} matched:{2}", ID, selector,
                                downloadElement.ToHtmlPretty()));
                        string href;
                        if (download.Attribute != null)
                        {
                            href = downloadElement.GetAttribute(download.Attribute);
                            if (href == null)
                                throw new Exception(
                                    string.Format(
                                        "Attribute \"{0}\" is not set for element {1}", download.Attribute,
                                        downloadElement.ToHtmlPretty()));
                        }
                        else
                            href = downloadElement.TextContent;

                        href = applyFilters(href, download.Filters, variables);
                        link = resolvePath(href, link);
                    }
                    else
                    {
                        logger.Error(
                            string.Format(
                                "CardigannIndexer ({0}): Download selector {1} didn't match:\n{2}", ID, download.Selector,
                                results));
                        throw new Exception(string.Format("Download selector {0} didn't match", download.Selector));
                    }
                }
            }

            return await DownloadAsync(link, method);
        }
    }
}
