#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Jackett.Common.Extensions;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers.Definitions
{
    /// <summary>
    /// Nostr Torrents indexer - queries Nostr relays for NIP-35 (kind 2003) torrent events.
    /// Uses WebSocket connection via Nostr.Client library for proper Nostr protocol support.
    /// See: https://github.com/nostr-protocol/nips/blob/master/35.md
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Nostr : IndexerBase
    {
        /// <summary>
        /// Bidirectional mapping between tcat values and Torznab categories.
        /// Used for capabilities, parsing incoming events, and generating search filters.
        /// Order matters: more specific mappings should come before generic ones.
        /// </summary>
        internal static readonly List<(string Tcat, TorznabCategory Category)> TcatMappings = new()
        {
            // Movies
            ("video,movie,4k", TorznabCatType.MoviesUHD),
            ("video,movie,uhd", TorznabCatType.MoviesUHD),
            ("video,movie,hd", TorznabCatType.MoviesHD),
            ("video,movie,sd", TorznabCatType.MoviesSD),
            ("video,movie,dvdr", TorznabCatType.MoviesDVD),
            ("video,movie", TorznabCatType.Movies),
            ("video,movies", TorznabCatType.Movies),

            // TV
            ("video,tv,4k", TorznabCatType.TVUHD),
            ("video,tv,uhd", TorznabCatType.TVUHD),
            ("video,tv,hd", TorznabCatType.TVHD),
            ("video,tv,sd", TorznabCatType.TVSD),
            ("video,tv", TorznabCatType.TV),

            // Anime
            ("anime,video,raw", TorznabCatType.TVAnime),
            ("anime,video", TorznabCatType.TVAnime),
            ("anime", TorznabCatType.TVAnime),

            // Audio
            ("audio,music,flac", TorznabCatType.AudioLossless),
            ("audio,lossless", TorznabCatType.AudioLossless),
            ("audio,audio-book", TorznabCatType.AudioAudiobook),
            ("audio,music", TorznabCatType.Audio),
            ("audio", TorznabCatType.Audio),

            // PC/Software
            ("application,mac", TorznabCatType.PCMac),
            ("application,ios", TorznabCatType.PCMobileiOS),
            ("application,android", TorznabCatType.PCMobileAndroid),
            ("application", TorznabCatType.PC),
            ("software", TorznabCatType.PC),

            // Games
            ("game,pc", TorznabCatType.PCGames),
            ("game,mac", TorznabCatType.PCMac),
            ("game,ios", TorznabCatType.PCMobileiOS),
            ("game,android", TorznabCatType.PCMobileAndroid),
            ("game,psx", TorznabCatType.ConsolePS3),
            ("game,ps4", TorznabCatType.ConsolePS4),
            ("game,ps5", TorznabCatType.ConsolePS4),
            ("game,xbox", TorznabCatType.ConsoleXBox),
            ("game,wii", TorznabCatType.ConsoleWii),
            ("game,nintendo", TorznabCatType.ConsoleWii),
            ("game", TorznabCatType.PCGames),

            // Books
            ("other,e-book", TorznabCatType.BooksEBook),
            ("other,comic", TorznabCatType.BooksComics),
            ("ebook", TorznabCatType.BooksEBook),
            ("book", TorznabCatType.Books),

            // XXX
            ("porn,picture", TorznabCatType.XXXImageSet),
            ("porn,movie,4k", TorznabCatType.XXXUHD),
            ("porn,movie", TorznabCatType.XXX),
            ("porn,movie,dvdr", TorznabCatType.XXXDVD),
            ("porn,movie,hd", TorznabCatType.XXXDVD),
            ("porn,game", TorznabCatType.XXXOther),

            // Other
            ("other", TorznabCatType.Other),

            // Reverse-only mappings for search (not used for parsing)
            // These provide tcat lookups for categories without exact matches
            // Movies
            ("movie", TorznabCatType.MoviesForeign),
            ("movie", TorznabCatType.MoviesOther),
            ("movie", TorznabCatType.MoviesBluRay),
            ("movie", TorznabCatType.Movies3D),
            ("movie", TorznabCatType.MoviesWEBDL),
            // TV
            ("tv", TorznabCatType.TVWEBDL),
            ("tv", TorznabCatType.TVForeign),
            ("tv", TorznabCatType.TVOther),
            ("tv", TorznabCatType.TVSport),
            ("tv", TorznabCatType.TVDocumentary),
            // Audio
            ("audio", TorznabCatType.AudioOther),
            ("audio,music", TorznabCatType.AudioMP3),
            ("audio,music", TorznabCatType.AudioVideo),
            ("audio,music", TorznabCatType.AudioForeign),
            // PC
            ("software", TorznabCatType.PC0day),
            ("software", TorznabCatType.PCISO),
            ("software", TorznabCatType.PCMobileOther),
            // Console
            ("game", TorznabCatType.Console),
            ("game", TorznabCatType.ConsoleNDS),
            ("game", TorznabCatType.ConsoleOther),
            ("game", TorznabCatType.Console3DS),
            ("game,psx", TorznabCatType.ConsolePSP),
            ("game,psx", TorznabCatType.ConsolePSVita),
            ("game,wii", TorznabCatType.ConsoleWiiware),
            ("game,wii", TorznabCatType.ConsoleWiiU),
            ("game,xbox", TorznabCatType.ConsoleXBox360),
            // XXX
            ("porn", TorznabCatType.XXXWEBDL),
            // Books
            ("ebook", TorznabCatType.BooksMags),
            ("ebook", TorznabCatType.BooksTechnical),
            ("ebook", TorznabCatType.BooksOther),
            ("ebook", TorznabCatType.BooksForeign),
            // Other
            ("other", TorznabCatType.OtherMisc),
            ("other", TorznabCatType.OtherHashed),
        };

        public override string Id => "nostr";
        public override string Name => "Nostr";

        public override string Description =>
            "Nostr torrents is a decentralized torrent index using the Nostr protocol (NIP-35)";

        public override string SiteLink { get; protected set; } = "https://dtan.xyz/";
        public override string Language => "en-US";
        public override string Type => "public";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private string RelayUrl => ((ConfigurationData.StringConfigurationItem)configData.GetDynamic("relayUrl")).Value;

        public Nostr(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps,
                     ICacheService cs) : base(
            configService: configService, client: wc, logger: l, p: ps, cacheService: cs,
            configData: new ConfigurationData())
        {
            var relayUrl = new ConfigurationData.StringConfigurationItem("Nostr Relay URL (must support NIP-50 search)")
            {
                Value = "wss://relay.dtan.xyz/"
            };
            configData.AddDynamic("relayUrl", relayUrl);
            var timeout = new ConfigurationData.StringConfigurationItem("Search Timeout (seconds)") { Value = "15" };
            configData.AddDynamic("timeout", timeout);
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities
            {
                SupportsRawSearch = true,
                TvSearchParams =
                    new List<TvSearchParam>
                    {
                        TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId
                    },
                MovieSearchParams =
                    new List<MovieSearchParam> { MovieSearchParam.Q, MovieSearchParam.ImdbId, MovieSearchParam.TmdbId },
                MusicSearchParams = new List<MusicSearchParam> { MusicSearchParam.Q },
                BookSearchParams = new List<BookSearchParam> { BookSearchParam.Q }
            };

            // Register all tcat mappings from the central mapping table
            foreach (var (tcat, category) in TcatMappings)
            {
                caps.Categories.AddCategoryMapping(tcat, category);
            }

            return caps;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            // Test the relay connection
            try
            {
                var testResults = await PerformQuery(new TorznabQuery { SearchTerm = "test", Limit = 1 });
                logger.Info($"Nostr relay connection successful. Found {testResults.Count()} results for test query.");
            }
            catch (Exception ex)
            {
                logger.Warn($"Nostr relay test query failed: {ex.Message}. Configuration saved anyway.");
            }

            IsConfigured = true;
            SaveConfig();
            return IndexerConfigurationStatus.Completed;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query) =>
            (await QueryNostrRelayAsync(query)).OrderByDescending(o => o.PublishDate).ToArray();

        class NostrFilter
        {
            [JsonProperty("kinds", NullValueHandling = NullValueHandling.Ignore)]
            public List<int>? Kinds { get; set; }

            [JsonProperty("limit", NullValueHandling = NullValueHandling.Ignore)]
            public int? Limit { get; set; }

            [JsonProperty("search", NullValueHandling = NullValueHandling.Ignore)]
            public string? Search { get; set; }

            [JsonProperty("#t", NullValueHandling = NullValueHandling.Ignore)]
            public List<string>? Hashtags { get; set; }

            [JsonProperty("#i", NullValueHandling = NullValueHandling.Ignore)]
            public List<string>? Identifiers { get; set; }

            public static NostrFilter FromQuery(TorznabQuery query)
            {
                var filter = new NostrFilter
                {
                    Kinds = new List<int> { 2003 }, Limit = query.Limit == 0 ? 100 : query.Limit
                };
                if (query.SearchTerm.IsNotNullOrWhiteSpace())
                {
                    filter.Search = query.SearchTerm;
                }

                // Map query categories to tcat filters (used as hashtags)
                var tcats = query.Categories.Select(s => TcatMappings.FirstOrDefault(z => z.Category.ID == s))
                                 .Where(a => a != default).Distinct().ToList();
                foreach (var tcat in tcats)
                {
                    filter.Identifiers ??= new List<string>();
                    filter.Identifiers.Add($"tcat:{tcat.Tcat}");
                    filter.Hashtags ??= new List<string>();
                    foreach (var ht in tcat.Tcat.Split(','))
                    {
                        filter.Hashtags.Add(ht);
                    }
                }

                return filter;
            }
        }

        class NostrEvent
        {
            [JsonProperty("id")]
            public string Id { get; set; } = null!;

            [JsonProperty("pubkey")]
            public string Pubkey { get; set; } = null!;

            [JsonProperty("created_at")]
            public ulong CreatedAt { get; set; }

            [JsonProperty("kind")]
            public uint Kind { get; set; }

            [JsonProperty("sig")]
            public string Signature { get; set; } = null!;

            [JsonProperty("content")]
            public string Content { get; set; } = null!;

            [JsonProperty("tags")]
            public List<List<string>> Tags { get; set; } = null!;
        }

        private async Task<List<ReleaseInfo>> QueryNostrRelayAsync(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var timeoutSeconds = 15;
            if (int.TryParse(
                    ((ConfigurationData.StringConfigurationItem)configData.GetDynamic("timeout"))?.Value,
                    out var configTimeout))
            {
                timeoutSeconds = configTimeout;
            }

            var filter = NostrFilter.FromQuery(query);
            var events = await FetchEvents(RelayUrl, filter, timeoutSeconds);
            foreach (var ev in events)
            {
                try
                {
                    var release = ParseNostrEvent(ev);
                    if (release != null)
                    {
                        releases.Add(release);
                    }
                }
                catch (Exception ex)
                {
                    logger.Warn("Failed to parse release from event {}", ev.Id);
                }
            }

            return releases;
        }

        private async Task<List<NostrEvent>> FetchEvents(string relay, NostrFilter filter, int timeoutSeconds)
        {
            var ret = new List<NostrEvent>();
            try
            {
                var relayUri = new Uri(relay);
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                using var client = new ClientWebSocket();
                var subscriptionId = Guid.NewGuid().ToString("n").Substring(0, 8);
                await client.ConnectAsync(relayUri, cts.Token);
                var reqMessage = JsonConvert.SerializeObject(
                    new object[]
                    {
                        "REQ",
                        subscriptionId,
                        filter
                    }, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, });
                logger.Info($"[{relayUri}]: {reqMessage}");
                await client.SendAsync(
                    new ArraySegment<byte>(Encoding.UTF8.GetBytes(reqMessage)), WebSocketMessageType.Text, true, cts.Token);
                var buf = new ArraySegment<byte>(new byte[1024 * 256]);
                while (!cts.IsCancellationRequested)
                {
                    var msg = await client.ReceiveAsync(buf, cts.Token);
                    var json = Encoding.UTF8.GetString(buf.Array!, 0, msg.Count);
                    logger.Debug($"[{relayUri}]: {json}");
                    try
                    {
                        if (json.StartsWith("[\"EVENT\","))
                        {
                            var msgParsed = JArray.Parse(json);
                            var subId = msgParsed[1].ToString();
                            if (subId == subscriptionId)
                            {
                                var ev = msgParsed[2].ToObject<NostrEvent>();
                                ret.Add(ev!);
                            }
                        }
                        else if (json.StartsWith("[\"NOTICE\","))
                        {
                            var msgParsed = JArray.Parse(json);
                            logger.Warn($"[{relayUri}]: {msgParsed[1]}");
                            break;
                        }
                        else if (json.StartsWith("[\"EOSE\","))
                        {
                            var msgParsed = JArray.Parse(json);
                            if (msgParsed[1].ToString() == subscriptionId)
                            {
                                break;
                            }
                        }
                        else
                        {
                            logger.Warn($"Unhandled server message: {json}");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"Nostr relay query failed, could not parse response: {json} {ex.Message}");
                    }
                }

                await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", cts.Token);
            }
            catch (OperationCanceledException)
            {
                logger.Warn($"Nostr relay query timed out after {timeoutSeconds} seconds");
            }
            catch (Exception ex)
            {
                logger.Error($"Nostr relay query failed: {ex.Message}");
                throw;
            }

            return ret;
        }

        /// <summary>
        /// Converts a hex event id into `nevent` format (https://github.com/nostr-protocol/nips/blob/master/19.md)
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="withRelayHints">Should relay hints be included</param>
        /// <returns></returns>
        private string EventIdToNEvent(string eventId, bool withRelayHints = true)
        {
            var enc = new Bech32Encoder(
                new[]
                {
                    (byte)'n',
                    (byte)'e',
                    (byte)'v',
                    (byte)'e',
                    (byte)'n',
                    (byte)'t'
                });
            var payload = new List<byte>
            {
                0x0, // special
                32, // ID length
            };
            payload.AddRange(StringUtil.HexToBytes(eventId));
            if (withRelayHints)
            {
                var relayBytes = Encoding.ASCII.GetBytes(RelayUrl);
                if (relayBytes.Length <= byte.MaxValue)
                {
                    payload.AddRange(
                        new byte[]
                        {
                            0x01, // relay type
                            (byte)relayBytes.Length
                        });
                    payload.AddRange(relayBytes);
                }
            }

            var encodeBytes = payload.ToArray();
#if NETCOREAPP
            var bits = enc.ConvertBits(encodeBytes, 8, 5);
            return enc.EncodeData(bits, Bech32EncodingType.BECH32);
#else
            var bits = enc.ConvertBits(encodeBytes, 8, 5);
            return enc.EncodeData(bits, 0, bits.Length, Bech32EncodingType.BECH32);
#endif
        }

        private ReleaseInfo? ParseNostrEvent(NostrEvent evt)
        {
            if (evt.Tags.Count == 0 || string.IsNullOrEmpty(evt.Id) || string.IsNullOrEmpty(evt.Signature) ||
                string.IsNullOrEmpty(evt.Pubkey))
            {
                return null;
            }

#if NETCOREAPP
            if (!NBitcoin.Secp256k1.SecpSchnorrSignature.TryCreate(StringUtil.HexToBytes(evt.Signature), out var sig))
            {
                throw new Exception("Invalid signature, not a valid schnorr signature");
            }

            if (!NBitcoin.Secp256k1.ECXOnlyPubKey.TryCreate(StringUtil.HexToBytes(evt.Pubkey), out var key))
            {
                throw new Exception("Invalid event, not a valid schnorr pubkey");
            }

            if (!key.SigVerifyBIP340(sig, StringUtil.HexToBytes(evt.Id)))
            {
                logger.Warn($"Invalid signature, skipping event {evt.Id}");
                return null;
            }
#endif
            string? title = null, infoHash = null, imdbId = null, tcat = null;
            long? size = null;
            var categories = new List<int>();
            var trackers = new List<string>();
            var legacyTags = new List<string>();
            foreach (var tag in evt.Tags)
            {
                switch (tag[0])
                {
                    case "title":
                        title = tag[1];
                        break;
                    case "x":
                        // Info hash (v1 BitTorrent hash)
                        if (Regex.IsMatch(tag[1], "^[a-fA-F0-9]{40}$"))
                        {
                            infoHash = tag[1].ToUpperInvariant();
                        }

                        break;
                    case "file":
                        // File entries - accumulate size
                        if (long.TryParse(tag[2], out var fileSize))
                        {
                            size = (size ?? 0) + fileSize;
                        }

                        break;
                    case "tracker":
                        trackers.Add(tag[1]);
                        break;
                    case "i":
                        // Identifier tags for categories and external IDs
                        if (tag[1].StartsWith("tcat:"))
                        {
                            tcat = tag[1].Substring(5);
                        }
                        else if (tag[1].StartsWith("newznab:"))
                        {
                            if (int.TryParse(tag[1].Substring(8), out var newznabCat))
                            {
                                categories.Add(newznabCat);
                            }
                        }
                        else if (tag[1].StartsWith("imdb:"))
                        {
                            imdbId = tag[1].Substring(5);
                        }

                        break;
                    case "t":
                        // Collect legacy tags for later processing
                        legacyTags.Add(tag[1]);
                        break;
                }
            }

            // Must have at least title and info hash
            if (title.IsNullOrWhiteSpace() || infoHash.IsNullOrWhiteSpace())
            {
                return null;
            }

            // Process categories: prefer tcat if set, otherwise parse legacy tags
            if (!string.IsNullOrEmpty(tcat))
            {
                // Use tcat exactly as-is
                var mappedCat = TcatMappings.FirstOrDefault(c => c.Tcat == tcat);
                if (mappedCat != default)
                {
                    categories.Add(mappedCat.Category.ID);
                }
            }
            else if (legacyTags.Any())
            {
                // No tcat found, parse legacy tags into tcat format
                var tcatLegacy = string.Join(",", legacyTags);
                var legacyCat = TcatMappings.FirstOrDefault(c => c.Tcat == tcatLegacy);
                if (legacyCat != default)
                {
                    categories.Add(legacyCat.Category.ID);
                }
            }

            // Build magnet URI with trackers
            var magnetUri = BuildMagnetUri(infoHash!, title!, trackers);

            // Default to Other category if none mapped
            if (!categories.Any())
            {
                categories.Add(TorznabCatType.Other.ID);
            }

            var release = new ReleaseInfo
            {
                Title = title,
                Guid = new Uri($"{SiteLink}e/{EventIdToNEvent(evt.Id, false)}"),
                Details = new Uri($"{SiteLink}e/{EventIdToNEvent(evt.Id)}"),
                MagnetUri = magnetUri,
                InfoHash = infoHash,
                Category = categories.Distinct().ToList(),
                PublishDate = DateTimeOffset.FromUnixTimeSeconds((long)evt.CreatedAt).DateTime,
                Size = size ?? 0,
                Description = evt.Content,
                Seeders = 1, // Nostr doesn't track seeders
                Peers = 0,
                Imdb = ParseImdbId(imdbId)
            };
            return release;
        }

        private static Uri BuildMagnetUri(string infoHash, string title, List<string> trackers)
        {
            var magnetParams = new List<string> { $"xt=urn:btih:{infoHash}", $"dn={Uri.EscapeDataString(title)}" };
            foreach (var tracker in trackers.Take(5))
            {
                magnetParams.Add($"tr={Uri.EscapeDataString(tracker)}");
            }

            return new Uri($"magnet:?{string.Join("&", magnetParams)}");
        }

        private static long? ParseImdbId(string? imdbId)
        {
            if (string.IsNullOrEmpty(imdbId))
            {
                return null;
            }

            var match = Regex.Match(imdbId, @"tt?(\d+)");
            if (match.Success && long.TryParse(match.Groups[1].Value, out var id))
            {
                return id;
            }

            return null;
        }
    }
}
