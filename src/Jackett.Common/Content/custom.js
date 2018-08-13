var basePath = '';
var indexers = [];
var configuredIndexers = [];
var unconfiguredIndexers = [];

$.fn.inView = function () {
    if (!this.length) return false;
    var rect = this.get(0).getBoundingClientRect();

    return (
        rect.top >= 0 &&
        rect.left >= 0 &&
        rect.bottom <= (window.innerHeight || document.documentElement.clientHeight) &&
        rect.right <= (window.innerWidth || document.documentElement.clientWidth)
    );
};

$(document).ready(function () {
    $.ajaxSetup({ cache: false });
    window.jackettIsLocal = false;
    Handlebars.registerHelper('if_eq', function (a, b, opts) {
        if (a == b)
            return opts.fn(this);
        else
            return opts.inverse(this);
    });
    var index = window.location.pathname.indexOf("/UI");
    var pathPrefix = window.location.pathname.substr(0, index);
    api.root = pathPrefix + api.root;
    bindUIButtons();
    loadConfiguredIndexers();

});

$.fn.focusWithoutScrolling = function () {
    if (this.inView())
        this.focus();
    return this;
};

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

function loadConfigurationPage() {
    var template = $('#configuration').html();
    var templateScript = Handlebars.compile(template);
    var html = templateScript(null);
    var html2 = "<h2>Configuration</h2>";
    $(document.body.getElementsByClassName("page")).replaceWith(html);
    $(document.body.getElementsByClassName("currentPageHeader")).replaceWith(html2);

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
        $("#jackett-omdburl").val(data.omdburl);
        var password = data.password;
        $("#jackett-adminpwd").val(password);
        if (password != null && password != '') {
            $("#logoutBtn").show();
        }
        $.each(data.notices, function (index, value) {
            console.log(value);
            doNotify(value, "danger", "glyphicon glyphicon-alert", false);
        })
    });
}

function getJackettConfig(callback) {
    api.getServerConfig(callback).fail(function () {
        doNotify("Error loading Jackett settings, request to Jackett server failed, is server running ?", "danger", "glyphicon glyphicon-alert");
    });
}

function loadConfiguredIndexers() {
    var template = $('#displayIndexers').html();
    var templateScript = Handlebars.compile(template);
    var html = templateScript(null);
    var html2 = "<h2>Indexers</h2>";
    $(document.body.getElementsByClassName("page")).replaceWith(html);
    $(document.body.getElementsByClassName("currentPageHeader")).replaceWith(html2);
    reloadIndexers();
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
            var main_cats_list = item.caps.filter(function (c) {
                return c.ID < 100000;
            }).map(function (c) {
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

function resolveUrl(url) {
    var a = document.createElement('a');
    a.href = url;
    url = a.href;
    return url;
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

function clearNotifications() {
    $('[data-notify="container"]').remove();
}

function updateReleasesRow(row) {
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

function openSearchIfNecessary() {
    const hashArgs = location.hash.substring(1).split('&').reduce((prev, item) => Object.assign({ [item.split('=')[0]]: (item.split('=').length < 2 ? undefined : decodeURIComponent(item.split('=')[1])) }, prev), {});
    if ("search" in hashArgs) {
        showSearch(hashArgs.tracker, hashArgs.search, hashArgs.category);
    }
}

function insertWordWrap(str) {
    // insert optional word wrap after punctuation to avoid overflows on long scene titles
    return str.replace(/([\.\-_\/\\])/g, "$1\u200B");
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
        $btn.click(function () {
            window.location.hash = "search&tracker=" + id;
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

function updateTestState(id, state, message, parent) {
    var btn = parent.find(".indexer-button-test[data-id=" + id + "]");

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
    var dt = $.fn.dataTable.tables({ visible: true, api: true }).rows().invalidate('dom');
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
        if (data.responseJSON.error !== undefined && notifyResult) {
            doNotify("An error occured while testing this indexer<br /><b>" + data.responseJSON.error + "</b><br /><i><a href=\"https://github.com/Jackett/Jackett/issues/new?title=[" + id + "] " + data.responseJSON.error + " (Test)\" target=\"_blank\">Click here to open an issue on GitHub for this indexer.</a><i>", "danger", "glyphicon glyphicon-alert", false);
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

function showReleases() {
    api.getServerCache(function (data) {
        for (var i = 0; i < data.length; i++) {
            var item = data[i];
            item.Title = insertWordWrap(item.Title);
            item.CategoryDesc = insertWordWrap(item.CategoryDesc);
        }
        var template = $('#jackett-releases').html();
        var templateScript = Handlebars.compile(template);
        var item = { releases: data, Title: 'Releases' };
        var html = $(templateScript(item));
        var html2 = "<h2>Configuration</h2>";
        var table = html.find('table');
        html.find('tr.jackett-releases-row').each(function () { updateReleasesRow(this); });
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

        $(document.body.getElementsByClassName("page")).replaceWith(html);
        $(document.body.getElementsByClassName("currentPageHeader")).replaceWith(html2);
    }).fail(function () {
        doNotify("Request to Jackett server failed", "danger", "glyphicon glyphicon-alert");
    });
    

}

function bindUIButtons() {

    $("#jackett-config").click(function () {
        loadConfigurationPage()
        loadJackettSettings();
    });

    $("#jackett-configured-indexers").click(function () {
        loadConfiguredIndexers();
    })

    $("#jackett-show-releases").click(function () {
        showReleases();
    })
}
