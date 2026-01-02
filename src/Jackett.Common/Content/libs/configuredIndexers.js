import $ from 'jquery';
import Handlebars from 'handlebars';
import { Modal } from 'bootstrap';
import { reloadIndexers } from '../custom.js';
import { populateSetupForm } from './configure.js';
import { displaySearch } from './search.js';
import 'datatables.net-bs5';
import * as utils from './utils.ts';
import { api } from './api.js';

export function displayConfiguredIndexersList(indexers) {
    const indexersTemplate = Handlebars.compile($("#configured-indexer-table").html());
    const indexersTable = $(indexersTemplate({
        indexers,
        total_configured_indexers: indexers.length
    }));
    prepareTestButtons(indexersTable[0]);
    prepareSearchButtons(indexersTable);
    prepareSetupButtons(indexersTable);
    prepareDeleteButtons(indexersTable);
    prepareCopyButtons(indexersTable);
    indexersTable.find("table").DataTable({
        stateSave: true,
        stateDuration: 0,
        pageLength: -1,
        lengthMenu: [
            [10, 20, 50, 100, 250, 500, -1],
            [10, 20, 50, 100, 250, 500, "All"]
        ],
        order: [
            [0, "asc"]
        ],
        columnDefs: [{
            targets: 0,
            visible: true,
            searchable: true,
            orderable: true
        },
        {
            targets: 1,
            visible: true,
            searchable: true,
            orderable: true
        },
        {
            targets: 2,
            visible: false,
            searchable: true,
            orderable: false
        }
        ]
    });

    $('#indexers').empty();
    $('#indexers').append(indexersTable);
    $('#indexers').fadeIn();
}

function prepareTestButtons(element) {
    const buttons = element.querySelectorAll(".indexer-button-test");
    buttons.forEach((btn, i) => {
        const id = btn.getAttribute("data-id");
        const state = btn.getAttribute("data-state");
        const title = btn.getAttribute("title");
        utils.updateTestState(id, state, title, element);
        btn.addEventListener('click', () => {
            testIndexer(id, true);
        });
    });
}

export function testIndexer(id, notifyResult) {
    const indexers = document.querySelector('#indexers');
    utils.updateTestState(id, "inprogress", null, indexers);

    if (notifyResult)
        utils.notify(`Test started for ${id}`, "info", "fa fa-exchange");

    api.testIndexer(id, (data) => {
        if (data == undefined) {
            utils.updateTestState(id, "success", "Test successful", indexers);
            if (notifyResult)
                utils.notify(`Test successful for ${id}`, "success", "fa fa-check");
        } else if (data.result == "error") {
            utils.updateTestState(id, "error", data.error, indexers);
            if (notifyResult)
                utils.notify(`Test failed for ${id}: \n${data.error}`, "danger", "fa fa-exclamation-triangle");
        }
    }).fail((data) => {
        utils.updateTestState(id, "error", data.error, indexers);
        utils.notifyError(id, data.responseJSON.error, "testing");
    });
}

function prepareSearchButtons(element) {
    element.find('.indexer-button-search').each((i, btn) => {
        const $btn = $(btn);
        const id = $btn.data("id");
        $btn.on('click', () => {
            window.location.hash = `search&tracker=${id}${utils.filters.current ? `&filter=${utils.filters.current}` : ""}`;
            displaySearch(utils.filters.current, id);
        });
    });
}

function prepareSetupButtons(element) {
    element.find('.indexer-setup').each((_, btn) => {
        const $btn = $(btn);
        const id = $btn.data("id");
        const indexer = utils.indexers.configured.find(i => i.id === id);
        if (indexer) {
            btn.addEventListener('click', () => {
                displayIndexerSetup(indexer.id, indexer.name, indexer.caps, indexer.site_link, indexer.alternativesitelinks, indexer.description);
            });
        }
    });
}

function prepareDeleteButtons(element) {
    element.find(".indexer-button-delete").each((i, btn) => {
        const $btn = $(btn);
        const id = $btn.data("id");
        $btn.on('click', () => {
            api.deleteIndexer(id, (data) => {
                if (data == undefined) {
                    utils.notify(`Deleted ${id}`, "success", "fa fa-check");
                } else if (data.result == "error") {
                    utils.notify(`Delete error for ${id}\n${data.error}`, "danger", "fa fa-exclamation-triangle");
                }
            }).fail(() => {
                utils.notify("Error deleting indexer, request to Jackett server error", "danger", "fa fa-exclamation-triangle");
            }).always(() => {
                reloadIndexers();
            });
        });
    });
}

function prepareCopyButtons(element) {
    element.find(".indexer-button-copy").each((i, btn) => {
        const $btn = $(btn);
        const title = $btn[0].title;

        $btn.on('click', () => {
            utils.copyToClipboard(title);
            return false;
        });
    });
}

export function displayIndexerSetup(id, name, caps, link, alternativesitelinks, description) {
    api.getIndexerConfig(id, (data) => {
        if (data.result !== undefined && data.result == "error") {
            utils.notify(`Error: ${data.error}`, "danger", "fa fa-exclamation-triangle");
            return;
        }
        populateSetupForm(id, name, data, caps, link, alternativesitelinks, description);
    }).fail(() => {
        utils.notify("Request to Jackett server failed", "danger", "fa fa-exclamation-triangle");
    });
}
