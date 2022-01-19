using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.Xml.Parser;
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

namespace Jackett.Common.Indexers
{
    public class CardigannIndexer : BaseWebIndexer
    {
        protected IndexerDefinition Definition;
        protected WebResult landingResult;
        protected IHtmlDocument landingResultDocument;

        protected List<string> DefaultCategories = new List<string>();

        private new ConfigurationData configData
        {
            get => base.configData;
            set => base.configData = value;
        }

        protected readonly string[] OptionalFields = { "imdb", "imdbid", "rageid", "tmdbid", "tvdbid", "poster", "description" };

        private static readonly string[] _SupportedLogicFunctions =
        {
            "and",
            "or",
            "eq",
            "ne"
        };

        private static readonly string[] _LogicFunctionsUsingStringLiterals =
        {
            "eq",
            "ne"
        };

        // Matches a logic function above and 2 or more of (.varname) or .varname or "string literal" in any combination
        private static readonly Regex _LogicFunctionRegex = new Regex(
            @$"\b({string.Join("|", _SupportedLogicFunctions.Select(Regex.Escape))})(?:\s+(\(?\.[^\)\s]+\)?|""[^""]+"")){{2,}}");

        public CardigannIndexer(IIndexerConfigurationService configService, Utils.Clients.WebClient wc, Logger l,
                                IProtectionService ps, ICacheService cs, IndexerDefinition Definition)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs
                   )
        {
            this.Definition = Definition;
            Id = Definition.Id;

            // Add default data if necessary
            if (Definition.Settings == null)
            {
                Definition.Settings = new List<settingsField>
                {
                    new settingsField { Name = "username", Label = "Username", Type = "text" },
                    new settingsField { Name = "password", Label = "Password", Type = "password" }
                };
            }

            if (Definition.Encoding == null)
                Definition.Encoding = "UTF-8";

            if (Definition.RequestDelay != null)
                webclient.requestDelay = Definition.RequestDelay.Value;

            if (Definition.Login != null && Definition.Login.Method == null)
                Definition.Login.Method = "form";

            if (Definition.Search.Paths == null)
            {
                Definition.Search.Paths = new List<searchPathBlock>();
            }

            // convert definitions with a single search Path to a Paths entry
            if (Definition.Search.Path != null)
            {
                Definition.Search.Paths.Add(new searchPathBlock
                {
                    Path = Definition.Search.Path,
                    Inheritinputs = true
                });
            }

            // init missing mandatory attributes
            DisplayName = Definition.Name;
            DisplayDescription = Definition.Description;
            if (Definition.Links.Count > 1)
                AlternativeSiteLinks = Definition.Links.ToArray();
            DefaultSiteLink = Definition.Links[0];
            if (Definition.Legacylinks != null)
                LegacySiteLinks = Definition.Legacylinks.ToArray();
            Encoding = Encoding.GetEncoding(Definition.Encoding);
            if (!DefaultSiteLink.EndsWith("/"))
                DefaultSiteLink += "/";
            Language = Definition.Language;
            Type = Definition.Type;
            TorznabCaps = new TorznabCapabilities();
            TorznabCaps.ParseCardigannSearchModes(Definition.Caps.Modes);

            // init config Data
            configData = new ConfigurationData();
            foreach (var Setting in Definition.Settings)
            {
                ConfigurationItem item;

                var itemName = Setting.Label ?? Setting.Name;

                if (Setting.Type != null)
                {
                    switch (Setting.Type)
                    {
                        case "checkbox":
                            item = new BoolConfigurationItem(itemName) { Value = false };

                            if (Setting.Default != null && Setting.Default == "true")
                            {
                                ((BoolConfigurationItem)item).Value = true;
                            }
                            break;
                        case "password":
                        case "text":
                            item = new StringConfigurationItem(itemName) { Value = Setting.Default };
                            break;
                        case "multi-select":
                            if (Setting.Options == null)
                            {
                                throw new Exception("Options must be given for the 'multi-select' type.");
                            }

                            item = new MultiSelectConfigurationItem(itemName, Setting.Options) { Values = Setting.Defaults };
                            break;
                        case "select":
                            if (Setting.Options == null)
                            {
                                throw new Exception("Options must be given for the 'select' type.");
                            }

                            item = new SingleSelectConfigurationItem(itemName, Setting.Options) { Value = Setting.Default };
                            break;
                        case "info":
                            item = new DisplayInfoConfigurationItem(itemName, Setting.Default);
                            break;
                        default:
                            throw new Exception($"Invalid setting type '{Setting.Type}' specified.");
                    }
                }
                else
                {
                    item = new StringConfigurationItem(itemName) { Value = Setting.Default };
                }

                configData.AddDynamic(Setting.Name, item);
            }

            if (Definition.Caps.Categories != null)
            {
                foreach (var Category in Definition.Caps.Categories)
                {
                    var cat = TorznabCatType.GetCatByName(Category.Value);
                    if (cat == null)
                    {
                        logger.Error(string.Format("CardigannIndexer ({0}): invalid Torznab category for id {1}: {2}", Id, Category.Key, Category.Value));
                        continue;
                    }
                    AddCategoryMapping(Category.Key, cat);
                }
            }

            if (Definition.Caps.Categorymappings != null)
            {
                foreach (var Categorymapping in Definition.Caps.Categorymappings)
                {
                    TorznabCategory TorznabCat = null;

                    if (Categorymapping.cat != null)
                    {
                        TorznabCat = TorznabCatType.GetCatByName(Categorymapping.cat);
                        if (TorznabCat == null)
                        {
                            logger.Error(string.Format("CardigannIndexer ({0}): invalid Torznab category for id {1}: {2}", Id, Categorymapping.id, Categorymapping.cat));
                            continue;
                        }
                    }
                    AddCategoryMapping(Categorymapping.id, TorznabCat, Categorymapping.desc);
                    if (Categorymapping.Default)
                        DefaultCategories.Add(Categorymapping.id);
                }
            }
            LoadValuesFromJson(null);
        }

        public override void LoadValuesFromJson(JToken jsonConfig, bool useProtectionService = false)
        {
            base.LoadValuesFromJson(jsonConfig, useProtectionService);

            // add self signed cert to trusted certs
            if (Definition.Certificates != null)
            {
                foreach (var certificateHash in Definition.Certificates)
                    webclient.AddTrustedCertificate(new Uri(SiteLink).Host, certificateHash);
            }
        }

        protected Dictionary<string, object> GetBaseTemplateVariables()
        {
            var variables = new Dictionary<string, object>
            {
                [".Config.sitelink"] = SiteLink,
                [".True"] = "True",
                [".False"] = null,
                [".Today.Year"] = DateTime.Today.Year.ToString()
            };

            foreach (var setting in Definition.Settings)
            {
                var configurationItem = configData.GetDynamic(setting.Name);
                if (configurationItem == null)
                    continue;

                var variableKey = ".Config." + setting.Name;

                switch (configurationItem)
                {
                    case BoolConfigurationItem boolItem:
                        {
                            variables[variableKey] = variables[boolItem.Value ? ".True" : ".False"];
                            break;
                        }
                    case StringConfigurationItem stringItem:
                        {
                            variables[variableKey] = stringItem.Value;
                            break;
                        }
                    case PasswordConfigurationItem passwordItem:
                        {
                            variables[variableKey] = passwordItem.Value;
                            break;
                        }
                    case SingleSelectConfigurationItem selectItem:
                        {
                            variables[variableKey] = selectItem.Value;
                            break;
                        }
                    case MultiSelectConfigurationItem multiSelectItem:
                        {
                            variables[variableKey] = multiSelectItem.Values;
                            break;
                        }
                    case DisplayImageConfigurationItem displayImageItem:
                        {
                            variables[variableKey] = displayImageItem.Value;
                            break;
                        }
                    case DisplayInfoConfigurationItem displayInfoItem:
                        {
                            variables[variableKey] = displayInfoItem.Value;
                            break;
                        }
                    case HiddenStringConfigurationItem hiddenStringItem:
                        {
                            variables[variableKey] = hiddenStringItem.Value;
                            break;
                        }
                    default:
                        {
                            //TODO Should this throw a NotSupportedException, as it used to?
                            break;
                        }
                }
            }

            return variables;
        }

        // A very bad implementation of the golang template/text templating engine.
        // But it should work for most basic constucts used by Cardigann definitions.
        protected delegate string TemplateTextModifier(string str);
        protected string applyGoTemplateText(string template, Dictionary<string, object> variables = null, TemplateTextModifier modifier = null)
        {
            if (variables == null)
            {
                variables = GetBaseTemplateVariables();
            }

            // handle re_replace expression
            // Example: {{ re_replace .Query.Keywords "[^a-zA-Z0-9]+" "%" }}
            var ReReplaceRegex = new Regex(@"{{\s*re_replace\s+(\..+?)\s+""(.*?)""\s+""(.*?)""\s*}}");
            var ReReplaceRegexMatches = ReReplaceRegex.Match(template);

            while (ReReplaceRegexMatches.Success)
            {
                var all = ReReplaceRegexMatches.Groups[0].Value;
                var variable = ReReplaceRegexMatches.Groups[1].Value;
                var regexp = ReReplaceRegexMatches.Groups[2].Value;
                var newvalue = ReReplaceRegexMatches.Groups[3].Value;

                var ReplaceRegex = new Regex(regexp);
                var input = (string)variables[variable];
                var expanded = ReplaceRegex.Replace(input, newvalue);

                if (modifier != null)
                    expanded = modifier(expanded);

                template = template.Replace(all, expanded);
                ReReplaceRegexMatches = ReReplaceRegexMatches.NextMatch();
            }

            // handle join expression
            // Example: {{ join .Categories "," }}
            var JoinRegex = new Regex(@"{{\s*join\s+(\..+?)\s+""(.*?)""\s*}}");
            var JoinMatches = JoinRegex.Match(template);

            while (JoinMatches.Success)
            {
                var all = JoinMatches.Groups[0].Value;
                var variable = JoinMatches.Groups[1].Value;
                var delimiter = JoinMatches.Groups[2].Value;

                var input = (ICollection<string>)variables[variable];
                var expanded = string.Join(delimiter, input);

                if (modifier != null)
                    expanded = modifier(expanded);

                template = template.Replace(all, expanded);
                JoinMatches = JoinMatches.NextMatch();
            }

            var logicMatch = _LogicFunctionRegex.Match(template);

            while (logicMatch.Success)
            {
                var functionStartIndex = logicMatch.Groups[0].Index;
                var functionLength = logicMatch.Groups[0].Length;
                var functionName = logicMatch.Groups[1].Value;
                // Use Group.Captures to get each matching string in a repeating Match.Group
                // Strip () around variable names here, as they are optional. Use quotes to differentiate variables and literals
                var parameters = logicMatch.Groups[2].Captures.Cast<Capture>().Select(c => c.Value.Trim('(', ')')).ToList();
                var functionResult = "";

                // If the function can't use string literals, fail silently by removing the literals.
                if (!_LogicFunctionsUsingStringLiterals.Contains(functionName))
                    parameters.RemoveAll(param => param.StartsWith("\""));

                switch (functionName)
                {
                    case "and": // returns first null or empty, else last variable
                    case "or": // returns first not null or empty, else last variable
                        var isAnd = functionName == "and";
                        foreach (var parameter in parameters)
                        {
                            functionResult = parameter;
                            // (null as string) == null
                            // (if null or empty) break if and, continue if or
                            // (if neither null nor empty) continue if and, break if or
                            if (string.IsNullOrWhiteSpace(variables[parameter] as string) == isAnd)
                                break;
                        }
                        break;
                    case "eq": // Returns .True if equal
                    case "ne": // Returns .False if equal
                        {
                            var wantEqual = functionName == "eq";
                            // eq/ne take exactly 2 params. Update the length to match
                            // This removes the whitespace between params 2 and 3.
                            // It shouldn't matter because the match starts at a word boundary
                            if (parameters.Count > 2)
                                functionLength = logicMatch.Groups[2].Captures[2].Index - functionStartIndex;

                            // Take first two parameters, convert vars to values and strip quotes on string literals
                            // Counting distinct gives us 1 if equal and 2 if not.
                            var isEqual =
                                parameters.Take(2).Select(param => param.StartsWith("\"") ? param.Trim('"') : variables[param] as string)
                                          .Distinct().Count() == 1;

                            functionResult = isEqual == wantEqual ? ".True" : ".False";
                            break;
                        }
                }

                template = template.Remove(functionStartIndex, functionLength)
                                   .Insert(functionStartIndex, functionResult);
                // Rerunning match instead of using nextMatch allows us to support nested functions
                // like {{if and eq (.Var1) "string1" eq (.Var2) "string2"}}
                // No performance is lost because Match/NextMatch are lazy evaluated and pause execution after first match
                logicMatch = _LogicFunctionRegex.Match(template);
            }

            // handle if ... else ... expression
            var IfElseRegex = new Regex(@"{{\s*if\s*(.+?)\s*}}(.*?){{\s*else\s*}}(.*?){{\s*end\s*}}");
            var IfElseRegexMatches = IfElseRegex.Match(template);

            while (IfElseRegexMatches.Success)
            {
                string conditionResult = null;

                var all = IfElseRegexMatches.Groups[0].Value;
                var condition = IfElseRegexMatches.Groups[1].Value;
                var onTrue = IfElseRegexMatches.Groups[2].Value;
                var onFalse = IfElseRegexMatches.Groups[3].Value;

                if (condition.StartsWith("."))
                {
                    var conditionResultState = false;
                    var value = variables[condition];

                    if (value == null)
                        conditionResultState = false;
                    else if (value is string)
                        conditionResultState = !string.IsNullOrWhiteSpace((string)value);
                    else if (value is ICollection)
                        conditionResultState = ((ICollection)value).Count > 0;
                    else
                        throw new Exception(string.Format("Unexpceted type for variable {0}: {1}", condition, value.GetType()));

                    if (conditionResultState)
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
            var RangeRegex = new Regex(@"{{\s*range\s*(.+?)\s*}}(.*?){{\.}}(.*?){{end}}");
            var RangeRegexMatches = RangeRegex.Match(template);

            while (RangeRegexMatches.Success)
            {
                var expanded = string.Empty;

                var all = RangeRegexMatches.Groups[0].Value;
                var variable = RangeRegexMatches.Groups[1].Value;
                var prefix = RangeRegexMatches.Groups[2].Value;
                var postfix = RangeRegexMatches.Groups[3].Value;

                foreach (var value in (ICollection<string>)variables[variable])
                {
                    var newvalue = value;
                    if (modifier != null)
                        newvalue = modifier(newvalue);
                    expanded += prefix + newvalue + postfix;
                }
                template = template.Replace(all, expanded);
                RangeRegexMatches = RangeRegexMatches.NextMatch();
            }

            // handle simple variables
            var VariablesRegEx = new Regex(@"{{\s*(\..+?)\s*}}");
            var VariablesRegExMatches = VariablesRegEx.Match(template);

            while (VariablesRegExMatches.Success)
            {
                var expanded = string.Empty;

                var all = VariablesRegExMatches.Groups[0].Value;
                var variable = VariablesRegExMatches.Groups[1].Value;

                var value = (string)variables[variable];
                if (modifier != null)
                    value = modifier(value);
                template = template.Replace(all, value);
                VariablesRegExMatches = VariablesRegExMatches.NextMatch();
            }

            return template;
        }

        protected bool checkForError(WebResult loginResult, IList<errorBlock> errorBlocks)
        {
            if (loginResult.Status == HttpStatusCode.Unauthorized) // e.g. used by YGGtorrent
                throw new ExceptionWithConfigData("401 Unauthorized, check your credentials", configData);

            if (errorBlocks == null)
                return true; // no error

            var ResultParser = new HtmlParser();
            var ResultDocument = ResultParser.ParseDocument(loginResult.ContentString);
            foreach (var error in errorBlocks)
            {
                var selection = ResultDocument.QuerySelector(error.Selector);
                if (selection != null)
                {
                    var errorMessage = selection.TextContent;
                    if (error.Message != null)
                    {
                        errorMessage = handleSelector(error.Message, ResultDocument.FirstElementChild);
                    }
                    throw new ExceptionWithConfigData(string.Format("Error: {0}", errorMessage.Trim()), configData);
                }
            }
            return true; // no error
        }

        protected async Task<bool> DoLogin()
        {
            var Login = Definition.Login;

            if (Login == null)
                return true;

            if (Login.Method == "post")
            {
                var pairs = new Dictionary<string, string>();
                foreach (var Input in Definition.Login.Inputs)
                {
                    var value = applyGoTemplateText(Input.Value);
                    pairs.Add(Input.Key, value);
                }

                var LoginUrl = resolvePath(Login.Path).ToString();
                configData.CookieHeader.Value = null;
                var loginResult = await RequestLoginAndFollowRedirect(LoginUrl, pairs, null, true, null, SiteLink, true);
                configData.CookieHeader.Value = loginResult.Cookies;

                checkForError(loginResult, Definition.Login.Error);
            }
            else if (Login.Method == "form")
            {
                var LoginUrl = resolvePath(Login.Path).ToString();

                var queryCollection = new NameValueCollection();
                var pairs = new Dictionary<string, string>();

                var FormSelector = Login.Form;
                if (FormSelector == null)
                    FormSelector = "form";

                // landingResultDocument might not be initiated if the login is caused by a relogin during a query
                if (landingResultDocument == null)
                {
                    var ConfigurationResult = await GetConfigurationForSetup(true);
                    if (ConfigurationResult == null) // got captcha
                    {
                        return false;
                    }
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

                var submitUrlstr = form.GetAttribute("action");
                if (Login.Submitpath != null)
                    submitUrlstr = Login.Submitpath;

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
                    var input = Input.Key;
                    if (Login.Selectors)
                    {
                        var inputElement = landingResultDocument.QuerySelector(Input.Key);
                        if (inputElement == null)
                            throw new ExceptionWithConfigData(string.Format("Login failed: No input found using selector {0}", Input.Key), configData);
                        input = inputElement.GetAttribute("name");
                    }
                    pairs[input] = value;
                }

                // selector inputs
                if (Login.Selectorinputs != null)
                {
                    foreach (var Selectorinput in Login.Selectorinputs)
                    {
                        string value = null;
                        try
                        {
                            value = handleSelector(Selectorinput.Value, landingResultDocument.FirstElementChild);
                            pairs[Selectorinput.Key] = value;
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(string.Format("Error while parsing selector input={0}, selector={1}, value={2}: {3}", Selectorinput.Key, Selectorinput.Value.Selector, value, ex.Message));
                        }
                    }
                }

                // getselector inputs
                if (Login.Getselectorinputs != null)
                {
                    foreach (var Selectorinput in Login.Getselectorinputs)
                    {
                        string value = null;
                        try
                        {
                            value = handleSelector(Selectorinput.Value, landingResultDocument.FirstElementChild);
                            queryCollection[Selectorinput.Key] = value;
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(string.Format("Error while parsing get selector input={0}, selector={1}, value={2}: {3}", Selectorinput.Key, Selectorinput.Value.Selector, value, ex.Message));
                        }
                    }
                }
                if (queryCollection.Count > 0)
                    submitUrlstr += "?" + queryCollection.GetQueryString();
                var submitUrl = resolvePath(submitUrlstr, new Uri(LoginUrl));

                // automatically solve simpleCaptchas, if used
                var simpleCaptchaPresent = landingResultDocument.QuerySelector("script[src*=\"simpleCaptcha\"]");
                if (simpleCaptchaPresent != null)
                {
                    var captchaUrl = resolvePath("simpleCaptcha.php?numImages=1");
                    var simpleCaptchaResult = await RequestWithCookiesAsync(captchaUrl.ToString(), referer: LoginUrl);
                    var simpleCaptchaJSON = JObject.Parse(simpleCaptchaResult.ContentString);
                    var captchaSelection = simpleCaptchaJSON["images"][0]["hash"].ToString();
                    pairs["captchaSelection"] = captchaSelection;
                    pairs["submitme"] = "X";
                }

                if (Login.Captcha != null)
                {
                    var Captcha = Login.Captcha;
                    if (Captcha.Type == "image")
                    {
                        var CaptchaText = (StringConfigurationItem)configData.GetDynamic("CaptchaText");
                        if (CaptchaText != null)
                        {
                            var input = Captcha.Input;
                            if (Login.Selectors)
                            {
                                var inputElement = landingResultDocument.QuerySelector(Captcha.Input);
                                if (inputElement == null)
                                    throw new ExceptionWithConfigData(string.Format("Login failed: No captcha input found using {0}", Captcha.Input), configData);
                                input = inputElement.GetAttribute("name");
                            }
                            pairs[input] = CaptchaText.Value;
                        }
                    }
                    if (Captcha.Type == "text")
                    {
                        var CaptchaAnswer = (StringConfigurationItem)configData.GetDynamic("CaptchaAnswer");
                        if (CaptchaAnswer != null)
                        {
                            var input = Captcha.Input;
                            if (Login.Selectors)
                            {
                                var inputElement = landingResultDocument.QuerySelector(Captcha.Input);
                                if (inputElement == null)
                                    throw new ExceptionWithConfigData(string.Format("Login failed: No captcha input found using {0}", Captcha.Input), configData);
                                input = inputElement.GetAttribute("name");
                            }
                            pairs[input] = CaptchaAnswer.Value;
                        }
                    }
                }

                // clear landingResults/Document, otherwise we might use an old version for a new relogin (if GetConfigurationForSetup() wasn't called before)
                landingResult = null;
                landingResultDocument = null;

                WebResult loginResult = null;
                var enctype = form.GetAttribute("enctype");
                if (enctype == "multipart/form-data")
                {
                    var headers = new Dictionary<string, string>();
                    var boundary = "---------------------------" + (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds.ToString().Replace(".", "");
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

                    headers.Add("Content-Type", "multipart/form-data; boundary=" + boundary);
                    var body = string.Join("\r\n", bodyParts);
                    loginResult = await RequestWithCookiesAsync(
                        submitUrl.ToString(), configData.CookieHeader.Value, RequestType.POST, SiteLink, pairs, headers,
                        body);
                }
                else
                {
                    loginResult = await RequestLoginAndFollowRedirect(submitUrl.ToString(), pairs, configData.CookieHeader.Value, true, null, LoginUrl, true);
                }

                configData.CookieHeader.Value = loginResult.Cookies;

                checkForError(loginResult, Definition.Login.Error);
            }
            else if (Login.Method == "cookie")
            {
                configData.CookieHeader.Value = ((StringConfigurationItem)configData.GetDynamic("cookie")).Value;
            }
            else if (Login.Method == "get")
            {
                var queryCollection = new NameValueCollection();
                foreach (var Input in Definition.Login.Inputs)
                {
                    var value = applyGoTemplateText(Input.Value);
                    queryCollection.Add(Input.Key, value);
                }

                var LoginUrl = resolvePath(Login.Path + "?" + queryCollection.GetQueryString()).ToString();
                configData.CookieHeader.Value = null;
                var loginResult = await RequestWithCookiesAsync(LoginUrl, referer: SiteLink);
                configData.CookieHeader.Value = loginResult.Cookies;

                checkForError(loginResult, Definition.Login.Error);
            }
            else if (Login.Method == "oneurl")
            {
                var OneUrl = applyGoTemplateText(Definition.Login.Inputs["oneurl"]);
                var LoginUrl = resolvePath(Login.Path + OneUrl).ToString();
                configData.CookieHeader.Value = null;
                var loginResult = await RequestWithCookiesAsync(LoginUrl, referer: SiteLink);
                configData.CookieHeader.Value = loginResult.Cookies;

                checkForError(loginResult, Definition.Login.Error);
            }
            else
            {
                throw new NotImplementedException("Login method " + Definition.Login.Method + " not implemented");
            }
            logger.Debug(string.Format("CardigannIndexer ({0}): Cookies after login: {1}", Id, CookieHeader));
            return true;
        }

        protected string getRedirectDomainHint(string requestUrl, string RedirectUrl)
        {
            if (requestUrl.StartsWith(SiteLink) && !RedirectUrl.StartsWith(SiteLink))
            {
                var uri = new Uri(RedirectUrl);
                return uri.Scheme + "://" + uri.Host + "/";
            }
            return null;
        }

        protected string getRedirectDomainHint(WebResult result) => getRedirectDomainHint(result.Request.Url, result.RedirectingTo);

        protected async Task<bool> TestLogin()
        {
            var Login = Definition.Login;

            if (Login == null || Login.Test == null)
                return false;

            // test if login was successful
            var LoginTestUrl = resolvePath(Login.Test.Path).ToString();
            var headers = ParseCustomHeaders(Definition.Search?.Headers, GetBaseTemplateVariables());
            var testResult = await RequestWithCookiesAsync(LoginTestUrl, headers: headers);

            if (testResult.IsRedirect)
            {
                var errormessage = "Login Failed, got redirected.";
                var DomainHint = getRedirectDomainHint(testResult);
                if (DomainHint != null)
                {
                    errormessage += " Try changing the indexer URL to " + DomainHint + ".";
                    if (Definition.Followredirect)
                    {
                        configData.SiteLink.Value = DomainHint;
                        SiteLink = configData.SiteLink.Value;
                        SaveConfig();
                        errormessage += " Updated site link, please try again.";
                    }
                }
                throw new ExceptionWithConfigData(errormessage, configData);
            }

            if (Login.Test.Selector != null)
            {
                var testResultParser = new HtmlParser();
                var testResultDocument = testResultParser.ParseDocument(testResult.ContentString);
                var selection = testResultDocument.QuerySelectorAll(Login.Test.Selector);
                if (selection.Length == 0)
                {
                    throw new ExceptionWithConfigData(string.Format("Login failed: Selector \"{0}\" didn't match", Login.Test.Selector), configData);
                }
            }
            return true;
        }

        protected bool CheckIfLoginIsNeeded(WebResult Result, IHtmlDocument document)
        {
            if (Result.IsRedirect)
            {
                var DomainHint = getRedirectDomainHint(Result);
                if (DomainHint != null)
                {
                    var errormessage = "Got redirected to another domain. Try changing the indexer URL to " + DomainHint + ".";
                    if (Definition.Followredirect)
                    {
                        configData.SiteLink.Value = DomainHint;
                        SiteLink = configData.SiteLink.Value;
                        SaveConfig();
                        errormessage += " Updated site link, please try again.";
                    }
                    throw new ExceptionWithConfigData(errormessage, configData);
                }

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
            try
            {
                return await GetConfigurationForSetup(false);
            }
            catch (Exception e)
            {
                logger.Error("Exception in GetConfigurationForSetup (" + Id + "): " + e);
                return configData;
            }
        }

        public async Task<ConfigurationData> GetConfigurationForSetup(bool automaticlogin)
        {
            var Login = Definition.Login;

            if (Login == null || Login.Method != "form")
                return configData;

            var LoginUrl = resolvePath(Login.Path);

            configData.CookieHeader.Value = null;
            if (Login.Cookies != null)
                configData.CookieHeader.Value = string.Join("; ", Login.Cookies);
            landingResult = await RequestWithCookiesAsync(LoginUrl.AbsoluteUri, referer: SiteLink);

            // Some sites have a temporary redirect before the login page, we need to process it.
            if (Definition.Followredirect)
            {
                await FollowIfRedirect(landingResult, LoginUrl.AbsoluteUri, overrideCookies: landingResult.Cookies, accumulateCookies: true);
            }

            var hasCaptcha = false;
            var htmlParser = new HtmlParser();
            landingResultDocument = htmlParser.ParseDocument(landingResult.ContentString);

            if (Login.Captcha != null)
            {
                var Captcha = Login.Captcha;
                if (Captcha.Type == "image")
                {
                    var captchaElement = landingResultDocument.QuerySelector(Captcha.Selector);
                    if (captchaElement != null)
                    {
                        hasCaptcha = true;

                        var CaptchaUrl = resolvePath(captchaElement.GetAttribute("src"), LoginUrl);
                        var captchaImageData = await RequestWithCookiesAsync(
                            CaptchaUrl.ToString(), landingResult.Cookies, referer: LoginUrl.AbsoluteUri);
                        var CaptchaImage = new DisplayImageConfigurationItem("Captcha Image");
                        var CaptchaText = new StringConfigurationItem("Captcha Text");

                        CaptchaImage.Value = captchaImageData.ContentBytes;

                        configData.AddDynamic("CaptchaImage", CaptchaImage);
                        configData.AddDynamic("CaptchaText", CaptchaText);
                    }
                    else
                    {
                        logger.Debug(string.Format("CardigannIndexer ({0}): No captcha image found", Id));
                    }
                }
                else if (Captcha.Type == "text")
                {
                    var captchaElement = landingResultDocument.QuerySelector(Captcha.Selector);
                    if (captchaElement != null)
                    {
                        hasCaptcha = true;

                        var CaptchaChallenge = new DisplayInfoConfigurationItem("Captcha Challenge", captchaElement.TextContent);
                        var CaptchaAnswer = new StringConfigurationItem("Captcha Answer");

                        configData.AddDynamic("CaptchaChallenge", CaptchaChallenge);
                        configData.AddDynamic("CaptchaAnswer", CaptchaAnswer);
                    }
                    else
                    {
                        logger.Debug(string.Format("CardigannIndexer ({0}): No captcha image found", Id));
                    }
                }
                else
                {
                    throw new NotImplementedException(string.Format("Captcha type \"{0}\" is not implemented", Captcha.Type));
                }
            }

            if (hasCaptcha && automaticlogin)
            {
                configData.LastError.Value = "Got captcha during automatic login, please reconfigure manually";
                logger.Error(string.Format("CardigannIndexer ({0}): Found captcha during automatic login, aborting", Id));
                return null;
            }

            return configData;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            await DoLogin();
            await TestLogin();

            IsConfigured = true;
            SaveConfig();
            return IndexerConfigurationStatus.Completed;
        }

        protected string applyFilters(string Data, List<filterBlock> Filters, Dictionary<string, object> variables = null)
        {
            if (Filters == null)
                return Data;

            foreach (var Filter in Filters)
            {
                switch (Filter.Name)
                {
                    case "querystring":
                        var param = (string)Filter.Args;
                        Data = ParseUtil.GetArgumentFromQueryString(Data, param);
                        break;
                    case "timeparse":
                    case "dateparse":
                        var layout = (string)Filter.Args;
                        try
                        {
                            var Date = DateTimeUtil.ParseDateTimeGoLang(Data, layout);
                            Data = Date.ToString(DateTimeUtil.Rfc1123ZPattern);
                        }
                        catch (FormatException ex)
                        {
                            logger.Debug(ex.Message);
                        }
                        break;
                    case "regexp":
                        var pattern = (string)Filter.Args;
                        var Regexp = new Regex(pattern);
                        var Match = Regexp.Match(Data);
                        Data = Match.Groups[1].Value;
                        break;
                    case "re_replace":
                        var regexpreplace_pattern = (string)Filter.Args[0];
                        var regexpreplace_replacement = (string)Filter.Args[1];
                        regexpreplace_replacement = applyGoTemplateText(regexpreplace_replacement, variables);
                        var regexpreplace_regex = new Regex(regexpreplace_pattern);
                        Data = regexpreplace_regex.Replace(Data, regexpreplace_replacement);
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
                        to = applyGoTemplateText(to, variables);
                        Data = Data.Replace(from, to);
                        break;
                    case "trim":
                        var cutset = (string)Filter.Args;
                        if (cutset != null)
                            Data = Data.Trim(cutset[0]);
                        else
                            Data = Data.Trim();
                        break;
                    case "prepend":
                        var prependstr = (string)Filter.Args;
                        Data = applyGoTemplateText(prependstr, variables) + Data;
                        break;
                    case "append":
                        var str = (string)Filter.Args;
                        Data += applyGoTemplateText(str, variables);
                        break;
                    case "tolower":
                        Data = Data.ToLower();
                        break;
                    case "toupper":
                        Data = Data.ToUpper();
                        break;
                    case "urldecode":
                        Data = WebUtilityHelpers.UrlDecode(Data, Encoding);
                        break;
                    case "urlencode":
                        Data = WebUtilityHelpers.UrlEncode(Data, Encoding);
                        break;
                    case "timeago":
                    case "reltime":
                        Data = DateTimeUtil.FromTimeAgo(Data).ToString(DateTimeUtil.Rfc1123ZPattern);
                        break;
                    case "fuzzytime":
                        Data = DateTimeUtil.FromUnknown(Data).ToString(DateTimeUtil.Rfc1123ZPattern);
                        break;
                    case "validfilename":
                        Data = StringUtil.MakeValidFileName(Data, '_', false);
                        break;
                    case "diacritics":
                        var diacriticsOp = (string)Filter.Args;
                        if (diacriticsOp == "replace")
                        {
                            // Should replace diacritics charcaters with their base character
                            // It's not perfect, e.g. "ŠĐĆŽ - šđčćž" becomes "SĐCZ-sđccz"
                            var stFormD = Data.Normalize(NormalizationForm.FormD);
                            var len = stFormD.Length;
                            var sb = new StringBuilder();
                            for (var i = 0; i < len; i++)
                            {
                                var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(stFormD[i]);
                                if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                                {
                                    sb.Append(stFormD[i]);
                                }
                            }
                            Data = (sb.ToString().Normalize(NormalizationForm.FormC));
                        }
                        else
                            throw new Exception("unsupported diacritics filter argument");
                        break;
                    case "jsonjoinarray":
                        var jsonjoinarrayJSONPath = (string)Filter.Args[0];
                        var jsonjoinarraySeparator = (string)Filter.Args[1];
                        var jsonjoinarrayO = JObject.Parse(Data);
                        var jsonjoinarrayOResult = jsonjoinarrayO.SelectToken(jsonjoinarrayJSONPath);
                        var jsonjoinarrayOResultStrings = jsonjoinarrayOResult.Select(j => j.ToString());
                        Data = string.Join(jsonjoinarraySeparator, jsonjoinarrayOResultStrings);
                        break;
                    case "hexdump":
                        // this is mainly for debugging invisible special char related issues
                        var HexData = string.Join("", Data.Select(c => c + "(" + ((int)c).ToString("X2") + ")"));
                        logger.Debug(string.Format("CardigannIndexer ({0}): strdump: {1}", Id, HexData));
                        break;
                    case "strdump":
                        // for debugging
                        var DebugData = Data.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\xA0", "\\xA0");
                        var strTag = (string)Filter.Args;
                        if (strTag != null)
                            strTag = string.Format("({0}):", strTag);
                        else
                            strTag = ":";
                        logger.Debug(string.Format("CardigannIndexer ({0}): strdump{1} {2}", Id, strTag, DebugData));
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

        protected string handleSelector(selectorBlock Selector, IElement Dom, Dictionary<string, object> variables = null, bool required = true)
        {
            if (Selector.Text != null)
            {
                return applyFilters(applyGoTemplateText(Selector.Text, variables), Selector.Filters, variables);
            }

            var selection = Dom;
            string value = null;

            if (Selector.Selector != null)
            {
                var selector_Selector = applyGoTemplateText(Selector.Selector, variables);
                if (Dom.Matches(selector_Selector))
                    selection = Dom;
                else
                    selection = QuerySelector(Dom, selector_Selector);
                if (selection == null)
                {
                    if (required)
                        throw new Exception(string.Format("Selector \"{0}\" didn't match {1}", selector_Selector, Dom.ToHtmlPretty()));
                    return null;
                }
            }

            if (Selector.Remove != null)
            {
                foreach (var i in selection.QuerySelectorAll(Selector.Remove))
                {
                    i.Remove();
                }
            }

            if (Selector.Case != null)
            {
                foreach (var Case in Selector.Case)
                {
                    if (selection.Matches(Case.Key) || QuerySelector(selection, Case.Key) != null)
                    {
                        value = Case.Value;
                        break;
                    }
                }
                if (value == null)
                {
                    if (required)
                        throw new Exception(string.Format("None of the case selectors \"{0}\" matched {1}", string.Join(",", Selector.Case), selection.ToHtmlPretty()));
                    return null;
                }
            }
            else if (Selector.Attribute != null)
            {
                value = selection.GetAttribute(Selector.Attribute);
                if (value == null)
                {
                    if (required)
                        throw new Exception(string.Format("Attribute \"{0}\" is not set for element {1}", Selector.Attribute, selection.ToHtmlPretty()));
                    return null;
                }
            }
            else
            {
                value = selection.TextContent;
            }

            return applyFilters(ParseUtil.NormalizeSpace(value), Selector.Filters, variables);
        }

        protected string handleJsonSelector(selectorBlock Selector, JToken parentObj, Dictionary<string, object> variables = null, bool required = true)
        {
            if (Selector.Text != null)
            {
                return applyFilters(applyGoTemplateText(Selector.Text, variables), Selector.Filters, variables);
            }

            string value = null;

            if (Selector.Selector != null)
            {
                var selector_Selector = applyGoTemplateText(Selector.Selector.TrimStart('.'), variables);
                var selection = parentObj.SelectToken(selector_Selector);
                if (selection == null)
                {
                    if (required)
                        throw new Exception(string.Format("Selector \"{0}\" didn't match {1}", selector_Selector, parentObj.ToString()));
                    return null;
                }
                value = selection.Value<string>();
            }

            if (Selector.Case != null)
            {
                foreach (var Case in Selector.Case)
                {
                    if (value.Equals(Case.Key) || Case.Key.Equals("*"))
                    {
                        value = Case.Value;
                        break;
                    }
                }
                if (value == null)
                {
                    if (required)
                        throw new Exception(string.Format("None of the case selectors \"{0}\" matched {1}", string.Join(",", Selector.Case), parentObj.ToString()));
                    return null;
                }
            }

            return applyFilters(ParseUtil.NormalizeSpace(value), Selector.Filters, variables);
        }

        protected Uri resolvePath(string path, Uri currentUrl = null) => new Uri(currentUrl ?? new Uri(SiteLink), path);

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();

            var Search = Definition.Search;

            // init template context
            var variables = GetBaseTemplateVariables();

            variables[".Query.Type"] = query.QueryType;
            variables[".Query.Q"] = query.SearchTerm;
            variables[".Query.Series"] = null;
            variables[".Query.Ep"] = query.Episode;
            variables[".Query.Season"] = query.Season > 0 ? query.Season.ToString() : null;
            variables[".Query.Movie"] = null;
            variables[".Query.Year"] = query.Year?.ToString() ?? null;
            variables[".Query.Limit"] = query.Limit.ToString() ?? null;
            variables[".Query.Offset"] = query.Offset.ToString() ?? null;
            variables[".Query.Extended"] = query.Extended.ToString();
            variables[".Query.Categories"] = query.Categories;
            variables[".Query.APIKey"] = query.ApiKey;
            variables[".Query.TVDBID"] = query.TvdbID?.ToString() ?? null;
            variables[".Query.TVRageID"] = query.RageID?.ToString() ?? null;
            variables[".Query.IMDBID"] = query.ImdbID;
            variables[".Query.IMDBIDShort"] = query.ImdbIDShort;
            variables[".Query.TMDBID"] = query.TmdbID?.ToString() ?? null;
            variables[".Query.TVMazeID"] = null;
            variables[".Query.TraktID"] = null;
            variables[".Query.Album"] = query.Album;
            variables[".Query.Artist"] = query.Artist;
            variables[".Query.Label"] = query.Label;
            variables[".Query.Track"] = query.Track;
            //variables[".Query.Genre"] = query.Genre ?? new List<string>();
            variables[".Query.Episode"] = query.GetEpisodeSearchString();
            variables[".Query.Author"] = query.Author;
            variables[".Query.Title"] = query.Title;

            var mappedCategories = MapTorznabCapsToTrackers(query);
            if (mappedCategories.Count == 0)
            {
                mappedCategories = DefaultCategories;
            }

            variables[".Categories"] = mappedCategories;

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
            variables[".Keywords"] = applyFilters((string)variables[".Query.Keywords"], Search.Keywordsfilters);

            // TODO: prepare queries first and then send them parallel
            var SearchPaths = Search.Paths;
            foreach (var SearchPath in SearchPaths)
            {
                // skip path if categories don't match
                if (SearchPath.Categories.Count > 0)
                {
                    var invertMatch = (SearchPath.Categories[0] == "!");
                    var hasIntersect = mappedCategories.Intersect(SearchPath.Categories).Any();
                    if (invertMatch)
                        hasIntersect = !hasIntersect;
                    if (!hasIntersect)
                        continue;
                }

                // build search URL
                // HttpUtility.UrlPathEncode seems to only encode spaces, we use UrlEncode and replace + with %20 as a workaround
                var searchUrl = resolvePath(applyGoTemplateText(SearchPath.Path, variables, WebUtility.UrlEncode).Replace("+", "%20")).AbsoluteUri;
                var queryCollection = new List<KeyValuePair<string, string>>();
                var method = RequestType.GET;

                if (string.Equals(SearchPath.Method, "post", StringComparison.OrdinalIgnoreCase))
                {
                    method = RequestType.POST;
                }

                var InputsList = new List<Dictionary<string, string>>();
                if (SearchPath.Inheritinputs)
                    InputsList.Add(Search.Inputs);
                InputsList.Add(SearchPath.Inputs);

                foreach (var Inputs in InputsList)
                {
                    if (Inputs != null)
                    {
                        foreach (var Input in Inputs)
                        {
                            if (Input.Key == "$raw")
                            {
                                var rawStr = applyGoTemplateText(Input.Value, variables, WebUtility.UrlEncode);
                                foreach (var part in rawStr.Split('&'))
                                {
                                    var parts = part.Split(new[] { '=' }, 2);
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
                                queryCollection.Add(Input.Key, applyGoTemplateText(Input.Value, variables));
                        }
                    }
                }

                if (method == RequestType.GET)
                {
                    if (queryCollection.Count > 0)
                        searchUrl += "?" + queryCollection.GetQueryString(Encoding);
                }
                var searchUrlUri = new Uri(searchUrl);

                // send HTTP request
                var headers = ParseCustomHeaders(Search.Headers, variables);
                var response = await RequestWithCookiesAsync(
                    searchUrl, method: method, headers: headers, data: queryCollection);

                if (response.IsRedirect && SearchPath.Followredirect)
                    await FollowIfRedirect(response);

                var results = response.ContentString;

                if (SearchPath.Response != null && SearchPath.Response.Type.Equals("json"))
                {
                    if (response.Status != HttpStatusCode.OK)
                        throw new Exception($"Error Parsing Json Response: Status={response.Status} Response={results}");
                    if (response.Status == HttpStatusCode.OK && SearchPath.Response != null && SearchPath.Response.NoResultsMessage != null && ((SearchPath.Response.NoResultsMessage.Equals(results)) || (SearchPath.Response.NoResultsMessage == String.Empty && results == String.Empty)))
                        continue;
                    var parsedJson = JToken.Parse(results);
                    if (parsedJson == null)
                        throw new Exception("Error Parsing Json Response");

                    if (Search.Rows.Count != null)
                    {
                        var countVal = handleJsonSelector(Search.Rows.Count, parsedJson, variables);
                        if (int.TryParse(countVal, out var count))
                            if (count < 1)
                                continue;
                    }

                    var rowsObj = parsedJson.SelectToken(Search.Rows.Selector);
                    if (rowsObj == null)
                        throw new Exception("Error Parsing Rows Selector");

                    foreach (var Row in rowsObj.Value<JArray>())
                    {
                        var selObj = SearchPath.Response.Attribute != null ? Row.SelectToken(SearchPath.Response.Attribute).Value<JToken>() : Row;
                        var mulRows = SearchPath.Response.Multiple == true ? selObj.Values<JObject>() : new List<JObject> { selObj.Value<JObject>() };

                        foreach (var mulRow in mulRows)
                        {
                            var release = new ReleaseInfo();

                            foreach (var Field in Search.Fields)
                            {
                                var FieldParts = Field.Key.Split('|');
                                var FieldName = FieldParts[0];
                                var FieldModifiers = new List<string>();
                                for (var i = 1; i < FieldParts.Length; i++)
                                    FieldModifiers.Add(FieldParts[i]);

                                string value = null;
                                var variablesKey = ".Result." + FieldName;
                                var isOptional = OptionalFields.Contains(Field.Key) || FieldModifiers.Contains("optional") || Field.Value.Optional;
                                try
                                {
                                    var parentObj = mulRow;
                                    if (Field.Value.Selector != null && Field.Value.Selector.StartsWith(".."))
                                        parentObj = Row.Value<JObject>();

                                    value = handleJsonSelector(Field.Value, parentObj, variables, !isOptional);
                                    if (isOptional && string.IsNullOrWhiteSpace(value))
                                    {
                                        variables[variablesKey] = null;
                                        continue;
                                    }

                                    variables[variablesKey] = ParseFields(value, FieldName, release, FieldModifiers, searchUrlUri);
                                }
                                catch (Exception ex)
                                {
                                    if (!variables.ContainsKey(variablesKey))
                                        variables[variablesKey] = null;
                                    if (isOptional)
                                    {
                                        variables[variablesKey] = null;
                                        continue;
                                    }
                                    throw new Exception(string.Format("Error while parsing field={0}, selector={1}, value={2}: {3}", Field.Key, Field.Value.Selector, (value == null ? "<null>" : value), ex.Message));
                                }

                                var Filters = Definition.Search.Rows.Filters;
                                var SkipRelease = ParseRowFilters(Filters, release, query, variables, Row);

                                if (SkipRelease)
                                    continue;
                            }

                            releases.Add(release);
                        }
                    }
                }
                else
                {
                    try
                    {
                        IHtmlCollection<IElement> rowsDom;

                        if (SearchPath.Response != null && SearchPath.Response.Type.Equals("xml"))
                        {
                            var SearchResultParser = new XmlParser();
                            var SearchResultDocument = SearchResultParser.ParseDocument(results);

                            if (Search.Preprocessingfilters != null)
                            {
                                results = applyFilters(results, Search.Preprocessingfilters, variables);
                                SearchResultDocument = SearchResultParser.ParseDocument(results);
                                logger.Debug(string.Format("CardigannIndexer ({0}): result after preprocessingfilters: {1}", Definition.Id, results));
                            }

                            var rowsSelector = applyGoTemplateText(Search.Rows.Selector, variables);
                            rowsDom = SearchResultDocument.QuerySelectorAll(rowsSelector);
                        }
                        else
                        {
                            var SearchResultParser = new HtmlParser();
                            var SearchResultDocument = SearchResultParser.ParseDocument(results);

                            // check if we need to login again
                            var loginNeeded = CheckIfLoginIsNeeded(response, SearchResultDocument);
                            if (loginNeeded)
                            {
                                logger.Info(string.Format("CardigannIndexer ({0}): Relogin required", Id));
                                var LoginResult = await DoLogin();
                                if (!LoginResult)
                                    throw new Exception(string.Format("Relogin failed"));
                                await TestLogin();
                                response = await RequestWithCookiesAsync(searchUrl, method: method, data: queryCollection);
                                if (response.IsRedirect && SearchPath.Followredirect)
                                    await FollowIfRedirect(response);

                                results = response.ContentString;
                                SearchResultDocument = SearchResultParser.ParseDocument(results);
                            }

                            checkForError(response, Definition.Search.Error);

                            if (Search.Preprocessingfilters != null)
                            {
                                results = applyFilters(results, Search.Preprocessingfilters, variables);
                                SearchResultDocument = SearchResultParser.ParseDocument(results);
                                logger.Debug(string.Format("CardigannIndexer ({0}): result after preprocessingfilters: {1}", Id, results));
                            }

                            var rowsSelector = applyGoTemplateText(Search.Rows.Selector, variables);
                            rowsDom = SearchResultDocument.QuerySelectorAll(rowsSelector);

                        }

                        var Rows = new List<IElement>();
                        foreach (var RowDom in rowsDom)
                        {
                            Rows.Add(RowDom);
                        }

                        // merge following rows for After selector
                        var After = Definition.Search.Rows.After;
                        if (After > 0)
                        {
                            for (var i = 0; i < Rows.Count; i += 1)
                            {
                                var CurrentRow = Rows[i];
                                for (var j = 0; j < After; j += 1)
                                {
                                    var MergeRowIndex = i + j + 1;
                                    var MergeRow = Rows[MergeRowIndex];
                                    var MergeNodes = new List<INode>();
                                    foreach (var node in MergeRow.ChildNodes)
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

                                // Parse fields
                                foreach (var Field in Search.Fields)
                                {
                                    var FieldParts = Field.Key.Split('|');
                                    var FieldName = FieldParts[0];
                                    var FieldModifiers = new List<string>();
                                    for (var i = 1; i < FieldParts.Length; i++)
                                        FieldModifiers.Add(FieldParts[i]);

                                    string value = null;
                                    var variablesKey = ".Result." + FieldName;
                                    var isOptional = OptionalFields.Contains(Field.Key) || FieldModifiers.Contains("optional") || Field.Value.Optional;
                                    try
                                    {
                                        value = handleSelector(Field.Value, Row, variables, !isOptional);
                                        if (isOptional && string.IsNullOrWhiteSpace(value))
                                        {
                                            variables[variablesKey] = null;
                                            continue;
                                        }

                                        variables[variablesKey] = ParseFields(value, FieldName, release, FieldModifiers, searchUrlUri);
                                    }
                                    catch (Exception ex)
                                    {
                                        if (!variables.ContainsKey(variablesKey))
                                            variables[variablesKey] = null;
                                        if (isOptional)
                                        {
                                            variables[variablesKey] = null;
                                            continue;
                                        }
                                        throw new Exception(string.Format("Error while parsing field={0}, selector={1}, value={2}: {3}", Field.Key, Field.Value.Selector, (value == null ? "<null>" : value), ex.Message));
                                    }
                                }

                                var Filters = Definition.Search.Rows.Filters;
                                var SkipRelease = ParseRowFilters(Filters, release, query, variables, Row);

                                if (SkipRelease)
                                    continue;

                                // if DateHeaders is set go through the previous rows and look for the header selector
                                var DateHeaders = Definition.Search.Rows.Dateheaders;
                                if (release.PublishDate == DateTime.MinValue && DateHeaders != null)
                                {
                                    var PrevRow = Row.PreviousElementSibling;
                                    string value = null;
                                    if (PrevRow == null) // continue with parent
                                    {
                                        var Parent = Row.ParentElement;
                                        if (Parent != null)
                                            PrevRow = Parent.PreviousElementSibling;
                                    }
                                    while (PrevRow != null)
                                    {
                                        var CurRow = PrevRow;
                                        logger.Debug(PrevRow.OuterHtml);
                                        try
                                        {
                                            value = handleSelector(DateHeaders, CurRow);
                                            break;
                                        }
                                        catch (Exception)
                                        {
                                            // do nothing
                                        }
                                        PrevRow = CurRow.PreviousElementSibling;
                                        if (PrevRow == null) // continue with parent
                                        {
                                            var Parent = CurRow.ParentElement;
                                            if (Parent != null)
                                                PrevRow = Parent.PreviousElementSibling;
                                        }
                                    }

                                    if (value == null && DateHeaders.Optional == false)
                                        throw new Exception(string.Format("No date header row found for {0}", release.ToString()));
                                    if (value != null)
                                        release.PublishDate = DateTimeUtil.FromUnknown(value);
                                }

                                releases.Add(release);
                            }
                            catch (Exception ex)
                            {
                                logger.Error(string.Format("CardigannIndexer ({0}): Error while parsing row '{1}':\n\n{2}", Id, Row.ToHtmlPretty(), ex));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        OnParseError(results, ex);
                    }
                }
            }
            if (query.Limit > 0)
                releases = releases.Take(query.Limit).ToList();
            return releases;
        }

        protected async Task<WebResult> handleRequest(requestBlock request, Dictionary<string, object> variables = null, string referer = null)
        {
            var requestLinkStr = resolvePath(applyGoTemplateText(request.Path, variables)).ToString();
            logger.Debug($"CardigannIndexer ({Id}): handleRequest() requestLinkStr= {requestLinkStr}");

            Dictionary<string, string> pairs = null;
            var queryCollection = new NameValueCollection();

            var method = RequestType.GET;
            if (string.Equals(request.Method, "post", StringComparison.OrdinalIgnoreCase))
            {
                method = RequestType.POST;
                pairs = new Dictionary<string, string>();
            }

            if (request.Inputs != null)
            {
                foreach (var Input in request.Inputs)
                {
                    var value = applyGoTemplateText(Input.Value, variables);
                    if (method == RequestType.GET)
                        queryCollection.Add(Input.Key, value);
                    else if (method == RequestType.POST)
                        pairs.Add(Input.Key, value);
                }
            }

            if (queryCollection.Count > 0)
            {
                if (!requestLinkStr.Contains("?"))
                    requestLinkStr += "?";
                requestLinkStr += queryCollection.GetQueryString(Encoding, separator: request.Queryseparator);
            }

            var response = await RequestWithCookiesAndRetryAsync(requestLinkStr, null, method, referer, pairs);
            logger.Debug($"CardigannIndexer ({Id}): handleRequest() remote server returned {response.Status.ToString()}" + (response.IsRedirect ? " => " + response.RedirectingTo : ""));
            return response;
        }

        protected async Task<WebResult> HandleRedirectableRequestAsync(string url, Dictionary<string, string> headers = null, int maxRedirects = 5)
        {
            var response = await RequestWithCookiesAsync(url, headers: headers);
            for (var i = 0; i < maxRedirects; i++)
            {
                if (response.IsRedirect)
                    response = await RequestWithCookiesAsync(response.RedirectingTo, headers: headers);
                else
                    break;
            }
            return response;
        }

        protected IDictionary<string, object> AddTemplateVariablesFromUri(IDictionary<string, object> variables, Uri uri, string prefix = "")
        {
            variables[prefix + ".AbsoluteUri"] = uri.AbsoluteUri;
            variables[prefix + ".AbsolutePath"] = uri.AbsolutePath;
            variables[prefix + ".Scheme"] = uri.Scheme;
            variables[prefix + ".Host"] = uri.Host;
            variables[prefix + ".Port"] = uri.Port.ToString();
            variables[prefix + ".PathAndQuery"] = uri.PathAndQuery;
            variables[prefix + ".Query"] = uri.Query;
            var queryString = QueryHelpers.ParseQuery(uri.Query);
            foreach (var key in queryString.Keys)
            {
                //If we have supplied the same query string multiple time, just take the first.
                variables[prefix + ".Query." + key] = queryString[key].First();
            }
            return variables;
        }

        protected string MatchSelector(WebResult response, selectorField selector, Dictionary<string, object> variables, bool debugMatch = false)
        {
            var selectorText = applyGoTemplateText(selector.Selector, variables);
            var parser = new HtmlParser();

            var results = response.ContentString;
            var resultDocument = parser.ParseDocument(results);

            var element = resultDocument.QuerySelector(selectorText);
            if (element == null)
            {
                logger.Debug(
                    $"CardigannIndexer ({Id}): Selector {selectorText} could not match any elements.");
                return null;
            }

            if (debugMatch)
                logger.Debug(
                    $"CardigannIndexer ({Id}): Download selector {selector} matched:{element.ToHtmlPretty()}");

            string val;
            if (selector.Attribute != null)
            {
                val = element.GetAttribute(selector.Attribute);
                if (val == null)
                    throw new Exception(
                        $"Attribute \"{selector.Attribute}\" is not set for element {element.ToHtmlPretty()}");
            }
            else
                val = element.TextContent;

            val = applyFilters(val, selector.Filters, variables);
            return val;
        }

        public override async Task<byte[]> Download(Uri link)
        {
            var method = RequestType.GET;
            var headers = new Dictionary<string, string>();
            if (Definition.Download != null)
            {
                var Download = Definition.Download;
                var variables = GetBaseTemplateVariables();
                AddTemplateVariablesFromUri(variables, link, ".DownloadUri");

                headers = ParseCustomHeaders(Definition.Search?.Headers, variables);
                WebResult response = null;

                var beforeBlock = Download.Before;
                if (beforeBlock != null)
                {
                    if (beforeBlock.Pathselector != null)
                    {
                        response = await HandleRedirectableRequestAsync(link.ToString(), headers);
                        beforeBlock.Path = MatchSelector(response, beforeBlock.Pathselector, variables);
                    }

                    response = await handleRequest(beforeBlock, variables, link.ToString());
                }

                if (Download.Method == "post")
                    method = RequestType.POST;
                if (Download.Infohash != null)
                {
                    try
                    {
                        headers = ParseCustomHeaders(Definition.Search?.Headers, variables);

                        if (!Download.Infohash.Usebeforeresponse || Download.Before == null || response == null)
                            response = await HandleRedirectableRequestAsync(link.ToString(), headers);

                        var hash = MatchSelector(response, Download.Infohash.Hash, variables);
                        if (hash == null)
                            throw new Exception($"InfoHash selectors didn't match");

                        var title = MatchSelector(response, Download.Infohash.Title, variables);
                        if (title == null)
                            throw new Exception($"InfoHash selectors didn't match");

                        var magnet = MagnetUtil.InfoHashToPublicMagnet(hash, title);
                        var torrentLink = resolvePath(magnet.AbsoluteUri, link);
                        return await base.Download(torrentLink, method, torrentLink.ToString());
                    }
                    catch (Exception e)
                    {
                        logger.Error(e,
                            $"CardigannIndexer ({Id}): An exception occurred while trying Infohash block with hashSelector {Download.Infohash.Hash.Selector} and titleSelector {Download.Infohash.Title.Selector}"
                            );
                    }

                }
                else if (Download.Selectors != null)
                {
                    headers = ParseCustomHeaders(Definition.Search?.Headers, variables);

                    foreach (var selector in Download.Selectors)
                    {
                        var querySelector = applyGoTemplateText(selector.Selector, variables);
                        try
                        {

                            if (!selector.Usebeforeresponse || Download.Before == null || response == null)
                                response = await HandleRedirectableRequestAsync(link.ToString(), headers);
                            var href = MatchSelector(response, selector, variables, debugMatch: true);
                            if (href == null)
                                continue;

                            var torrentLink = resolvePath(href, link);
                            if (torrentLink.Scheme != "magnet" && Definition.Testlinktorrent)
                            {
                                // Test link
                                response = await HandleRedirectableRequestAsync(torrentLink.ToString(), headers);
                                var content = response.ContentBytes;
                                if (content.Length >= 1 && content[0] != 'd')
                                {
                                    logger.Debug(
                                        $"CardigannIndexer ({Id}): Download selector {querySelector}'s torrent file is invalid, retrying with next available selector");
                                    continue;
                                }
                            }

                            link = torrentLink;
                            return await base.Download(link, method, link.ToString(), headers);
                        }
                        catch (Exception e)
                        {
                            logger.Error(e,
                                $"CardigannIndexer ({Id}): An exception occurred while trying selector {querySelector}, retrying with next available selector"
                                );
                        }
                    }

                    logger.Error(
                        $"CardigannIndexer ({Id}): Download selectors didn't match:\n{response.ContentString}");
                    throw new Exception($"Download selectors didn't match");
                }
            }
            headers = ParseCustomHeaders(Definition.Search?.Headers, GetBaseTemplateVariables());
            return await base.Download(link, method, link.ToString(), headers);
        }

        private Dictionary<string, string> ParseCustomHeaders(Dictionary<string, List<string>> customHeaders,
                                                              Dictionary<string, object> variables)
        {
            if (customHeaders == null)
                return null;

            // FIXME: fix jackett header handling (allow it to specifiy the same header multipe times)
            var headers = new Dictionary<string, string>();
            foreach (var header in customHeaders)
                headers.Add(header.Key, applyGoTemplateText(header.Value[0], variables));

            return headers;
        }

        private string ParseFields(string value, string FieldName, ReleaseInfo release, List<string> FieldModifiers, Uri searchUrlUri)
        {
            switch (FieldName)
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
                    break;
                case "infohash":
                    release.InfoHash = value;
                    break;
                case "details":
                    var url = resolvePath(value, searchUrlUri);
                    release.Details = url;
                    value = url.ToString();
                    break;
                case "title":
                    if (FieldModifiers.Contains("append"))
                        release.Title += value;
                    else
                        release.Title = value;
                    value = release.Title;
                    break;
                case "description":
                    if (FieldModifiers.Contains("append"))
                        release.Description += value;
                    else
                        release.Description = value;
                    value = release.Description;
                    break;
                case "category":
                    var cats = MapTrackerCatToNewznab(value);
                    if (cats.Any())
                    {
                        if (release.Category == null || FieldModifiers.Contains("noappend"))
                            release.Category = cats;
                        else
                            release.Category = release.Category.Union(cats).ToList();
                    }
                    value = release.Category.ToString();
                    break;
                case "categorydesc":
                    var catsDesc = MapTrackerCatDescToNewznab(value);
                    if (catsDesc.Any())
                    {
                        if (release.Category == null || FieldModifiers.Contains("noappend"))
                            release.Category = catsDesc;
                        else
                            release.Category = release.Category.Union(catsDesc).ToList();
                    }
                    value = release.Category.ToString();
                    break;
                case "size":
                    release.Size = ReleaseInfo.GetBytes(value);
                    value = release.Size.ToString();
                    break;
                case "leechers":
                    var leechers = ReleaseInfo.GetBytes(value);
                    leechers = leechers < 5000000L ? leechers : 0; // to fix #6558
                    if (release.Peers == null)
                        release.Peers = leechers;
                    else
                        release.Peers += leechers;
                    value = leechers.ToString();
                    break;
                case "seeders":
                    release.Seeders = ReleaseInfo.GetBytes(value);
                    release.Seeders = release.Seeders < 5000000L ? release.Seeders : 0; // to fix #6558
                    if (release.Peers == null)
                        release.Peers = release.Seeders;
                    else
                        release.Peers += release.Seeders;
                    value = release.Seeders.ToString();
                    break;
                case "date":
                    release.PublishDate = DateTimeUtil.FromUnknown(value);
                    value = release.PublishDate.ToString(DateTimeUtil.Rfc1123ZPattern);
                    break;
                case "files":
                    release.Files = ReleaseInfo.GetBytes(value);
                    value = release.Files.ToString();
                    break;
                case "grabs":
                    release.Grabs = ReleaseInfo.GetBytes(value);
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
                case "imdbid":
                    release.Imdb = ParseUtil.GetLongFromString(value);
                    value = release.Imdb.ToString();
                    break;
                case "tmdbid":
                    var TmdbIDRegEx = new Regex(@"(\d+)", RegexOptions.Compiled);
                    var TmdbIDMatch = TmdbIDRegEx.Match(value);
                    var TmdbID = TmdbIDMatch.Groups[1].Value;
                    release.TMDb = ParseUtil.CoerceLong(TmdbID);
                    value = release.TMDb.ToString();
                    break;
                case "rageid":
                    var RageIDRegEx = new Regex(@"(\d+)", RegexOptions.Compiled);
                    var RageIDMatch = RageIDRegEx.Match(value);
                    var RageID = RageIDMatch.Groups[1].Value;
                    release.RageID = ParseUtil.CoerceLong(RageID);
                    value = release.RageID.ToString();
                    break;
                case "tvdbid":
                    var TVDBIdRegEx = new Regex(@"(\d+)", RegexOptions.Compiled);
                    var TVDBIdMatch = TVDBIdRegEx.Match(value);
                    var TVDBId = TVDBIdMatch.Groups[1].Value;
                    release.TVDBId = ParseUtil.CoerceLong(TVDBId);
                    value = release.TVDBId.ToString();
                    break;
                case "author":
                    release.Author = value;
                    break;
                case "booktitle":
                    release.BookTitle = value;
                    break;
                case "poster":
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        var poster = resolvePath(value, searchUrlUri);
                        release.Poster = poster;
                    }
                    value = release.Poster.ToString();
                    break;
                default:
                    break;
            }

            return value;
        }

        private bool ParseRowFilters(List<filterBlock> Filters, ReleaseInfo release, TorznabQuery query, Dictionary<string, object> variables, object Row)
        {
            var SkipRelease = false;
            if (Filters != null)
            {
                foreach (var Filter in Filters)
                {
                    switch (Filter.Name)
                    {
                        case "andmatch":
                            var CharacterLimit = -1;
                            if (Filter.Args != null)
                                CharacterLimit = int.Parse(Filter.Args);

                            if (query.ImdbID != null && TorznabCaps.MovieSearchImdbAvailable)
                                break; // skip andmatch filter for imdb searches

                            if (query.TmdbID != null && TorznabCaps.MovieSearchTmdbAvailable)
                                break; // skip andmatch filter for tmdb searches

                            if (query.TvdbID != null && TorznabCaps.TvSearchTvdbAvailable)
                                break; // skip andmatch filter for tvdb searches

                            var queryKeywords = variables[".Keywords"] as string;

                            if (!query.MatchQueryStringAND(release.Title, CharacterLimit, queryKeywords))
                            {
                                logger.Debug(string.Format("CardigannIndexer ({0}): skipping {1} (andmatch filter)", Id, release.Title));
                                SkipRelease = true;
                            }
                            break;
                        case "strdump":
                            // for debugging
                            logger.Debug(string.Format("CardigannIndexer ({0}): row strdump: {1}", Id, Row.ToString()));
                            break;
                        default:
                            logger.Error(string.Format("CardigannIndexer ({0}): Unsupported rows filter: {1}", Id, Filter.Name));
                            break;
                    }
                }
            }
            return SkipRelease;
        }
    }
}

