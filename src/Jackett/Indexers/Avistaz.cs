using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jackett.Models;
using Newtonsoft.Json.Linq;
using NLog;
using Jackett.Utils;
using System.Net;
using System.Net.Http;
using CsQuery;
using System.Web;
using Jackett.Services;
using Jackett.Utils.Clients;
using System.Text.RegularExpressions;
using Jackett.Models.IndexerConfig;

namespace Jackett.Indexers
{
    public class Avistaz : AvistazTracker
    {
        public Avistaz(IIndexerConfigurationService configService, IWebClient webClient, Logger logger, IProtectionService protectionService)
            : base(name: "Avistaz",
                desc: "Aka AsiaTorrents",
                link: "https://avistaz.to/",
                configService: configService,
                logger: logger,
                protectionService: protectionService,
                webClient: webClient
                )
        {
            Type = "private";
        }
    }
}