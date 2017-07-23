function getAllIndexers(callback) {
    return $.get("/Api/Indexers/", callback);
}

function getServerConfig(callback) {
    return $.get("/Api/Server/Config", callback);
}

function getIndexerConfig(indexerId, callback) {
    return $.get("/Api/Indexers/" + indexerId + "/Config", calback);
}

function updateIndexerConfig(indexerId, config, callback) {
    return $.ajax({
        url: "/Api/Indexers/" + indexerId + "/Config",
        type: 'POST',
        data: JSON.stringify(config),
        dataType: 'json',
        contentType: 'application/json',
        cache: false,
        success: callback
    });
}

function deleteIndexer(indexerId, callback) {
    return $.ajax({
        url: "/Api/Indexers/" + indexerId,
        type: 'DELETE',
        cache: false,
        success: callback
    });
}

function testIndexer(indexerId, callback) {
    return $.post("/Api/Indexers/" + indexerId + "/Test", callback);
}

function resultsForIndexer(indexerId, query, callback) {
    return $.get("/Api/Indexers/" + trackerId + "/Results", query, callback);
}

function getServerCache(callback) {
    return $.get("/Api/Indexers/Cache", callback);
}

function getServerLogs(callback) {
    return $.get("/Api/Server/Logs", callback);
}

function updateServerConfig(serverConfig, callback) {
    return $.ajax({
        url: "/Api/Server/Config",
        type: 'POST',
        data: JSON.stringify(serverConfig),
        dataType: 'json',
        contentType: 'application/json',
        cache: false,
        success: callback
    });
}

function updateServer(callback) {
    return $.post("/Api/Server/Update", callback);
}

function updateAdminPassword(password, callback) {
    return $.ajax({
        url: "/Api/Server/AdminPassword",
        type: 'POST',
        data: JSON.stringify(password),
        dataType: 'json',
        contentType: 'application/json',
        cache: false,
        success: callback
    });
}
