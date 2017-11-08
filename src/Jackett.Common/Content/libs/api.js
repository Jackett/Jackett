var api = {
	version: "2.0",
	root: "/api",
    key: "",

	getApiPath: function(category, action) {
	    var path = this.root + "/v" + this.version + "/" + category;
	    if (action !== undefined)
	        path = path + "/" + action
	    return path;
	},

	getAllIndexers: function(callback) {
	    return $.get(this.getApiPath("indexers"), callback);
	},

	getServerConfig: function(callback) {
	    return $.get(this.getApiPath("server", "config"), callback);
	},

	getIndexerConfig: function(indexerId, callback) {
	    return $.get(this.getApiPath("indexers", indexerId + "/config"), callback);
	},

	updateIndexerConfig: function(indexerId, config, callback) {
	    return $.ajax({
	        url: this.getApiPath("indexers", indexerId + "/config"),
	        type: 'POST',
	        data: JSON.stringify(config),
	        dataType: 'json',
	        contentType: 'application/json',
	        cache: false,
	        success: callback
	    });
	},

	deleteIndexer: function(indexerId, callback) {
	    return $.ajax({
	        url: this.getApiPath("indexers", indexerId),
	        type: 'DELETE',
	        cache: false,
	        success: callback
	    });
	},

	testIndexer: function(indexerId, callback) {
	    return $.post(this.getApiPath("indexers", indexerId + "/test"), callback);
	},

	resultsForIndexer: function(indexerId, query, callback) {
	    return $.get(this.getApiPath("indexers", indexerId + "/results?apikey=" + this.key), query, callback);
	},

	getServerCache: function(callback) {
	    return $.get(this.getApiPath("indexers", "cache"), callback);
	},

	getServerLogs: function(callback) {
	    return $.get(this.getApiPath("server", "logs"), callback);
	},

	updateServerConfig: function(serverConfig, callback) {
	    return $.ajax({
	        url: this.getApiPath("server", "config"),
	        type: 'POST',
	        data: JSON.stringify(serverConfig),
	        dataType: 'json',
	        contentType: 'application/json',
	        cache: false,
	        success: callback
	    });
	},

	updateServer: function(callback) {
	    return $.post(this.getApiPath("server", "update"), callback);
	},

	updateAdminPassword: function(password, callback) {
	    return $.ajax({
	        url: this.getApiPath("server", "adminpassword"),
	        type: 'POST',
	        data: JSON.stringify(password),
	        dataType: 'json',
	        contentType: 'application/json',
	        cache: false,
	        success: callback
	    });
	}
}