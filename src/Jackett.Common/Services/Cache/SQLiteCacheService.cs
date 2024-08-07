using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Jackett.Common.Indexers;
using Jackett.Common.Models;
using Jackett.Common.Models.Config;
using Jackett.Common.Services.Interfaces;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using NLog;
using ZstdSharp;

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
            TrackerId TEXT,
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
            TrackerCacheQueryId INTEGER,
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
            UploadVolumeFactor REAL,
            FOREIGN KEY(TrackerCacheQueryId) REFERENCES TrackerCacheQueries(Id)
        );
        ";
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

                            foreach (var release in releases)
                            {
                                var command = connection.CreateCommand();
                                command.CommandText = @"
                INSERT INTO ReleaseInfos (TrackerCacheQueryId, Title, Guid, Link, Details, PublishDate, Category, Size, Files, Grabs, Description, RageID, TVDBId, Imdb, TMDb, TVMazeId, TraktId, DoubanId, Genres, Languages, Subs, Year, Author, BookTitle, Publisher, Artist, Album, Label, Track, Seeders, Peers, Poster, InfoHash, MagnetUri, MinimumRatio, MinimumSeedTime, DownloadVolumeFactor, UploadVolumeFactor)
                VALUES ($trackerCacheQueryId, $title, $guid, $link, $details, $publishDate, $category, $size, $files, $grabs, $description, $rageID, $tvdbId, $imdb, $tmdb, $tvMazeId, $traktId, $doubanId, $genres, $languages, $subs, $year, $author, $bookTitle, $publisher, $artist, $album, $label, $track, $seeders, $peers, $poster, $infoHash, $magnetUri, $minimumRatio, $minimumSeedTime, $downloadVolumeFactor, $uploadVolumeFactor)
                ";
                                command.Parameters.AddWithValue("$title", release.Title);
                                command.Parameters.AddWithValue("$guid", release.Guid?.ToString());
                                command.Parameters.AddWithValue("$link", release.Link?.ToString());
                                command.Parameters.AddWithValue("$details", release.Details?.ToString());
                                command.Parameters.AddWithValue("$publishDate", release.PublishDate);
                                command.Parameters.AddWithValue(
                                    "$category", string.Join(",", release.Category ?? new List<int>()));
                                command.Parameters.AddWithValue("$size", release.Size ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("$files", release.Files ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("$grabs", release.Grabs ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("$description", release.Description ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("$rageID", release.RageID ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("$tvdbId", release.TVDBId ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("$imdb", release.Imdb ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("$tmdb", release.TMDb ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("$tvMazeId", release.TVMazeId ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("$traktId", release.TraktId ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("$doubanId", release.DoubanId ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue(
                                    "$genres", string.Join(",", release.Genres ?? new List<string>()));
                                command.Parameters.AddWithValue(
                                    "$languages", string.Join(",", release.Languages ?? new List<string>()));
                                command.Parameters.AddWithValue(
                                    "$subs", string.Join(",", release.Subs ?? new List<string>()));
                                command.Parameters.AddWithValue("$year", release.Year ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("$author", release.Author ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("$bookTitle", release.BookTitle ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("$publisher", release.Publisher ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("$artist", release.Artist ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("$album", release.Album ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("$label", release.Label ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("$track", release.Track ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("$seeders", release.Seeders ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("$peers", release.Peers ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue(
                                    "$poster", release.Poster?.ToString() ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("$infoHash", release.InfoHash ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue(
                                    "$magnetUri", release.MagnetUri?.ToString() ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue(
                                    "$minimumRatio", release.MinimumRatio ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue(
                                    "$minimumSeedTime", release.MinimumSeedTime ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue(
                                    "$downloadVolumeFactor", release.DownloadVolumeFactor ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue(
                                    "$uploadVolumeFactor", release.UploadVolumeFactor ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue(
                                    "$trackerCacheQueryId", trackerCacheQueryId);

                                command.ExecuteNonQuery();
                            }

                            transaction.Commit();
                        }
                    }
                    PruneCacheByMaxResultsPerIndexer(indexer.Id);
                }
                catch (Exception e)
                {
                    _logger.Error("CacheResults adds parameter to the collections, {0}", e.Message);
                }
            }
        }

        private int GetOrAddTrackerCache(SqliteConnection connection, IIndexer indexer)
        {
            var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT Id FROM TrackerCaches WHERE TrackerId = $trackerId;";
            command.Parameters.AddWithValue("$trackerId", indexer.Id);

            var trackerCacheId = command.ExecuteScalar();

            if (trackerCacheId == null)
            {
                command.CommandText = @"
                INSERT INTO TrackerCaches (TrackerId, TrackerName, TrackerType)
                VALUES ($trackerId, $trackerName, $trackerType);
                SELECT last_insert_rowid();";
                command.Parameters.AddWithValue("$trackerName", indexer.Name);
                command.Parameters.AddWithValue("$trackerType", indexer.Type);
                trackerCacheId = command.ExecuteScalar();
            }

            return Convert.ToInt32(trackerCacheId);
        }

        private int AddTrackerCacheQuery(SqliteConnection connection, int trackerCacheId, TorznabQuery query)
        {
            var command = connection.CreateCommand();
            command.CommandText = @"
            INSERT INTO TrackerCacheQueries (TrackerCacheId, QueryHash, Created)
            VALUES ($trackerCacheId, $queryHash, $created);
            SELECT last_insert_rowid();";
            command.Parameters.AddWithValue("$trackerCacheId", trackerCacheId);
            command.Parameters.AddWithValue("$queryHash", GetQueryHash(query));
            command.Parameters.AddWithValue("$created", DateTime.Now);

            return Convert.ToInt32(command.ExecuteScalar());
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
                var command = connection.CreateCommand();
                command.CommandText = @"
                SELECT ri.*
                FROM ReleaseInfos ri
                JOIN TrackerCacheQueries tcq ON ri.TrackerCacheQueryId = tcq.Id
                JOIN TrackerCaches tc ON tcq.TrackerCacheId = tc.Id
                WHERE tc.TrackerId = $trackerId AND tcq.QueryHash = $queryHash";
                command.Parameters.AddWithValue("$trackerId", indexer.Id);
                command.Parameters.AddWithValue("$queryHash", queryHash);

                var results = new List<ReleaseInfo>();

                using (var reader = command.ExecuteReader())
                {
                    try
                    {
                        while (reader.Read())
                        {
                            var releaseInfo = new ReleaseInfo
                            {
                                Title = reader.GetString(reader.GetOrdinal("Title")),
                                Guid = new Uri(reader.GetString(reader.GetOrdinal("Guid"))),
                                Link = new Uri(reader.GetString(reader.GetOrdinal("Link"))),
                                Details = new Uri(reader.GetString(reader.GetOrdinal("Details"))),
                                PublishDate = reader.GetDateTime(reader.GetOrdinal("PublishDate")),
                                Category = reader.GetString(reader.GetOrdinal("Category"))
                                                 .Split(',')
                                                 .Select(int.Parse)
                                                 .ToList(),
                                Size =
                                    reader.IsDBNull(reader.GetOrdinal("Size"))
                                        ? (long?)null
                                        : reader.GetInt64(reader.GetOrdinal("Size")),
                                Files =
                                    reader.IsDBNull(reader.GetOrdinal("Files"))
                                        ? (long?)null
                                        : reader.GetInt64(reader.GetOrdinal("Files")),
                                Grabs =
                                    reader.IsDBNull(reader.GetOrdinal("Grabs"))
                                        ? (long?)null
                                        : reader.GetInt64(reader.GetOrdinal("Grabs")),
                                Description =
                                    reader.IsDBNull(reader.GetOrdinal("Description"))
                                        ? null
                                        : reader.GetString(reader.GetOrdinal("Description")),
                                RageID =
                                    reader.IsDBNull(reader.GetOrdinal("RageID"))
                                        ? (long?)null
                                        : reader.GetInt64(reader.GetOrdinal("RageID")),
                                TVDBId =
                                    reader.IsDBNull(reader.GetOrdinal("TVDBId"))
                                        ? (long?)null
                                        : reader.GetInt64(reader.GetOrdinal("TVDBId")),
                                Imdb =
                                    reader.IsDBNull(reader.GetOrdinal("Imdb"))
                                        ? (long?)null
                                        : reader.GetInt64(reader.GetOrdinal("Imdb")),
                                TMDb =
                                    reader.IsDBNull(reader.GetOrdinal("TMDb"))
                                        ? (long?)null
                                        : reader.GetInt64(reader.GetOrdinal("TMDb")),
                                TVMazeId =
                                    reader.IsDBNull(reader.GetOrdinal("TVMazeId"))
                                        ? (long?)null
                                        : reader.GetInt64(reader.GetOrdinal("TVMazeId")),
                                TraktId =
                                    reader.IsDBNull(reader.GetOrdinal("TraktId"))
                                        ? (long?)null
                                        : reader.GetInt64(reader.GetOrdinal("TraktId")),
                                DoubanId =
                                    reader.IsDBNull(reader.GetOrdinal("DoubanId"))
                                        ? (long?)null
                                        : reader.GetInt64(reader.GetOrdinal("DoubanId")),
                                Genres =
                                    JsonConvert.DeserializeObject<List<string>>(
                                        reader.GetString(reader.GetOrdinal("Genres"))),
                                Languages =
                                    JsonConvert.DeserializeObject<List<string>>(
                                        reader.GetString(reader.GetOrdinal("Languages"))),
                                Subs =
                                    JsonConvert.DeserializeObject<List<string>>(
                                        reader.GetString(reader.GetOrdinal("Subs"))),
                                Year =
                                    reader.IsDBNull(reader.GetOrdinal("Year"))
                                        ? (long?)null
                                        : reader.GetInt64(reader.GetOrdinal("Year")),
                                Author =
                                    reader.IsDBNull(reader.GetOrdinal("Author"))
                                        ? null
                                        : reader.GetString(reader.GetOrdinal("Author")),
                                BookTitle =
                                    reader.IsDBNull(reader.GetOrdinal("BookTitle"))
                                        ? null
                                        : reader.GetString(reader.GetOrdinal("BookTitle")),
                                Publisher =
                                    reader.IsDBNull(reader.GetOrdinal("Publisher"))
                                        ? null
                                        : reader.GetString(reader.GetOrdinal("Publisher")),
                                Artist =
                                    reader.IsDBNull(reader.GetOrdinal("Artist"))
                                        ? null
                                        : reader.GetString(reader.GetOrdinal("Artist")),
                                Album =
                                    reader.IsDBNull(reader.GetOrdinal("Album"))
                                        ? null
                                        : reader.GetString(reader.GetOrdinal("Album")),
                                Label =
                                    reader.IsDBNull(reader.GetOrdinal("Label"))
                                        ? null
                                        : reader.GetString(reader.GetOrdinal("Label")),
                                Track =
                                    reader.IsDBNull(reader.GetOrdinal("Track"))
                                        ? null
                                        : reader.GetString(reader.GetOrdinal("Track")),
                                Seeders =
                                    reader.IsDBNull(reader.GetOrdinal("Seeders"))
                                        ? (long?)null
                                        : reader.GetInt64(reader.GetOrdinal("Seeders")),
                                Peers =
                                    reader.IsDBNull(reader.GetOrdinal("Peers"))
                                        ? (long?)null
                                        : reader.GetInt64(reader.GetOrdinal("Peers")),
                                Poster =
                                    reader.IsDBNull(reader.GetOrdinal("Poster"))
                                        ? null
                                        : new Uri(reader.GetString(reader.GetOrdinal("Poster"))),
                                InfoHash =
                                    reader.IsDBNull(reader.GetOrdinal("InfoHash"))
                                        ? null
                                        : reader.GetString(reader.GetOrdinal("InfoHash")),
                                MagnetUri =
                                    reader.IsDBNull(reader.GetOrdinal("MagnetUri"))
                                        ? null
                                        : new Uri(reader.GetString(reader.GetOrdinal("MagnetUri"))),
                                MinimumRatio =
                                    reader.IsDBNull(reader.GetOrdinal("MinimumRatio"))
                                        ? (double?)null
                                        : reader.GetDouble(reader.GetOrdinal("MinimumRatio")),
                                MinimumSeedTime =
                                    reader.IsDBNull(reader.GetOrdinal("MinimumSeedTime"))
                                        ? (long?)null
                                        : reader.GetInt64(reader.GetOrdinal("MinimumSeedTime")),
                                DownloadVolumeFactor =
                                    reader.IsDBNull(reader.GetOrdinal("DownloadVolumeFactor"))
                                        ? (double?)null
                                        : reader.GetDouble(reader.GetOrdinal("DownloadVolumeFactor")),
                                UploadVolumeFactor = reader.IsDBNull(reader.GetOrdinal("UploadVolumeFactor"))
                                    ? (double?)null
                                    : reader.GetDouble(reader.GetOrdinal("UploadVolumeFactor"))
                            };
                            results.Add(releaseInfo);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.Error("Search adds parameter to the collections, {0}", e.Message);
                    }
                }

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

                List<TrackerCacheResult> results = new List<TrackerCacheResult>();

                using (var connection = new SqliteConnection("Data Source=" + GetConnectionString(_cacheconnectionString)))
                {
                    connection.Open();

                    var command = connection.CreateCommand();
                    command.CommandText = @"
                    SELECT ReleaseInfos.*, TrackerCaches.TrackerName, TrackerCaches.TrackerId, TrackerCaches.TrackerType, TrackerCacheQueries.Created
                    FROM ReleaseInfos
                    INNER JOIN TrackerCacheQueries ON ReleaseInfos.TrackerCacheQueryId = TrackerCacheQueries.Id
                    INNER JOIN TrackerCaches ON TrackerCacheQueries.TrackerCacheId = TrackerCaches.Id
                    ORDER BY ReleaseInfos.PublishDate DESC
                    LIMIT 3000;";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            try
                            {
                                results.Add(
                                    new TrackerCacheResult(
                                        new ReleaseInfo
                                        {
                                            Title = reader["Title"].ToString(),
                                            Guid = new Uri(reader["Guid"].ToString()),
                                            Link = new Uri(reader["Link"].ToString()),
                                            Details = new Uri(reader["Details"].ToString()),
                                            PublishDate = DateTime.Parse(reader["PublishDate"].ToString()),
                                            Category = reader["Category"].ToString().Split(',').Select(int.Parse).ToList(),
                                            Size =
                                                reader["Size"] != DBNull.Value
                                                    ? (long?)Convert.ToInt64(reader["Size"])
                                                    : null,
                                            Files =
                                                reader["Files"] != DBNull.Value
                                                    ? (long?)Convert.ToInt64(reader["Files"])
                                                    : null,
                                            Grabs =
                                                reader["Grabs"] != DBNull.Value
                                                    ? (long?)Convert.ToInt64(reader["Grabs"])
                                                    : null,
                                            Description = reader["Description"].ToString(),
                                            RageID =
                                                reader["RageID"] != DBNull.Value
                                                    ? (long?)Convert.ToInt64(reader["RageID"])
                                                    : null,
                                            TVDBId =
                                                reader["TVDBId"] != DBNull.Value
                                                    ? (long?)Convert.ToInt64(reader["TVDBId"])
                                                    : null,
                                            Imdb =
                                                reader["Imdb"] != DBNull.Value
                                                    ? (long?)Convert.ToInt64(reader["Imdb"])
                                                    : null,
                                            TMDb =
                                                reader["TMDb"] != DBNull.Value
                                                    ? (long?)Convert.ToInt64(reader["TMDb"])
                                                    : null,
                                            TVMazeId =
                                                reader["TVMazeId"] != DBNull.Value
                                                    ? (long?)Convert.ToInt64(reader["TVMazeId"])
                                                    : null,
                                            TraktId =
                                                reader["TraktId"] != DBNull.Value
                                                    ? (long?)Convert.ToInt64(reader["TraktId"])
                                                    : null,
                                            DoubanId =
                                                reader["DoubanId"] != DBNull.Value
                                                    ? (long?)Convert.ToInt64(reader["DoubanId"])
                                                    : null,
                                            Genres = reader["Genres"].ToString().Split(',').ToList(),
                                            Languages = reader["Languages"].ToString().Split(',').ToList(),
                                            Subs = reader["Subs"].ToString().Split(',').ToList(),
                                            Year =
                                                reader["Year"] != DBNull.Value
                                                    ? (long?)Convert.ToInt64(reader["Year"])
                                                    : null,
                                            Author = reader["Author"].ToString(),
                                            BookTitle = reader["BookTitle"].ToString(),
                                            Publisher = reader["Publisher"].ToString(),
                                            Artist = reader["Artist"].ToString(),
                                            Album = reader["Album"].ToString(),
                                            Label = reader["Label"].ToString(),
                                            Track = reader["Track"].ToString(),
                                            Seeders =
                                                reader["Seeders"] != DBNull.Value
                                                    ? (long?)Convert.ToInt64(reader["Seeders"])
                                                    : null,
                                            Peers =
                                                reader["Peers"] != DBNull.Value
                                                    ? (long?)Convert.ToInt64(reader["Peers"])
                                                    : null,
                                            Poster =
                                                reader["Poster"] != DBNull.Value
                                                    ? new Uri(reader["Poster"].ToString())
                                                    : null,
                                            InfoHash = reader["InfoHash"].ToString(),
                                            MagnetUri =
                                                reader["MagnetUri"] != DBNull.Value
                                                    ? new Uri(reader["MagnetUri"].ToString())
                                                    : null,
                                            MinimumRatio =
                                                reader["MinimumRatio"] != DBNull.Value
                                                    ? (double?)Convert.ToDouble(reader["MinimumRatio"])
                                                    : null,
                                            MinimumSeedTime =
                                                reader["MinimumSeedTime"] != DBNull.Value
                                                    ? (long?)Convert.ToInt64(reader["MinimumSeedTime"])
                                                    : null,
                                            DownloadVolumeFactor =
                                                reader["DownloadVolumeFactor"] != DBNull.Value
                                                    ? (double?)Convert.ToDouble(reader["DownloadVolumeFactor"])
                                                    : null,
                                            UploadVolumeFactor =
                                                reader["UploadVolumeFactor"] != DBNull.Value
                                                    ? (double?)Convert.ToDouble(reader["UploadVolumeFactor"])
                                                    : null,
                                            Origin = null // Restore Origin not required
                                        })
                                    {
                                        FirstSeen = DateTime.Parse(reader["Created"].ToString()),
                                        TrackerId = reader["TrackerId"].ToString(),
                                        Tracker = reader["TrackerName"].ToString(),
                                        TrackerType = reader["TrackerType"].ToString()
                                    });
                            }
                            catch (Exception e)
                            {
                                _logger.Error("GetCachedResults adds parameter to the collections, {0}", e.Message);
                            }
                        }
                    }
                }

                return results;
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
                    var command = connection.CreateCommand();
                    command.CommandText = @"
                DELETE FROM ReleaseInfos WHERE TrackerCacheQueryId IN (
                    SELECT Id FROM TrackerCacheQueries WHERE TrackerCacheId = (
                        SELECT Id FROM TrackerCaches WHERE TrackerId = $trackerId
                    )
                );
                DELETE FROM TrackerCacheQueries WHERE TrackerCacheId = (
                    SELECT Id FROM TrackerCaches WHERE TrackerId = $trackerId
                );
                DELETE FROM TrackerCaches WHERE TrackerId = $trackerId;";
                    command.Parameters.AddWithValue("$trackerId", indexer.Id);
                    command.ExecuteNonQuery();
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
                    var command = connection.CreateCommand();
                    command.CommandText = @"
                    DELETE FROM ReleaseInfos;
                    DELETE FROM TrackerCacheQueries;
                    DELETE FROM TrackerCaches;";
                    command.ExecuteNonQuery();
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
                    var command = connection.CreateCommand();
                    command.CommandText = @"
                    DELETE FROM ReleaseInfos 
                    WHERE TrackerCacheQueryId IN (
                        SELECT Id FROM TrackerCacheQueries 
                        WHERE Created < $expirationDate)";
                    command.Parameters.AddWithValue("$expirationDate", expirationDate);

                    var prunedCounter = command.ExecuteNonQuery();

                    _logger.Debug("CACHE PruneCacheByTtl / Pruned queries: {0}", prunedCounter);
                    PrintCacheStatus();
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
                    SELECT COUNT(*) FROM ReleaseInfos ri
                    WHERE ri.TrackerCacheQueryId = tcq.Id
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

                    var deleteCommand = connection.CreateCommand();
                    deleteCommand.CommandText = @"
                    DELETE FROM TrackerCacheQueries WHERE Id = $queryId;
                    DELETE FROM ReleaseInfos WHERE TrackerCacheQueryId = $queryId;";
                    deleteCommand.Parameters.AddWithValue("$queryId", queryIdToRemove);
                    deleteCommand.ExecuteNonQuery();

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
                if (string.IsNullOrEmpty(cacheconnectionString))
                    throw new ArgumentNullException("Cache Connection String: Is Empty");

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
