import $ from 'jquery';
window.$ = window.jQuery = $;
import { Modal } from 'bootstrap';
import Handlebars from 'handlebars';
import 'datatables.net-bs5';
import registerHandler from './libs/handlebarshelper.js';
import initMultiselect from './libs/bootstrap-multiselect.mod.js';

import * as utils from './libs/utils.ts';
import { api } from './libs/api.js';
import { displayConfiguredIndexersList, testIndexer } from './libs/configuredIndexers.js';
import { displayUnconfiguredIndexersList } from './libs/unconfiguredIndexers.js';
import { displaySearch, displaySearchIfNeed } from './libs/search.js';

initMultiselect($);
registerHandler(Handlebars);

document.addEventListener('DOMContentLoaded', () => {
    $.ajaxSetup({ cache: false });

    $.fn.stableFocus = function() { utils.stableFocus(this[0]); };

    const index = window.location.pathname.indexOf("/UI");
    const pathPrefix = window.location.pathname.substring(0, index);
    api.root = pathPrefix + api.root;

    const hashArgs = utils.getHashArgs();
    if ("indexers" in hashArgs)
        utils.filters.setCurrentFilter(hashArgs.filter);
    bindButtons();
    loadSettings();
});

export function reloadIndexers() {
    $('#filters').hide();
    $('#indexers').hide();
    api.getAllIndexers(data => {
        utils.indexers.clear();
        utils.tags.clear();
        utils.filters.clear();

        const formattedData = data.map(item => utils.formatIndexer(item));
        utils.indexers.setAllIndexers(formattedData);

        utils.tags.setConfiguredTags(utils.indexers.configured);
        utils.filters.setAvailableFilters(utils.indexers.configured);
        displayFilteredIndexersList(utils.filters.current);

        $('#indexers div.dt-search input').stableFocus();
        displaySearchIfNeed();
    }).fail(() => {
        utils.notify("Error loading indexers, request to Jackett server failed, is server running ?", "danger", "fa fa-exclamation-triangle");
    });
}

function loadSettings() {
    api.getServerConfig(data => {
        api.key = data.api_key;
        $("#api-key-input").val(api.key);
        $(".api-key-text").text(api.key);
        $("#app-version").html(data.app_version);
        $("#jackett-port").val(data.port);

        $("#jackett-proxy-type").val(data.proxy_type);
        $("#jackett-proxy-url").val(data.proxy_url);
        $("#jackett-proxy-port").val(data.proxy_port);
        $("#jackett-proxy-username").val(data.proxy_username);
        $("#jackett-proxy-password").val(data.proxy_password);
        proxyWarning(data.proxy_type);

        $("#jackett-basepathoverride").val(data.basepathoverride);
        utils.config.basePath = data.basepathoverride || '';

        $("#jackett-baseurloverride").val(data.baseurloverride);
        utils.config.baseUrl = data.baseurloverride || '';

        $("#jackett-savedir").val(data.blackholedir);
        $("#jackett-allowext").attr('checked', data.external);
        $("#jackett-local-bind-address").val(data.local_bind_address);
        $("#jackett-allowcors").attr('checked', data.cors);
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
        $("#jackett-flaresolverr-maxtimeout").val(data.flaresolverr_maxtimeout);
        $("#jackett-omdbkey").val(data.omdbkey);
        $("#jackett-omdburl").val(data.omdburl);
        $("#jackett-adminpwd").val(data.password);
        if (data.password) {
            $("#logoutBtn").show();
        }

        if (data.can_run_netcore) {
            $("#can-upgrade-from-mono").show();
        }

        if (data.external != null && data.external === true && data.password === '' && !localStorage.getItem('external-access-warning-hidden')) {
            $("#warning-external-access").show();
        }

        data.notices.forEach(value => {
            utils.notify(value, "danger", "fa fa-exclamation-triangle", false);
        });

        reloadIndexers();
    });
}

function displayFilteredIndexersList(filter) {
    let indexers = utils.indexers.configured;
    const active = utils.filters.available.find(x => x.id == filter);
    if (utils.filters.available.length > 0) {
        const filtersTemplate = Handlebars.compile($("#jackett-filters").html());
        const filters = $(filtersTemplate({
            filters: utils.filters.available,
            active: active ? active.id : null
        }));

        $("li a", filters).on('click', function() {
            displayFilteredIndexersList($(this).data("id"));
        });

        $('#filters').empty();
        $('#filters').append(filters);
        $('#filters').fadeIn();
    }
    if (active) {
        indexers = indexers.filter(active.apply, active);
        utils.filters.setCurrentFilter(active.id);
    }
    else {
        utils.filters.setCurrentFilter(null);
    }
    displayConfiguredIndexersList(indexers);
}

function bindButtons() {
    document.body.addEventListener('click', (e) => {
        if (e.target.classList.contains('downloadlink'))
            e.target.classList.add('jackettdownloaded');
    });

    document.addEventListener('click', (event) => {
        const target = event.target.closest('.jacketdownloadserver');
        if (!target) return;

        const href = target.parentElement.getAttribute('href');
        if (!href) return;

        $.get(href, (data) => {
            if (data.result === "error") {
                utils.notify(`Error: ${data.error}`, "danger", "fa fa-exclamation-triangle");
                return;
            }
            utils.notify("Downloaded sent to the blackhole successfully.", "success", "fa fa-check");
        }).fail(() => {
            utils.notify("Request to Jackett server failed", "danger", "fa fa-exclamation-triangle");
        });

        event.preventDefault();
        return false;
    });

    document.getElementById('remind-external-access-button')?.addEventListener('click', () => {
        document.getElementById('warning-external-access')?.classList.add('d-none');
    });

    document.getElementById('dismiss-external-access-button')?.addEventListener('click', () => {
        localStorage.setItem('external-access-warning-hidden', 'true');
        document.getElementById('warning-external-access')?.classList.add('d-none');
    });

    document.getElementById('api-key-copy-button')?.addEventListener('click', () => {
        utils.copyToClipboard(api.key);
    });

    document.getElementById('jackett-add-indexer')?.addEventListener('click', () => {
        document.getElementById('modals').innerHTML = '';
        displayUnconfiguredIndexersList();
        const table = document.getElementById('unconfigured-indexer-datatable');
        if (!table) {
            return;
        }
        const thead = table.querySelector('thead');
        const tfoot = table.querySelector('tfoot');
        if (thead && tfoot) {
            thead.appendChild(tfoot.querySelector('tr'));
        }
        const dt = window.$(table).DataTable();
        dt.search('').columns().search('').draw();
    });

    document.getElementById('jackett-test-all')?.addEventListener('click', () => {
        document.querySelectorAll('.indexer-button-test').forEach(btn => {
            const id = btn.dataset.id;
            testIndexer(id, false);
        });
    });

    document.getElementById('jackett-show-releases')?.addEventListener('click', () => {
        api.getServerCache((data) => {
            const releaseTemplate = Handlebars.compile(document.getElementById('jackett-releases')?.innerHTML || '');
            const item = {
                releases: data,
                Title: 'Releases'
            };
            const releaseDialog = $(releaseTemplate(item));
            const table = releaseDialog.find('table');

            releaseDialog.find('tr.jackett-releases-row').each(function() {
                utils.updateReleasesRow(this);
            });

            releaseDialog.on('hidden.bs.modal', () => {
                const input = document.querySelector('#indexers .dt-search input');
                utils.stableFocus(input);
            });

            table.DataTable({
                stateSave: true,
                stateDuration: 0,
                autoWidth: false,
                pageLength: 20,
                lengthMenu: [
                    [10, 20, 50, -1],
                    [10, 20, 50, "All"]
                ],
                order: [[0, "desc"]],
                columnDefs: [
                    { targets: 0, visible: false, searchable: false, type: 'date' },
                    { targets: 1, visible: false, searchable: false, type: 'date' },
                    { targets: 2, visible: true, searchable: false, orderData: 0 },
                    { targets: 3, visible: true, searchable: false, orderData: 1 },
                    { targets: 6, visible: false, searchable: false, type: 'num' },
                    { targets: 7, visible: true, searchable: false, orderData: 6 }
                ],
                initComplete() {
                    let count = 0;
                    this.api().columns().every(function() {
                        count++;
                        if (!(count == 5 || count == 10)) {
                            return;
                        }
                        const column = this;
                        const footer = column.footer();
                        footer.innerHTML = '';
                        const select = document.createElement('select');
                        const defaultOption = document.createElement('option');
                        defaultOption.value = '';
                        defaultOption.textContent = '';
                        select.appendChild(defaultOption);
                        footer.appendChild(select);

                        select.addEventListener('change', function() {
                            const val = utils.escape(this.value);
                            column.search(val ? `^${val}$` : '', true, false).draw();
                        });
                        const datas = column.data().toArray().filter((item, pos, self) => self.indexOf(item) === pos).sort();
                        datas.forEach(d => {
                            const option = document.createElement('option');
                            option.value = d;
                            option.textContent = d;
                            select.appendChild(option);
                        });
                    });
                }
            });

            const releaseDialogModal = new Modal(releaseDialog[0]);
            releaseDialogModal.show();
        }).fail(() => {
            utils.notify("Request to Jackett server failed", "danger", "fa fa-exclamation-triangle");
        });
    });

    document.getElementById('jackett-show-search')?.addEventListener('click', () => {
        displaySearch(utils.filters.current);
        window.location.hash = `search${utils.filters.current ? `&filter=${utils.filters.current}` : ""}`;
    });

    document.getElementById('view-jackett-logs')?.addEventListener('click', () => {
        api.getServerLogs((data) => {
            const releaseTemplate = Handlebars.compile(document.getElementById('jackett-logs')?.innerHTML || '');
            const item = { logs: data };
            const releaseDialog = window.$(releaseTemplate(item));

            const modals = document.getElementById('modals');
            if (!modals) {
                return;
            }
            modals.innerHTML = releaseDialog[0].outerHTML;
            const modalEl = modals.querySelector('.modal');
            if (modalEl) {
                const releaseDialogModal = new Modal(modalEl);
                releaseDialogModal.show();
            }
        }).fail(() => {
            utils.notify("Request to Jackett server failed", "danger", "fa fa-exclamation-triangle");
        });
    });

    document.getElementById('change-jackett-port')?.addEventListener('click', () => {
        const jackett_port = Number(document.getElementById('jackett-port')?.value || 0);
        const jackett_basepathoverride = document.getElementById('jackett-basepathoverride')?.value || '';
        const jackett_baseurloverride = document.getElementById('jackett-baseurloverride')?.value || '';
        const jackett_external = document.getElementById('jackett-allowext')?.checked || false;
        const jackett_local_bind_address = document.getElementById('jackett-local-bind-address')?.value || '';
        const jackett_cors = document.getElementById('jackett-allowcors')?.checked || false;
        const jackett_update = document.getElementById('jackett-allowupdate')?.checked || false;
        const jackett_prerelease = document.getElementById('jackett-prerelease')?.checked || false;
        const jackett_logging = document.getElementById('jackett-logging')?.checked || false;
        const jackett_cache_enabled = document.getElementById('jackett-cache-enabled')?.checked || false;
        const jackett_cache_ttl = document.getElementById('jackett-cache-ttl')?.value || '';
        const jackett_cache_max_results_per_indexer = document.getElementById('jackett-cache-max-results-per-indexer')?.value || '';
        const jackett_flaresolverr_url = document.getElementById('jackett-flaresolverrurl')?.value || '';
        const jackett_flaresolverr_maxtimeout = document.getElementById('jackett-flaresolverr-maxtimeout')?.value || '';
        const jackett_omdb_key = document.getElementById('jackett-omdbkey')?.value || '';
        const jackett_omdb_url = document.getElementById('jackett-omdburl')?.value || '';

        const jackett_proxy_url = document.getElementById('jackett-proxy-url')?.value || '';
        const jackett_proxy_type = document.getElementById('jackett-proxy-type')?.value || '';
        const jackett_proxy_port = document.getElementById('jackett-proxy-port')?.value || '';
        const jackett_proxy_username = document.getElementById('jackett-proxy-username')?.value || '';
        const jackett_proxy_password = document.getElementById('jackett-proxy-password')?.value || '';

        const jsonObject = {
            port: jackett_port,
            external: jackett_external,
            local_bind_address: jackett_local_bind_address,
            cors: jackett_cors,
            updatedisabled: jackett_update,
            prerelease: jackett_prerelease,
            blackholedir: $("#jackett-savedir").val(),
            logging: jackett_logging,
            basepathoverride: jackett_basepathoverride,
            baseurloverride: jackett_baseurloverride,
            cache_enabled: jackett_cache_enabled,
            cache_ttl: jackett_cache_ttl,
            cache_max_results_per_indexer: jackett_cache_max_results_per_indexer,
            flaresolverrurl: jackett_flaresolverr_url,
            flaresolverr_maxtimeout: jackett_flaresolverr_maxtimeout,
            omdbkey: jackett_omdb_key,
            omdburl: jackett_omdb_url,
            proxy_type: jackett_proxy_type,
            proxy_url: jackett_proxy_url,
            proxy_port: jackett_proxy_port,
            proxy_username: jackett_proxy_username,
            proxy_password: jackett_proxy_password
        };
        api.updateServerConfig(jsonObject, _ => {
            utils.notify("Redirecting you to complete configuration update..", "success", "fa fa-check");
            window.setTimeout(() => {
                window.location.reload(true);
            }, 5000);
        }).fail((data) => {
            if (data.responseJSON !== undefined && data.responseJSON.result == "error") {
                utils.notify(`Error: ${data.responseJSON.error}`, "danger", "fa fa-exclamation-triangle");
                return;
            }
            utils.notify("Request to Jackett server failed", "danger", "fa fa-exclamation-triangle");
        });
    });

    $("#trigger-updater").on('click', () => {
        api.updateServer((data) => {
            if (data.result == "error") {
                utils.notify(`Error: ${data.error}`, "danger", "fa fa-exclamation-triangle");
                return;
            }
            utils.notify("Updater triggered see log for details..", "success", "fa fa-check");
        }).fail(() => {
            utils.notify("Request to Jackett server failed", "danger", "fa fa-exclamation-triangle");
        });
    });

    $("#change-jackett-password").on('click', () => {
        const password = $("#jackett-adminpwd").val();

        api.updateAdminPassword(password, (data) => {
            if (data == undefined) {
                utils.notify("Admin password has been set.", "success", "fa fa-check");

                window.setTimeout(() => {
                    window.location = window.location.pathname;
                }, 1000);
            } else if (data.result == "error") {
                utils.notify(`Error: ${data.error}`, "danger", "fa fa-exclamation-triangle");
                return;
            }
        }).fail(() => {
            utils.notify("Request to Jackett server failed", "danger", "fa fa-exclamation-triangle");
        });
    });

    $('#jackett-proxy-type').on('input', function() {
        proxyWarning($(this).val());
    });
}

function proxyWarning(input) {
    if (input != null && input.toString().trim() !== "-1") { // disabled = -1
        $('#proxy-warning').show();
    } else {
        $('#proxy-warning').hide();
    }
}
