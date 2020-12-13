var basePath = '';

var indexers = [];
var configuredIndexers = [];
var unconfiguredIndexers = [];

$.fn.inView = function() {
    if(!this.length) return false;
    var rect = this.get(0).getBoundingClientRect();

    return (
        rect.top >= 0 &&
        rect.left >= 0 &&
        rect.bottom <= (window.innerHeight || document.documentElement.clientHeight) &&
        rect.right <= (window.innerWidth || document.documentElement.clientWidth)
    );
};

$.fn.focusWithoutScrolling = function () {
    if (this.inView())
        this.focus();
    return this;
};

$(document).ready(function () {
    $.ajaxSetup({ cache: false });

    Handlebars.registerHelper('if_eq', function(a, b, opts) {
	    if (a == b)
	        return opts.fn(this);
	    else
	        return opts.inverse(this);
    });

    Handlebars.registerHelper('if_in', function(elem, list, opts) {
        if(list.indexOf(elem) > -1) {
            return opts.fn(this);
        }

        return opts.inverse(this);
    });

    var index = window.location.pathname.indexOf("/UI");
    var pathPrefix = window.location.pathname.substr(0, index);
    api.root = pathPrefix + api.root;

    bindUIButtons();
    loadJackettSettings();
});

function openSearchIfNecessary() {
    const hashArgs = location.hash.substring(1).split('&').reduce((prev, item) =>
      Object.assign({
        [item.split('=')[0]]: (item.split('=').length < 2 ?
          undefined :
          decodeURIComponent(item.split('=')[1].replace(/\+/g,'%20')))
      }, prev), {});
    if ("search" in hashArgs) {
        showSearch(hashArgs.tracker, hashArgs.search, hashArgs.category);
    }
}

function insertWordWrap(str) {
    // insert optional word wrap after punctuation to avoid overflows on long scene titles
    return str.replace(/([\.\-_\/\\])/g, "$1\u200B");
}

function getJackettConfig(callback) {
    api.getServerConfig(callback).fail(function () {
        doNotify("Error loading Jackett settings, request to Jackett server failed, is server running ?", "danger", "glyphicon glyphicon-alert");
    });
}

function loadJackettSettings() {
    getJackettConfig(function (data) {
        $("#api-key-input").val(data.api_key);
        $(".api-key-text").text(data.api_key);
        $("#app-version").html(data.app_version);
        $("#jackett-port").val(data.port);

        $("#jackett-proxy-type").val(data.proxy_type);
        $("#jackett-proxy-url").val(data.proxy_url);
        $("#jackett-proxy-port").val(data.proxy_port);
        $("#jackett-proxy-username").val(data.proxy_username);
        $("#jackett-proxy-password").val(data.proxy_password);
        proxyWarning(data.proxy_type);

        $("#jackett-basepathoverride").val(data.basepathoverride);
        basePath = data.basepathoverride;
        if (basePath === null || basePath === undefined) {
            basePath = '';
        }

        api.key = data.api_key;

        $("#jackett-savedir").val(data.blackholedir);
        $("#jackett-allowext").attr('checked', data.external);
        $("#jackett-allowupdate").attr('checked', data.updatedisabled);
        $("#jackett-prerelease").attr('checked', data.prerelease);
        $("#jackett-logging").attr('checked', data.logging);

        $("#jackett-cache-enabled").attr('checked', data.cache_enabled);
        $("#jackett-cache-ttl").val(data.cache_ttl);
        $("#jackett-cache-max-results-per-indexer").val(data.cache_max_results_per_indexer);
        if (!data.cache_enabled) {
            $("#jackett-show-releases").attr("disabled", true);
        }

        $("#jackett-flaresolverrurl").val(data.flaresolverrurl);
        $("#jackett-omdbkey").val(data.omdbkey);
        $("#jackett-omdburl").val(data.omdburl);
        var password = data.password;
        $("#jackett-adminpwd").val(password);
        if (password != null && password != '') {
            $("#logoutBtn").show();
        }

        if (data.can_run_netcore != null && data.can_run_netcore === true) {
            $("#can-upgrade-from-mono").show();
        }

        $.each(data.notices, function (index, value) {
            console.log(value);
            doNotify(value, "danger", "glyphicon glyphicon-alert", false);
        });

        reloadIndexers();
    });
}

function reloadIndexers() {
    $('#indexers').hide();
    api.getAllIndexers(function (data) {
        indexers = data;
        configuredIndexers = [];
        unconfiguredIndexers = [];
        for (var i = 0; i < data.length; i++) {
            var item = data[i];
            item.rss_host = resolveUrl(basePath + "/api/v2.0/indexers/" + item.id + "/results/torznab/api?apikey=" + api.key + "&t=search&cat=&q=");
            item.torznab_host = resolveUrl(basePath + "/api/v2.0/indexers/" + item.id + "/results/torznab/");
            item.potato_host = resolveUrl(basePath + "/api/v2.0/indexers/" + item.id + "/results/potato/");

            if (item.last_error)
                item.state = "error";
            else
                item.state = "success";

            if (item.type == "public") {
                item.type_label = "success";
            }
            else if (item.type == "private") {
                item.type_label = "danger";
            }
            else if (item.type == "semi-private") {
                item.type_label = "warning";
            }
            else {
                item.type_label = "default";
            }

            var main_cats_list = item.caps.filter(function(c) {
                return c.ID < 100000;
            }).map(function(c) {
                return c.Name.split("/")[0];
            });
            item.mains_cats = $.unique(main_cats_list).join(", ");

            if (item.configured)
                configuredIndexers.push(item);
            else
                unconfiguredIndexers.push(item);
        }
        displayConfiguredIndexersList(configuredIndexers);
        $('#indexers div.dataTables_filter input').focusWithoutScrolling();
        openSearchIfNecessary();
    }).fail(function () {
        doNotify("Error loading indexers, request to Jackett server failed, is server running ?", "danger", "glyphicon glyphicon-alert");
    });
}

function displayConfiguredIndexersList(indexers) {
    var indexersTemplate = Handlebars.compile($("#configured-indexer-table").html());
    var indexersTable = $(indexersTemplate({ indexers: indexers, total_configured_indexers: indexers.length }));
    prepareTestButtons(indexersTable);
    prepareSearchButtons(indexersTable);
    prepareSetupButtons(indexersTable);
    prepareDeleteButtons(indexersTable);
    prepareCopyButtons(indexersTable);
    indexersTable.find("table").dataTable(
         {
             "stateSave": true,
             "stateDuration": 0,
             "pageLength": -1,
             "lengthMenu": [[10, 20, 50, 100, 250, 500, -1], [10, 20, 50, 100, 250, 500, "All"]],
             "order": [[0, "asc"]],
             "columnDefs": [
                {
                    "targets": 0,
                    "visible": true,
                    "searchable": true,
                    "orderable": true
                },
                {
                    "targets": 1,
                    "visible": true,
                    "searchable": true,
                    "orderable": true
                }
             ]
         });

    $('#indexers').empty();
    $('#indexers').append(indexersTable);
    $('#indexers').fadeIn();
}

function displayUnconfiguredIndexersList() {
    var UnconfiguredIndexersDialog = $($("#select-indexer").html());

    var indexersTemplate = Handlebars.compile($("#unconfigured-indexer-table").html());
    var indexersTable = $(indexersTemplate({ indexers: unconfiguredIndexers, total_unconfigured_indexers: unconfiguredIndexers.length  }));
    indexersTable.find('.indexer-setup').each(function (i, btn) {
        var indexer = unconfiguredIndexers[i];
        $(btn).click(function () {
            $('#select-indexer-modal').modal('hide').on('hidden.bs.modal', function (e) {
                displayIndexerSetup(indexer.id, indexer.name, indexer.caps, indexer.link, indexer.alternativesitelinks, indexer.description);
            });
        });
    });
    indexersTable.find('.indexer-add').each(function (i, btn) {
        $(btn).click(function () {
            $('#select-indexer-modal').modal('hide').on('hidden.bs.modal', function (e) {
                var indexerId = $(btn).attr("data-id");
                api.getIndexerConfig(indexerId, function (data) {
			        if (data.result !== undefined && data.result == "error") {
			            doNotify("Error: " + data.error, "danger", "glyphicon glyphicon-alert");
			            return;
			        }
	                api.updateIndexerConfig(indexerId, data, function (data) {
		                if (data == undefined) {
		                    reloadIndexers();
		                    doNotify("Successfully configured " + name, "success", "glyphicon glyphicon-ok");
		                } else if (data.result == "error") {
		                    if (data.config) {
		                        populateConfigItems(configForm, data.config);
		                    }
		                    doNotify("Configuration failed: " + data.error, "danger", "glyphicon glyphicon-alert");
		                }
			        }).fail(function (data) {
                if(data.responseJSON.error !== undefined) {
                  var indexEnd = 2048 - "https://github.com/Jackett/Jackett/issues/new?title=[".length - indexerId.length - "] ".length - " (Config)".length; // keep url <= 2k #5104
                  doNotify("An error occurred while configuring this indexer<br /><b>" + data.responseJSON.error.substring(0, indexEnd) + "</b><br /><i><a href=\"https://github.com/Jackett/Jackett/issues/new?title=[" + indexerId + "] " + data.responseJSON.error.substring(0, indexEnd) + " (Config)\" target=\"_blank\">Click here to open an issue on GitHub for this indexer.</a><i>", "danger", "glyphicon glyphicon-alert", false);
                } else {
                  doNotify("An error occurred while configuring this indexer, is Jackett server running ?", "danger", "glyphicon glyphicon-alert");
                }
			        });
                });
            });
        });
    });
    indexersTable.find("table").DataTable(
        {
            "stateSave": true,
            "stateDuration": 0,
            "fnStateSaveParams": function (oSettings, sValue) {
                sValue.search.search = ""; // don't save the search filter content
                return sValue;
            },
            "bAutoWidth": false,
            "pageLength": -1,
            "lengthMenu": [[10, 20, 50, 100, 250, 500, -1], [10, 20, 50, 100, 250, 500, "All"]],
            "order": [[0, "asc"]],
            "columnDefs": [
                {
                    "name": "name",
                    "targets": 0,
                    "visible": true,
                    "searchable": true,
                    "orderable": true
                },
                {
                    "name": "description",
                    "targets": 1,
                    "visible": true,
                    "searchable": true,
                    "orderable": true
                },
                {
                    "name": "type",
                    "targets": 2,
                    "visible": true,
                    "searchable": true,
                    "orderable": true
                },
                {
                    "name": "type_string",
                    "targets": 3,
                    "visible": false,
                    "searchable": true,
                    "orderable": true,
                },
                {
                    "name": "language",
                    "targets": 4,
                    "visible": true,
                    "searchable": true,
                    "orderable": true
                },
                {
                    "name": "buttons",
                    "targets": 5,
                    "visible": true,
                    "searchable" : false,
                    "orderable": false
                }
            ]
        });

    var undefindexers = UnconfiguredIndexersDialog.find('#unconfigured-indexers');
    undefindexers.append(indexersTable);

    UnconfiguredIndexersDialog.on('shown.bs.modal', function() {
        $(this).find('div.dataTables_filter input').focusWithoutScrolling();
    });

    UnconfiguredIndexersDialog.on('hidden.bs.modal', function (e) {
        $('#indexers div.dataTables_filter input').focusWithoutScrolling();
    });

    $("#modals").append(UnconfiguredIndexersDialog);

    UnconfiguredIndexersDialog.modal("show");
}

function copyToClipboard(text) {
    // create hidden text element, if it doesn't already exist
    var targetId = "_hiddenCopyText_";
    // must use a temporary form element for the selection and copy
    target = document.getElementById(targetId);
    if (!target) {
        var target = document.createElement("textarea");
        target.style.position = "fixed";
        target.style.left = "-9999px";
        target.style.top = "0";
        target.id = targetId;
        document.body.appendChild(target);
    }
    target.textContent = text;
    // select the content
    var currentFocus = document.activeElement;
    target.focus();
    target.setSelectionRange(0, target.value.length);

    // copy the selection
    var succeed;
    try {
        succeed = document.execCommand("copy");
        doNotify("Copied to clipboard!", "success", "glyphicon glyphicon-ok");
    } catch (e) {
        succeed = false;
    }
    // restore original focus
    if (currentFocus && typeof currentFocus.focus === "function") {
        $(currentFocus).focusWithoutScrolling();
    }

    target.textContent = "";

    return succeed;
}

function prepareCopyButtons(element) {
    element.find(".indexer-button-copy").each(function (i, btn) {
        var $btn = $(btn);
        var title = $btn[0].title;

        $btn.click(function () {
            copyToClipboard(title);
            return false;
        });
    });
}

function prepareDeleteButtons(element) {
    element.find(".indexer-button-delete").each(function (i, btn) {
        var $btn = $(btn);
        var id = $btn.data("id");
        $btn.click(function () {
            api.deleteIndexer(id, function (data) {
                if (data == undefined) {
                    doNotify("Deleted " + id, "success", "glyphicon glyphicon-ok");
                } else if (data.result == "error") {
                    doNotify("Delete error for " + id + "\n" + data.error, "danger", "glyphicon glyphicon-alert");
                }
            }).fail(function () {
                doNotify("Error deleting indexer, request to Jackett server error", "danger", "glyphicon glyphicon-alert");
            }).always(function () {
                reloadIndexers();
            });
        });
    });
}

function prepareSearchButtons(element) {
    element.find('.indexer-button-search').each(function (i, btn) {
        var $btn = $(btn);
        var id = $btn.data("id");
        $btn.click(function() {
            window.location.hash = "search&tracker=" +  id;
            showSearch(id);
        });
    });
}

function prepareSetupButtons(element) {
    element.find('.indexer-setup').each(function (i, btn) {
        var indexer = configuredIndexers[i];
        $(btn).click(function () {
            displayIndexerSetup(indexer.id, indexer.name, indexer.caps, indexer.link, indexer.alternativesitelinks, indexer.description);
        });
    });
}

function updateTestState(id, state, message, parent)
{
    var btn = parent.find(".indexer-button-test[data-id=" +id + "]");

    var sortmsg = message;
    if (!sortmsg || state == "success")
        sortmsg = "";

    var td = btn.closest("td");
    td.attr("data-sort", sortmsg);
    td.attr("data-filter", sortmsg);

    if (message) {
        btn.tooltip("hide");
        btn.attr("title", message);
        btn.data('bs.tooltip', false).tooltip({ title: message });

    }
    var icon = btn.find("span");
    icon.removeClass("glyphicon-ok test-success glyphicon-alert test-error glyphicon-refresh spinner test-inprogres");

    if (state == "success") {
        icon.addClass("glyphicon-ok test-success");
    } else if (state == "error") {
        icon.addClass("glyphicon-alert test-error");
    } else if (state == "inprogres") {
        icon.addClass("glyphicon-refresh test-inprogres spinner");
    }
    var dt = $.fn.dataTable.tables({ visible: true, api: true}).rows().invalidate('dom');
    if (state != "inprogres")
        dt.draw();
}

function testIndexer(id, notifyResult) {
    var indexers = $('#indexers');
    updateTestState(id, "inprogres", null, indexers);

    if (notifyResult)
        doNotify("Test started for " + id, "info", "glyphicon glyphicon-transfer");
    api.testIndexer(id, function (data) {
        if (data == undefined) {
            updateTestState(id, "success", "Test successful", indexers);
            if (notifyResult)
                doNotify("Test successful for " + id, "success", "glyphicon glyphicon-ok");
        } else if (data.result == "error") {
            updateTestState(id, "error", data.error, indexers);
            if (notifyResult)
                doNotify("Test failed for " + id + ": \n" + data.error, "danger", "glyphicon glyphicon-alert");
        }
    }).fail(function (data) {
      updateTestState(id, "error", data.error, indexers);
      if(data.responseJSON.error !== undefined && notifyResult) {
        var indexEnd = 2048 - "https://github.com/Jackett/Jackett/issues/new?title=[".length - id.length - "] ".length - " (Test)".length; // keep url <= 2k #5104
        doNotify("An error occurred while testing this indexer<br /><b>" + data.responseJSON.error.substring(0, indexEnd) + "</b><br /><i><a href=\"https://github.com/Jackett/Jackett/issues/new?title=[" + id + "] " + data.responseJSON.error.substring(0, indexEnd) + " (Test)\" target=\"_blank\">Click here to open an issue on GitHub for this indexer.</a><i>", "danger", "glyphicon glyphicon-alert", false);
      } else {
        doNotify("An error occurred while testing indexers, please take a look at indexers with failed test for more informations.", "danger", "glyphicon glyphicon-alert");
      }
    });
}

function prepareTestButtons(element) {
    element.find(".indexer-button-test").each(function (i, btn) {
        var $btn = $(btn);
        var id = $btn.data("id");
        var state = $btn.data("state");
        var title = $btn.attr("title");
        $btn.tooltip();
        updateTestState(id, state, title, element);
        $btn.click(function () {
            testIndexer(id, true);
        });
    });
}

function displayIndexerSetup(id, name, caps, link, alternativesitelinks, description) {
    api.getIndexerConfig(id, function (data) {
        if (data.result !== undefined && data.result == "error") {
            doNotify("Error: " + data.error, "danger", "glyphicon glyphicon-alert");
            return;
        }

        populateSetupForm(id, name, data, caps, link, alternativesitelinks, description);
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

    var setupItemTemplate = Handlebars.compile($("#setup-item").html());
    for (var i = 0; i < config.length; i++) {
        var item = config[i];
        var setupValueTemplate = Handlebars.compile($("#setup-item-" + item.type).html());
        item.value_element = setupValueTemplate(item);
        var template = setupItemTemplate(item);
        $formItemContainer.append(template);
    }
}

function newConfigModal(title, config, caps, link, alternativesitelinks, description) {
    var configTemplate = Handlebars.compile($("#jackett-config-setup-modal").html());
    var configForm = $(configTemplate({ title: title, caps: caps, link: link, description: description }));
    $("#modals").append(configForm);
    populateConfigItems(configForm, config);

    if (alternativesitelinks.length >= 1) {
        var AlternativeSiteLinksTemplate = Handlebars.compile($("#setup-item-alternativesitelinks").html());
        var template = $(AlternativeSiteLinksTemplate({ "alternativesitelinks": alternativesitelinks }));
        configForm.find("div[data-id='sitelink']").after(template);
        template.find("a.alternativesitelink").click(function (a) {
            $("div[data-id='sitelink'] input").val(this.href);
            return false;
        });
    }

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
            case "inputcheckbox":
                itemEntry.values = [];
                $el.find(".setup-item-inputcheckbox input:checked").each(function () {
                  itemEntry.values.push($(this).val());
                });
            case "inputselect":
                itemEntry.value = $el.find(".setup-item-inputselect select").val();
                break;
        }
        configJson.push(itemEntry)
    });
    return configJson;
}

function populateSetupForm(indexerId, name, config, caps, link, alternativesitelinks, description) {
    var configForm = newConfigModal(name, config, caps, link, alternativesitelinks, description);
    var $goButton = configForm.find(".setup-indexer-go");
    $goButton.click(function () {
        var data = getConfigModalJson(configForm);

        var originalBtnText = $goButton.html();
        $goButton.prop('disabled', true);
        $goButton.html($('#spinner').html());

        api.updateIndexerConfig(indexerId, data, function (data) {
            if (data == undefined) {
                configForm.modal("hide");
                reloadIndexers();
                doNotify("Successfully configured " + name, "success", "glyphicon glyphicon-ok");
            } else if (data.result == "error") {
                if (data.config) {
                    populateConfigItems(configForm, data.config);
                }
                doNotify("Configuration failed: " + data.error, "danger", "glyphicon glyphicon-alert");
            }
        }).fail(function (data) {
          if(data.responseJSON.error !== undefined) {
            var indexEnd = 2048 - "https://github.com/Jackett/Jackett/issues/new?title=[".length - indexerId.length - "] ".length - " (Config)".length; // keep url <= 2k #5104
            doNotify("An error occurred while updating this indexer<br /><b>" + data.responseJSON.error.substring(0, indexEnd) + "</b><br /><i><a href=\"https://github.com/Jackett/Jackett/issues/new?title=[" + indexerId + "] " + data.responseJSON.error.substring(0, indexEnd) + " (Config)\" target=\"_blank\">Click here to open an issue on GitHub for this indexer.</a><i>", "danger", "glyphicon glyphicon-alert", false);
          } else {
            doNotify("An error occurred while updating this indexer, request to Jackett server failed, is server running ?", "danger", "glyphicon glyphicon-alert");
          }
        }).always(function () {
            $goButton.html(originalBtnText);
            $goButton.prop('disabled', false);
        });
    });

    configForm.on('hidden.bs.modal', function (e) {
        $('#indexers div.dataTables_filter input').focusWithoutScrolling();
        });
    configForm.modal("show");
}

function resolveUrl(url) {
    var a = document.createElement('a');
    a.href = url;
    url = a.href;
    return url;
}

function doNotify(message, type, icon, autoHide) {
    if (typeof autoHide === "undefined" || autoHide === null)
        autoHide = true;

    var delay = 5000;
    if (!autoHide)
        delay = -1;

    $.notify({
        message: message,
        icon: icon
    }, {
        element: 'body',
        autoHide: autoHide,
        delay: delay,
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

function updateReleasesRow(row)
{
    var labels = $(row).find("span.release-labels");
    var TitleLink = $(row).find("td.Title > a");
    var IMDBId = $(row).data("imdb");
    var Poster = $(row).data("poster");
    var Description = $(row).data("description");
    var DownloadVolumeFactor = parseFloat($(row).find("td.DownloadVolumeFactor").html());
    var UploadVolumeFactor = parseFloat($(row).find("td.UploadVolumeFactor").html());

    var TitleTooltip = "";
    if (Poster)
        TitleTooltip += "<img src='" + Poster + "' /><br />";
    if (Description)
        TitleTooltip += Description;

    if (TitleTooltip) {
        TitleLink.data("toggle", "tooltip");
        TitleLink.tooltip({
            title: TitleTooltip,
            html: true
        });
    }

    labels.empty();

    if (IMDBId) {
        labels.append('\n<a href="http://www.imdb.com/title/tt' + ("0000000" + IMDBId).slice(-8) + '/" class="label label-imdb" alt="IMDB" title="IMDB">IMDB</a>');
    }

    if (!isNaN(DownloadVolumeFactor)) {
        if (DownloadVolumeFactor == 0) {
            labels.append('\n<span class="label label-success">FREELEECH</span>');
        } else if (DownloadVolumeFactor < 1) {
            labels.append('\n<span class="label label-primary">' + (DownloadVolumeFactor * 100).toFixed(0) + '%DL</span>');
        } else if (DownloadVolumeFactor > 1) {
            labels.append('\n<span class="label label-danger">' + (DownloadVolumeFactor * 100).toFixed(0) + '%DL</span>');
        }
    }

    if (!isNaN(UploadVolumeFactor)) {
        if (UploadVolumeFactor == 0) {
            labels.append('\n<span class="label label-warning">NO UPLOAD</span>');
        } else if (UploadVolumeFactor != 1) {
            labels.append('\n<span class="label label-info">' + (UploadVolumeFactor * 100).toFixed(0) + '%UL</span>');
        }
    }
}

function showSearch(selectedIndexer, query, category) {
    var selectedIndexers = [];
    if (selectedIndexer)
        selectedIndexers = selectedIndexer.split(",");
    $('#select-indexer-modal').remove();
    var releaseTemplate = Handlebars.compile($("#jackett-search").html());
    var releaseDialog = $(releaseTemplate({
        indexers: configuredIndexers
    }));

    $("#modals").append(releaseDialog);

    releaseDialog.on('shown.bs.modal', function () {
        releaseDialog.find('#searchquery').focusWithoutScrolling();
    });

    releaseDialog.on('hidden.bs.modal', function (e) {
        $('#indexers div.dataTables_filter input').focusWithoutScrolling();
        window.location.hash = '';
    }) ;

    var setCategories = function (trackers, items) {
        var cats = {};
        for (var i = 0; i < items.length; i++) {
            if (trackers.length == 0 || $.inArray(items[i].id, trackers) !== -1) {
                for (var j in items[i].caps) {
                    var cat = items[i].caps[j]
                    if (cat.ID < 100000 || trackers.length == 1)
                        cats[cat.ID] = cat.Name;
                }
            }
        }
        var select = $('#searchCategory');
        var selected = select.val();
        var options = []
        $.each(cats, function (ID, Name) {
            options.push({ label: ID + ' (' + Name + ')', value: ID });
        });
        select.multiselect('dataprovider', options);
        select.val(selected).multiselect("refresh");
    };

    $('#searchTracker').change(jQuery.proxy(function () {
        var trackerIds = $('#searchTracker').val();
        setCategories(trackerIds, this.items);
    }, { items: configuredIndexers }));

    var queryField = document.getElementById("searchquery");
    queryField.addEventListener("keyup", function (event) {
        event.preventDefault();
        if (event.keyCode == 13) {
            document.getElementById("jackett-search-perform").click();
        }
    });

    var searchButton = $('#jackett-search-perform');
    searchButton.click(function () {
        if ($('#jackett-search-perform span').hasClass("spinner")) {
            // We are searchin already
            return;
        }
        var searchString = releaseDialog.find('#searchquery').val();
        var queryObj = {
            Query: searchString,
            Category: releaseDialog.find('#searchCategory').val(),
            Tracker: releaseDialog.find('#searchTracker').val()
        };

        window.location.hash = Object.entries({
          search: encodeURIComponent(queryObj.Query).replace(/%20/g,'+'),
          tracker: queryObj.Tracker.join(","),
          category: queryObj.Category.join(",")
        }).map(([k, v], i) => k + '=' + v).join('&');

        $('#jackett-search-perform').html($('#spinner').html());
        $('#searchResults div.dataTables_filter input').val("");
        clearSearchResultTable($('#searchResults'));

        var trackerId = "all";
        api.resultsForIndexer(trackerId, queryObj, function (data) {
            for (var i = 0; i < data.Results.length; i++) {
                var item = data.Results[i];
                item.Title = insertWordWrap(item.Title);
                item.CategoryDesc = insertWordWrap(item.CategoryDesc);
            }

            $('#jackett-search-perform').html($('#search-button-ready').html());
            var searchResults = $('#searchResults');
            searchResults.empty();
            updateSearchResultTable(searchResults, data).search('').columns().search('').draw();
            searchResults.find('div.dataTables_filter input').focusWithoutScrolling();
        }).fail(function () {
            $('#jackett-search-perform').html($('#search-button-ready').html());
            doNotify("Request to Jackett server failed", "danger", "glyphicon glyphicon-alert");
        });
    });

    var searchTracker = releaseDialog.find("#searchTracker");
    var searchCategory = releaseDialog.find('#searchCategory');
    searchCategory.multiselect({
        maxHeight: 400,
        enableFiltering: true,
        includeSelectAllOption: true,
        enableCaseInsensitiveFiltering: true,
        nonSelectedText: 'Any'
    });
    if (selectedIndexers)
        searchTracker.val(selectedIndexers);
    searchTracker.trigger("change");

    updateSearchResultTable($('#searchResults'), []);
    clearSearchResultTable($('#searchResults'));

    searchTracker.multiselect({
        maxHeight: 400,
        enableFiltering: true,
        includeSelectAllOption: true,
        enableCaseInsensitiveFiltering: true,
        nonSelectedText: 'All'
    });


    if (category !== undefined) {
        searchCategory.val(category.split(","));
        searchCategory.multiselect("refresh");
    }

    releaseDialog.modal("show");
    if (query !== undefined) {
        queryField.value = query;
        searchButton.click();
    }
}

function clearSearchResultTable(element) {
    element.find("#jackett-search-results-datatable > tbody").empty();
    element.find("#jackett-search-results-datatable > tfoot").empty();
    element.find("#jackett-search-results-datatable_info").empty();
    element.find("#jackett-search-results-datatable_paginate").empty();
}

// dataTable dead torrent filter
$.fn.dataTable.ext.search = [
    function (settings, data, dataIndex) {
        if (settings.sInstance != "jackett-search-results-datatable")
            return true;
        var deadfiltercheckbox = $(settings.nTableWrapper).find(".dataTables_deadfilter input");
        if (!deadfiltercheckbox.length) {
            return true;
        }
        var seeders = data[9];
        if (!deadfiltercheckbox.get(0).checked && seeders == 0)
            return false;
        return true;
    }
];

function updateSearchResultTable(element, results) {
    var resultsTemplate = Handlebars.compile($("#jackett-search-results").html());
    element.html($(resultsTemplate(results)));
    element.find('tr.jackett-search-results-row').each(function () { updateReleasesRow(this); });
    var settings = { "deadfilter": true };
    var datatable = element.find('table').DataTable(
        {
            "fnStateSaveParams": function (oSettings, sValue) {
                sValue.search.search = ""; // don't save the search filter content
                sValue.deadfilter = settings.deadfilter;
                return sValue;
            },
            "fnStateLoadParams": function (oSettings, sValue) {
                if ("deadfilter" in sValue)
                    settings.deadfilter = sValue.deadfilter;
            },

            "dom": "lfr<\"dataTables_deadfilter\">tip",
            "stateSave": true,
            "stateDuration": 0,
            "bAutoWidth": false,
            "pageLength": 20,
            "lengthMenu": [[10, 20, 50, 100, 250, 500, -1], [10, 20, 50, 100, 250, 500, "All"]],
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
            fnPreDrawCallback: function () {
                var table = this;

                var inputSearch = element.find("input[type=search]");
                if (!inputSearch.attr("custom")) {
                  var newInputSearch = inputSearch.clone();
                  newInputSearch.attr("custom", "true");
                  newInputSearch.attr("data-toggle", "tooltip");
                  newInputSearch.attr("title", "Search query consists of several keywords.\nKeyword starting with \"-\" is considered a negative match.");
                  newInputSearch.on("input", function () {
                    var newKeywords = [];
                    var filterTextKeywords = $(this).val().split(" ");
                    $.each(filterTextKeywords, function(index, keyword) {
                      if (keyword === "" || keyword === "+" || keyword === "-")
                        return;
                      var newKeyword;
                      if (keyword.startsWith("+"))
                        newKeyword = $.fn.dataTable.util.escapeRegex(keyword.substring(1));
                      else if (keyword.startsWith("-"))
                        newKeyword = "^((?!" + $.fn.dataTable.util.escapeRegex(keyword.substring(1)) + ").)*$";
                      else
                        newKeyword = $.fn.dataTable.util.escapeRegex(keyword);
                      newKeywords.push(newKeyword);
                    });
                    var filterText = newKeywords.join(" ");
                    table.api().search(filterText, true, true).draw();
                  });
                  inputSearch.replaceWith(newInputSearch);
                }

                var deadfilterdiv = element.find(".dataTables_deadfilter");
                var deadfiltercheckbox = deadfilterdiv.find("input");
                if (!deadfiltercheckbox.length) {
                    deadfilterlabel = $('<label><input type="checkbox" id="jackett-search-results-datatable_deadfilter_checkbox" value="1"> Show dead torrents</label>'
                        );
                    deadfilterdiv.append(deadfilterlabel);
                    deadfiltercheckbox = deadfilterlabel.find("input");
                    deadfiltercheckbox.on("change", function () {
                        settings.deadfilter = this.checked;
                        table.api().draw();
                    });
                    deadfiltercheckbox.prop('checked', settings.deadfilter);
                }
            },
            initComplete: function () {
                var count = 0;
                this.api().columns().every(function () {
                    count++;
                    if (count === 3 || count === 8) {
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
    return datatable;
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

    $('#jackett-add-indexer').click(function () {
        $("#modals").empty();
        displayUnconfiguredIndexersList();
    });

    $("#jackett-test-all").click(function () {
        $(".indexer-button-test").each(function (i, btn) {
            var $btn = $(btn);
            var id = $btn.data("id");
            testIndexer(id, false);
        });
    });

    $("#jackett-show-releases").click(function () {
        api.getServerCache(function (data) {
            for (var i = 0; i < data.length; i++) {
                var item = data[i];
                item.Title = insertWordWrap(item.Title);
                item.CategoryDesc = insertWordWrap(item.CategoryDesc);
            }
            var releaseTemplate = Handlebars.compile($("#jackett-releases").html());
            var item = { releases: data, Title: 'Releases' };
            var releaseDialog = $(releaseTemplate(item));
            var table = releaseDialog.find('table');
            releaseDialog.find('tr.jackett-releases-row').each(function () { updateReleasesRow(this); });
            releaseDialog.on('hidden.bs.modal', function (e) {
                $('#indexers div.dataTables_filter input').focusWithoutScrolling();
            });

            table.DataTable(
                 {
                     "stateSave": true,
                     "stateDuration": 0,
                     "bAutoWidth": false,
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
                             if (count === 5 || count === 10) {
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
        showSearch(null);
        window.location.hash = "search";
    });

    $("#view-jackett-logs").click(function () {
        api.getServerLogs(function (data) {
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
        var jackett_port = Number($("#jackett-port").val());
        var jackett_basepathoverride = $("#jackett-basepathoverride").val();
        var jackett_external = $("#jackett-allowext").is(':checked');
        var jackett_update = $("#jackett-allowupdate").is(':checked');
        var jackett_prerelease = $("#jackett-prerelease").is(':checked');
        var jackett_logging = $("#jackett-logging").is(':checked');
        var jackett_cache_enabled = $("#jackett-cache-enabled").is(':checked');
        var jackett_cache_ttl = $("#jackett-cache-ttl").val();
        var jackett_cache_max_results_per_indexer = $("#jackett-cache-max-results-per-indexer").val();
        var jackett_flaresolverr_url = $("#jackett-flaresolverrurl").val();
        var jackett_omdb_key = $("#jackett-omdbkey").val();
        var jackett_omdb_url = $("#jackett-omdburl").val();

        var jackett_proxy_url = $("#jackett-proxy-url").val();
        var jackett_proxy_type = $("#jackett-proxy-type").val();
        var jackett_proxy_port = $("#jackett-proxy-port").val();
        var jackett_proxy_username = $("#jackett-proxy-username").val();
        var jackett_proxy_password = $("#jackett-proxy-password").val();

        var jsonObject = {
            port: jackett_port,
            external: jackett_external,
            updatedisabled: jackett_update,
            prerelease: jackett_prerelease,
            blackholedir: $("#jackett-savedir").val(),
            logging: jackett_logging,
            basepathoverride: jackett_basepathoverride,
            logging: jackett_logging,
            cache_enabled: jackett_cache_enabled,
            cache_ttl: jackett_cache_ttl,
            cache_max_results_per_indexer: jackett_cache_max_results_per_indexer,
            flaresolverrurl: jackett_flaresolverr_url,
            omdbkey: jackett_omdb_key,
            omdburl: jackett_omdb_url,
            proxy_type: jackett_proxy_type,
            proxy_url: jackett_proxy_url,
            proxy_port: jackett_proxy_port,
            proxy_username: jackett_proxy_username,
            proxy_password: jackett_proxy_password
        };
        api.updateServerConfig(jsonObject, function (data) {
            doNotify("Redirecting you to complete configuration update..", "success", "glyphicon glyphicon-ok");
            window.setTimeout(function () {
                window.location.reload(true);
            }, 5000);
        }).fail(function (data) {
            if (data.responseJSON !== undefined && data.responseJSON.result == "error") {
                doNotify("Error: " + data.responseJSON.error, "danger", "glyphicon glyphicon-alert");
                return;
            } else {
                doNotify("Request to Jackett server failed", "danger", "glyphicon glyphicon-alert");
            }
        });
    });

    $("#trigger-updater").click(function () {
        api.updateServer(function (data) {
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

        api.updateAdminPassword(password, function (data) {
            if (data == undefined) {
                doNotify("Admin password has been set.", "success", "glyphicon glyphicon-ok");

                window.setTimeout(function () {
                    window.location = window.location.pathname;
                }, 1000);
            } else if (data.result == "error") {
                doNotify("Error: " + data.error, "danger", "glyphicon glyphicon-alert");
                return;
            }
        }).fail(function () {
            doNotify("Request to Jackett server failed", "danger", "glyphicon glyphicon-alert");
        });
    });

    $('#jackett-proxy-type').on('input', function () {
        proxyWarning($(this).val());
    });
}

function proxyWarning(input) {
    if (input != null && input.toString().trim() !== "-1") { // disabled = -1
        $('#proxy-warning').show();
    }
    else
    {
        $('#proxy-warning').hide();
    }
}
