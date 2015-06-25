

reloadIndexers();
loadSonarrInfo();

function loadSonarrInfo() {
    getSonarrConfig(function (data) {
        $("#sonarr-host").val("");
        var host, port, apiKey;
        for (var i = 0; i < data.config.length; i++) {
            if (data.config[i].id == "host")
                host = data.config[i].value;
            if (data.config[i].id == "port")
                port = data.config[i].value;
            if (data.config[i].id == "apikey")
                apiKey = data.config[i].value;
        }
        if (!apiKey)
            $("#sonarr-warning").show();
        else {
            $("#sonarr-warning").hide();
            $("#sonarr-host").val(host + ":" + port);
        }
    });
}

function getSonarrConfig(callback) {
    var jqxhr = $.get("get_sonarr_config", function (data) {
        callback(data);
    }).fail(function () {
        doNotify("Error loading Sonarr API configuration, request to Jackett server failed", "danger", "glyphicon glyphicon-alert");
    });
}

$("#sonarr-test").click(function () {
    var jqxhr = $.get("get_indexers", function (data) {
        if (data.result == "error")
            doNotify("Test failed for Sonarr API\n" + data.error, "danger", "glyphicon glyphicon-alert");
        else
            doNotify("Test successful for Sonarr API", "success", "glyphicon glyphicon-ok");
    }).fail(function () {
        doNotify("Error testing Sonarr, request to Jackett server failed", "danger", "glyphicon glyphicon-alert");
    });
});

$("#sonarr-settings").click(function () {
    getSonarrConfig(function (data) {
        var config = data.config;

        var configForm = newConfigModal("Sonarr API", config);

        var $goButton = configForm.find(".setup-indexer-go");
        $goButton.click(function () {
            var data = getConfigModalJson(configForm);

            var originalBtnText = $goButton.html();
            $goButton.prop('disabled', true);
            $goButton.html($('#templates > .spinner')[0].outerHTML);

            var jqxhr = $.post("apply_sonarr_config", JSON.stringify(data), function (data) {
                if (data.result == "error") {
                    if (data.config) {
                        populateSetupForm(data.indexer, data.name, data.config);
                    }
                    doNotify("Configuration failed: " + data.error, "danger", "glyphicon glyphicon-alert");
                }
                else {
                    configForm.modal("hide");
                    loadSonarrInfo();
                    doNotify("Successfully configured Sonarr API", "success", "glyphicon glyphicon-ok");
                }
            }).fail(function () {
                doNotify("Request to Jackett server failed", "danger", "glyphicon glyphicon-alert");
            }).always(function () {
                $goButton.html(originalBtnText);
                $goButton.prop('disabled', false);
            });
        });

        configForm.modal("show");

    });
});


function reloadIndexers() {
    $('#indexers').hide();
    $('#indexers > .indexer').remove();
    $('#unconfigured-indexers').empty();
    var jqxhr = $.get("get_indexers", function (data) {
        $("#api-key-input").val(data.api_key);
        displayIndexers(data.items);
    }).fail(function () {
        doNotify("Error loading indexers, request to Jackett server failed", "danger", "glyphicon glyphicon-alert");
    });
}

function displayIndexers(items) {
    var indexerTemplate = Handlebars.compile($("#templates > .configured-indexer")[0].outerHTML);
    var unconfiguredIndexerTemplate = Handlebars.compile($("#templates > .unconfigured-indexer")[0].outerHTML);
    for (var i = 0; i < items.length; i++) {
        var item = items[i];
        item.torznab_host = resolveUrl("/api/" + item.id);
        if (item.configured)
            $('#indexers').append(indexerTemplate(item));
        else
            $('#unconfigured-indexers').append($(unconfiguredIndexerTemplate(item)));
    }

    var addIndexerButton = $("#templates > .add-indexer")[0].outerHTML;
    $('#indexers').append(addIndexerButton);

    $('#indexers').fadeIn();
    prepareSetupButtons();
    prepareTestButtons();
    prepareDeleteButtons();
}

function prepareDeleteButtons() {
    $(".indexer-button-delete").each(function (i, btn) {
        var $btn = $(btn);
        var id = $btn.data("id");
        $btn.click(function () {
            var jqxhr = $.post("delete_indexer", JSON.stringify({ indexer: id }), function (data) {
                if (data.result == "error") {
                    doNotify("Delete error for " + id + "\n" + data.error, "danger", "glyphicon glyphicon-alert");
                }
                else {
                    doNotify("Deleted " + id, "success", "glyphicon glyphicon-ok");
                }
            }).fail(function () {
                doNotify("Error deleting indexer, request to Jackett server error", "danger", "glyphicon glyphicon-alert");
            }).always(function () {
                reloadIndexers();
            });
        });
    });
}

function prepareSetupButtons() {
    $('.indexer-setup').each(function (i, btn) {
        var $btn = $(btn);
        var id = $btn.data("id");
        $btn.click(function () {
            displayIndexerSetup(id);
        });
    });
}

function prepareTestButtons() {
    $(".indexer-button-test").each(function (i, btn) {
        var $btn = $(btn);
        var id = $btn.data("id");
        $btn.click(function () {
            doNotify("Test started for " + id, "info", "glyphicon glyphicon-transfer");
            var jqxhr = $.post("test_indexer", JSON.stringify({ indexer: id }), function (data) {
                if (data.result == "error") {
                    doNotify("Test failed for " + data.name + "\n" + data.error, "danger", "glyphicon glyphicon-alert");
                }
                else {
                    doNotify("Test successful for " + data.name, "success", "glyphicon glyphicon-ok");
                }
            }).fail(function () {
                doNotify("Error testing indexer, request to Jackett server error", "danger", "glyphicon glyphicon-alert");
            });
        });
    });
}

function displayIndexerSetup(id) {

    var jqxhr = $.post("get_config_form", JSON.stringify({ indexer: id }), function (data) {
        if (data.result == "error") {
            doNotify("Error: " + data.error, "danger", "glyphicon glyphicon-alert");
            return;
        }
        populateSetupForm(id, data.name, data.config);

    }).fail(function () {
        doNotify("Request to Jackett server failed", "danger", "glyphicon glyphicon-alert");
    });

    $("#select-indexer-modal").modal("hide");
}

function populateConfigItems(configForm, config) {
    var $formItemContainer = configForm.find(".config-setup-form");
    $formItemContainer.empty();
    var setupItemTemplate = Handlebars.compile($("#templates > .setup-item")[0].outerHTML);
    for (var i = 0; i < config.length; i++) {
        var item = config[i];
        var setupValueTemplate = Handlebars.compile($("#templates > .setup-item-" + item.type)[0].outerHTML);
        item.value_element = setupValueTemplate(item);
        $formItemContainer.append(setupItemTemplate(item));
    }
}

function newConfigModal(title, config) {
    //config-setup-modal
    var configTemplate = Handlebars.compile($("#templates > .config-setup-modal")[0].outerHTML);
    var configForm = $(configTemplate({ title: title }));

    $("#modals").append(configForm);

    populateConfigItems(configForm, config);

    return configForm;
    //modal.remove();
}

function getConfigModalJson(configForm) {
    var configJson = {};
    configForm.find(".config-setup-form").children().each(function (i, el) {
        $el = $(el);
        var type = $el.data("type");
        var id = $el.data("id");
        switch (type) {
            case "inputstring":
                configJson[id] = $el.find(".setup-item-inputstring").val();
                break;
            case "inputbool":
                configJson[id] = $el.find(".setup-item-checkbox").val();
                break;
        }
    });
    return configJson;
}

function populateSetupForm(indexerId, name, config) {

    var configForm = newConfigModal(name, config);

    var $goButton = configForm.find(".setup-indexer-go");
    $goButton.click(function () {
        var data = { indexer: indexerId, name: name };
        data.config = getConfigModalJson(configForm);

        var originalBtnText = $goButton.html();
        $goButton.prop('disabled', true);
        $goButton.html($('#templates > .spinner')[0].outerHTML);

        var jqxhr = $.post("configure_indexer", JSON.stringify(data), function (data) {
            if (data.result == "error") {
                if (data.config) {
                    populateConfigItems(configForm, data.config);
                }
                doNotify("Configuration failed: " + data.error, "danger", "glyphicon glyphicon-alert");
            }
            else {
                configForm.modal("hide");
                reloadIndexers();
                doNotify("Successfully configured " + data.name, "success", "glyphicon glyphicon-ok");
            }
        }).fail(function () {
            doNotify("Request to Jackett server failed", "danger", "glyphicon glyphicon-alert");
        }).always(function () {
            $goButton.html(originalBtnText);
            $goButton.prop('disabled', false);
        });
    });

    configForm.modal("show");
}

function resolveUrl(url) {
    var a = document.createElement('a');
    a.href = url;
    url = a.href;
    return url;
}



function doNotify(message, type, icon) {
    $.notify({
        message: message,
        icon: icon
    }, {
        element: 'body',
        type: type,
        allow_dismiss: true,
        z_index: 9000,
        mouse_over: 'pause',
        placement: {
            from: "bottom",
            align: "center"
        }
    });
}

function clearNotifications() {
    $('[data-notify="container"]').remove();
}

$('#test').click(doNotify);

