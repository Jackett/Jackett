$(document).ready(function () {
    $.ajaxSetup({ cache: false });
    HandlebarsIntl.registerWith(Handlebars);
    reloadIndexers();
    loadJackettSettings();
});

function loadJackettSettings() {
    getJackettConfig(function (data) {

        $("#api-key-input").val(data.config.api_key);
        $("#app-version").html(data.app_version);
        $("#jackett-port").val(data.config.port);
        $("#jackett-allowext").attr('checked', data.config.external);
        var password = data.config.password;
        $("#jackett-adminpwd").val(password);
        if (password != null && password != '') {
            $("#logoutBtn").show();
        }
    });
}

$("#jackett-show-releases").click(function () {
    var jqxhr = $.get("/admin/GetCache", function (data) {
        var releaseTemplate = Handlebars.compile($("#jackett-releases").html());
        var item = { releases: data, Title: 'Releases' };
        var releaseDialog = $(releaseTemplate(item));
        releaseDialog.find('table').DataTable(
            {
                "pageLength": 20,
                "lengthMenu": [[10, 20, 50, -1], [10, 20, 50, "All"]],
                "order": [[2, "desc"]],
                "columnDefs": [
                   {
                       "targets": 0,
                       "visible": false,
                       "searchable": false
                   },
                   {
                       "targets": 1,
                       "visible": false,
                       "searchable": false
                   },
                   {
                       "targets": 2,
                       "visible": true,
                       "searchable": false,
                       "iDataSort": 0
                   },
                   {
                       "targets": 3,
                       "visible": true,
                       "searchable": false,
                       "iDataSort": 1
                   }
                ]
            });
        $("#modals").append(releaseDialog);
        releaseDialog.modal("show");

    }).fail(function () {
        doNotify("Request to Jackett server failed", "danger", "glyphicon glyphicon-alert");
    });
});


$("#change-jackett-port").click(function () {
    var jackett_port = $("#jackett-port").val();
    var jackett_external = $("#jackett-allowext").is(':checked');
    var jsonObject = { port: jackett_port, external: jackett_external };
    var jqxhr = $.post("/admin/set_port", JSON.stringify(jsonObject), function (data) {
        if (data.result == "error") {
            doNotify("Error: " + data.error, "danger", "glyphicon glyphicon-alert");
            return;
        } else {
            doNotify("The port has been changed. Redirecting you to the new port.", "success", "glyphicon glyphicon-ok");
            window.setTimeout(function () {
                url = window.location.href;
                if (data.external) {
                    window.location.href = url.substr(0, url.lastIndexOf(":") + 1) + data.port;
                } else {
                    window.location.href = 'http://127.0.0.1:' + data.port;
                }
            }, 3000);

        }
    }).fail(function () {
        doNotify("Request to Jackett server failed", "danger", "glyphicon glyphicon-alert");
    });
});

$("#change-jackett-password").click(function () {
    var password = $("#jackett-adminpwd").val();
    var jsonObject = { password: password };

    var jqxhr = $.post("/admin/set_admin_password", JSON.stringify(jsonObject), function (data) {

        if (data.result == "error") {
            doNotify("Error: " + data.error, "danger", "glyphicon glyphicon-alert");
            return;
        } else {
            doNotify("Admin password has been set.", "success", "glyphicon glyphicon-ok");

            window.setTimeout(function () {
                window.location = window.location.pathname;
            }, 1000);

        }
    }).fail(function () {
        doNotify("Request to Jackett server failed", "danger", "glyphicon glyphicon-alert");
    });
});

function getJackettConfig(callback) {
    var jqxhr = $.get("/admin/get_jackett_config", function (data) {

        callback(data);
    }).fail(function () {
        doNotify("Error loading Jackett settings, request to Jackett server failed", "danger", "glyphicon glyphicon-alert");
    });
}

function reloadIndexers() {
    $('#indexers').hide();
    $('#indexers > .indexer').remove();
    $('#unconfigured-indexers').empty();
    var jqxhr = $.get("/admin/get_indexers", function (data) {
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
            var jqxhr = $.post("/admin/delete_indexer", JSON.stringify({ indexer: id }), function (data) {
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
            var jqxhr = $.post("/admin/test_indexer", JSON.stringify({ indexer: id }), function (data) {
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

    var jqxhr = $.post("/admin/get_config_form", JSON.stringify({ indexer: id }), function (data) {
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
    // Set flag so we show fields named password as a password input
    for (var i = 0; i < config.length; i++) {
        config[i].ispassword = config[i].id.toLowerCase() === 'password';
    }
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
                configJson[id] = $el.find(".setup-item-inputstring input").val();
                break;
            case "inputbool":
                configJson[id] = $el.find(".setup-item-inputbool input").is(":checked");
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

        var jqxhr = $.post("/admin/configure_indexer", JSON.stringify(data), function (data) {
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

