using CuttingEdge.Conditions;
using Jackett.Models.AutoDL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Jackett.Services
{
    public static class ElementExtensions
    {
        public static string AttributeString(this XElement element, string name)
        {
            var attr = element.Attribute(name);
            if (attr == null)
                return string.Empty;
            return attr.Value;
        }

        public static List<string> AttributeStringList(this XElement element, string name)
        {
            var value = element.AttributeString(name);
            return value.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
        }
    }

    public interface IAutoDLProfileService {
        void Load();
        List<NetworkSummary> GetNetworks();
    }

    class AutoDLProfileService : IAutoDLProfileService
    {
        private IConfigurationService configSerivce;
        private List<TrackerInfo> trackers = new List<TrackerInfo>();

        public AutoDLProfileService(IConfigurationService c)
        {
            configSerivce = c;
            Load();
        }

        public void Load()
        {
            trackers.Clear();

            // Add Jackett Profile
            var jackettProfile = new TrackerInfo();
            jackettProfile.LongName = jackettProfile.ShortName = jackettProfile.SiteName = jackettProfile.Type = "Jackett";
            jackettProfile.Servers.Add(new ServerInfo()
            {
                Network = "FreeNode",
                Servers = new List<string>() { "chat.freenode.net" },
                Channels = new List<string>() { "#jackett" }
            });

            trackers.Add(jackettProfile);

            foreach (var path in Directory.GetFiles(configSerivce.GetAutoDLFolder(), "*.tracker")) {
                var xml = XDocument.Load(path);
                var info = new TrackerInfo()
                {
                    FileName = Path.GetFileName(path),
                    LongName = xml.Root.AttributeString("longName"),
                    ShortName = xml.Root.AttributeString("shortName"),
                    SiteName =  xml.Root.AttributeString("siteName"),
                    Type =  xml.Root.AttributeString("type")
                };

                Condition.Requires<string>(info.FileName, "FileName").IsNotNullOrWhiteSpace();
                Condition.Requires<string>(info.LongName, "LongName").IsNotNullOrWhiteSpace();
                Condition.Requires<string>(info.ShortName, "ShortName").IsNotNullOrWhiteSpace();
               // Condition.Requires<string>(info.SiteName, "SiteName").IsNotNullOrWhiteSpace();
                Condition.Requires<string>(info.Type, "Type").IsNotNullOrWhiteSpace();


                foreach(var setting in xml.Root.Element("settings").Descendants())
                {

                }

                foreach (var server in xml.Root.Element("servers").Descendants())
                {
                    var serverInfo = new ServerInfo()
                    {
                        Announcers = server.AttributeStringList("announcerNames"),
                        Channels = server.AttributeStringList("channelNames"),
                        Network =  server.AttributeString("network"),
                        Servers = server.AttributeStringList("serverNames"),
                    };

                    info.Servers.Add(serverInfo);
                }

                trackers.Add(info);
            }
        }

        public List<NetworkSummary> GetNetworks()
        {
            var list = new List<NetworkSummary>();

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
