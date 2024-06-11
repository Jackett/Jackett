using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Dapper;
using Jackett.Common.Indexers;
using Jackett.Common.Models;
using Jackett.Common.Models.Config;
using Jackett.Common.Services.Interfaces;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using NLog;

namespace Jackett.Common.Services.Cache
{
    public class SQLiteCacheService : ICacheService
    {
        private readonly Logger _logger;
        private string _cacheconnectionString;
        private readonly ServerConfig _serverConfig;
        private readonly SHA256Managed _sha256 = new SHA256Managed();
        private readonly object _dbLock = new object();

        public SQLiteCacheService(Logger logger, string cacheconnectionString, ServerConfig serverConfig)
        {
            _logger = logger;
            _cacheconnectionString = cacheconnectionString;
            _serverConfig = serverConfig;
        }

        public void Initialize()
        {
            try
            {
                using (var connection = new SqliteConnection("Data Source=" + GetConnectionString(_cacheconnectionString)))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = @"
        CREATE TABLE IF NOT EXISTS TrackerCaches (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            TrackerId UNIQUE,
            TrackerName TEXT,
            TrackerType TEXT
        );

        CREATE TABLE IF NOT EXISTS TrackerCacheQueries (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            TrackerCacheId INTEGER,
            QueryHash TEXT,
            Created TEXT,
            FOREIGN KEY(TrackerCacheId) REFERENCES TrackerCaches(Id)
        );

        CREATE TABLE IF NOT EXISTS ReleaseInfos (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Title TEXT,
            Guid TEXT,
            Link TEXT,
            Details TEXT,
            PublishDate TEXT,
            Category TEXT,
            Size INTEGER,
            Files INTEGER,
            Grabs INTEGER,
            Description TEXT,
            RageID INTEGER,
            TVDBId INTEGER,
            Imdb INTEGER,
            TMDb INTEGER,
            TVMazeId INTEGER,
            TraktId INTEGER,
            DoubanId INTEGER,
            Genres TEXT,
            Languages TEXT,
            Subs TEXT,
            Year INTEGER,
            Author TEXT,
            BookTitle TEXT,
            Publisher TEXT,
            Artist TEXT,
            Album TEXT,
            Label TEXT,
            Track TEXT,
            Seeders INTEGER,
            Peers INTEGER,
            Poster TEXT,
            InfoHash TEXT,
            MagnetUri TEXT,
            MinimumRatio REAL,
            MinimumSeedTime INTEGER,
            DownloadVolumeFactor REAL,
            UploadVolumeFactor REAL
        );
        CREATE TABLE IF NOT EXISTS TrackerCacheQueryReleaseInfos (
            TrackerCacheQueryId INTEGER,
            ReleaseInfoId INTEGER,
            FOREIGN KEY(TrackerCacheQueryId) REFERENCES TrackerCacheQueries(Id),
            FOREIGN KEY(ReleaseInfoId) REFERENCES ReleaseInfos(Id)
        );";

                    command.ExecuteNonQuery();
                }
                _logger.Info("Cache SQLite Initialized");
            }
            catch (Exception e)
            {
                _logger.Error("Cache SQLite Initialize error: {0}", e.Message);
            }
        }

        public void CacheResults(IIndexer indexer, TorznabQuery query, List<ReleaseInfo> releases)
        {
            if (query.IsTest)
                return;

            lock (_dbLock)
            {
                try
                {
                    using (var connection = new SqliteConnection("Data Source=" + GetConnectionString(_cacheconnectionString)))
                    {
                        connection.Open();
                        using (var transaction = connection.BeginTransaction())
                        {
                            var trackerCacheId = GetOrAddTrackerCache(connection, indexer);
                            var trackerCacheQueryId = AddTrackerCacheQuery(connection, trackerCacheId, query);

                            // Вставка релизов в ReleaseInfos
                            var sqlInsertReleaseInfos = @"
                        INSERT INTO ReleaseInfos 
                        (
                            Title, Guid, Link, Details, PublishDate, Category, Size, Files, Grabs, 
                            Description, RageID, TVDBId, Imdb, TMDb, TVMazeId, TraktId, DoubanId, Genres, Languages, 
                            Subs, Year, Author, BookTitle, Publisher, Artist, Album, Label, Track, Seeders, Peers, 
                            Poster, InfoHash, MagnetUri, MinimumRatio, MinimumSeedTime, DownloadVolumeFactor, UploadVolumeFactor
                        ) 
                        VALUES 
                        (
                            @Title, @Guid, @Link, @Details, @PublishDate, @Category, @Size, @Files, @Grabs, 
                            @Description, @RageID, @TVDBId, @Imdb, @TMDb, @TVMazeId, @TraktId, @DoubanId, @Genres, @Languages, 
                            @Subs, @Year, @Author, @BookTitle, @Publisher, @Artist, @Album, @Label, @Track, @Seeders, @Peers, 
                            @Poster, @InfoHash, @MagnetUri, @MinimumRatio, @MinimumSeedTime, @DownloadVolumeFactor, @UploadVolumeFactor
                        );";

                            var releaseIds = new List<int>();
                            foreach (var release in releases)
                            {
                                connection.Query<ReleaseInfo>(sqlInsertReleaseInfos, release, transaction);
                                var releaseId = connection.QuerySingle<int>("SELECT last_insert_rowid()", transaction: transaction);
                                releaseIds.Add(releaseId);
                            }

                            var sqlInsertTrackerCacheQueryReleaseInfos = @"
                                INSERT INTO TrackerCacheQueryReleaseInfos (TrackerCacheQueryId, ReleaseInfoId) 
                                VALUES (@TrackerCacheQueryId, @ReleaseInfoId);";

                            var trackerCacheQueryReleaseInfos = releases.Zip(releaseIds, (release, releaseId) => new
                            {
                                TrackerCacheQueryId = trackerCacheQueryId,
                                ReleaseInfoId = releaseId
                            });

                            connection.Execute(sqlInsertTrackerCacheQueryReleaseInfos, trackerCacheQueryReleaseInfos, transaction);

                            transaction.Commit();

                        }
                    }
                    PruneCacheByMaxResultsPerIndexer(indexer.Id);
                }
                catch (Exception e)
                {
                    _logger.Error("CacheResults adds parameter to the collections, {0}", e.Message);
                    _logger.Error("CacheResults adds parameter to the collections, {0}", e.InnerException);
                }
            }
        }

        private int GetOrAddTrackerCache(SqliteConnection connection, IIndexer indexer)
        {
            // Получить или добавить TrackerCache и вернуть его ID
            var sql = @"
        INSERT INTO TrackerCaches (TrackerId, TrackerName, TrackerType)
        VALUES (@TrackerId, @TrackerName, @TrackerType)
        ON CONFLICT(TrackerId) DO UPDATE SET
        TrackerName = excluded.TrackerName,
        TrackerType = excluded.TrackerType;

        SELECT Id FROM TrackerCaches WHERE TrackerId = @TrackerId;";

            return connection.QuerySingle<int>(sql, new
            {
                TrackerId = indexer.Id,
                TrackerName = indexer.Name,
                TrackerType = indexer.Type
            });
        }

        private int AddTrackerCacheQuery(SqliteConnection connection, int trackerCacheId, TorznabQuery query)
        {
            // Добавить TrackerCacheQuery и вернуть его ID
            var sql = @"
        INSERT INTO TrackerCacheQueries (TrackerCacheId, QueryHash, Created)
        VALUES (@TrackerCacheId, @QueryHash, @Created);

        SELECT last_insert_rowid();";

            return connection.QuerySingle<int>(sql, new
            {
                TrackerCacheId = trackerCacheId,
                QueryHash = GetQueryHash(query),
                Created = DateTime.Now
            });
        }

        public List<ReleaseInfo> Search(IIndexer indexer, TorznabQuery query)
        {
            if (_serverConfig.CacheType == CacheType.Disabled)
                return null;

            PruneCacheByTtl();

            var queryHash = GetQueryHash(query);

            using (var connection = new SqliteConnection("Data Source=" + GetConnectionString(_cacheconnectionString)))
            {
                connection.Open();
                var sql = @"
        SELECT ri.*
        FROM ReleaseInfos ri
        JOIN TrackerCacheQueryReleaseInfos tcqri ON ri.Id = tcqri.ReleaseInfoId
        JOIN TrackerCacheQueries tcq ON tcqri.TrackerCacheQueryId = tcq.Id
        JOIN TrackerCaches tc ON tcq.TrackerCacheId = tc.Id
        WHERE tc.TrackerId = @trackerId AND tcq.QueryHash = @queryHash";

                var results = connection.Query<ReleaseInfo>(sql, new { trackerId = indexer.Id, queryHash = queryHash }).ToList();

                if (results.Count > 0)
                {
                    _logger.Debug("CACHE Search Hit / Indexer: {0} / Found: {1} releases", indexer.Id, results.Count);
                    return results;
                }
            }

            return null;
        }

        public IReadOnlyList<TrackerCacheResult> GetCachedResults()
        {
            lock (_dbLock)
            {
                if (_serverConfig.CacheType == CacheType.Disabled)
                    return Array.Empty<TrackerCacheResult>();

                PruneCacheByTtl(); // remove expired results

                using (var connection = new SqliteConnection("Data Source=" + GetConnectionString(_cacheconnectionString)))
                {
                    connection.Open();

                    var sql = @"
            SELECT ri.*, tc.TrackerName, tc.TrackerId, tc.TrackerType, tcq.Created
            FROM ReleaseInfos ri
            INNER JOIN TrackerCacheQueryReleaseInfos tcqri ON ri.Id = tcqri.ReleaseInfoId
            INNER JOIN TrackerCacheQueries tcq ON tcqri.TrackerCacheQueryId = tcq.Id
            INNER JOIN TrackerCaches tc ON tcq.TrackerCacheId = tc.Id
            ORDER BY ri.PublishDate DESC
            LIMIT 3000;";

                    var results = connection.Query<ReleaseInfo, string, string, string, DateTime, TrackerCacheResult>(sql,
                        (ri, trackerName, trackerId, trackerType, created) =>
                        {
                            var result = new TrackerCacheResult(ri)
                            {
                                FirstSeen = created,
                                TrackerId = trackerId,
                                Tracker = trackerName,
                                TrackerType = trackerType
                            };
                            return result;
                        }, splitOn: "TrackerName,TrackerId,TrackerType,Created").ToList();

                    return results.AsReadOnly();
                }
            }
        }

        public void CleanIndexerCache(IIndexer indexer)
        {
            if (_serverConfig.CacheType == CacheType.Disabled)
                return;

            lock (_dbLock)
            {
                using (var connection = new SqliteConnection("Data Source=" + GetConnectionString(_cacheconnectionString)))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        // Удаление записей из промежуточной таблицы
                        var deleteIntermediateCommand = connection.CreateCommand();
                        deleteIntermediateCommand.CommandText = @"
                DELETE FROM TrackerCacheQueryReleaseInfos WHERE TrackerCacheQueryId IN (
                    SELECT Id FROM TrackerCacheQueries WHERE TrackerCacheId = (
                        SELECT Id FROM TrackerCaches WHERE TrackerId = $trackerId
                    )
                );";
                        deleteIntermediateCommand.Parameters.AddWithValue("$trackerId", indexer.Id);
                        deleteIntermediateCommand.ExecuteNonQuery();

                        // Удаление запросов из TrackerCacheQueries
                        var deleteQueryCommand = connection.CreateCommand();
                        deleteQueryCommand.CommandText = @"
                DELETE FROM TrackerCacheQueries WHERE TrackerCacheId = (
                    SELECT Id FROM TrackerCaches WHERE TrackerId = $trackerId
                );";
                        deleteQueryCommand.Parameters.AddWithValue("$trackerId", indexer.Id);
                        deleteQueryCommand.ExecuteNonQuery();

                        // Удаление записей из ReleaseInfos, которые больше не связаны с TrackerCacheQueryReleaseInfos
                        var deleteOrphanedReleaseInfosCommand = connection.CreateCommand();
                        deleteOrphanedReleaseInfosCommand.CommandText = @"
                DELETE FROM ReleaseInfos
                WHERE Id NOT IN (
                    SELECT ReleaseInfoId FROM TrackerCacheQueryReleaseInfos
                );";
                        deleteOrphanedReleaseInfosCommand.ExecuteNonQuery();

                        // Удаление трекеров из TrackerCaches
                        var deleteCacheCommand = connection.CreateCommand();
                        deleteCacheCommand.CommandText = @"
                DELETE FROM TrackerCaches WHERE TrackerId = $trackerId;";
                        deleteCacheCommand.Parameters.AddWithValue("$trackerId", indexer.Id);
                        deleteCacheCommand.ExecuteNonQuery();

                        transaction.Commit();
                    }
                }

                _logger.Debug("CACHE CleanIndexerCache / Indexer: {0}", indexer.Id);

                PruneCacheByTtl(); // remove expired results
            }
        }

        public void CleanCache()
        {
            if (_serverConfig.CacheType == CacheType.Disabled)
                return;

            lock (_dbLock)
            {
                using (var connection = new SqliteConnection("Data Source=" + GetConnectionString(_cacheconnectionString)))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        // Удаление всех записей из промежуточной таблицы
                        var deleteIntermediateCommand = connection.CreateCommand();
                        deleteIntermediateCommand.CommandText = "DELETE FROM TrackerCacheQueryReleaseInfos;";
                        deleteIntermediateCommand.ExecuteNonQuery();

                        // Удаление всех запросов из TrackerCacheQueries
                        var deleteQueryCommand = connection.CreateCommand();
                        deleteQueryCommand.CommandText = "DELETE FROM TrackerCacheQueries;";
                        deleteQueryCommand.ExecuteNonQuery();

                        // Удаление всех записей из ReleaseInfos
                        var deleteReleaseInfosCommand = connection.CreateCommand();
                        deleteReleaseInfosCommand.CommandText = "DELETE FROM ReleaseInfos;";
                        deleteReleaseInfosCommand.ExecuteNonQuery();

                        // Удаление всех трекеров из TrackerCaches
                        var deleteCacheCommand = connection.CreateCommand();
                        deleteCacheCommand.CommandText = "DELETE FROM TrackerCaches;";
                        deleteCacheCommand.ExecuteNonQuery();

                        transaction.Commit();
                    }
                }

                _logger.Debug("CACHE CleanCache");
                PrintCacheStatus();
            }
        }

        private void PruneCacheByTtl()
        {
            lock (_dbLock)
            {
                using (var connection = new SqliteConnection("Data Source=" + GetConnectionString(_cacheconnectionString)))
                {
                    connection.Open();
                    var expirationDate = DateTime.Now.AddSeconds(-_serverConfig.CacheTtl);

                    using (var transaction = connection.BeginTransaction())
                    {
                        // Удаление записей из промежуточной таблицы для устаревших запросов
                        var sqlDeleteFromIntermediateTable = @"
                    DELETE FROM TrackerCacheQueryReleaseInfos
                    WHERE TrackerCacheQueryId IN (
                        SELECT Id FROM TrackerCacheQueries
                        WHERE Created < @ExpirationDate
                    );";

                        var prunedCounterIntermediate = connection.Execute(sqlDeleteFromIntermediateTable, new { ExpirationDate = expirationDate }, transaction);

                        // Удаление устаревших запросов
                        var sqlDeleteExpiredQueries = @"
                    DELETE FROM TrackerCacheQueries
                    WHERE Created < @ExpirationDate;";

                        var prunedCounterQueries = connection.Execute(sqlDeleteExpiredQueries, new { ExpirationDate = expirationDate }, transaction);

                        // Удаление "осиротевших" записей из ReleaseInfos
                        var sqlDeleteOrphanedReleaseInfos = @"
                    DELETE FROM ReleaseInfos
                    WHERE Id NOT IN (
                        SELECT ReleaseInfoId FROM TrackerCacheQueryReleaseInfos
                    );";

                        var prunedCounterReleaseInfos = connection.Execute(sqlDeleteOrphanedReleaseInfos, null, transaction);

                        transaction.Commit();

                        var totalPruned = prunedCounterIntermediate + prunedCounterQueries + prunedCounterReleaseInfos;
                        _logger.Debug("CACHE PruneCacheByTtl / Pruned queries: {0}", totalPruned);
                        PrintCacheStatus();
                    }
                }
            }
        }

        private void PruneCacheByMaxResultsPerIndexer(string trackerId)
        {
            using (var connection = new SqliteConnection("Data Source=" + GetConnectionString(_cacheconnectionString)))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = @"
        SELECT tcq.Id, tcq.Created, (
            SELECT COUNT(*)
            FROM TrackerCacheQueryReleaseInfos tcqri
            WHERE tcqri.TrackerCacheQueryId = tcq.Id
        ) AS ResultCount
        FROM TrackerCacheQueries tcq
        JOIN TrackerCaches tc ON tcq.TrackerCacheId = tc.Id
        WHERE tc.TrackerId = $trackerId
        ORDER BY tcq.Created DESC";
                command.Parameters.AddWithValue("$trackerId", trackerId);

                var resultsPerQuery = new List<Tuple<long, int>>();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var queryId = reader.GetInt64(0);
                        var resultCount = reader.GetInt32(2);
                        resultsPerQuery.Add(new Tuple<long, int>(queryId, resultCount));
                    }
                }

                var prunedCounter = 0;
                while (true)
                {
                    var total = resultsPerQuery.Select(q => q.Item2).Sum();
                    if (total <= _serverConfig.CacheMaxResultsPerIndexer)
                        break;

                    var olderQuery = resultsPerQuery.Last();
                    var queryIdToRemove = olderQuery.Item1;

                    using (var transaction = connection.BeginTransaction())
                    {
                        // Удаление из промежуточной таблицы
                        var deleteIntermediateCommand = connection.CreateCommand();
                        deleteIntermediateCommand.CommandText = @"
                DELETE FROM TrackerCacheQueryReleaseInfos WHERE TrackerCacheQueryId = $queryId;";
                        deleteIntermediateCommand.Parameters.AddWithValue("$queryId", queryIdToRemove);
                        deleteIntermediateCommand.ExecuteNonQuery();

                        // Удаление из TrackerCacheQueries
                        var deleteQueryCommand = connection.CreateCommand();
                        deleteQueryCommand.CommandText = @"
                DELETE FROM TrackerCacheQueries WHERE Id = $queryId;";
                        deleteQueryCommand.Parameters.AddWithValue("$queryId", queryIdToRemove);
                        deleteQueryCommand.ExecuteNonQuery();

                        // Удаление осиротевших записей из ReleaseInfos
                        var deleteOrphanedReleaseInfosCommand = connection.CreateCommand();
                        deleteOrphanedReleaseInfosCommand.CommandText = @"
                DELETE FROM ReleaseInfos
                WHERE Id NOT IN (
                    SELECT ReleaseInfoId FROM TrackerCacheQueryReleaseInfos
                );";
                        deleteOrphanedReleaseInfosCommand.ExecuteNonQuery();

                        transaction.Commit();
                    }

                    resultsPerQuery.Remove(olderQuery);
                    prunedCounter++;
                }

                if (_logger.IsDebugEnabled)
                {
                    _logger.Debug("CACHE PruneCacheByMaxResultsPerIndexer / Indexer: {0} / Pruned queries: {1}", trackerId, prunedCounter);
                    PrintCacheStatus();
                }
            }
        }

        public TimeSpan CacheTTL => TimeSpan.FromSeconds(_serverConfig.CacheTtl);
        public void UpdateCacheConnectionString(string cacheconnectionString)
        {
            lock (_dbLock)
            {
                if (string.IsNullOrEmpty(cacheconnectionString) || !Regex.IsMatch(cacheconnectionString, @"^.+\.db$"))
                    throw new Exception("Cache Connection String: Is Empty or Bad name. Example: cache.db");

                _cacheconnectionString = cacheconnectionString;
                Initialize();
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

        private void PrintCacheStatus()
        {
            using (var connection = new SqliteConnection("Data Source=" + GetConnectionString(_cacheconnectionString)))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM ReleaseInfos";
                var totalCount = Convert.ToInt32(command.ExecuteScalar());
                _logger.Debug("CACHE STATUS / Total cache entries: {0}", totalCount);
            }
        }

        private string GetConnectionString(string cacheconnectionString)
        {
            if (!Path.IsPathRooted(cacheconnectionString))
            {
                cacheconnectionString = Path.Combine(_serverConfig.RuntimeSettings.DataFolder, cacheconnectionString);
            }
            return cacheconnectionString;
        }
    }
}
