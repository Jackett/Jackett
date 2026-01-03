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
using Jackett.Common.Utils.Clients;
using NBitcoin.DataEncoders;
using NBitcoin.Secp256k1;
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

            // NIP-35 supports newznab categories via the "i" tag
            caps.Categories.AddCategoryMapping("video,movie", TorznabCatType.Movies);
            caps.Categories.AddCategoryMapping("video,movie,4k", TorznabCatType.MoviesUHD);
            caps.Categories.AddCategoryMapping("video,movie,hd", TorznabCatType.MoviesHD);
            caps.Categories.AddCategoryMapping("video,movie,sd", TorznabCatType.MoviesSD);
            caps.Categories.AddCategoryMapping("video,tv", TorznabCatType.TV);
            caps.Categories.AddCategoryMapping("video,tv,4k", TorznabCatType.TVUHD);
            caps.Categories.AddCategoryMapping("video,tv,hd", TorznabCatType.TVHD);
            caps.Categories.AddCategoryMapping("video,tv,sd", TorznabCatType.TVSD);
            caps.Categories.AddCategoryMapping("video,anime", TorznabCatType.TVAnime);
            caps.Categories.AddCategoryMapping("audio", TorznabCatType.Audio);
            caps.Categories.AddCategoryMapping("audio,music", TorznabCatType.Audio);
            caps.Categories.AddCategoryMapping("audio,lossless", TorznabCatType.AudioLossless);
            caps.Categories.AddCategoryMapping("audio,audiobook", TorznabCatType.AudioAudiobook);
            caps.Categories.AddCategoryMapping("software", TorznabCatType.PC);
            caps.Categories.AddCategoryMapping("software,game", TorznabCatType.PCGames);
            caps.Categories.AddCategoryMapping("ebook", TorznabCatType.BooksEBook);
            caps.Categories.AddCategoryMapping("book", TorznabCatType.Books);
            caps.Categories.AddCategoryMapping("other", TorznabCatType.Other);
            return caps;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            // Test the relay connection
            try
            {
                var testResults = await PerformQuery(new TorznabQuery { SearchTerm = "test" });
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

            var relayUri = new Uri(RelayUrl);
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                using var client = new ClientWebSocket();
                var receivedEvents = new List<NostrEvent>();
                var subscriptionId = Guid.NewGuid().ToString("n").Substring(0, 8);

                // Start the connection
                await client.ConnectAsync(relayUri, cts.Token);

                // Create filter for kind 2003 (NIP-35 torrents) with NIP-50 search
                var filter = new NostrFilter { Kinds = new List<int> { 2003 }, Limit = query.Limit == 0 ? 20 : query.Limit };
                if (query.SearchTerm.IsNotNullOrWhiteSpace() && query.IsSearch)
                {
                    filter.Search = query.SearchTerm;
                }

                // Map query categories to tcat or hashtag filters
                var (tcats, hashtags) = MapQueryToFilters(query);
                if (tcats.Count > 0)
                {
                    filter.Identifiers = tcats.Select(t => $"tcat:{t}").ToList();
                }

                if (hashtags.Count > 0)
                {
                    filter.Hashtags = hashtags;
                }

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
                                receivedEvents.Add(ev!);
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

                // Parse received events
                foreach (var evt in receivedEvents)
                {
                    try
                    {
                        var release = ParseNostrEvent(evt);
                        if (release != null)
                        {
                            releases.Add(release);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Warn($"Failed to parse Nostr event {evt.Id}: {ex.Message}");
                    }
                }

                logger.Debug(
                    $"Nostr search for '{query.SearchTerm}' returned {releases.Count} results from {receivedEvents.Count} events");
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

            return releases;
        }

        /// From internal <see cref="Bech32Encoder.ConvertBits"/>
        private byte[] ConvertBits(ReadOnlySpan<byte> data, int fromBits, int toBits, bool pad = true)
        {
            var acc = 0;
            var bits = 0;
            var maxv = (1 << toBits) - 1;
            var ret = new List<byte>(64);
            foreach (var value in data)
            {
                if ((value >> fromBits) > 0)
                    throw new FormatException("Invalid Bech32 string");
                acc = (acc << fromBits) | value;
                bits += fromBits;
                while (bits >= toBits)
                {
                    bits -= toBits;
                    ret.Add((byte)((acc >> bits) & maxv));
                }
            }

            if (pad)
            {
                if (bits > 0)
                {
                    ret.Add((byte)((acc << (toBits - bits)) & maxv));
                }
            }
            else if (bits >= fromBits || (byte)(((acc << (toBits - bits)) & maxv)) != 0)
            {
                throw new FormatException("Invalid Bech32 string");
            }

            return ret.ToArray();
        }

        private string EventIdToNEvent(string eventId)
        {
            var hex = new HexEncoder();
            // NIP-19 encoded entity (https://github.com/nostr-protocol/nips/blob/master/19.md)
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
            payload.AddRange(hex.DecodeData(eventId));
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

            var encodeBytes = payload.ToArray();
            var bits = ConvertBits(encodeBytes.AsSpan(), 8, 5);
            return enc.EncodeData(bits, Bech32EncodingType.BECH32);
        }

        private ReleaseInfo? ParseNostrEvent(NostrEvent evt)
        {
            if (evt.Tags.Count == 0 || string.IsNullOrEmpty(evt.Id) || string.IsNullOrEmpty(evt.Signature) ||
                string.IsNullOrEmpty(evt.Pubkey))
            {
                return null;
            }

            var hex = new HexEncoder();
            if (!SecpSchnorrSignature.TryCreate(hex.DecodeData(evt.Signature), out var sig))
            {
                throw new Exception("Invalid signature, not a valid schnorr signature");
            }

            if (!ECXOnlyPubKey.TryCreate(hex.DecodeData(evt.Pubkey), out var key))
            {
                throw new Exception("Invalid event, not a valid schnorr pubkey");
            }

            if (!key.SigVerifyBIP340(sig, hex.DecodeData(evt.Id)))
            {
                logger.Warn($"Invalid signature, skipping event {evt.Id}");
                return null;
            }

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
                        if (IsValidInfoHash(tag[1]))
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

            // Process categories: prefer tcat if set, otherwise parse legacy tags
            if (!string.IsNullOrEmpty(tcat))
            {
                // Use tcat exactly as-is
                var mappedCat = MapCategory(tcat);
                if (mappedCat.HasValue)
                {
                    categories.Add(mappedCat.Value);
                }
            }
            else if (legacyTags.Any())
            {
                // No tcat found, parse legacy tags into tcat format
                var legacyCat = MapLegacyTagsToCategory(legacyTags);
                if (legacyCat.HasValue)
                {
                    categories.Add(legacyCat.Value);
                }
            }

            // Must have at least title and info hash
            if (title.IsNullOrWhiteSpace() || infoHash.IsNullOrWhiteSpace())
            {
                return null;
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
                Guid = new Uri($"magnet:?xt=urn:btih:{infoHash}"),
                Details = new Uri($"{SiteLink}e/{EventIdToNEvent(evt.Id)}"),
                MagnetUri = magnetUri,
                InfoHash = infoHash,
                Category = categories.Distinct().ToList(),
                PublishDate = DateTimeOffset.FromUnixTimeSeconds((long)evt.CreatedAt).DateTime,
                Size = size ?? 0,
                Description = evt.Content,
                Seeders = 1, // Nostr doesn't track seeders
                Peers = 0,
                DownloadVolumeFactor = 0,
                UploadVolumeFactor = 1,
                Imdb = ParseImdbId(imdbId)
            };
            return release;
        }

        private static bool IsValidInfoHash(string hash)
        {
            // V1 info hash is 40 hex characters
            return Regex.IsMatch(hash, "^[a-fA-F0-9]{40}$");
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

        /// <summary>
        /// Maps a tcat path to Torznab category IDs.
        /// Uses Contains matching to handle tags in any order.
        /// Checks top-level categories first to avoid mismatches (e.g., porn,movies -> XXX not Movies).
        /// </summary>
        private static int? MapCategory(string tcat)
        {
            var parts = tcat.ToLowerInvariant().Split(',').Select(p => p.Trim()).ToHashSet();

            // Porn (check first - has subcategory "movies" that shouldn't match Video)
            if (parts.Contains("porn") || parts.Contains("xxx") || parts.Contains("adult"))
            {
                if (parts.Contains("pictures") || parts.Contains("imageset"))
                    return TorznabCatType.XXXImageSet.ID;
                return TorznabCatType.XXX.ID;
            }

            // Games (check before video - has subcategories)
            if (parts.Contains("games") || parts.Contains("game"))
            {
                if (parts.Contains("psx") || parts.Contains("playstation") || parts.Contains("ps4") || parts.Contains("ps5"))
                    return TorznabCatType.ConsolePS4.ID;
                if (parts.Contains("xbox"))
                    return TorznabCatType.ConsoleXBoxOne.ID;
                if (parts.Contains("wii") || parts.Contains("nintendo"))
                    return TorznabCatType.ConsoleWii.ID;
                if (parts.Contains("mac"))
                    return TorznabCatType.PCMac.ID;
                if (parts.Contains("ios"))
                    return TorznabCatType.PCMobileiOS.ID;
                if (parts.Contains("android"))
                    return TorznabCatType.PCMobileAndroid.ID;
                return TorznabCatType.PCGames.ID;
            }

            // Applications
            if (parts.Contains("applications") || parts.Contains("software") || parts.Contains("app"))
            {
                if (parts.Contains("mac"))
                    return TorznabCatType.PCMac.ID;
                if (parts.Contains("ios"))
                    return TorznabCatType.PCMobileiOS.ID;
                if (parts.Contains("android"))
                    return TorznabCatType.PCMobileAndroid.ID;
                return TorznabCatType.PC.ID;
            }

            // Audio
            if (parts.Contains("audio"))
            {
                if (parts.Contains("audiobooks") || parts.Contains("audiobook"))
                    return TorznabCatType.AudioAudiobook.ID;
                if (parts.Contains("flac") || parts.Contains("lossless"))
                    return TorznabCatType.AudioLossless.ID;
                return TorznabCatType.Audio.ID;
            }

            // Other (check before video for e-books, comics)
            if (parts.Contains("other"))
            {
                if (parts.Contains("e-books") || parts.Contains("ebooks") || parts.Contains("ebook"))
                    return TorznabCatType.BooksEBook.ID;
                if (parts.Contains("comics") || parts.Contains("comic"))
                    return TorznabCatType.BooksComics.ID;
                return TorznabCatType.Other.ID;
            }

            // Video - Movies
            if (parts.Contains("video"))
            {
                if (parts.Contains("movie") || parts.Contains("movies"))
                {
                    if (parts.Contains("4k") || parts.Contains("uhd"))
                        return TorznabCatType.MoviesUHD.ID;
                    if (parts.Contains("hd"))
                        return TorznabCatType.MoviesHD.ID;
                    if (parts.Contains("dvdr"))
                        return TorznabCatType.MoviesDVD.ID;
                    return TorznabCatType.Movies.ID;
                }

                if (parts.Contains("tv"))
                {
                    if (parts.Contains("4k") || parts.Contains("uhd"))
                        return TorznabCatType.TVUHD.ID;
                    if (parts.Contains("hd"))
                        return TorznabCatType.TVHD.ID;
                    return TorznabCatType.TV.ID;
                }

                // Generic video defaults to Movies
                return TorznabCatType.Movies.ID;
            }

            return null;
        }

        /// <summary>
        /// Maps legacy tags (v0 format before tcat proposal) to Torznab category.
        /// Uses the same matching logic as MapCategory by joining tags.
        /// </summary>
        private static int? MapLegacyTagsToCategory(List<string> tags) =>
            MapCategory(string.Join(",", tags));

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

        /// <summary>
        /// Maps a TorznabQuery to tcat and hashtag filters for Nostr relay queries.
        /// Returns exact tcat matches when available, otherwise falls back to hashtags.
        /// </summary>
        private static (List<string> Tcats, List<string> Hashtags) MapQueryToFilters(TorznabQuery query)
        {
            var tcats = new List<string>();
            var hashtags = new List<string>();
            if (query.Categories == null || query.Categories.Length == 0)
            {
                return (tcats, hashtags);
            }

            foreach (var cat in query.Categories)
            {
                var (tcat, tags) = MapCategoryToFilter(cat);
                if (tcat != null)
                {
                    tcats.Add(tcat);
                }
                else if (tags != null)
                {
                    hashtags.AddRange(tags);
                }
            }

            return (tcats.Distinct().ToList(), hashtags.Distinct().ToList());
        }

        /// <summary>
        /// Maps a category to either an exact tcat match or fallback hashtags.
        /// </summary>
        private static (string? Tcat, string[]? Hashtags) MapCategoryToFilter(int cat)
        {
            // Movies - exact tcat matches
            if (cat == TorznabCatType.MoviesHD.ID)
                return ("video,movie,hd", null);
            if (cat == TorznabCatType.MoviesUHD.ID)
                return ("video,movie,4k", null);
            if (cat == TorznabCatType.MoviesDVD.ID)
                return ("video,movie,dvdr", null);

            // Movies - hashtag fallback
            if (cat == TorznabCatType.Movies.ID ||
                cat == TorznabCatType.MoviesForeign.ID ||
                cat == TorznabCatType.MoviesOther.ID ||
                cat == TorznabCatType.MoviesSD.ID ||
                cat == TorznabCatType.MoviesBluRay.ID ||
                cat == TorznabCatType.Movies3D.ID ||
                cat == TorznabCatType.MoviesWEBDL.ID)
                return (null, new[] { "movie" });

            // TV - exact tcat matches
            if (cat == TorznabCatType.TVHD.ID)
                return ("video,tv,hd", null);
            if (cat == TorznabCatType.TVUHD.ID)
                return ("video,tv,4k", null);

            // TV - hashtag fallback
            if (cat == TorznabCatType.TV.ID ||
                cat == TorznabCatType.TVWEBDL.ID ||
                cat == TorznabCatType.TVForeign.ID ||
                cat == TorznabCatType.TVSD.ID ||
                cat == TorznabCatType.TVOther.ID ||
                cat == TorznabCatType.TVSport.ID ||
                cat == TorznabCatType.TVDocumentary.ID)
                return (null, new[] { "tv" });
            if (cat == TorznabCatType.TVAnime.ID)
                return (null, new[] { "anime" });

            // Audio - exact tcat matches
            if (cat == TorznabCatType.AudioLossless.ID)
                return ("audio,music,flac", null);
            if (cat == TorznabCatType.AudioAudiobook.ID)
                return ("audio,audiobooks", null);

            // Audio - hashtag fallback
            if (cat == TorznabCatType.Audio.ID || cat == TorznabCatType.AudioOther.ID)
                return (null, new[] { "audio" });
            if (cat == TorznabCatType.AudioMP3.ID ||
                cat == TorznabCatType.AudioVideo.ID ||
                cat == TorznabCatType.AudioForeign.ID)
                return (null, new[] { "audio", "music" });

            // PC - exact tcat matches
            if (cat == TorznabCatType.PCMac.ID)
                return ("applications,mac", null);
            if (cat == TorznabCatType.PCMobileiOS.ID)
                return ("applications,ios", null);
            if (cat == TorznabCatType.PCMobileAndroid.ID)
                return ("applications,android", null);
            if (cat == TorznabCatType.PCGames.ID)
                return ("games,pc", null);

            // PC - hashtag fallback
            if (cat == TorznabCatType.PC.ID ||
                cat == TorznabCatType.PC0day.ID ||
                cat == TorznabCatType.PCISO.ID ||
                cat == TorznabCatType.PCMobileOther.ID)
                return (null, new[] { "software" });

            // Console - exact tcat matches
            if (cat == TorznabCatType.ConsolePSP.ID ||
                cat == TorznabCatType.ConsolePS3.ID ||
                cat == TorznabCatType.ConsolePSVita.ID ||
                cat == TorznabCatType.ConsolePS4.ID)
                return ("games,psx", null);
            if (cat == TorznabCatType.ConsoleWii.ID ||
                cat == TorznabCatType.ConsoleWiiware.ID ||
                cat == TorznabCatType.ConsoleWiiU.ID)
                return ("games,wii", null);
            if (cat == TorznabCatType.ConsoleXBox.ID ||
                cat == TorznabCatType.ConsoleXBox360.ID ||
                cat == TorznabCatType.ConsoleXBoxOne.ID)
                return ("games,xbox", null);

            // Console - hashtag fallback
            if (cat == TorznabCatType.Console.ID ||
                cat == TorznabCatType.ConsoleNDS.ID ||
                cat == TorznabCatType.ConsoleOther.ID ||
                cat == TorznabCatType.Console3DS.ID)
                return (null, new[] { "games" });

            // XXX - exact tcat matches
            if (cat == TorznabCatType.XXXImageSet.ID)
                return ("porn,pictures", null);

            // XXX - hashtag fallback
            if (cat == TorznabCatType.XXX.ID ||
                cat == TorznabCatType.XXXx264.ID ||
                cat == TorznabCatType.XXXUHD.ID ||
                cat == TorznabCatType.XXXPack.ID ||
                cat == TorznabCatType.XXXOther.ID ||
                cat == TorznabCatType.XXXSD.ID ||
                cat == TorznabCatType.XXXWEBDL.ID ||
                cat == TorznabCatType.XXXDVD.ID)
                return (null, new[] { "xxx" });

            // Books - exact tcat matches
            if (cat == TorznabCatType.BooksEBook.ID)
                return ("other,e-books", null);
            if (cat == TorznabCatType.BooksComics.ID)
                return ("other,comics", null);

            // Books - hashtag fallback
            if (cat == TorznabCatType.Books.ID ||
                cat == TorznabCatType.BooksMags.ID ||
                cat == TorznabCatType.BooksTechnical.ID ||
                cat == TorznabCatType.BooksOther.ID ||
                cat == TorznabCatType.BooksForeign.ID)
                return (null, new[] { "ebook" });

            // Other - hashtag fallback only
            if (cat == TorznabCatType.Other.ID ||
                cat == TorznabCatType.OtherMisc.ID ||
                cat == TorznabCatType.OtherHashed.ID)
                return (null, new[] { "other" });

            return (null, null);
        }
    }
}
