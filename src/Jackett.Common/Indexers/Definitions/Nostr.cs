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

                if (query.IsTVSearch)
                {
                    filter.Hashtags = new List<string> { "tv" };
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
                    logger.Info($"[{relayUri}]: {json}");
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

            string? title = null, infoHash = null, imdbId = null;
            long? size = null;
            var categories = new List<int>();
            var trackers = new List<string>();
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
                            var catPath = tag[1].Substring(5);
                            var mappedCats = MapCategory(catPath);
                            categories.AddRange(mappedCats);
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
                        // General tags - try to map to categories
                        var tagCats = MapTag(tag[1]);
                        categories.AddRange(tagCats);
                        break;
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

        private IEnumerable<int> MapCategory(string categoryPath)
        {
            var normalized = categoryPath.ToLowerInvariant();
            if (normalized.Contains("movie"))
            {
                if (normalized.Contains("4k") || normalized.Contains("uhd"))
                {
                    yield return TorznabCatType.MoviesUHD.ID;
                }
                else if (normalized.Contains("hd") || normalized.Contains("1080") || normalized.Contains("720"))
                {
                    yield return TorznabCatType.MoviesHD.ID;
                }
                else if (normalized.Contains("sd") || normalized.Contains("dvd"))
                {
                    yield return TorznabCatType.MoviesSD.ID;
                }
                else
                {
                    yield return TorznabCatType.Movies.ID;
                }
            }
            else if (normalized.Contains("tv") || normalized.Contains("series") || normalized.Contains("show"))
            {
                if (normalized.Contains("4k") || normalized.Contains("uhd"))
                {
                    yield return TorznabCatType.TVUHD.ID;
                }
                else if (normalized.Contains("hd") || normalized.Contains("1080") || normalized.Contains("720"))
                {
                    yield return TorznabCatType.TVHD.ID;
                }
                else if (normalized.Contains("sd"))
                {
                    yield return TorznabCatType.TVSD.ID;
                }
                else
                {
                    yield return TorznabCatType.TV.ID;
                }
            }
            else if (normalized.Contains("anime"))
            {
                yield return TorznabCatType.TVAnime.ID;
            }
            else if (normalized.Contains("audio") || normalized.Contains("music"))
            {
                if (normalized.Contains("lossless") || normalized.Contains("flac"))
                {
                    yield return TorznabCatType.AudioLossless.ID;
                }
                else if (normalized.Contains("audiobook"))
                {
                    yield return TorznabCatType.AudioAudiobook.ID;
                }
                else
                {
                    yield return TorznabCatType.Audio.ID;
                }
            }
            else if (normalized.Contains("software") || normalized.Contains("app"))
            {
                if (normalized.Contains("game"))
                {
                    yield return TorznabCatType.PCGames.ID;
                }
                else
                {
                    yield return TorznabCatType.PC.ID;
                }
            }
            else if (normalized.Contains("book") || normalized.Contains("ebook"))
            {
                yield return TorznabCatType.BooksEBook.ID;
            }
            else if (normalized.Contains("xxx") || normalized.Contains("adult"))
            {
                yield return TorznabCatType.XXX.ID;
            }
        }

        private IEnumerable<int> MapTag(string tag)
        {
            var normalized = tag.ToLowerInvariant();
            return normalized switch
            {
                "movie" or "movies" or "film" => new[]
                {
                    TorznabCatType.Movies.ID
                },
                "tv" or "television" or "series" or "show" => new[]
                {
                    TorznabCatType.TV.ID
                },
                "anime" => new[]
                {
                    TorznabCatType.TVAnime.ID
                },
                "music" or "audio" => new[]
                {
                    TorznabCatType.Audio.ID
                },
                "game" or "games" => new[]
                {
                    TorznabCatType.PCGames.ID
                },
                "software" or "app" or "apps" => new[]
                {
                    TorznabCatType.PC.ID
                },
                "book" or "books" or "ebook" or "ebooks" => new[]
                {
                    TorznabCatType.BooksEBook.ID
                },
                "4k" or "uhd" or "2160p" => new[]
                {
                    TorznabCatType.MoviesUHD.ID,
                    TorznabCatType.TVUHD.ID
                },
                "hd" or "1080p" or "720p" => new[]
                {
                    TorznabCatType.MoviesHD.ID,
                    TorznabCatType.TVHD.ID
                },
                _ => Enumerable.Empty<int>()
            };
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
