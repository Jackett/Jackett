using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Jackett.Common.Indexers;
using Jackett.Common.Models;
using Jackett.Common.Models.Config;
using Jackett.Common.Services.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;
using Newtonsoft.Json;
using NLog;

namespace Jackett.Common.Services.Cache
{
    public class MongoDBCacheService : ICacheService
    {
        private readonly Logger _logger;
        private string _cacheconnectionString;
        private readonly ServerConfig _serverConfig;
        private IMongoDatabase _database;
        private readonly SHA256Managed _sha256 = new SHA256Managed();
        private readonly object _dbLock = new object();

        public MongoDBCacheService(Logger logger, string cacheconnectionString, ServerConfig serverConfig)
        {
            _logger = logger;
            _cacheconnectionString = cacheconnectionString;
            _serverConfig = serverConfig;
            Initialize();
        }

        public void Initialize()
        {
            var client = new MongoClient("mongodb://" + _cacheconnectionString);
            _database = client.GetDatabase("CacheDatabase");
            var trackerCaches = _database.GetCollection<BsonDocument>("TrackerCaches");
            var trackerCacheQueries = _database.GetCollection<BsonDocument>("TrackerCacheQueries");
            var releaseInfos = _database.GetCollection<BsonDocument>("ReleaseInfos");

            var releaseInfosindexModel = new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("TrackerCacheQueryId"));
            releaseInfos.Indexes.CreateOne(releaseInfosindexModel);

            var trackerCacheQueriesIndexes = Builders<BsonDocument>.IndexKeys
                                                                   .Ascending("QueryHash")
                                                                   .Ascending("TrackerCacheId");

            var trackerCacheQueriesIndexModel = new CreateIndexModel<BsonDocument>(trackerCacheQueriesIndexes);
            trackerCacheQueries.Indexes.CreateOne(trackerCacheQueriesIndexModel);

            var indextrackerCacheQueries = new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("QueryHash"));
            trackerCacheQueries.Indexes.CreateOne(indextrackerCacheQueries);
            indextrackerCacheQueries = new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("TrackerCacheId"));
            trackerCacheQueries.Indexes.CreateOne(indextrackerCacheQueries);

            var trackerCachesIndexes = new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("TrackerId"));
            trackerCaches.Indexes.CreateOne(trackerCachesIndexes);
            trackerCachesIndexes = new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("TrackerCacheId"));
            trackerCaches.Indexes.CreateOne(trackerCachesIndexes);

            _logger.Info("Cache MongoDB Initialized");
        }

        public void CacheResults(IIndexer indexer, TorznabQuery query, List<ReleaseInfo> releases)
        {
            if (query.IsTest)
                return;

            //lock (_dbLock)TODO not needs
            {
                try
                {
                    var trackerCacheId = GetOrAddTrackerCache(indexer);
                    var trackerCacheQueryId = AddTrackerCacheQuery(trackerCacheId, query);

                    var releaseInfosCollection = _database.GetCollection<BsonDocument>("ReleaseInfos");

                    foreach (var release in releases)
                    {
                        var document = new BsonDocument
                        {
                            { "TrackerCacheQueryId", trackerCacheQueryId },
                            { "Title", (BsonValue)release.Title ?? BsonString.Empty },
                            { "Guid", (BsonValue)release.Guid?.ToString() ?? BsonNull.Value },
                            { "Link", (BsonValue)release.Link?.ToString() ?? BsonNull.Value },
                            { "Details", (BsonValue)release.Details?.ToString()  ?? BsonNull.Value},
                            { "PublishDate", (BsonValue)release.PublishDate ?? BsonNull.Value },
                            { "Category", new BsonArray(release.Category ?? new List<int>()) },
                            { "Size", (BsonValue)release.Size ?? BsonNull.Value },
                            { "Files", (BsonValue)release.Files ?? BsonNull.Value },
                            { "Grabs", (BsonValue)release.Grabs ?? BsonNull.Value },
                            { "Description", (BsonValue)release.Description ?? BsonString.Empty},
                            { "RageID", (BsonValue)release.RageID ?? BsonNull.Value },
                            { "TVDBId", (BsonValue)release.TVDBId ?? BsonNull.Value },
                            { "Imdb", (BsonValue)release.Imdb ?? BsonNull.Value },
                            { "TMDb", (BsonValue)release.TMDb ?? BsonNull.Value },
                            { "TVMazeId", (BsonValue)release.TVMazeId ?? BsonNull.Value },
                            { "TraktId", (BsonValue)release.TraktId ?? BsonNull.Value },
                            { "DoubanId", (BsonValue)release.DoubanId ?? BsonNull.Value },
                            { "Genres", new BsonArray(release.Genres ?? new List<string>()) },
                            { "Languages", new BsonArray(release.Languages ?? new List<string>()) },
                            { "Subs", new BsonArray(release.Subs ?? new List<string>()) },
                            { "Year", (BsonValue)release.Year ?? BsonNull.Value },
                            { "Author", (BsonValue)release.Author ?? BsonString.Empty },
                            { "BookTitle", (BsonValue)release.BookTitle ?? BsonString.Empty },
                            { "Publisher", (BsonValue)release.Publisher ?? BsonString.Empty },
                            { "Artist", (BsonValue)release.Artist ?? BsonString.Empty },
                            { "Album", (BsonValue)release.Album ?? BsonString.Empty },
                            { "Label", (BsonValue)release.Label ?? BsonString.Empty },
                            { "Track", (BsonValue)release.Track ?? BsonString.Empty },
                            { "Seeders", (BsonValue)release.Seeders ?? BsonNull.Value },
                            { "Peers", (BsonValue)release.Peers ?? BsonNull.Value },
                            { "Poster", (BsonValue)release.Poster?.ToString() ?? BsonNull.Value },
                            { "InfoHash", (BsonValue)release.InfoHash ?? BsonString.Empty },
                            { "MagnetUri", (BsonValue)release.MagnetUri?.ToString() ?? BsonNull.Value },
                            { "MinimumRatio", (BsonValue)release.MinimumRatio ?? BsonNull.Value },
                            { "MinimumSeedTime", (BsonValue)release.MinimumSeedTime ?? BsonNull.Value },
                            { "DownloadVolumeFactor", (BsonValue)release.DownloadVolumeFactor ?? BsonNull.Value },
                            { "UploadVolumeFactor", (BsonValue)release.UploadVolumeFactor ?? BsonNull.Value }
                        };
                        releaseInfosCollection.InsertOne(document);
                    }
                    if (_logger.IsDebugEnabled)
                        _logger.Debug("CACHE CacheResults / Indexer: {0} / Added: {1} releases", indexer.Id, releases.Count);

                    PruneCacheByMaxResultsPerIndexer(indexer.Id); // remove old results if we exceed the maximum limit
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Failed CacheResults in indexer {0}", indexer);
                }
            }
        }

        private ObjectId GetOrAddTrackerCache(IIndexer indexer)
        {
            var trackerCachesCollection = _database.GetCollection<BsonDocument>("TrackerCaches");
            var filter = Builders<BsonDocument>.Filter.Eq("TrackerId", indexer.Id);
            var trackerCache = trackerCachesCollection.Find(filter).FirstOrDefault();

            if (trackerCache == null)
            {
                var document = new BsonDocument
                {
                    { "TrackerId", indexer.Id }, { "TrackerName", indexer.Name }, { "TrackerType", indexer.Type }
                };
                trackerCachesCollection.InsertOne(document);
                return document["_id"].AsObjectId;
            }
            return trackerCache["_id"].AsObjectId;
        }

        private ObjectId AddTrackerCacheQuery(ObjectId trackerCacheId, TorznabQuery query)
        {
            var trackerCacheQueriesCollection = _database.GetCollection<BsonDocument>("TrackerCacheQueries");
            var document = new BsonDocument
            {
                { "TrackerCacheId", trackerCacheId }, { "QueryHash", GetQueryHash(query) }, { "Created", DateTime.Now }
            };
            trackerCacheQueriesCollection.InsertOne(document);
            return document["_id"].AsObjectId;
        }

        public List<ReleaseInfo> Search(IIndexer indexer, TorznabQuery query)
        {
            if (_serverConfig.CacheType == CacheType.Disabled)
                return null;

            PruneCacheByTtl();

            var queryHash = GetQueryHash(query);

            var trackerCaches = _database.GetCollection<BsonDocument>("TrackerCaches")
                                         .Find(Builders<BsonDocument>.Filter.Eq("TrackerId", indexer.Id))
                                         .ToList();

            var trackerCacheIds = trackerCaches.Select(doc => doc.GetValue("_id").AsObjectId).ToList();

            var trackerCacheQueries = _database.GetCollection<BsonDocument>("TrackerCacheQueries")
                                               .Find(Builders<BsonDocument>.Filter.And(
                                                         Builders<BsonDocument>.Filter.Eq("QueryHash", queryHash),
                                                         Builders<BsonDocument>.Filter.In("TrackerCacheId", trackerCacheIds)
                                                         ))
                                               .ToList();

            var trackerCacheQueryIds = trackerCacheQueries.Select(q => q["_id"].AsObjectId).ToList();

            var results = _database.GetCollection<BsonDocument>("ReleaseInfos")
                                   .Find(Builders<BsonDocument>.Filter.And(
                                             Builders<BsonDocument>.Filter.In("TrackerCacheQueryId", trackerCacheQueryIds)))
                                   .ToList();

            if (results.Count > 0)
            {
                if (_logger.IsDebugEnabled)
                    _logger.Debug("CACHE Search Hit / Indexer: {0} / Found: {1} releases", indexer.Id, results.Count);

                return results.Select(ConvertBsonToReleaseInfo).ToList();
            }
            return null;
        }

        private ReleaseInfo ConvertBsonToReleaseInfo(BsonDocument doc)
        {
            return new ReleaseInfo
            {
                Title = doc["Title"].IsBsonNull ? null : doc["Title"].AsString,
                Guid = doc["Guid"].IsBsonNull || !doc["Guid"].IsString ? null : new Uri(doc["Guid"].AsString),
                Link = doc["Link"].IsBsonNull || !doc["Link"].IsString ? null : new Uri(doc["Link"].AsString),
                Details = doc["Details"].IsBsonNull || !doc["Details"].IsString ? null : new Uri(doc["Details"].AsString),
                PublishDate = doc["PublishDate"].ToLocalTime(),
                Category = doc["Category"].AsBsonArray.Select(c => c.AsInt32).ToList(),
                Size = doc["Size"].IsInt64 ? doc["Size"].AsInt64 : (long?)null,
                Files = doc["Files"].IsInt64 ? doc["Files"].AsInt64 : (long?)null,
                Grabs = doc["Grabs"].IsInt64 ? doc["Grabs"].AsInt64 : (long?)null,
                Description = doc["Description"].AsString,
                RageID = doc["RageID"].IsInt64 ? doc["RageID"].AsInt64 : (long?)null,
                TVDBId = doc["TVDBId"].IsInt64 ? doc["TVDBId"].AsInt64 : (long?)null,
                Imdb = doc["Imdb"].IsInt64 ? doc["Imdb"].AsInt64 : (long?)null,
                TMDb = doc["TMDb"].IsInt64 ? doc["TMDb"].AsInt64 : (long?)null,
                TVMazeId = doc["TVMazeId"].IsInt64 ? doc["TVMazeId"].AsInt64 : (long?)null,
                TraktId = doc["TraktId"].IsInt64 ? doc["TraktId"].AsInt64 : (long?)null,
                DoubanId = doc["DoubanId"].IsInt64 ? doc["DoubanId"].AsInt64 : (long?)null,
                Genres = doc["Genres"].AsBsonArray.Select(g => g.AsString).ToList(),
                Languages = doc["Languages"].AsBsonArray.Select(l => l.AsString).ToList(),
                Subs = doc["Subs"].AsBsonArray.Select(s => s.AsString).ToList(),
                Year = doc["Year"].IsInt64 ? doc["Year"].AsInt64 : (long?)null,
                Author = doc["Author"].IsBsonNull ? null : doc["Author"].AsString,
                BookTitle = doc["BookTitle"].IsBsonNull ? null : doc["BookTitle"].AsString,
                Publisher = doc["Publisher"].IsBsonNull ? null : doc["Publisher"].AsString,
                Artist = doc["Artist"].IsBsonNull ? null : doc["Artist"].AsString,
                Album = doc["Album"].IsBsonNull ? null : doc["Album"].AsString,
                Label = doc["Label"].IsBsonNull ? null : doc["Label"].AsString,
                Track = doc["Track"].IsBsonNull ? null : doc["Track"].AsString,
                Seeders = doc["Seeders"].IsInt64 ? doc["Seeders"].AsInt64 : (long?)null,
                Peers = doc["Peers"].IsInt64 ? doc["Peers"].AsInt64 : (long?)null,
                Poster = doc["Poster"].IsBsonNull || !doc["Poster"].IsString ? null : new Uri(doc["Poster"].AsString),
                InfoHash = doc["InfoHash"].IsBsonNull ? null : doc["InfoHash"].AsString,
                MagnetUri = doc["MagnetUri"].IsBsonNull || !doc["MagnetUri"].IsString ? null : new Uri(doc["MagnetUri"].AsString),
                MinimumRatio = doc["MinimumRatio"].IsDouble ? doc["MinimumRatio"].AsDouble : (double?)null,
                MinimumSeedTime = doc["MinimumSeedTime"].IsInt64 ? doc["MinimumSeedTime"].AsInt64 : (long?)null,
                DownloadVolumeFactor =
                                      doc["DownloadVolumeFactor"].IsDouble ? doc["DownloadVolumeFactor"].AsDouble : (double?)null,
                UploadVolumeFactor = doc["UploadVolumeFactor"].IsDouble
                                      ? doc["UploadVolumeFactor"].AsDouble
                                      : (double?)null
            };
        }

        public IReadOnlyList<TrackerCacheResult> GetCachedResults()
        {
            if (_serverConfig.CacheType == CacheType.Disabled)
                return Array.Empty<TrackerCacheResult>();

            PruneCacheByTtl(); // remove expired results

            var releaseInfos = _database.GetCollection<BsonDocument>("ReleaseInfos");

            var results = releaseInfos.Aggregate()
                                      .Lookup("TrackerCacheQueries", "TrackerCacheQueryId", "_id", "TrackerCacheQuery")
                                      .Unwind("TrackerCacheQuery")
                                      .Lookup("TrackerCaches", "TrackerCacheQuery.TrackerCacheId", "_id", "TrackerCache")
                                      .Unwind("TrackerCache").SortByDescending(doc => doc["PublishDate"]).Limit(_serverConfig.CacheMaxResultsPerIndexer)
                                      .ToList();
            if (_logger.IsDebugEnabled)
                _logger.Debug("CACHE GetCachedResults / Results: {0} (cache may contain more results)", results.Count);

            PrintCacheStatus();

            return results.Select(doc =>
            {
                var releaseInfo = ConvertBsonToReleaseInfo(doc);

                return new TrackerCacheResult(
                    new ReleaseInfo()
                    {
                        // Initialize the properties of the base class (ReleaseInfo) manually
                        Title = releaseInfo.Title,
                        Guid = releaseInfo.Guid,
                        Link = releaseInfo.Link,
                        Details = releaseInfo.Details,
                        PublishDate = releaseInfo.PublishDate,
                        Category = releaseInfo.Category,
                        Size = releaseInfo.Size,
                        Files = releaseInfo.Files,
                        Grabs = releaseInfo.Grabs,
                        Description = releaseInfo.Description,
                        RageID = releaseInfo.RageID,
                        TVDBId = releaseInfo.TVDBId,
                        Imdb = releaseInfo.Imdb,
                        TMDb = releaseInfo.TMDb,
                        TVMazeId = releaseInfo.TVMazeId,
                        TraktId = releaseInfo.TraktId,
                        DoubanId = releaseInfo.DoubanId,
                        Genres = releaseInfo.Genres,
                        Languages = releaseInfo.Languages,
                        Subs = releaseInfo.Subs,
                        Year = releaseInfo.Year,
                        Author = releaseInfo.Author,
                        BookTitle = releaseInfo.BookTitle,
                        Publisher = releaseInfo.Publisher,
                        Artist = releaseInfo.Artist,
                        Album = releaseInfo.Album,
                        Label = releaseInfo.Label,
                        Track = releaseInfo.Track,
                        Seeders = releaseInfo.Seeders,
                        Peers = releaseInfo.Peers,
                        Poster = releaseInfo.Poster,
                        InfoHash = releaseInfo.InfoHash,
                        MagnetUri = releaseInfo.MagnetUri,
                        MinimumRatio = releaseInfo.MinimumRatio,
                        MinimumSeedTime = releaseInfo.MinimumSeedTime,
                        DownloadVolumeFactor = releaseInfo.DownloadVolumeFactor,
                        UploadVolumeFactor = releaseInfo.UploadVolumeFactor,
                        Origin = releaseInfo.Origin
                    })
                {
                    Tracker = doc["TrackerCache"]["TrackerName"].AsString,
                    TrackerId = doc["TrackerCache"]["TrackerId"].AsString,
                    TrackerType = doc["TrackerCache"]["TrackerType"].AsString,
                    FirstSeen = doc["TrackerCacheQuery"]["Created"].ToLocalTime()
                };
            }).ToList();
        }

        public void CleanIndexerCache(IIndexer indexer)
        {
            if (indexer == null)
            {
                if (_logger.IsDebugEnabled)
                    _logger.Debug("Indexer is null, skipping cache cleaning.");

                return;
            }

            var trackerCachesCollection = _database.GetCollection<BsonDocument>("TrackerCaches");
            var trackerCacheQueriesCollection = _database.GetCollection<BsonDocument>("TrackerCacheQueries");
            var releaseInfosCollection = _database.GetCollection<BsonDocument>("ReleaseInfos");

            var trackerCachesFilter = Builders<BsonDocument>.Filter.Eq("TrackerId", indexer.Id);
            var trackerCachesDocs = trackerCachesCollection.Find(trackerCachesFilter).ToList();

            if (!trackerCachesDocs.Any())
            {
                if (_logger.IsDebugEnabled)
                    _logger.Debug("No TrackerCaches documents found for indexer {0}, skipping cache cleaning.", indexer.Id);

                return;
            }

            var trackerCachesIds = trackerCachesDocs.Select(doc => doc["_id"].AsObjectId).ToList();
            var trackerCacheQueriesFilter = Builders<BsonDocument>.Filter.In("TrackerCacheId", trackerCachesIds);
            var trackerCacheQueriesDocs = trackerCacheQueriesCollection.Find(trackerCacheQueriesFilter).ToList();

            if (trackerCacheQueriesDocs.Any())
            {
                var trackerCacheQueryIds = trackerCacheQueriesDocs.Select(doc => doc["_id"].AsObjectId).ToList();
                var releaseInfosFilter = Builders<BsonDocument>.Filter.In("TrackerCacheQueryId", trackerCacheQueryIds);
                var deleteReleaseInfosResult = releaseInfosCollection.DeleteMany(releaseInfosFilter);
                if (_logger.IsDebugEnabled)
                    _logger.Debug("Deleted {0} documents from ReleaseInfos for indexer {1}", deleteReleaseInfosResult.DeletedCount, indexer.Id);

                var deleteTrackerCacheQueriesResult = trackerCacheQueriesCollection.DeleteMany(trackerCacheQueriesFilter);
                if (_logger.IsDebugEnabled)
                    _logger.Debug("Deleted {0} documents from TrackerCacheQueries for indexer {1}", deleteTrackerCacheQueriesResult.DeletedCount, indexer.Id);
            }
            else
            {
                if (_logger.IsDebugEnabled)
                    _logger.Debug("No TrackerCacheQueries documents found for TrackerCaches of indexer {0}", indexer.Id);
            }
            var deleteTrackerCachesResult = trackerCachesCollection.DeleteMany(trackerCachesFilter);
            if (_logger.IsDebugEnabled)
                _logger.Debug("Deleted {0} documents from TrackerCaches for indexer {1}", deleteTrackerCachesResult.DeletedCount, indexer.Id);
        }

        public void CleanCache()
        {
            //lock (_dbLock)TODO not needs
            {
                _database.DropCollection("ReleaseInfos");
                _database.DropCollection("TrackerCaches");
                _database.DropCollection("TrackerCacheQueries");
            }
        }

        public TimeSpan CacheTTL => TimeSpan.FromSeconds(_serverConfig.CacheTtl);

        public void UpdateCacheConnectionString(string cacheconnectionString)
        {
            try
            {
                lock (_dbLock)
                {
                    if (string.IsNullOrEmpty(cacheconnectionString) || !Regex.IsMatch(cacheconnectionString,
                            @"(?:(?<username>[^:@\/]+):(?<password>[^@\/]+)@)?(?<hosts>(?:[a-zA-Z0-9.-]+(?::\d{2,5})?(?:,)?)+)(?:\/(?<database>[a-zA-Z0-9_-]+))?(?:\?(?<options>.*))?$"))
                        throw new Exception("Cache Connection String: Is Empty or Bad name. Example: localhost:27017");

                    if (_cacheconnectionString != cacheconnectionString)
                    {
                        _cacheconnectionString = cacheconnectionString;
                        Initialize();
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed UpdateConnectionString MongoDB");
                throw new Exception("Failed UpdateConnectionString MongoDB");
            }
        }

        private string GetQueryHash(TorznabQuery query)
        {
            var json = GetSerializedQuery(query);
            return BitConverter.ToString(_sha256.ComputeHash(Encoding.UTF8.GetBytes(json)));
        }
        private static string GetSerializedQuery(TorznabQuery query)
        {
            var json = JsonConvert.SerializeObject(query);
            json = json.Replace("\"SearchTerm\":null", "\"SearchTerm\":\"\"");
            return json;
        }

        private void PruneCacheByTtl()
        {
            if (_serverConfig.CacheTtl <= 0)
            {
                if (_logger.IsDebugEnabled)
                    _logger.Debug("Cache TTL is disabled or set to a non-positive value, skipping pruning.");

                return;
            }

            //lock (_dbLock)TODO not needs
            {
                var expirationDate = DateTime.Now.AddSeconds(-_serverConfig.CacheTtl);

                var trackerCacheQueriesCollection = _database.GetCollection<BsonDocument>("TrackerCacheQueries");
                var releaseInfosCollection = _database.GetCollection<BsonDocument>("ReleaseInfos");
                var trackerCachesCollection = _database.GetCollection<BsonDocument>("TrackerCaches");

                var trackerCacheQueryFilter = Builders<BsonDocument>.Filter.Lt("Created", expirationDate);
                var expiredTrackerCacheQueryDocs = trackerCacheQueriesCollection.Find(trackerCacheQueryFilter).ToList();

                if (!expiredTrackerCacheQueryDocs.Any())
                {
                    if (_logger.IsDebugEnabled)
                        _logger.Debug("No expired documents found in TrackerCacheQueries for pruning.");

                    return;
                }
                var expiredTrackerCacheQueryIds = expiredTrackerCacheQueryDocs.Select(doc => doc["_id"].AsObjectId).ToList();
                var releaseInfoFilter = Builders<BsonDocument>.Filter.In("TrackerCacheQueryId", expiredTrackerCacheQueryIds);
                var deleteResult1 = releaseInfosCollection.DeleteMany(releaseInfoFilter);
                var expiredTrackerCacheIds =
                    expiredTrackerCacheQueryDocs.Select(doc => doc["TrackerCacheId"].AsObjectId).ToList();
                var trackerCachesFilter = Builders<BsonDocument>.Filter.In("_id", expiredTrackerCacheIds);
                var deleteResult2 = trackerCachesCollection.DeleteMany(trackerCachesFilter);
                var deleteResult3 = trackerCacheQueriesCollection.DeleteMany(trackerCacheQueryFilter);
                if (_logger.IsDebugEnabled)
                {
                    _logger.Debug("Pruned {0} documents from ReleaseInfos", deleteResult1.DeletedCount);
                    _logger.Debug("Pruned {0} documents from TrackerCaches", deleteResult2.DeletedCount);
                    _logger.Debug("Pruned {0} documents from TrackerCacheQueries", deleteResult3.DeletedCount);
                    PrintCacheStatus();
                }
            }
        }

        private void PruneCacheByMaxResultsPerIndexer(string trackerId)
        {
            var trackerCachesCollection = _database.GetCollection<BsonDocument>("TrackerCaches");
            var trackerCacheQueriesCollection = _database.GetCollection<BsonDocument>("TrackerCacheQueries");
            var releaseInfosCollection = _database.GetCollection<BsonDocument>("ReleaseInfos");
            var trackerCachesFilter = Builders<BsonDocument>.Filter.Eq("TrackerId", trackerId);
            var trackerCaches = trackerCachesCollection.Find(trackerCachesFilter).ToList();

            if (!trackerCaches.Any())
            {
                if (_logger.IsDebugEnabled)
                    _logger.Debug("No TrackerCaches documents found for tracker {0}", trackerId);

                return;
            }
            var trackerCacheIds = trackerCaches.Select(tc => tc["_id"].AsObjectId).ToList();
            var trackerCacheQueriesFilter = Builders<BsonDocument>.Filter.In("TrackerCacheId", trackerCacheIds);
            var trackerCacheQueries = trackerCacheQueriesCollection.Find(trackerCacheQueriesFilter).ToList();

            if (!trackerCacheQueries.Any())
            {
                if (_logger.IsDebugEnabled)
                    _logger.Debug("No TrackerCacheQueries documents found for TrackerId {0}", trackerId);

                return;
            }
            var trackerCacheQueryIds = trackerCacheQueries.Select(tcq => tcq["_id"].AsObjectId).ToList();
            var releaseInfosFilter = Builders<BsonDocument>.Filter.In("TrackerCacheQueryId", trackerCacheQueryIds);
            var releaseInfos = releaseInfosCollection.Find(releaseInfosFilter).ToList();

            var totalResultsCount = releaseInfos.Count;

            if (totalResultsCount <= _serverConfig.CacheMaxResultsPerIndexer)
            {
                if (_logger.IsDebugEnabled)
                    _logger.Debug("Total results count {0} is within the limit {1}", totalResultsCount, _serverConfig.CacheMaxResultsPerIndexer);

                return;
            }
            var prunedCounter = 0;

            while (totalResultsCount > _serverConfig.CacheMaxResultsPerIndexer)
            {
                var latestReleaseInfo = releaseInfos.Last();
                totalResultsCount--;
                var deleteReleaseInfoFilter = Builders<BsonDocument>.Filter.Eq("_id", latestReleaseInfo["_id"].AsObjectId);
                releaseInfosCollection.DeleteOne(deleteReleaseInfoFilter);

                releaseInfos.RemoveAt(releaseInfos.Count - 1);
                prunedCounter++;
            }
            if (_logger.IsDebugEnabled)
            {
                _logger.Debug("CACHE PruneCacheByMaxResultsPerIndexer / Tracker: {0} / Pruned release infos: {1}", trackerId, prunedCounter);
                PrintCacheStatus();
            }
        }

        private void PrintCacheStatus()
        {
            var releaseInfosCollection = _database.GetCollection<BsonDocument>("ReleaseInfos");
            var releaseInfosCount = releaseInfosCollection.CountDocuments(Builders<BsonDocument>.Filter.Empty);
            _logger.Info("CACHE Status / Total cached results: {0} documents", releaseInfosCount);
        }
        public void ClearCacheConnectionString()
        {
            _cacheconnectionString = string.Empty;
        }

        public string GetCacheConnectionString { get => _cacheconnectionString; }
    }
}
