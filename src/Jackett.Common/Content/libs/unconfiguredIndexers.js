import $ from 'jquery';
import { Modal } from 'bootstrap';
import Handlebars from 'handlebars';
import 'datatables.net-bs5';

import * as utils from './utils.ts';
import { api } from './api.js';
import { reloadIndexers } from '../custom.js';
import { displayIndexerSetup } from './configuredIndexers.js';
import { populateConfigItems } from './configure.js';

export function displayUnconfiguredIndexersList() {
    const UnconfiguredIndexersDialog = $($("#select-indexer").html());
    UnconfiguredIndexersDialog.on('shown.bs.modal', function() {
        $(this).find('.dt-search input').stableFocus();
    });
    UnconfiguredIndexersDialog.on('hidden.bs.modal', (e) => {
        $('#indexers .dt-search input').stableFocus();
    });

    $("#modals").html(UnconfiguredIndexersDialog);
    const modalElement = document.getElementById('select-indexer-modal');
    const modal = new Modal(modalElement);

    const indexersTemplate = Handlebars.compile($("#unconfigured-indexer-table").html());
    const indexersTable = $(indexersTemplate({
        indexers: utils.indexers.unconfigured,
        total_unconfigured_indexers: utils.indexers.unconfigured.length
    }));
    const undefindexers = UnconfiguredIndexersDialog.find('#unconfigured-indexers');
    undefindexers.append(indexersTable);

    indexersTable.find('.indexer-setup').each((_, btn) => {
        btn.addEventListener('click', () => {
            modal.hide();
            displayIndexerSetup(btn.dataset.id);
        });
    });

    indexersTable.find('.indexer-add').each((_, btn) => {
        btn.addEventListener('click', () => {
            modal.hide();
            modalElement.addEventListener('hidden.bs.modal', (_) => {
                addIndexer(btn.dataset.id, true);
            }, { once: true });
        });
    });

    indexersTable.find("table").DataTable({
        initComplete() {
            const api = this.api();
            api.columns().every(function() {
                const headerText = this.header().innerText;

                if (headerText == 'Type') {
                    var select = utils.createDropDownHtml(this, true);
                    const typeStringColumn = api.column(4);
                    var distinctValues = [...new Set(typeStringColumn.data().unique().toArray())];
                    distinctValues.forEach((distinctVal) => {
                        select.append(`<option value="${distinctVal}">${distinctVal.replace(/^\w/, (c) => c.toUpperCase())}</option>`);
                    });
                    return;
                }
                if (headerText == 'Categories') {
                    var select = utils.createDropDownHtml(this, false);
                    const columnData = [];
                    this.data().unique().each((d) => {
                        d.split(',').forEach((val) => {
                            columnData.push(val.trim());
                        });
                    });
                    var distinctValues = [...new Set(columnData)].sort();
                    distinctValues.forEach((distinctVal) => {
                        select.append(`<option value="${distinctVal}">${distinctVal}</option>`);
                    });
                    return;
                }
                if (headerText == 'Language') {
                    var select = utils.createDropDownHtml(this, true);
                    this.data().unique().sort().each((d) => {
                        select.append(`<option value="${d}">${d}</option>`);
                    });
                } else {
                    $(this.footer()).empty();
                }
            });
        },
        drawCallback(settings) {
            addCheckOnCellClick();
        },
        stateSave: true,
        stateDuration: 0,
        stateSaveParams(settings, data) {
            data.search.search = "";
            return data;
        },
        autoWidth: false,
        pageLength: -1,
        lengthMenu: [
            [10, 20, 50, 100, 250, 500, -1],
            [10, 20, 50, 100, 250, 500, "All"]
        ],
        select: {
            style: 'os',
            selector: 'td:first-child'
        },
        order: [
            [1, "asc"]
        ],
        columnDefs: [
            {
                name: "select",
                targets: 0,
                visible: true,
                searchable: false,
                orderable: false
            },
            {
                name: "name",
                targets: 1,
                visible: true,
                searchable: true,
                orderable: true
            },
            {
                name: "description",
                targets: 2,
                visible: true,
                searchable: true,
                orderable: true
            },
            {
                name: "type",
                targets: 3,
                visible: true,
                searchable: true,
                orderable: true
            },
            {
                name: "type_string",
                targets: 4,
                visible: false,
                searchable: false,
                orderable: false
            },
            {
                name: "language",
                targets: 5,
                visible: true,
                searchable: true,
                orderable: true
            },
            {
                name: "buttons",
                targets: 6,
                visible: true,
                searchable: false,
                orderable: false
            },
            {
                name: "url",
                targets: 7,
                visible: false,
                searchable: true,
                orderable: false
            }
        ]
    });

    $('#add-selected-indexers').on('click', () => {
        const selectedIndexers = $('#unconfigured-indexer-datatable').DataTable().$('input[type="checkbox"]');
        const hasSelectedIndexers = selectedIndexers.is(':checked');
        if (!hasSelectedIndexers) {
            utils.notify("Error: You must select at least one indexer", "danger", "fa fa-exclamation-triangle");
            return;
        }
        utils.notify("Adding selected Indexers, please wait...", "info", "fa fa-exchange", false);
        $('#select-indexer-modal button').attr('disabled', true);
        addIndexers(selectedIndexers, addSelectedIndexersSuccess, addSelectedIndexersError);
    });

    modal.show();
    addCheckOnCellClick();
}

function addSelectedIndexersSuccess() {
    utils.clearNotify();
    const modalElement = document.getElementById('select-indexer-modal');
    const modal = Modal.getInstance(modalElement);
    if (modal) {
        modal.hide();
    }
    utils.notify("Selected indexers successfully added.", "success", "fa fa-check");
    $('#select-indexer-modal button').attr('disabled', false);
}

function addSelectedIndexersError(e, xhr, options, err) {
    utils.notify("Configuration failed", "danger", "fa fa-exclamation-triangle");
}

function addCheckOnCellClick() {
    $('td.checkboxColumn')
        .off('click')
        .on('click', event => {
            if (!$(event.target).is('input')) {
                $('input:checkbox', this).prop('checked', (i, value) => !value);
            }
        });
}

function addIndexer(indexerId, displayNotification) {
    api.getIndexerConfig(indexerId, data => {
        if (data.result !== undefined && data.result == "error") {
            utils.notify(`Error: ${data.error}`, "danger", "fa fa-exclamation-triangle");
            return;
        }

        api.updateIndexerConfig(indexerId, data, data => {
            if (data == undefined) {
                reloadIndexers();
                if (displayNotification) {
                    utils.notify(`Successfully configured ${indexerId}`, "success", "fa fa-check");
                }
            } else if (data.result == "error") {
                if (data.config) {
                    populateConfigItems(configForm, data.config);
                }
                utils.notify(`Configuration failed: ${data.error}`, "danger", "fa fa-exclamation-triangle");
            }
        }).fail((data) => {
            utils.notifyIndexerError(indexerId, data.responseJSON.error, "configuring");
        });
    });
}

function addIndexers(selectedIndexerList, successCallback, errorCallback) {
    $(document).ajaxStop(() => {
        if (successCallback == addSelectedIndexersSuccess) {
            $(document).ajaxStop().off(); // Keep future AJAX events from effecting this
            successCallback();
        }
    }).ajaxError((e, xhr, options, err) => {
        errorCallback(e, xhr, options, err);
    });

    selectedIndexerList.each(function() {
        if (this.checked) {
            addIndexer($(this).data('id'), false);
        }
    });
}
