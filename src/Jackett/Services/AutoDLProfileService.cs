using CuttingEdge.Conditions;
using Jackett.Models.AutoDL;
using Jackett.Models.AutoDL.Parser;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Jackett.Services
{
    public interface IAutoDLProfileService {
        void Load();
        List<NetworkSummary> GetNetworks();
        List<AutoDLProfileSummary> GetProfiles();
        void Set(AutoDLProfileSummary profile);
        TrackerInfo GetTracker(string id);
    }

    class AutoDLProfileService : IAutoDLProfileService
    {
        private IConfigurationService configSerivce;
        private List<TrackerInfo> trackers = new List<TrackerInfo>();
        Logger logger;


        public AutoDLProfileService(IConfigurationService c, Logger l)
        {
            configSerivce = c;
            logger = l;
            Load();
        }

        public List<AutoDLProfileSummary> GetProfiles()
        {
            return trackers.Select(t => new AutoDLProfileSummary()
            {
                LongName = t.LongName,
                Options = t.Options,
                ShortName = t.ShortName,
                SiteName= t.SiteName,
                Type = t.Type
            }).ToList();
        }

        public TrackerInfo GetTracker(string id)
        {
            return trackers.Where(t => t.Type == "tl").First();
        }

        public void Set(AutoDLProfileSummary profile)
        {
            var tracker = trackers.Where(t => t.Type == profile.Type).FirstOrDefault();
            if (tracker != null)
            {
                tracker.IsConfigured = true;
                tracker.Options = profile.Options;
            }

            Save();
        }

        private void Save()
        {
            var config = new SavedAutoDLConfigurations()
            {
                Configurations = trackers.Where(t => t.IsConfigured).Select(t =>
                  {
                      return new SavedAutoDLConfig()
                      {
                          Type = t.Type,
                          Options = t.Options.Where(o=>!string.IsNullOrWhiteSpace(o.Name)).ToDictionary(c => c.Name, c => c.Value)
                      };
                  }).ToList()
            };

            configSerivce.SaveConfig<SavedAutoDLConfigurations>(config);
        }

        public void Load()
        {
            trackers.Clear();

            foreach (var path in Directory.GetFiles(configSerivce.GetAutoDLFolder(), "*.tracker"))
            {
                var xml = XDocument.Load(path);
                var info = new TrackerInfo()
                {
                    FileName = Path.GetFileName(path),
                    LongName = xml.Root.AttributeString("longName"),
                    ShortName = xml.Root.AttributeString("shortName"),
                    SiteName = xml.Root.AttributeString("siteName"),
                    Type = xml.Root.AttributeString("type")
                };

                Condition.Requires<string>(info.FileName, "FileName").IsNotNullOrWhiteSpace();
                Condition.Requires<string>(info.LongName, "LongName").IsNotNullOrWhiteSpace();
                Condition.Requires<string>(info.ShortName, "ShortName").IsNotNullOrWhiteSpace();
                // Condition.Requires<string>(info.SiteName, "SiteName").IsNotNullOrWhiteSpace();
                Condition.Requires<string>(info.Type, "Type").IsNotNullOrWhiteSpace();

                info.Options.Add(new ConfigOption()
                {
                    Name = "enabled",
                    Label = "Enabled",
                    DefaultValue = "true",
                    Type = ConfigOptionType.Bool
                });

                info.Options.Add(new ConfigOption()
                {
                    Name = "upload-delay-secs",
                    Label = "Delay",
                    Tooltip = "Wait this many seconds before uploading/saving the torrent. Default is 0.",
                    DefaultValue = "0",
                    Type = ConfigOptionType.Integer
                });

                info.Options.Add(new ConfigOption()
                {
                    Name = "force-ssl",
                    Label = "Force HTTPS (SSL) downloads",
                    Tooltip = "If checked, all torrent file downloads from this tracker will be forced to use the HTTPS protocol. Not all trackers support this.",
                    DefaultValue = "false",
                    Type = ConfigOptionType.Bool
                });

                foreach (var setting in xml.Root.Element("settings").Elements())
                {
                    var tag = setting.Name;
                    var option = new ConfigOption()
                    {
                        Name = setting.AttributeString("name"),
                        DefaultValue = setting.AttributeString("defaultValue") ?? string.Empty,
                        Label = setting.AttributeString("text"),
                        EmptyText = setting.AttributeString("emptytext"),
                        Tooltip = setting.AttributeString("tooltiptext"),
                        PasteGroup = setting.AttributeString("pasteGroup"),
                        PasteRegex = setting.AttributeString("pasteRegex"),
                        MinValue = setting.AttributeString("minValue"),
                        MaxValue = setting.AttributeString("maxValue"),
                        IsDownloadVar = setting.AttributeString("name") == "true"
                    };

                    if (!string.IsNullOrEmpty(setting.AttributeString("type")))
                        option.Type = (ConfigOptionType)Enum.Parse(typeof(ConfigOptionType), setting.AttributeString("type"), true);

                    if (tag == "gazelle_description" || tag == "description" || tag == "cookie_description")
                    {
                        option.Type = ConfigOptionType.Description;
                        option.Label = "Paste (Ctrl+V) any torrent download link into any one of the two text boxes below to automatically extract authkey and torrent_pass.";
                    }
                    else if (tag == "gazelle_authkey" || tag == "authkey")
                    {
                        option.Type = ConfigOptionType.TextBox;
                        option.Name = "authkey";
                        option.Label = "authkey";
                        option.Tooltip = "The authkey in any torrent download link.";
                        option.PasteGroup = "authkey,torrent_pass";
                        option.PasteRegex = "[\\?&]authkey=([\\da-zA-Z]{32})";
                    }
                    else if (tag == "gazelle_torrent_pass")
                    {
                        option.Type = ConfigOptionType.TextBox;
                        option.Name = "torrent_pass";
                        option.Label = "torrent_pass";
                        option.Tooltip = "The torrent_pass in any torrent download link.";
                        option.PasteGroup = "authkey,torrent_pass";
                        option.PasteRegex = "[\\?&]torrent_pass=([\\da-zA-Z]{32})";
                    }
                    else if (tag == "description")
                    {
                        option.Type = ConfigOptionType.Description;
                    }
                    else if (tag == "authkey")
                    {
                        option.Type = ConfigOptionType.TextBox;
                        option.Name = "authkey";
                        option.Label = "authkey";
                        option.Tooltip = "The authkey in any torrent download link.";
                        option.PasteGroup = "authkey";
                        option.PasteRegex = "[\\?&]authkey=([\\da-fA-F]{32})";
                    }
                    else if (tag == "passkey")
                    {
                        option.Type = ConfigOptionType.TextBox;
                        option.Name = "passkey";
                        option.Label = "passkey";
                        option.Tooltip = "The passkey in any torrent download link.";
                        option.PasteGroup = "passkey";
                        option.PasteRegex = "[\\?&]passkey=([\\da-fA-F]{32})";
                    }
                    else if (tag == "cookie")
                    {
                        option.Type = ConfigOptionType.TextBox;
                        option.Name = "cookie";
                        option.Label = "Log in to your tracker's home page with your browser.<br><br><strong>Chrome:</strong> Options Menu -&gt; Privacy -&gt; Content Settings -&gt; All cookies and site data<br><strong>Firefox:</strong> Firefox Menu -&gt; Options -&gt; Privacy -&gt; Show cookies<br><strong>Safari:</strong> Action Menu -&gt; Preferences -&gt; Privacy -&gt; Details<br><br>Find your tracker site in the cookie or file list.The values needed may vary between trackers. Often these are _uid_ and _pass_.<br />Set the cookie like <strong>uid=XXX; pass=YYY</strong>, separating each key=value pair with a semicolon.";
                        option.Tooltip = "The cookie.";
                    }
                    else if (tag == "integer")
                    {
                        option.Type = ConfigOptionType.Integer;
                        option.MinValue = "-999999999";
                    }
                    else if (tag == "delta")
                    {
                        option.Type = ConfigOptionType.Integer;
                        option.Name = "delta";
                        option.Label = "Torrent ID delta";
                        option.MinValue = "-999999999";
                    }
                    else if (tag == "textbox")
                    {
                        option.Type = ConfigOptionType.TextBox;
                        option.Tooltip = $"{info.LongName} {setting.Name}";
                    }

                    if (string.IsNullOrWhiteSpace(option.Label))
                        option.Label = option.Name;
                    if (!option.Type.HasValue)
                        throw new Exception($"No option type specified for setting {tag} on tracker {info.LongName}");

                    info.Options.Add(option);
                }

                foreach (var server in xml.Root.Element("servers").Elements())
                {
                    var serverInfo = new ServerInfo()
                    {
                        Announcers = server.AttributeStringList("announcerNames"),
                        Channels = server.AttributeStringList("channelNames"),
                        Network = server.AttributeString("network"),
                        Servers = server.AttributeStringList("serverNames"),
                    };

                    info.Servers.Add(serverInfo);
                }

                foreach (var node in xml.Root.Element("parseinfo").Elements())
                {
                    switch (node.Name.ToString())
                    {
                        case "multilinepatterns":
                            // todo
                            break;
                        case "linepatterns":
                            info.Parser.SingleLineMatches.Add(new Models.AutoDL.Parser.LinePatterns()
                            {
                                Children = ParseCommands(node)
                            });
                            break;
                        case "linematched":
                            info.Parser.MatchParsers.Add(new Models.AutoDL.Parser.LineMatched()
                            {
                                Children = ParseCommands(node)
                            });
                            break;
                        case "ignore":
                            info.Parser.IgnoreMatches.Add(new Models.AutoDL.Parser.Ignore()
                            {
                                Children = ParseCommands(node)
                            });
                            break;
                    }
                }

                logger.Trace($"Loaded {info.FileName}");
                trackers.Add(info);
            }

            logger.Trace($"Loaded {trackers.Count} irc profiles.");

            var savedConfig = configSerivce.GetConfig<SavedAutoDLConfigurations>();
            if (savedConfig != null)
            {
                foreach(var config in savedConfig.Configurations)
                {
                    var tracker = trackers.Where(t => t.Type == config.Type).FirstOrDefault();
                    if (tracker != null)
                    {
                        tracker.IsConfigured = true;
                        foreach(var savedOption in config.Options)
                        {
                            var trackerOption = tracker.Options.Where(o => o.Name == savedOption.Key).FirstOrDefault();
                            if (trackerOption != null)
                            {
                                trackerOption.Value = savedOption.Value;
                            }
                        }
                    }
                }
            }
        }

        private List<IParserCommand> ParseCommands(XElement node)
        {
            var subCommands = new List<IParserCommand>();
            foreach(var childNode in node.Elements())
            {
                IParserCommand parsedAction = null;
                switch (childNode.Name.ToString().ToLowerInvariant())
                {
                    case "extract":
                        parsedAction = new Extract();
                        break;
                    case "http":
                        parsedAction = new Http(childNode);
                        break;
                    case "ignore":
                        parsedAction = new Ignore();
                        break;
                    case "regex":
                        parsedAction = new Regex(childNode);
                        break;
                    case "string":
                        parsedAction = new Models.AutoDL.Parser.String(childNode);
                        break;
                    case "var":
                        parsedAction = new Var(childNode);
                        break;
                    case "varenc":
                        parsedAction = new VarEnc(childNode);
                        break;
                    case "varreplace":
                        parsedAction = new VarReplace(childNode);
                        break;
                    case "vars":
                        parsedAction = new Vars(childNode);
                        break;
                    case "setregex":
                        parsedAction = new SetRegex(childNode);
                        break;
                    case "if":
                        parsedAction = new If(childNode);
                        break;
                    case "extractone":
                        parsedAction = new ExtractOne(childNode);
                        break;
                    case "extracttags":
                        parsedAction = new ExtractTags(childNode);
                        break;
                    case "setvarif":
                        parsedAction = new SetVarIf(childNode);
                        break;
                }

                if (parsedAction == null)
                {
                    var msg = "Failed to parse AutoDL action: " + childNode.Name;
                    logger.Error(msg);
                    //throw new Exception(msg);
                }

                var baseCommand = parsedAction as BaseParserCommand;
                if (baseCommand != null)
                {
                    baseCommand.Children = ParseCommands(childNode);
                }

                subCommands.Add(parsedAction);
            }

            return subCommands;
        }

        public List<NetworkSummary> GetNetworks()
        {
            var list = new List<NetworkSummary>();

            // Default profile
            list.Add(new NetworkSummary()
            {
                Name = "Other network"
            });

            // Freenode
            list.Add(new NetworkSummary()
            {
                Name = "FreeNode",
                Servers =new List<string>()
                {
                    "chat.freenode.net"
                }
            });

            foreach(var networkGroup in trackers.Where(t=>t.Servers.Count>0).GroupBy(t=>t.Servers.First().Network.ToLowerInvariant()))
            {
                var summary = new NetworkSummary()
                {
                    Name = networkGroup.First().Servers.First().Network,
                    Profiles = networkGroup.Select(p=>p.LongName).ToList(),
                    Servers = networkGroup.SelectMany(p=>p.Servers).SelectMany(s=>s.Servers).Select(s=>s.ToLowerInvariant().Trim()).Distinct().ToList()
                };
                list.Add(summary);
            }

            return list;
        }
    }
}
