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
    window.jackettIsLocal = window.location.hostname === '127.0.0.1';

    Handlebars.registerHelper('if_eq', function(a, b, opts) {
	    if (a == b)
	        return opts.fn(this);
	    else
	        return opts.inverse(this);
	});

    var index = window.location.pathname.indexOf("/UI");
    var pathPrefix = window.location.pathname.substr(0, index);
    api.root = pathPrefix + api.root;

    bindUIButtons();
    loadJackettSettings();
});

function openSearchIfNecessary() {
    var parser = document.createElement('a');
    parser.href = window.location.href;

    if (parser.hash.startsWith("#search")) {
        var query = parser.hash.split('=')[1];
        showSearch(null, query);
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
        $("#jackett-omdbkey").val(data.omdbkey);
        var password = data.password;
        $("#jackett-adminpwd").val(password);
        if (password != null && password != '') {
            $("#logoutBtn").show();
        }

        $.each(data.notices, function (index, value) {
            console.log(value);
            doNotify(value, "danger", "glyphicon glyphicon-alert", false);
        })

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
            item.torznab_host = resolveUrl(basePath + "/api/v2.0/indexers/" + item.id + "/results/torznab/");
            item.potato_host = resolveUrl(basePath + "/api/v2.0/indexers/" + item.id + "/results/potato/");

            if (item.last_error)
                item.state = "error";
            else
                item.state = "success";

            if (item.type == "public") {
                item.type_icon_content = "🔓\uFE0E";
            }
            else if (item.type == "private") {
                item.type_icon_content = "🔐\uFE0E";
            }
            else if (item.type == "semi-private") {
                item.type_icon_content = "🔒\uFE0E";
            }
            else {
                item.type_icon_content = "";
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
                displayIndexerSetup(indexer.id, indexer.name, indexer.caps, indexer.link, indexer.alternativesitelinks);
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
                doNotify("An error occured while configuring this indexer<br /><b>" + data.responseJSON.error + "</b><br /><i><a href=\"https://github.com/Jackett/Jackett/issues/new?title=[" + indexerId + "] " + data.responseJSON.error + " (Config)\" target=\"_blank\">Click here to open an issue on Github for this indexer.</a><i>", "danger", "glyphicon glyphicon-alert", false);
            } else {
                doNotify("An error occured while configuring this indexer, is Jackett server running ?", "danger", "glyphicon glyphicon-alert");
            }
                        
			        });
                });
            });
        });
    });
    indexersTable.find("table").DataTable(
        {
            "stateSave": true,
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
            showSearch(id);
        });
    });
}

function prepareSetupButtons(element) {
    element.find('.indexer-setup').each(function (i, btn) {
        var indexer = configuredIndexers[i];
        $(btn).click(function () {
            displayIndexerSetup(indexer.id, indexer.name, indexer.caps, indexer.link, indexer.alternativesitelinks);
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
                doNotify("An error occured while testing this indexer<br /><b>" + data.responseJSON.error + "</b><br /><i><a href=\"https://github.com/Jackett/Jackett/issues/new?title=[" + id + "] " + data.responseJSON.error + " (Test)\" target=\"_blank\">Click here to open an issue on Github for this indexer.</a><i>", "danger", "glyphicon glyphicon-alert", false);
            } else {
                doNotify("An error occured while testing indexers, please take a look at indexers with failed test for more informations.", "danger", "glyphicon glyphicon-alert");
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

function displayIndexerSetup(id, name, caps, link, alternativesitelinks) {
    api.getIndexerConfig(id, function (data) {
        if (data.result !== undefined && data.result == "error") {
            doNotify("Error: " + data.error, "danger", "glyphicon glyphicon-alert");
            return;
        }

        populateSetupForm(id, name, data, caps, link, alternativesitelinks);
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

function newConfigModal(title, config, caps, link, alternativesitelinks) {
    var configTemplate = Handlebars.compile($("#jackett-config-setup-modal").html());
    var configForm = $(configTemplate({ title: title, caps: caps, link:link }));
    $("#modals").append(configForm);
    populateConfigItems(configForm, config);

    if (alternativesitelinks.length >= 1) {
        var AlternativeSiteLinksTemplate = Handlebars.compile($("#setup-item-alternativesitelinks").html());
        var template = $(AlternativeSiteLinksTemplate({ "alternativesitelinks": alternativesitelinks }));
        configForm.find("div[data-id='sitelink']").after(template);
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
            case "inputselect":
                itemEntry.value = $el.find(".setup-item-inputselect select").val();
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

function populateSetupForm(indexerId, name, config, caps, link, alternativesitelinks) {
    var configForm = newConfigModal(name, config, caps, link, alternativesitelinks);
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
                doNotify("An error occured while updating this indexer<br /><b>" + data.responseJSON.error + "</b><br /><i><a href=\"https://github.com/Jackett/Jackett/issues/new?title=[" + indexerId + "] " + data.responseJSON.error + " (Config)\" target=\"_blank\">Click here to open an issue on Github for this indexer.</a><i>", "danger", "glyphicon glyphicon-alert", false);
            } else {
                doNotify("An error occured while updating this indexer, request to Jackett server failed, is server running ?", "danger", "glyphicon glyphicon-alert");
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
    var Banner = $(row).data("banner");
    var Description = $(row).data("description");
    var DownloadVolumeFactor = parseFloat($(row).find("td.DownloadVolumeFactor").html());
    var UploadVolumeFactor = parseFloat($(row).find("td.UploadVolumeFactor").html());

    var TitleTooltip = "";
    if (Banner)
        TitleTooltip += "<img src='" + Banner + "' /><br />";
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
        labels.append('\n<a href="http://www.imdb.com/title/tt' + IMDBId + '/" class="label label-imdb" alt="IMDB" title="IMDB">IMDB</a>');
    }

    if (!isNaN(DownloadVolumeFactor)) {
        if (DownloadVolumeFactor == 0) {
            labels.append('\n<span class="label label-success">FREELEECH</span>');
        } else if (DownloadVolumeFactor < 1) {
            labels.append('\n<span class="label label-primary">' + DownloadVolumeFactor * 100 + '%DL</span>');
        } else if (DownloadVolumeFactor > 1) {
            labels.append('\n<span class="label label-danger">' + DownloadVolumeFactor * 100 + '%DL</span>');
        }
    }

    if (!isNaN(UploadVolumeFactor)) {
        if (UploadVolumeFactor == 0) {
            labels.append('\n<span class="label label-warning">NO UPLOAD</span>');
        } else if (UploadVolumeFactor != 1) {
            labels.append('\n<span class="label label-info">' + UploadVolumeFactor * 100 + '%UL</span>');
        }
    }
}

function showSearch(selectedIndexer, query) {
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

    var setCategories = function (tracker, items) {
        var cats = {};
        for (var i = 0; i < items.length; i++) {
            if (items[i].configured === true && (items[i].id === tracker || tracker === '')) {
                indexers["'" + items[i].id + "'"] = items[i].name;
                for (var prop in items[i].caps) {
                    if (prop < 100000 || tracker)
                        cats[prop] = items[i].caps[prop];
                }
            }
        }
        var select = $('#searchCategory');
        select.html("<option value=''>-- All --</option>");
        $.each(cats, function (index, value) {
            select.append($("<option></option>")
                .attr("value", value["ID"]).text(value["ID"] + ' (' + value["Name"] + ')'));
        });
    };

    $('#searchTracker').change(jQuery.proxy(function () {
        var trackerId = $('#searchTracker').val();
        setCategories(trackerId, this.items);
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
            Tracker: releaseDialog.find('#searchTracker').val().replace("'", "").replace("'", ""),
        };

        window.location.hash = "search=" + searchString;

        $('#jackett-search-perform').html($('#spinner').html());
        $('#searchResults div.dataTables_filter input').val("");
        clearSearchResultTable($('#searchResults'));

        var trackerId = queryObj.Tracker;
        if (trackerId == null || trackerId == "")
            trackerId = "all";
        api.resultsForIndexer(trackerId, queryObj, function (data) {
            for (var i = 0; i < data.Results.length; i++) {
                var item = data.Results[i];
                item.Title = insertWordWrap(item.Title);
                item.CategoryDesc = insertWordWrap(item.CategoryDesc);
            }

            $('#jackett-search-perform').html($('#search-button-ready').html());
            var searchResults = $('#searchResults');
            searchResults.empty();
            var datatable = updateSearchResultTable(searchResults, data).search('').columns().search('').draw();
            searchResults.find('div.dataTables_filter input').focusWithoutScrolling();
        }).fail(function () {
            $('#jackett-search-perform').html($('#search-button-ready').html());
            doNotify("Request to Jackett server failed", "danger", "glyphicon glyphicon-alert");
        });
    });

    var searchTracker = releaseDialog.find("#searchTracker");
    if (selectedIndexer)
        searchTracker.val(selectedIndexer);
    searchTracker.trigger("change");

    updateSearchResultTable($('#searchResults'), []);
    clearSearchResultTable($('#searchResults'));
    releaseDialog.modal("show");

    if (query !== undefined) {
        queryField.value = decodeURIComponent(query);
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
        var deadfiltercheckbox = $(settings.nTableWrapper).find(".dataTables_deadfilter input")
        if (!deadfiltercheckbox.length) {
            return true;
        }
        var seeders = data[9];
        if (!deadfiltercheckbox.get(0).checked && seeders == 0)
            return false;
        return true;
    }
]

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
                var deadfilterdiv = element.find(".dataTables_deadfilter");
                var deadfiltercheckbox = deadfilterdiv.find("input");
                if (!deadfiltercheckbox.length) {
                    deadfilterlabel = $('<label><input type="checkbox" id="jackett-search-results-datatable_deadfilter_checkbox" value="1">Show dead torrents</label>'
                        );
                    deadfilterdiv.append(deadfilterlabel);
                    deadfiltercheckbox = deadfilterlabel.find("input")
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
        var jackett_omdb_key = $("#jackett-omdbkey").val();
        var jsonObject = {
            port: jackett_port,
            external: jackett_external,
            updatedisabled: jackett_update,
            prerelease: jackett_prerelease,
            blackholedir: $("#jackett-savedir").val(),
            logging: jackett_logging,
            basepathoverride: jackett_basepathoverride,
            omdbkey: jackett_omdb_key
        };
        api.updateServerConfig(jsonObject, function (data) {
            if (data !== undefined && data.result == "error") {
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
}
