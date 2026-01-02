import $ from 'jquery';
import Handlebars from 'handlebars';
import 'datatables.net-bs5';

import * as utils from './utils.ts';
import { api } from './api.js';
import { reloadIndexers } from '../custom.js';
import { populateSetupForm } from './configure.js';
import { displaySearch } from './search.js';

export function displayConfiguredIndexersList(indexers) {
    const indexersTemplate = Handlebars.compile($("#configured-indexer-table").html());
    const indexersTable = $(indexersTemplate({
        indexers,
        total_configured_indexers: indexers.length
    }));
    prepareTestButtons(indexersTable[0]);
    prepareSearchButtons(indexersTable[0]);
    prepareSetupButtons(indexersTable[0]);
    prepareDeleteButtons(indexersTable[0]);
    prepareCopyButtons(indexersTable[0]);
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

function prepareTestButtons(element) {
    element.querySelectorAll(".indexer-button-test").forEach((btn, _) => {
        const id = btn.getAttribute("data-id");
        const state = btn.getAttribute("data-state");
        const title = btn.getAttribute("title");
        utils.updateTestState(id, state, title, element);
        btn.addEventListener('click', () => testIndexer(id, true));
    });
}

function prepareSearchButtons(element) {
    element.querySelectorAll('.indexer-button-search').forEach(button => {
        const id = button.dataset.id;
        button.addEventListener('click', () => {
            const filterParam = utils.filters.current ? `&filter=${utils.filters.current}` : '';
            window.location.hash = `search&tracker=${id}${filterParam}`;
            displaySearch(utils.filters.current, id);
        });
    });
}

function prepareSetupButtons(element) {
    element.querySelectorAll('.indexer-setup').forEach(button => {
        const id = button.dataset.id;
        const indexer = utils.indexers.configured.find(i => i.id === id);
        if (!indexer) return;
        button.addEventListener('click', () => {
            displayIndexerSetup(indexer.id, indexer.name, indexer.caps, indexer.site_link, indexer.alternativesitelinks, indexer.description);
        });
    });
}

function prepareDeleteButtons(element) {
    element.querySelectorAll('.indexer-button-delete').forEach(button => {
        const id = button.dataset.id;
        button.addEventListener('click', () => {
            try {
                const data = api.deleteIndexer(id);
                if (data === undefined) {
                    utils.notify(`Deleted ${id}`, 'success', 'fa fa-check');
                } else if (data.result === 'error') {
                    utils.notify(`Delete error for ${id}\n${data.error}`, 'danger', 'fa fa-exclamation-triangle');
                }
            } catch (error) {
                utils.notify('Error deleting indexer, request to Jackett server error', 'danger', 'fa fa-exclamation-triangle');
                console.error('Delete indexer error:', error);
            } finally {
                reloadIndexers();
            }
        });
    });
}

function prepareCopyButtons(element) {
    element.querySelectorAll('.indexer-button-copy').forEach(button => {
        button.addEventListener('click', async (event) => {
            event.preventDefault();
            utils.copyToClipboard(button.title);
        });
    });
}
