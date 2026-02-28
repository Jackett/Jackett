import * as utils from './utils';

export const api = {
    version: "2.0",
    root: "/api",
    key: "",

    path(...segments) {
        return `${this.root}/v${this.version}/${segments.join("/")}`;
    },

    getAllIndexers(callback) {
        return $.get(this.path("indexers"), callback);
    },

    getServerConfig(callback) {
        return $.get(this.path("server", "config"), callback).fail((() => {
            utils.notify("Error loading Jackett settings, request to Jackett server failed, is server running ?", "danger", "fa fa-exclamation-triangle");
        }));
    },

    getIndexerConfig(indexerId, callback) {
        return $.get(this.path(`indexers`, indexerId, "config"), callback);
    },

    updateIndexerConfig(indexerId, config, callback) {
        return $.ajax({
            url: this.path("indexers", indexerId, "config"),
            type: 'POST',
            data: JSON.stringify(config),
            dataType: 'json',
            contentType: 'application/json',
            cache: false,
            success: callback
        });
    },

    deleteIndexer(indexerId, callback) {
        return $.ajax({
            url: this.path("indexers", indexerId),
            type: 'DELETE',
            cache: false,
            success: callback
        });
    },

    testIndexer(indexerId, callback) {
        return $.post(this.path("indexers", indexerId, "test"), callback);
    },

    resultsForIndexer(indexerId, query, callback) {
        return $.get(this.path("indexers", indexerId, `results?apikey=${this.key}`), query, callback);
    },

    getServerCache(callback) {
        return $.get(this.path("indexers", "cache"), callback);
    },

    getServerLogs(callback) {
        return $.get(this.path("server", "logs"), callback);
    },

    updateServerConfig(serverConfig, callback) {
        return $.ajax({
            url: this.path("server", "config"),
            type: 'POST',
            data: JSON.stringify(serverConfig),
            dataType: 'json',
            contentType: 'application/json',
            cache: false,
            success: callback
        });
    },

    updateServer(callback) {
        return $.post(this.path("server", "update"), callback);
    },

    updateAdminPassword(password, callback) {
        return $.ajax({
            url: this.path("server", "adminpassword"),
            type: 'POST',
            data: JSON.stringify(password),
            dataType: 'json',
            contentType: 'application/json',
            cache: false,
            success: callback
        });
    }
};
