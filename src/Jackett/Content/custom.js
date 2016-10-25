var basePath = '';

$(document).ready(function () {
    $.ajaxSetup({ cache: false });
    window.jackettIsLocal = window.location.hostname === 'localhost' ||
                    window.location.hostname === '127.0.0.1';

    bindUIButtons();
    loadJackettSettings();
   
});

function getJackettConfig(callback) {
    var jqxhr = $.get("get_jackett_config", function (data) {

        callback(data);
    }).fail(function () {
        doNotify("Error loading Jackett settings, request to Jackett server failed", "danger", "glyphicon glyphicon-alert");
    });
}

function loadJackettSettings() {
    getJackettConfig(function (data) {
        $("#api-key-input").val(data.config.api_key);
        $("#app-version").html(data.app_version);
        $("#jackett-port").val(data.config.port);
        $("#jackett-basepathoverride").val(data.config.basepathoverride);
        basePath = data.config.basepathoverride;
        if (basePath === null || basePath === undefined) {
            basePath = '';
        }
       
        $("#jackett-savedir").val(data.config.blackholedir);
        $("#jackett-allowext").attr('checked', data.config.external);
        $("#jackett-allowupdate").attr('checked', data.config.updatedisabled);
        $("#jackett-prerelease").attr('checked', data.config.prerelease);
        $("#jackett-logging").attr('checked', data.config.logging);
        var password = data.config.password;
        $("#jackett-adminpwd").val(password);
        if (password != null && password != '') {
            $("#logoutBtn").show();
        }

        reloadIndexers();
    });
}

function reloadIndexers() {
    $('#indexers').hide();
    $('#indexers > .indexer').remove();
    $('#unconfigured-indexers').empty();
    var jqxhr = $.get("get_indexers", function (data) {
        displayIndexers(data.items);
    }).fail(function () {
        doNotify("Error loading indexers, request to Jackett server failed", "danger", "glyphicon glyphicon-alert");
    });
}

function displayIndexers(items) {
    var indexerTemplate = Handlebars.compile($("#configured-indexer").html());
    var unconfiguredIndexerTemplate = Handlebars.compile($("#unconfigured-indexer").html());
    $('#unconfigured-indexers-template').empty();
    for (var i = 0; i < items.length; i++) {
        var item = items[i];
        item.torznab_host = resolveUrl(basePath + "/torznab/" + item.id);
        item.potato_host = resolveUrl(basePath + "/potato/" + item.id);
        if (item.configured)
            $('#indexers').append(indexerTemplate(item));
        else
            $('#unconfigured-indexers-template').append($(unconfiguredIndexerTemplate(item)));
    }

    var addIndexerButton = $($('#add-indexer').html());
    addIndexerButton.appendTo($('#indexers'));

    addIndexerButton.click(function () {
        $("#modals").empty();
        var dialog = $($("#select-indexer").html());
        dialog.find('#unconfigured-indexers').html($('#unconfigured-indexers-template').html());
        $("#modals").append(dialog);
        dialog.modal("show");
        $('.indexer-setup').each(function (i, btn) {
            var $btn = $(btn);
            var id = $btn.data("id");
            var link = $btn.data("link");
            $btn.click(function () {
                $('#select-indexer-modal').modal('hide').on('hidden.bs.modal', function (e) {
                    displayIndexerSetup(id, link);
                });
            });
        });
    });

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
        var link = $btn.data("link");
        $btn.click(function () {
            displayIndexerSetup(id, link);
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
                    doNotify("Test failed for " + id + ": \n" + data.error, "danger", "glyphicon glyphicon-alert");
                }
                else {
                    doNotify("Test successful for " + id, "success", "glyphicon glyphicon-ok");
                }
            }).fail(function () {
                doNotify("Error testing indexer, request to Jackett server error", "danger", "glyphicon glyphicon-alert");
            });
        });
    });
}

function displayIndexerSetup(id, link) {

    var jqxhr = $.post("get_config_form", JSON.stringify({ indexer: id }), function (data) {
        if (data.result == "error") {
            doNotify("Error: " + data.error, "danger", "glyphicon glyphicon-alert");
            return;
        }

        populateSetupForm(id, data.name, data.config, data.caps, link);

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

    $('.jackettrecaptcha').remove();

    var hasReacaptcha = false;
    var captchaItem = null;
    for (var i = 0; i < config.length; i++) {
        if (config[i].type === 'recaptcha') {
            hasReacaptcha = true;
            captchaItem = config[i];
        }
    }

    var setupItemTemplate = Handlebars.compile($("#setup-item").html());
    if (hasReacaptcha && !window.jackettIsLocal) {
        var setupValueTemplate = Handlebars.compile($("#setup-item-nonlocalrecaptcha").html());
        captchaItem.value_element = setupValueTemplate(captchaItem);
        var template = setupItemTemplate(captchaItem);
        $formItemContainer.append(template);
    } else {

        for (var i = 0; i < config.length; i++) {
            var item = config[i];
            var setupValueTemplate = Handlebars.compile($("#setup-item-" + item.type).html());
            item.value_element = setupValueTemplate(item);
            var template = setupItemTemplate(item);
            $formItemContainer.append(template);
            if (item.type === 'recaptcha') {
                var jackettrecaptcha = $('.jackettrecaptcha');
                jackettrecaptcha.data("version", item.version);
                switch (item.version) {
                    case "1":
                        // The v1 reCAPTCHA code uses document.write() calls to write the CAPTCHA to the location where the script was loaded.
                        // As it's loaded async this doesn't work.
                        // We use an iframe to work around this problem.
                        var html = '<script type="text/javascript" src="https://www.google.com/recaptcha/api/challenge?k='+encodeURIComponent(item.sitekey)+'"></script>';
                        var frame = document.createElement('iframe');
                        frame.id = "jackettrecaptchaiframe";
                        frame.style.height = "145px";
                        frame.style.weight = "326px";
                        frame.style.border = "none";
                        frame.onload = function () {
                            // auto resize iframe to content
                            frame.style.height = frame.contentWindow.document.body.scrollHeight + 'px';
                            frame.style.width = frame.contentWindow.document.body.scrollWidth + 'px';
                        }
                        jackettrecaptcha.append(frame);
                        frame.contentDocument.open();
                        frame.contentDocument.write(html);
                        frame.contentDocument.close();
                        break;
                    case "2":
                        grecaptcha.render(jackettrecaptcha[0], {
                            'sitekey': item.sitekey
                        });
                        break;
                }
            }
        }
    }
}

function newConfigModal(title, config, caps, link) {
    var configTemplate = Handlebars.compile($("#jackett-config-setup-modal").html());
    var configForm = $(configTemplate({ title: title, caps: caps, link:link }));
    $("#modals").append(configForm);
    populateConfigItems(configForm, config);
    return configForm;
}

function getConfigModalJson(configForm) {
    var configJson = [];
    configForm.find(".config-setup-form").children().each(function (i, el) {
        $el = $(el);
        var type = $el.data("type");
        var id = $el.data("id");
        var itemEntry = { id: id };
        switch (type) {
            case "hiddendata":
                itemEntry.value = $el.find(".setup-item-hiddendata input").val();
                break;
            case "inputstring":
                itemEntry.value = $el.find(".setup-item-inputstring input").val();
                break;
            case "inputbool":
                itemEntry.value = $el.find(".setup-item-inputbool input").is(":checked");
                break;
            case "recaptcha":
                if (window.jackettIsLocal) {
                    var version = $el.find('.jackettrecaptcha').data("version");
                    switch (version) {
                        case "1":
                            var frameDoc = $("#jackettrecaptchaiframe")[0].contentDocument;
                            itemEntry.version = version;
                            itemEntry.challenge = $("#recaptcha_challenge_field", frameDoc).val()
                            itemEntry.value = $("#recaptcha_response_field", frameDoc).val()
                            break;
                        case "2":
                            itemEntry.value = $('.g-recaptcha-response').val();
                            break;
                    }
                } else {
                    itemEntry.cookie = $el.find(".setup-item-recaptcha input").val();
                }
                break;
        }
        configJson.push(itemEntry)
    });
    return configJson;
}

function populateSetupForm(indexerId, name, config, caps, link) {
    var configForm = newConfigModal(name, config, caps, link);
    var $goButton = configForm.find(".setup-indexer-go");
    $goButton.click(function () {
        var data = { indexer: indexerId, name: name };
        data.config = getConfigModalJson(configForm);

        var originalBtnText = $goButton.html();
        $goButton.prop('disabled', true);
        $goButton.html($('#spinner').html());

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


function bindUIButtons() {
    $('body').on('click', '.downloadlink', function (e, b) {
        $(e.target).addClass('jackettdownloaded');
    });

    $('body').on('click', '.jacketdownloadserver', function (event) {
        var href = $(event.target).parent().attr('href');
        var jqxhr = $.get(href, function (data) {
            if (data.result == "error") {
                doNotify("Error: " + data.error, "danger", "glyphicon glyphicon-alert");
                return;
            } else {
                doNotify("Downloaded sent to the blackhole successfully.", "success", "glyphicon glyphicon-ok");
            }
        }).fail(function () {
            doNotify("Request to Jackett server failed", "danger", "glyphicon glyphicon-alert");
        });
        event.preventDefault();
        return false;
    });

    $("#jackett-show-releases").click(function () {
        var jqxhr = $.get("GetCache", function (data) {
            var releaseTemplate = Handlebars.compile($("#jackett-releases").html());
            var item = { releases: data, Title: 'Releases' };
            var releaseDialog = $(releaseTemplate(item));
            releaseDialog.find('table').DataTable(
                 {
                     "pageLength": 20,
                     "lengthMenu": [[10, 20, 50, -1], [10, 20, 50, "All"]],
                     "order": [[0, "desc"]],
                     "columnDefs": [
                        {
                            "targets": 0,
                            "visible": false,
                            "searchable": false,
                            "type": 'date'
                        },
                        {
                            "targets": 1,
                            "visible": false,
                            "searchable": false,
                            "type": 'date'
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
                        },
                        {
                            "targets": 6,
                            "visible": false,
                            "searchable": false,
                            "type": 'num'
                        },
                        {
                            "targets": 7,
                            "visible": true,
                            "searchable": false,
                            "iDataSort": 6
                        }
                     ],
                     initComplete: function () {
                         var count = 0;
                         this.api().columns().every(function () {
                             count++;
                             if (count === 5 || count === 9) {
                                 var column = this;
                                 var select = $('<select><option value=""></option></select>')
                                     .appendTo($(column.footer()).empty())
                                     .on('change', function () {
                                         var val = $.fn.dataTable.util.escapeRegex(
                                             $(this).val()
                                         );

                                         column
                                             .search(val ? '^' + val + '$' : '', true, false)
                                             .draw();
                                     });

                                 column.data().unique().sort().each(function (d, j) {
                                     select.append('<option value="' + d + '">' + d + '</option>')
                                 });
                             }
                         });
                     }
                 });
            $("#modals").append(releaseDialog);
            releaseDialog.modal("show");

        }).fail(function () {
            doNotify("Request to Jackett server failed", "danger", "glyphicon glyphicon-alert");
        });
    });

    $("#jackett-show-search").click(function () {
        $('#select-indexer-modal').remove();
        var jqxhr = $.get("get_indexers", function (data) {
            var scope = {
                items: data.items
            };

            var indexers = [];
            indexers.push({ id: '', name: '-- All --' });
            for (var i = 0; i < data.items.length; i++) {
                if (data.items[i].configured === true) {
                    indexers.push(data.items[i]);
                }
            }

            var releaseTemplate = Handlebars.compile($("#jackett-search").html());
            var releaseDialog = $(releaseTemplate({ indexers: indexers }));
            $("#modals").append(releaseDialog);
            releaseDialog.modal("show");
            
            releaseDialog.on('shown.bs.modal', function() {
                releaseDialog.find('#searchquery').focus();
            });

            var setCategories = function (tracker, items) {
                var cats = {};
                for (var i = 0; i < items.length; i++) {
                    if (items[i].configured === true && (items[i].id === tracker || tracker === '')) {
                        indexers["'" + items[i].id + "'"] = items[i].name;
                        for (var prop in items[i].caps) {
                            cats[prop] = items[i].caps[prop];
                        }
                    }
                }
                var select = $('#searchCategory');
                select.html("<option value=''>-- All --</option>");
                $.each(cats, function (value, key) {
                    select.append($("<option></option>")
                       .attr("value", value).text(key + ' (' + value + ')'));
                });
            };

            setCategories('', data.items);
            $('#searchTracker').change(jQuery.proxy(function () {
                var trackerId = $('#searchTracker').val();
                setCategories(trackerId, this.items);
            }, scope));

            document.getElementById("searchquery")
            .addEventListener("keyup", function (event) {
                event.preventDefault();
                if (event.keyCode == 13) {
                    document.getElementById("jackett-search-perform").click();
                }
            });

            $('#jackett-search-perform').click(function () {
                if ($('#jackett-search-perform').text().trim() !== 'Search trackers') {
                    // We are searchin already
                    return;
                }
                var queryObj = {
                    Query: releaseDialog.find('#searchquery').val(),
                    Category: releaseDialog.find('#searchCategory').val(),
                    Tracker: releaseDialog.find('#searchTracker').val().replace("'", "").replace("'", ""),
                };
                $('#searchResults').empty();

                $('#jackett-search-perform').html($('#spinner').html());
                var jqxhr = $.post("search", queryObj, function (data) {
                    $('#jackett-search-perform').html('Search trackers');
                    var resultsTemplate = Handlebars.compile($("#jackett-search-results").html());
                    var results = $('#searchResults');
                    results.html($(resultsTemplate(data)));

                    results.find('table').DataTable(
                    {
                        "pageLength": 20,
                        "lengthMenu": [[10, 20, 50, -1], [10, 20, 50, "All"]],
                        "order": [[0, "desc"]],
                        "columnDefs": [
                           {
                               "targets": 0,
                               "visible": false,
                               "searchable": false,
                               "type": 'date'
                           },
                           {
                               "targets": 1,
                               "visible": true,
                               "searchable": false,
                               "iDataSort": 0
                           },
                           {
                                "targets": 4,
                                "visible": false,
                                "searchable": false,
                                "type": 'num'
                           },
                           {
                                 "targets": 5,
                                 "visible": true,
                                 "searchable": false,
                                 "iDataSort": 4
                           }
                        ],
                        initComplete: function () {
                            var count = 0;
                            this.api().columns().every(function () {
                                count++;
                                if (count === 3 || count === 7) {
                                    var column = this;
                                    var select = $('<select><option value=""></option></select>')
                                        .appendTo($(column.footer()).empty())
                                        .on('change', function () {
                                            var val = $.fn.dataTable.util.escapeRegex(
                                                $(this).val()
                                            );

                                            column
                                                .search(val ? '^' + val + '$' : '', true, false)
                                                .draw();
                                        });

                                    column.data().unique().sort().each(function (d, j) {
                                        select.append('<option value="' + d + '">' + d + '</option>')
                                    });
                                }
                            });
                        }
                    });

                }).fail(function () {
                    $('#jackett-search-perform').html('Search trackers');
                    doNotify("Request to Jackett server failed", "danger", "glyphicon glyphicon-alert");
                });
            });

        }).fail(function () {
            doNotify("Error loading indexers, request to Jackett server failed", "danger", "glyphicon glyphicon-alert");
        });
    });

    $("#view-jackett-logs").click(function () {
        var jqxhr = $.get("GetLogs", function (data) {
            var releaseTemplate = Handlebars.compile($("#jackett-logs").html());
            var item = { logs: data };
            var releaseDialog = $(releaseTemplate(item));
            $("#modals").append(releaseDialog);
            releaseDialog.modal("show");

        }).fail(function () {
            doNotify("Request to Jackett server failed", "danger", "glyphicon glyphicon-alert");
        });
    });

    $("#change-jackett-port").click(function () {
        var jackett_port = $("#jackett-port").val();
        var jackett_basepathoverride = $("#jackett-basepathoverride").val();
        var jackett_external = $("#jackett-allowext").is(':checked');
        var jackett_update = $("#jackett-allowupdate").is(':checked'); 
        var jackett_prerelease = $("#jackett-prerelease").is(':checked'); 
        var jackett_logging = $("#jackett-logging").is(':checked');
        var jsonObject = {
            port: jackett_port,
            external: jackett_external,
            updatedisabled: jackett_update,
            prerelease: jackett_prerelease,
            blackholedir: $("#jackett-savedir").val(),
            logging: jackett_logging,
            basepathoverride: jackett_basepathoverride
        };
        var jqxhr = $.post("set_config", JSON.stringify(jsonObject), function (data) {
            if (data.result == "error") {
                doNotify("Error: " + data.error, "danger", "glyphicon glyphicon-alert");
                return;
            } else {
                doNotify("Redirecting you to complete configuration update..", "success", "glyphicon glyphicon-ok");
                window.setTimeout(function () {
                    window.location.reload(true);
                }, 3000);

            }
        }).fail(function () {
            doNotify("Request to Jackett server failed", "danger", "glyphicon glyphicon-alert");
        });
    });

    $("#trigger-updater").click(function () {
        var jqxhr = $.get("trigger_update", function (data) {
            if (data.result == "error") {
                doNotify("Error: " + data.error, "danger", "glyphicon glyphicon-alert");
                return;
            } else {
                doNotify("Updater triggered see log for details..", "success", "glyphicon glyphicon-ok");
            }
        }).fail(function () {
            doNotify("Request to Jackett server failed", "danger", "glyphicon glyphicon-alert");
        });
    });

    $("#change-jackett-password").click(function () {
        var password = $("#jackett-adminpwd").val();
        var jsonObject = { password: password };

        var jqxhr = $.post("set_admin_password", JSON.stringify(jsonObject), function (data) {

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
}
