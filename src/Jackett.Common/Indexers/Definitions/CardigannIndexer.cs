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
using AngleSharp.Html;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.Text;
using AngleSharp.Xml.Parser;
using Jackett.Common.Extensions;
using Jackett.Common.Helpers;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using static Jackett.Common.Models.IndexerConfig.ConfigurationData;

namespace Jackett.Common.Indexers.Definitions
{
    public class CardigannIndexer : BaseWebIndexer
    {
        public override string Id => Definition.Id;
        public override string[] Replaces => Definition.Replaces.ToArray();
        public override string Name => Definition.Name;
        public override string Description => Definition.Description;

        protected IndexerDefinition Definition;
        protected WebResult landingResult;
        protected IHtmlDocument landingResultDocument;

        protected List<string> DefaultCategories = new List<string>();

        private new ConfigurationData configData
        {
            get => base.configData;
            set => base.configData = value;
        }

        protected readonly string[] OptionalFields = { "imdb", "imdbid", "tmdbid", "rageid", "tvdbid", "tvmazeid", "traktid", "doubanid", "poster", "genre", "description" };

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

        // Matches CSS selectors for the JSON parser
        private static readonly Regex _JsonSelectorRegex = new Regex(@"\:(?<filter>.+?)\((?<key>.+?)\)(?=:|\z)", RegexOptions.Compiled);

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

            if (Definition.Login is { Method: null })
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
            TorznabCaps.SupportsRawSearch = Definition.Caps.Allowrawsearch;

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

                            if (Setting.Default is "true")
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
                        case "info_category_8000":
                            item = new DisplayInfoConfigurationItem($"About {Definition.Name} Categories", $"{Definition.Name} does not return categories in its search results.</br>To add to your Apps' Torznab indexer, replace all categories with 8000(Other).");
                            break;
                        case "info_cookie":
                            item = new DisplayInfoConfigurationItem("How to get the Cookie", "<ol><li>Login to this tracker with your browser</li><li>If present in the login page, ensure you have the <b>Remember me</b> ticked and the <b>Log Me Out if IP Changes</b> unticked when you login</li><li>Navigate to the web site's torrent search page to view the list of available torrents for download</li><li>Open the <b>DevTools</b> panel by pressing <b>F12</b></li><li>Select the <b>Network</b> tab</li><li>Click on the <b>Doc</b> button (Chrome Browser) or <b>HTML</b> button (FireFox)</li><li>Refresh the page by pressing <b>F5</b></li><li>Click on the first row entry</li><li>Select the <b>Headers</b> tab on the Right panel</li><li>Find <b>'cookie:'</b> in the <b>Request Headers</b> section</li><li><b>Select</b> and <b>Copy</b> the whole cookie string <i>(everything after 'cookie: ')</i> and <b>Paste</b> here.</li></ol>");
                            break;
                        case "info_flaresolverr":
                            item = new DisplayInfoConfigurationItem("FlareSolverr", "This site may use Cloudflare DDoS Protection, therefore Jackett requires <a href=\"https://github.com/Jackett/Jackett#configuring-flaresolverr\" target=\"_blank\">FlareSolverr</a> to access it.");
                            break;
                        case "info_useragent":
                            item = new DisplayInfoConfigurationItem("How to get the User-Agent", "<ol><li>From the same place you fetched the cookie,</li><li>Find <b>'user-agent:'</b> in the <b>Request Headers</b> section</li><li><b>Select</b> and <b>Copy</b> the whole user-agent string <i>(everything after 'user-agent: ')</i> and <b>Paste</b> here.</li></ol>");
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
                    TorznabCaps.Categories.AddCategoryMapping(Category.Key, cat);
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
                    TorznabCaps.Categories.AddCategoryMapping(Categorymapping.id, TorznabCat, Categorymapping.desc);
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
                [".Today.Year"] = DateTime.Today.Month > 1 ? DateTime.Today.Year.ToString() : (DateTime.Today.Year - 1).ToString()
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
            if (string.IsNullOrWhiteSpace(template) || !template.Contains("{{"))
                return template;

            variables ??= GetBaseTemplateVariables();

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
                var input = (string)variables[variable] ?? string.Empty;
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
            var RangeRegex = new Regex(@"{{\s*range\s*(((?<index>\$.+?),)((\s*(?<element>.+?)\s*(:=)\s*)))?(?<variable>.+?)\s*}}(?<prefix>.*?){{\.}}(?<postfix>.*?){{end}}");
            var RangeRegexMatches = RangeRegex.Match(template);

            while (RangeRegexMatches.Success)
            {
                var expanded = string.Empty;

                var all = RangeRegexMatches.Groups[0].Value;
                var index = RangeRegexMatches.Groups["index"].Value;
                var variable = RangeRegexMatches.Groups["variable"].Value;
                var prefix = RangeRegexMatches.Groups["prefix"].Value;
                var postfix = RangeRegexMatches.Groups["postfix"].Value;

                var arrayIndex = 0;
                var indexReplace = "{{" + index + "}}";

                foreach (var value in (ICollection<string>)variables[variable])
                {
                    var newvalue = value;
                    if (modifier != null)
                        newvalue = modifier(newvalue);
                    var indexValue = arrayIndex++;

                    if (index != null)
                    {
                        expanded += prefix.Replace(indexReplace, indexValue.ToString()) + newvalue + postfix.Replace(indexReplace, indexValue.ToString());
                    }
                    else
                    {
                        expanded += prefix + newvalue + postfix;
                    }
                }
                template = template.Replace(all, expanded);
                RangeRegexMatches = RangeRegexMatches.NextMatch();
            }

            // handle simple variables
            var variablesRegex = new Regex(@"{{\s*(\..+?)\s*}}");
            var variablesRegexMatches = variablesRegex.Match(template);

            while (variablesRegexMatches.Success)
            {
                var all = variablesRegexMatches.Groups[0].Value;
                var variable = variablesRegexMatches.Groups[1].Value;

                var value = (string)variables[variable];
                if (modifier != null)
                    value = modifier(value);

                template = template.Replace(all, value);
                variablesRegexMatches = variablesRegexMatches.NextMatch();
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
            using var ResultDocument = ResultParser.ParseDocument(loginResult.ContentString);
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

        protected async Task<bool> DoLogin(string cookies = null)
        {
            var Login = Definition.Login;

            if (Login == null)
                return true;

            var variables = GetBaseTemplateVariables();
            var headers = ParseCustomHeaders(Definition.Login?.Headers ?? Definition.Search?.Headers, variables);

            if (Login.Method == "post")
            {
                var pairs = new Dictionary<string, string>();

                if (Login.Inputs != null && Login.Inputs.Any())
                {
                    foreach (var input in Login.Inputs)
                    {
                        var value = applyGoTemplateText(input.Value);
                        pairs.Add(input.Key, value);
                    }
                }

                var loginUrl = resolvePath(applyGoTemplateText(Login.Path, variables)).ToString();

                configData.CookieHeader.Value = null;
                if (Login.Cookies != null)
                    configData.CookieHeader.Value = string.Join("; ", Login.Cookies);

                var loginResult = await RequestLoginAndFollowRedirect(loginUrl, pairs, null, true, null, SiteLink, true, headers);
                configData.CookieHeader.Value = loginResult.Cookies;

                checkForError(loginResult, Definition.Login.Error);
            }
            else if (Login.Method == "form")
            {
                var loginUrl = resolvePath(applyGoTemplateText(Login.Path, variables)).ToString();

                var queryCollection = new NameValueCollection();
                var pairs = new Dictionary<string, string>();

                var FormSelector = Login.Form ?? "form";

                // landingResultDocument might not be initiated if the login is caused by a re-login during a query
                if (landingResultDocument == null)
                {
                    var ConfigurationResult = await GetConfigurationForSetup(true, cookies);
                    if (ConfigurationResult == null) // got captcha
                    {
                        return false;
                    }
                }

                var form = landingResultDocument.QuerySelector(FormSelector);
                if (form == null)
                {
                    throw new ExceptionWithConfigData(string.Format("Login failed: No form found on {0} using form selector {1}", loginUrl, FormSelector), configData);
                }

                var inputs = form.QuerySelectorAll("input");
                if (inputs == null)
                {
                    throw new ExceptionWithConfigData(string.Format("Login failed: No inputs found on {0} using form selector {1}", loginUrl, FormSelector), configData);
                }

                var submitUrlstr = form.GetAttribute("action");
                if (Login.Submitpath != null)
                    submitUrlstr = Login.Submitpath;

                foreach (var input in inputs)
                {
                    var name = input.GetAttribute("name");

                    if (name == null || input.IsDisabled())
                    {
                        continue;
                    }

                    if (input is IHtmlInputElement element &&
                        element.Type.IsOneOf(InputTypeNames.Checkbox, InputTypeNames.Radio) &&
                        !input.IsChecked())
                    {
                        continue;
                    }

                    var value = input.GetAttribute("value") ?? "";

                    pairs[name] = value;
                }

                if (Login.Inputs != null && Login.Inputs.Any())
                {
                    foreach (var Input in Login.Inputs)
                    {
                        var value = applyGoTemplateText(Input.Value);
                        var input = Input.Key;

                        if (Login.Selectors)
                        {
                            var inputElement = landingResultDocument.QuerySelector(Input.Key);

                            if (inputElement == null)
                            {
                                throw new ExceptionWithConfigData($"Login failed: No input found using selector {Input.Key}", configData);
                            }

                            input = inputElement.GetAttribute("name");
                        }

                        pairs[input] = value;
                    }
                }

                // selector inputs
                if (Login.Selectorinputs != null && Login.Selectorinputs.Any())
                {
                    foreach (var selectorInput in Login.Selectorinputs)
                    {
                        try
                        {
                            var value = handleSelector(selectorInput.Value, landingResultDocument.FirstElementChild, required: !selectorInput.Value.Optional);

                            if (selectorInput.Value.Optional && value == null)
                            {
                                continue;
                            }

                            pairs[selectorInput.Key] = value;
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Error while parsing selector input={selectorInput.Key}, selector={selectorInput.Value.Selector}: {ex.Message}", ex);
                        }
                    }
                }

                // getselector inputs
                if (Login.Getselectorinputs != null && Login.Getselectorinputs.Any())
                {
                    foreach (var selectorInput in Login.Getselectorinputs)
                    {
                        try
                        {
                            var value = handleSelector(selectorInput.Value, landingResultDocument.FirstElementChild, required: !selectorInput.Value.Optional);

                            if (selectorInput.Value.Optional && value == null)
                            {
                                continue;
                            }

                            queryCollection[selectorInput.Key] = value;
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Error while parsing get selector input={selectorInput.Key}, selector={selectorInput.Value.Selector}: {ex.Message}", ex);
                        }
                    }
                }
                if (queryCollection.Count > 0)
                    submitUrlstr += "?" + queryCollection.GetQueryString();
                var submitUrl = resolvePath(submitUrlstr, new Uri(loginUrl));

                // automatically solve simpleCaptchas, if used
                var simpleCaptchaPresent = landingResultDocument.QuerySelector("script[src*=\"simpleCaptcha\"]");
                if (simpleCaptchaPresent != null)
                {
                    var captchaUrl = resolvePath("simpleCaptcha.php?numImages=1");
                    var simpleCaptchaResult = await RequestWithCookiesAsync(captchaUrl.ToString(), referer: loginUrl, headers: headers);
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
                    loginResult = await RequestLoginAndFollowRedirect(submitUrl.ToString(), pairs, configData.CookieHeader.Value, true, null, loginUrl, true, headers);

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

                if (Login.Inputs != null && Login.Inputs.Any())
                {
                    foreach (var input in Login.Inputs)
                    {
                        var value = applyGoTemplateText(input.Value);
                        queryCollection.Add(input.Key, value);
                    }
                }

                var loginUrl = resolvePath(applyGoTemplateText(Login.Path, variables) + "?" + queryCollection.GetQueryString()).ToString();
                configData.CookieHeader.Value = null;
                var loginResult = await RequestWithCookiesAsync(loginUrl, referer: SiteLink, headers: headers);
                configData.CookieHeader.Value = loginResult.Cookies;

                checkForError(loginResult, Login.Error);
            }
            else if (Login.Method == "oneurl")
            {
                var OneUrl = applyGoTemplateText(Definition.Login.Inputs["oneurl"]);
                var LoginUrl = resolvePath(applyGoTemplateText(Login.Path, variables) + OneUrl).ToString();
                configData.CookieHeader.Value = null;
                var loginResult = await RequestWithCookiesAsync(LoginUrl, referer: SiteLink, headers: headers);
                configData.CookieHeader.Value = loginResult.Cookies;

                checkForError(loginResult, Login.Error);
            }
            else
            {
                throw new NotImplementedException($"Login method {Login.Method} not implemented");
            }

            logger.Debug($"CardigannIndexer ({Id}): Cookies after login: {CookieHeader}");

            return true;
        }

        protected string GetRedirectDomainHint(string requestUrl, string redirectUrl)
        {
            if (redirectUrl.IsNullOrWhiteSpace() || !requestUrl.StartsWith(SiteLink) || redirectUrl.StartsWith(SiteLink))
            {
                return null;
            }

            var uri = new Uri(redirectUrl);
            return uri.Scheme + "://" + uri.Host + "/";
        }

        protected string GetRedirectDomainHint(WebResult result) => GetRedirectDomainHint(result.Request.Url, result.RedirectingTo);

        protected async Task<bool> TestLogin()
        {
            var Login = Definition.Login;

            if (Login == null || Login.Test == null)
            {
                return false;
            }

            // test if login was successful
            var loginTestUrl = resolvePath(Login.Test.Path).ToString();
            var headers = ParseCustomHeaders(Definition.Login?.Headers ?? Definition.Search?.Headers, GetBaseTemplateVariables());
            var testResult = await RequestWithCookiesAsync(loginTestUrl, headers: headers);

            // Follow the redirect on login if the domain doesn't change
            if (testResult.IsRedirect && GetRedirectDomainHint(testResult) == null)
            {
                logger.Warn("Redirected to {0} from test login request", testResult.RedirectingTo);

                testResult = await FollowIfRedirect(testResult, loginTestUrl, overrideCookies: testResult.Cookies, accumulateCookies: true, maxRedirects: 1);
            }

            if (testResult.IsRedirect)
            {
                var errormessage = $"Login Failed, got redirected to: {testResult.RedirectingTo}";
                var domainHint = GetRedirectDomainHint(testResult);

                if (domainHint != null)
                {
                    errormessage += " Try changing the indexer URL to " + domainHint + ".";

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

            if (Login.Test.Selector != null)
            {
                var testResultParser = new HtmlParser();
                using var testResultDocument = testResultParser.ParseDocument(testResult.ContentString);

                var selection = testResultDocument.QuerySelectorAll(Login.Test.Selector);

                if (selection.Length == 0)
                {
                    throw new ExceptionWithConfigData(string.Format("Login failed: Selector \"{0}\" didn't match", Login.Test.Selector), configData);
                }
            }

            return true;
        }

        private bool CheckIfLoginIsNeeded(WebResult response)
        {
            if (response.IsRedirect)
            {
                var domainHint = GetRedirectDomainHint(response);

                if (domainHint != null)
                {
                    var errorMessage = "Got redirected to another domain. Try changing the indexer URL to " + domainHint + ".";

                    if (Definition.Followredirect)
                    {
                        configData.SiteLink.Value = domainHint;
                        SiteLink = configData.SiteLink.Value;
                        SaveConfig();
                        errorMessage += " Updated site link, please try again.";
                    }

                    throw new ExceptionWithConfigData(errorMessage, configData);
                }

                logger.Error($"Redirected to: {response.RedirectingTo}");

                return true;
            }

            if (Definition.Login == null || Definition.Login.Test == null)
            {
                return false;
            }

            var contentType = response.Headers.TryGetValue("Content-Type", out var header) ? header.FirstOrDefault() : null;

            if (Definition.Login.Test.Selector != null && (contentType?.Contains("text/html") ?? true))
            {
                var parser = new HtmlParser();
                using var document = parser.ParseDocument(response.ContentString);

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

        public async Task<ConfigurationData> GetConfigurationForSetup(bool automaticlogin, string cookies = null)
        {
            var Login = Definition.Login;

            if (Login == null || Login.Method != "form")
                return configData;

            var variables = GetBaseTemplateVariables();
            var headers = ParseCustomHeaders(Definition.Login?.Headers ?? Definition.Search?.Headers, variables);

            var loginUrl = resolvePath(applyGoTemplateText(Login.Path, variables));

            configData.CookieHeader.Value = null;
            if (Login.Cookies != null)
                configData.CookieHeader.Value = string.Join("; ", Login.Cookies);

            landingResult = await RequestWithCookiesAsync(loginUrl.AbsoluteUri, cookies, referer: SiteLink, headers: headers);

            // Some sites have a temporary redirect before the login page, we need to process it.
            if (Definition.Followredirect)
            {
                landingResult = await FollowIfRedirect(landingResult, loginUrl.AbsoluteUri, overrideCookies: landingResult.Cookies, accumulateCookies: true);
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

                        var CaptchaUrl = resolvePath(captchaElement.GetAttribute("src"), loginUrl);
                        var captchaImageData = await RequestWithCookiesAsync(
                            CaptchaUrl.ToString(), landingResult.Cookies, referer: loginUrl.AbsoluteUri, headers: headers);
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
                            var datetime = DateTimeUtil.ParseDateTimeGoLang(Data, layout);
                            Data = datetime.ToString(DateTimeUtil.Rfc1123ZPattern, CultureInfo.InvariantCulture);
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
                    case "htmldecode":
                        Data = WebUtility.HtmlDecode(Data);
                        break;
                    case "htmlencode":
                        Data = WebUtility.HtmlEncode(Data);
                        break;
                    case "timeago":
                    case "reltime":
                        Data = DateTimeUtil.FromTimeAgo(Data).ToString(DateTimeUtil.Rfc1123ZPattern, CultureInfo.InvariantCulture);
                        break;
                    case "fuzzytime":
                        Data = DateTimeUtil.FromUnknown(Data).ToString(DateTimeUtil.Rfc1123ZPattern, CultureInfo.InvariantCulture);
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
                        var hexData = string.Join("", Data.Select(c => c + "(" + ((int)c).ToString("X2") + ")"));
                        logger.Debug($"CardigannIndexer ({Id}): strdump: {hexData}");
                        break;
                    case "strdump":
                        // for debugging
                        var debugData = Data.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\xA0", "\\xA0");
                        var strTag = (string)Filter.Args;
                        strTag = strTag != null ? $"({strTag}):" : ":";
                        logger.Debug($"CardigannIndexer ({Id}): strdump{strTag} {debugData}");
                        break;
                    case "validate":
                        char[] delimiters = { ',', ' ', '/', ')', '(', '.', ';', '[', ']', '"', '|', ':' };
                        var args = (string)Filter.Args;
                        var argsList = args.ToLower().Split(delimiters, System.StringSplitOptions.RemoveEmptyEntries);
                        var validList = argsList.ToList();
                        var validIntersect = validList.Intersect(Data.ToLower().Split(delimiters, System.StringSplitOptions.RemoveEmptyEntries)).ToList();
                        Data = string.Join(",", validIntersect);
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

                selection = Dom.Matches(selector_Selector) ? Dom : QuerySelector(Dom, selector_Selector);

                if (selection == null)
                {
                    if (required)
                    {
                        throw new Exception($"Selector \"{selector_Selector}\" didn't match {Dom.ToHtmlPretty()}");
                    }

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
                foreach (var switchCase in Selector.Case)
                {
                    if (selection.Matches(switchCase.Key) || QuerySelector(selection, switchCase.Key) != null)
                    {
                        value = applyGoTemplateText(switchCase.Value, variables);
                        break;
                    }
                }

                if (value == null)
                {
                    if (required)
                    {
                        throw new Exception($"None of the case selectors \"{string.Join(",", Selector.Case)}\" matched {selection.ToHtmlPretty()}");
                    }

                    return null;
                }
            }
            else if (Selector.Attribute != null)
            {
                value = selection.GetAttribute(Selector.Attribute);
                if (value == null)
                {
                    if (required)
                    {
                        throw new Exception($"Attribute \"{Selector.Attribute}\" is not set for element {selection.ToHtmlPretty()}");
                    }

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
                var selectorSelector = applyGoTemplateText(Selector.Selector.TrimStart('.'), variables);
                selectorSelector = JsonParseFieldSelector(parentObj, selectorSelector);

                JToken selection = null;

                if (selectorSelector != null)
                {
                    selection = parentObj.SelectToken(selectorSelector);
                }

                if (selection == null)
                {
                    if (required)
                    {
                        throw new Exception($"Selector \"{selectorSelector}\" didn't match {parentObj}");
                    }

                    return null;
                }

                if (selection.Type is JTokenType.Array)
                {
                    // turn this json array into a comma delimited string
                    var valueArray = selection.Value<JArray>();
                    value = String.Join(",", valueArray);
                }
                else
                {
                    value = selection.Value<string>();
                }
            }

            if (Selector.Case != null)
            {
                foreach (var switchCase in Selector.Case)
                {
                    if ((value != null && value.Equals(switchCase.Key)) || switchCase.Key.Equals("*"))
                    {
                        value = applyGoTemplateText(switchCase.Value, variables);
                        break;
                    }
                }

                if (value == null)
                {
                    if (required)
                    {
                        throw new Exception($"None of the case selectors \"{string.Join(",", Selector.Case)}\" matched {parentObj}");
                    }

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
            variables[".Query.TVMazeID"] = query.TvmazeID?.ToString() ?? null;
            variables[".Query.TraktID"] = query.TraktID?.ToString() ?? null;
            variables[".Query.DoubanID"] = query.DoubanID?.ToString() ?? null;
            variables[".Query.Album"] = query.Album;
            variables[".Query.Artist"] = query.Artist;
            variables[".Query.Label"] = query.Label;
            variables[".Query.Track"] = query.Track;
            variables[".Query.Genre"] = query.Genre;
            variables[".Query.Episode"] = query.GetEpisodeSearchString();
            variables[".Query.Author"] = query.Author;
            variables[".Query.Title"] = query.Title;
            variables[".Query.Publisher"] = query.Publisher;
            // boolean queries
            variables[".Query.IsBookSearch"] = query.IsBookSearch ? "True" : null;
            variables[".Query.IsDoubanQuery"] = query.IsDoubanQuery ? "True" : null;
            variables[".Query.IsGenreQuery"] = query.IsGenreQuery ? "True" : null;
            variables[".Query.IsIdSearch"] = query.IsIdSearch ? "True" : null;
            variables[".Query.IsImdbQuery"] = query.IsImdbQuery ? "True" : null;
            variables[".Query.IsMovieSearch"] = query.IsMovieSearch ? "True" : null;
            variables[".Query.IsMusicSearch"] = query.IsMusicSearch ? "True" : null;
            variables[".Query.IsRssSearch"] = query.IsRssSearch ? "True" : null;
            variables[".Query.IsSearch"] = query.IsSearch ? "True" : null;
            variables[".Query.IsTVRageQuery"] = query.IsTVRageQuery ? "True" : null;
            variables[".Query.IsTVSearch"] = query.IsTVSearch ? "True" : null;
            variables[".Query.IsTmdbQuery"] = query.IsTmdbQuery ? "True" : null;
            variables[".Query.IsTraktQuery"] = query.IsTraktQuery ? "True" : null;
            variables[".Query.IsTvdbQuery"] = query.IsTvdbQuery ? "True" : null;
            variables[".Query.IsTvmazeQuery"] = query.IsTvmazeQuery ? "True" : null;

            var mappedCategories = MapTorznabCapsToTrackers(query);
            if (mappedCategories.Count == 0)
            {
                mappedCategories = DefaultCategories;
            }

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
            variables[".Keywords"] = applyFilters((string)variables[".Query.Keywords"], Search.Keywordsfilters, variables);

            // TODO: prepare queries first and then send them parallel
            var SearchPaths = Search.Paths;
            foreach (var SearchPath in SearchPaths)
            {
                variables[".Categories"] = mappedCategories;

                // skip path if categories don't match
                if (SearchPath.Categories.Count > 0)
                {
                    var hasIntersect = mappedCategories.Intersect(SearchPath.Categories).Any();

                    if (SearchPath.Categories[0] == "!")
                    {
                        hasIntersect = !hasIntersect;
                    }

                    if (!hasIntersect)
                    {
                        variables[".Categories"] = mappedCategories.Except(SearchPath.Categories).ToList();

                        continue;
                    }

                    variables[".Categories"] = mappedCategories.Intersect(SearchPath.Categories).ToList();
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
                                    if (parts.Length == 2)
                                        value = parts[1];
                                    queryCollection.Add(key, value);
                                }
                            }
                            else
                            {
                                var inputValue = applyGoTemplateText(Input.Value, variables);

                                if (!string.IsNullOrWhiteSpace(inputValue) || Search.AllowEmptyInputs)
                                    queryCollection.Add(Input.Key, inputValue);
                            }
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
                {
                    response = await FollowIfRedirect(response);
                }

                var results = response.ContentString;

                if (SearchPath.Response is { Type: "json" })
                {
                    // check if we need to login again
                    var loginNeeded = CheckIfLoginIsNeeded(response);

                    if (loginNeeded)
                    {
                        logger.Info("CardigannIndexer({0}): Relogin required", Id);

                        var loginResult = await DoLogin(response.Cookies);

                        if (!loginResult)
                        {
                            throw new Exception("Relogin failed");
                        }

                        await TestLogin();

                        response = await RequestWithCookiesAsync(searchUrl, method: method, data: queryCollection, headers: headers);

                        if (response.IsRedirect && SearchPath.Followredirect)
                        {
                            response = await FollowIfRedirect(response);
                        }

                        results = response.ContentString;
                    }

                    if (response.Status != HttpStatusCode.OK)
                    {
                        throw new Exception($"Error Parsing Json Response: Status={response.Status} Response={results}");
                    }

                    if (response.Status == HttpStatusCode.OK
                        && SearchPath.Response is { NoResultsMessage: not null }
                        && (SearchPath.Response.NoResultsMessage != string.Empty && results.Contains(SearchPath.Response.NoResultsMessage) || (SearchPath.Response.NoResultsMessage == string.Empty && results == string.Empty)))
                    {
                        continue;
                    }

                    JToken parsedJson;

                    try
                    {
                        parsedJson = JToken.Parse(results);
                    }
                    catch (JsonReaderException ex)
                    {
                        logger.Warn("Unexpected response content ({0} bytes): {1}", response.ContentBytes.Length, response.ContentString);

                        throw new Exception("Error Parsing Json Response", ex);
                    }

                    if (parsedJson == null)
                    {
                        throw new Exception("Error Parsing Json Response");
                    }

                    if (Search.Rows.Count != null)
                    {
                        try
                        {
                            var countVal = handleJsonSelector(Search.Rows.Count, parsedJson, variables);

                            if (int.TryParse(countVal, out var count) && count < 1)
                            {
                                continue;
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Trace(ex, "Failed to parse JSON rows count.");
                        }
                    }

                    var rowsArray = JsonParseRowsSelector(parsedJson, Search.Rows.Selector);

                    if (rowsArray == null)
                    {
                        if (Search.Rows.MissingAttributeEqualsNoResults)
                        {
                            continue;
                        }

                        throw new Exception("Error Parsing Rows Selector. There are 0 rows.");
                    }

                    if (rowsArray.Count == 0)
                    {
                        continue;
                    }

                    foreach (var Row in rowsArray)
                    {
                        var selObj = Row;

                        if (Search.Rows.Attribute != null)
                        {
                            selObj = Row.SelectToken(Search.Rows.Attribute)?.Value<JToken>();

                            if (selObj == null && Search.Rows.MissingAttributeEqualsNoResults)
                            {
                                continue;
                            }
                        }

                        var mulRows = Search.Rows.Multiple ? selObj.Values<JObject>() : new List<JObject> { selObj.Value<JObject>() };

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
                                        var defaultValue = applyGoTemplateText(Field.Value.Default, variables);

                                        if (string.IsNullOrWhiteSpace(defaultValue))
                                        {
                                            variables[variablesKey] = null;
                                            continue;
                                        }

                                        value = defaultValue;
                                    }

                                    variables[variablesKey] = ParseFields(value, FieldName, release, FieldModifiers, searchUrlUri);
                                }
                                catch (Exception ex)
                                {
                                    if (!variables.ContainsKey(variablesKey) || isOptional)
                                        variables[variablesKey] = null;

                                    if (isOptional)
                                        continue;

                                    throw new Exception($"Error while parsing field={Field.Key}, selector={Field.Value.Selector}, value={value ?? "<null>"}: {ex.Message}", ex);
                                }
                            }

                            var Filters = Definition.Search.Rows.Filters;
                            var SkipRelease = ParseRowFilters(Filters, release, query, variables, Row);

                            if (SkipRelease)
                                continue;

                            releases.Add(release);
                        }
                    }
                }
                else
                {
                    try
                    {
                        IHtmlCollection<IElement> rowsDom;

                        if (SearchPath.Response is { Type: "xml" })
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
                            // check if we need to login again
                            var loginNeeded = CheckIfLoginIsNeeded(response);

                            if (loginNeeded)
                            {
                                logger.Info("CardigannIndexer({0}): Relogin required", Id);

                                var loginResult = await DoLogin(response.Cookies);

                                if (!loginResult)
                                {
                                    throw new Exception("Relogin failed");
                                }

                                await TestLogin();

                                response = await RequestWithCookiesAsync(searchUrl, method: method, data: queryCollection, headers: headers);

                                if (response.IsRedirect && SearchPath.Followredirect)
                                {
                                    response = await FollowIfRedirect(response);
                                }

                                results = response.ContentString;
                            }

                            var searchResultParser = new HtmlParser();
                            var searchResultDocument = searchResultParser.ParseDocument(results);

                            checkForError(response, Definition.Search.Error);

                            if (Search.Preprocessingfilters != null)
                            {
                                results = applyFilters(results, Search.Preprocessingfilters, variables);
                                searchResultDocument = searchResultParser.ParseDocument(results);
                                logger.Debug(string.Format("CardigannIndexer ({0}): result after preprocessingfilters: {1}", Id, results));
                            }

                            var rowsSelector = applyGoTemplateText(Search.Rows.Selector, variables);
                            rowsDom = searchResultDocument.QuerySelectorAll(rowsSelector);
                        }

                        var Rows = rowsDom.ToList();

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
                                    var MergeRow = Rows.ElementAtOrDefault(MergeRowIndex);
                                    if (MergeRow != null)
                                    {
                                        CurrentRow.Append(MergeRow.ChildNodes.ToArray());
                                    }
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
                                            var defaultValue = applyGoTemplateText(Field.Value.Default, variables);

                                            if (string.IsNullOrWhiteSpace(defaultValue))
                                            {
                                                variables[variablesKey] = null;
                                                continue;
                                            }

                                            value = defaultValue;
                                        }

                                        variables[variablesKey] = ParseFields(value, FieldName, release, FieldModifiers, searchUrlUri);
                                    }
                                    catch (Exception ex)
                                    {
                                        if (!variables.ContainsKey(variablesKey) || isOptional)
                                            variables[variablesKey] = null;

                                        if (isOptional)
                                            continue;

                                        throw new Exception($"Error while parsing field={Field.Key}, selector={Field.Value.Selector}, value={value ?? "<null>"}: {ex.Message}", ex);
                                    }
                                }

                                var Filters = Definition.Search.Rows.Filters;
                                var SkipRelease = ParseRowFilters(Filters, release, query, variables, Row.ToHtmlPretty());

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

            logger.Debug($"CardigannIndexer ({Id}): handleRequest() requestLinkStr= {requestLinkStr}");

            var response = await RequestWithCookiesAndRetryAsync(requestLinkStr, null, method, referer, pairs);

            logger.Debug($"CardigannIndexer ({Id}): handleRequest() remote server returned {response.Status.ToString()}" + (response.IsRedirect ? " => " + response.RedirectingTo : ""));

            return response;
        }

        protected async Task<WebResult> HandleRedirectableRequestAsync(string url, Dictionary<string, string> headers = null, int maxRedirects = 5)
        {
            var response = await RequestWithCookiesAsync(url, headers: headers);

            for (var i = 0; i < maxRedirects; i++)
            {
                if (!response.IsRedirect)
                {
                    break;
                }

                response = await RequestWithCookiesAsync(response.RedirectingTo, headers: headers);
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
            using var resultDocument = parser.ParseDocument(results);

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

                headers = ParseCustomHeaders(Definition.Download?.Headers ?? Definition.Search?.Headers, variables);
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
                        headers = ParseCustomHeaders(Definition.Download?.Headers ?? Definition.Search?.Headers, variables);

                        if (!Download.Infohash.Usebeforeresponse || Download.Before == null || response == null)
                            response = await HandleRedirectableRequestAsync(link.ToString(), headers);

                        var hash = MatchSelector(response, Download.Infohash.Hash, variables);
                        if (hash == null)
                            throw new Exception("InfoHash selectors didn't match hash.");

                        var title = MatchSelector(response, Download.Infohash.Title, variables);
                        if (title == null)
                            throw new Exception("InfoHash selectors didn't match title.");

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
                    headers = ParseCustomHeaders(Definition.Download?.Headers ?? Definition.Search?.Headers, variables);

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
            headers = ParseCustomHeaders(Definition.Download?.Headers ?? Definition.Search?.Headers, GetBaseTemplateVariables());
            return await base.Download(link, method, link.ToString(), headers);
        }

        private Dictionary<string, string> ParseCustomHeaders(Dictionary<string, List<string>> customHeaders, Dictionary<string, object> variables)
        {
            var headers = new Dictionary<string, string>();

            if (customHeaders == null)
            {
                return headers;
            }

            // FIXME: fix jackett header handling (allow it to specifiy the same header multipe times)
            foreach (var header in customHeaders)
            {
                headers.Add(header.Key, applyGoTemplateText(header.Value[0], variables));
            }

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
                    if (FieldModifiers.Contains("noappend"))
                    {
                        logger.Warn("CardigannIndexer ({0}): The \"noappend\" modifier is deprecated. Please switch to \"default\". See the Definition Format in the Wiki for more information.", Id);
                    }

                    var cats = MapTrackerCatToNewznab(value);

                    if (cats.Any())
                    {
                        release.Category = release.Category == null || FieldModifiers.Contains("noappend")
                            ? cats
                            : release.Category.Union(cats).ToList();
                    }

                    if (value.IsNotNullOrWhiteSpace() && !release.Category.Any())
                    {
                        logger.Warn("[{0}] Invalid category for value: '{1}'", Id, value);
                    }
                    else
                    {
                        value = release.Category.ToString();
                    }

                    break;
                case "categorydesc":
                    if (FieldModifiers.Contains("noappend"))
                    {
                        logger.Warn("CardigannIndexer ({0}): The \"noappend\" modifier is deprecated. Please switch to \"default\". See the Definition Format in the Wiki for more information.", Id);
                    }

                    var catsDesc = MapTrackerCatDescToNewznab(value);

                    if (catsDesc.Any())
                    {
                        release.Category = release.Category == null || FieldModifiers.Contains("noappend")
                            ? catsDesc
                            : release.Category.Union(catsDesc).ToList();
                    }

                    if (value.IsNotNullOrWhiteSpace() && !release.Category.Any())
                    {
                        logger.Warn("[{0}] Invalid category for value: '{1}'", Id, value);
                    }
                    else
                    {
                        value = release.Category.ToString();
                    }

                    break;
                case "size":
                    release.Size = ParseUtil.GetBytes(value);
                    value = release.Size.ToString();
                    break;
                case "leechers":
                    var leechers = ParseUtil.CoerceLong(value);
                    leechers = leechers < 5000000L ? leechers : 0; // to fix #6558
                    if (release.Peers == null)
                        release.Peers = leechers;
                    else
                        release.Peers += leechers;
                    value = leechers.ToString();
                    break;
                case "seeders":
                    release.Seeders = ParseUtil.CoerceLong(value);
                    release.Seeders = release.Seeders < 5000000L ? release.Seeders : 0; // to fix #6558
                    if (release.Peers == null)
                        release.Peers = release.Seeders;
                    else
                        release.Peers += release.Seeders;
                    value = release.Seeders.ToString();
                    break;
                case "date":
                    release.PublishDate = DateTimeUtil.FromUnknown(value);
                    value = release.PublishDate.ToString(DateTimeUtil.Rfc1123ZPattern, CultureInfo.InvariantCulture);
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
                case "imdbid":
                    release.Imdb = ParseUtil.GetLongFromString(value).GetValueOrDefault();
                    value = release.Imdb.ToString();
                    break;
                case "tmdbid":
                    release.TMDb = ParseUtil.GetLongFromString(value).GetValueOrDefault();
                    value = release.TMDb.ToString();
                    break;
                case "rageid":
                    release.RageID = ParseUtil.GetLongFromString(value).GetValueOrDefault();
                    value = release.RageID.ToString();
                    break;
                case "tvdbid":
                    release.TVDBId = ParseUtil.GetLongFromString(value).GetValueOrDefault();
                    value = release.TVDBId.ToString();
                    break;
                case "tvmazeid":
                    release.TVMazeId = ParseUtil.GetLongFromString(value).GetValueOrDefault();
                    value = release.TVMazeId.ToString();
                    break;
                case "traktid":
                    release.TraktId = ParseUtil.GetLongFromString(value).GetValueOrDefault();
                    value = release.TraktId.ToString();
                    break;
                case "doubanid":
                    release.DoubanId = ParseUtil.GetLongFromString(value).GetValueOrDefault();
                    value = release.DoubanId.ToString();
                    break;
                case "genre":
                    release.Genres ??= new List<string>();
                    char[] delimiters = { ',', ' ', '/', ')', '(', '.', ';', '[', ']', '"', '|', ':' };
                    var releaseGenres = release.Genres.Union(value.Split(delimiters, StringSplitOptions.RemoveEmptyEntries));
                    release.Genres = releaseGenres.Select(x => x.Replace("_", " ")).ToList();
                    value = string.Join(",", release.Genres);
                    break;
                case "year":
                    release.Year = ParseUtil.CoerceLong(value);
                    value = release.Year.ToString();
                    break;
                case "author":
                    release.Author = value;
                    break;
                case "booktitle":
                    release.BookTitle = value;
                    break;
                case "publisher":
                    release.Publisher = value;
                    break;
                case "artist":
                    release.Artist = value;
                    break;
                case "album":
                    release.Album = value;
                    break;
                case "label":
                    release.Label = value;
                    break;
                case "track":
                    release.Track = value;
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

                            if (query.ImdbID != null && (TorznabCaps.MovieSearchImdbAvailable || TorznabCaps.TvSearchImdbAvailable))
                                break; // skip andmatch filter for imdb searches

                            if (query.TmdbID != null && (TorznabCaps.MovieSearchTmdbAvailable || TorznabCaps.TvSearchTmdbAvailable))
                                break; // skip andmatch filter for tmdb searches

                            if (query.TvdbID != null && TorznabCaps.TvSearchTvdbAvailable)
                                break; // skip andmatch filter for tvdb searches

                            if (query.DoubanID != null && (TorznabCaps.MovieSearchImdbAvailable || TorznabCaps.TvSearchImdbAvailable))
                                break; // skip andmatch filter for douban searches

                            if (query.TraktID != null && (TorznabCaps.MovieSearchImdbAvailable || TorznabCaps.TvSearchImdbAvailable))
                                break; // skip andmatch filter for trakt searches

                            if (query.TvmazeID != null && TorznabCaps.TvSearchImdbAvailable)
                                break; // skip andmatch filter for tvmaze searches

                            if (query.RageID != null && TorznabCaps.TvSearchImdbAvailable)
                                break; // skip andmatch filter for tvmaze searches

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

        private JArray JsonParseRowsSelector(JToken parsedJson, string rowSelector)
        {
            rowSelector = applyGoTemplateText(rowSelector);
            var selector = rowSelector.Split(':')[0];

            try
            {
                var rowsObj = parsedJson.SelectToken(selector).Value<JArray>();

                return new JArray(rowsObj.Where(t => JsonParseFieldSelector(t.Value<JObject>(), rowSelector.Remove(0, selector.Length)) != null));
            }
            catch (Exception ex)
            {
                logger.Trace(ex, "Failed to parse JSON rows for selector \"{0}\"", rowSelector);

                return null;
            }
        }

        private string JsonParseFieldSelector(JToken parsedJson, string rowSelector)
        {
            var selector = rowSelector.Split(':')[0];
            JToken parsedObject;
            if (string.IsNullOrWhiteSpace(selector))
                parsedObject = parsedJson;
            else if (parsedJson.SelectToken(selector) != null)
                parsedObject = parsedJson.SelectToken(selector);
            else
                return null;

            foreach (Match match in _JsonSelectorRegex.Matches(rowSelector))
            {
                var filter = match.Result("${filter}");
                var key = match.Result("${key}");
                Match innerMatch;
                switch (filter)
                {
                    case "has":
                        innerMatch = _JsonSelectorRegex.Match(key);
                        if (innerMatch.Success)
                        {
                            if (JsonParseFieldSelector(parsedObject, key) == null)
                                return null;
                        }
                        else
                        {
                            if (parsedObject.SelectToken(key) == null)
                                return null;
                        }
                        break;
                    case "not":
                        innerMatch = _JsonSelectorRegex.Match(key);
                        if (innerMatch.Success)
                        {
                            if (JsonParseFieldSelector(parsedObject, key) != null)
                                return null;
                        }
                        else
                        {
                            if (parsedObject.SelectToken(key) != null)
                                return null;
                        }
                        break;
                    case "contains":
                        if (!parsedObject.ToString().Contains(key))
                            return null;
                        break;
                    default:
                        logger.Error(string.Format("CardigannIndexer ({0}): Unsupported selector: {1}", Id, rowSelector));
                        continue;
                }
            }
            return selector;
        }
    }
}
