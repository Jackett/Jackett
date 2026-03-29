import $ from 'jquery';
import { Modal } from 'bootstrap';
import Handlebars from 'handlebars';
import 'datatables.net-bs5';

import * as utils from './utils.ts';
import { api } from './api.js';

export function displaySearch(selectedFilter, selectedIndexer, query, category) {
    const selectedIndexers = selectedIndexer ? selectedIndexer.split(",") : [];
    const releaseTemplate = Handlebars.compile(document.getElementById('jackett-search').innerHTML);
    const releaseDialogHtml = releaseTemplate({
        filters: utils.filters.available,
        active: selectedFilter
    });

    document.getElementById('modals').innerHTML = releaseDialogHtml;

    const releaseDialog = document.getElementById('modals').firstElementChild;
    const releaseDialogModal = new Modal(releaseDialog);

    releaseDialog.addEventListener('shown.bs.modal', () => {
        const searchQuery = releaseDialog.querySelector('#searchquery');
        if (searchQuery) utils.stableFocus(searchQuery);
    });

    releaseDialog.addEventListener('hidden.bs.modal', () => {
        const input = document.querySelector('#indexers div.dt-search input');
        if (input) utils.stableFocus(input);

        window.location.hash = utils.filters.current ? `indexers&filter=${utils.filters.current}` : '';
        document.title = "Jackett";
    });

    const searchFilter = document.getElementById('searchFilter');
    const searchTracker = document.getElementById('searchTracker');
    const searchCategory = document.getElementById('searchCategory');
    const searchQuery = document.getElementById("searchQuery");
    const searchButton = document.getElementById('jackett-search-perform');
    const searchResults = document.getElementById('searchResults');

    searchQuery.addEventListener("keyup", (event) => {
        event.preventDefault();
        if (event.key === 'Enter') {
            document.getElementById("jackett-search-perform").click();
        }
    });
    searchButton.addEventListener('click', () => {
        const spinnerElement = searchButton.querySelector('span');
        if (spinnerElement?.classList.contains("spinner")) {
            // We are searchin already
            return;
        }
        const searchString = $(searchQuery).val();
        const filterId = $(searchFilter).val();
        const queryObj = {
            Query: searchString,
            Category: $(searchCategory).val(),
            Tracker: $(searchTracker).val()
        };

        window.location.hash = Object.entries({
            search: encodeURIComponent(queryObj.Query).replace(/%20/g, '+'),
            tracker: queryObj.Tracker.join(","),
            category: queryObj.Category.join(","),
            filter: filterId ? encodeURIComponent(filterId) : ""
        }).filter(([_, v]) => v).map(([k, v], _) => `${k}=${v}`).join('&');

        searchButton.innerHTML = "<div class='spinner-border spinner-border-sm' role='status'><span class='sr-only'>Loading...</span></div>";
        const searchResultsInput = document.querySelector('#searchResults div.dt-search input');
        searchResultsInput.value = "";
        clearSearchResultTable();

        document.title = `(...) ${searchString}`;

        const trackerId = filterId || "all";
        searchButton.innerHTML = '<span class="fa fa-search"></span>';
        api.resultsForIndexer(trackerId, queryObj, (data) => {
            searchResults.innerHTML = '';
            updateSearchResultTable(searchResults, data).search('').columns().search('').draw();
            const input = searchResults.querySelector('div.dt-search input');
            if (input) utils.stableFocus(input);
            document.title = `(${data.Results.length}) ${searchString}`;
        }).fail(() => {
            utils.notify("Request to Jackett server failed", "danger", "fa fa-exclamation-triangle");
            document.title = `(err) ${searchString}`;
        });
    });

    updateSearchResultTable(searchResults, []);
    clearSearchResultTable();

    const multiselectConfig = {
        buttonWidth: 'auto',
        maxHeight: 400,
        enableFiltering: true,
        enableCaseInsensitiveFiltering: true,
        nonSelectedText: 'All'
    };
    $(searchFilter).multiselect({
        ...multiselectConfig,
        onChange() {
            setTrackers($(searchFilter).val());
        }
    });
    $(searchTracker).multiselect({
        ...multiselectConfig,
        includeSelectAllOption: true,
        onChange() {
            setCategories($(searchTracker).val());
        }
    });
    $(searchCategory).multiselect({
        ...multiselectConfig,
        includeSelectAllOption: true,
    });

    if (utils.filters.available.length > 0) {
        if (selectedFilter) {
            $(searchFilter).val(selectedFilter);
            $(searchFilter).multiselect("refresh");
        }
        setTrackers($(searchFilter).val());
    }
    else {
        setTrackers(selectedFilter);
    }

    if (selectedIndexers) {
        $(searchTracker).val(selectedIndexers);
        $(searchTracker).multiselect("refresh");
    }
    setCategories($(searchTracker).val());

    if (category !== undefined) {
        $(searchCategory).val(category.split(","));
        $(searchCategory).multiselect("refresh");
    }

    releaseDialogModal.show();
    if (query !== undefined) {
        searchQuery.value = query;
        searchButton.click();
    }
}

export function displaySearchIfNeed() {
    const hashArgs = utils.getHashArgs();
    if ("search" in hashArgs) {
        displaySearch(hashArgs.filter, hashArgs.tracker, hashArgs.search, hashArgs.category);
    }
}

function setTrackers(filterId) {
    let trackers = utils.indexers.configured;
    const releaseDialog = document.getElementById('modals').firstElementChild;
    const searchTracker = releaseDialog.querySelector('#searchTracker');
    const selected = $(searchTracker).val() || [];
    const filter = utils.filters.available.find(f => f.id == filterId);
    if (filter)
        trackers = trackers.filter(filter.apply, filter);
    const options = trackers.map(t => ({
        label: t.name,
        value: t.id
    }));
    $(searchTracker).multiselect('dataprovider', options);
    $(searchTracker).val(selected).multiselect("refresh");
}

function setCategories(trackers) {
    const releaseDialog = document.getElementById('modals').firstElementChild;
    const cats = {};
    utils.indexers.configured.forEach(item => {
        if ((trackers.length != 0) && !trackers.includes(item.id))
            return;
        item.caps.forEach(cat => {
            if (cat.ID < 100000 || trackers.length == 1)
                cats[cat.ID] = cat.Name;
        });
    });
    const searchCategory = releaseDialog.querySelector('#searchCategory');
    const selected = $(searchCategory).val() || [];
    const options = Object.entries(cats).map(([ID, Name]) => ({
        label: `${ID} (${Name})`,
        value: ID
    }));
    $(searchCategory).multiselect('dataprovider', options);
    $(searchCategory).val(selected).multiselect("refresh");
}

// dataTable dead torrent filter
$.fn.DataTable.ext.search.push((settings, data, _) => {
    if (settings.sInstance !== "jackett-search-results-datatable") {
        return true;
    }
    const deadfilterCheckbox = settings.nTableWrapper.querySelector(".dataTables_deadfilter input");
    if (!deadfilterCheckbox) {
        return true;
    }
    const seeders = data[9];
    return deadfilterCheckbox.checked || seeders !== 0;
});

function updateSearchResultTable(element, results) {
    const resultsTemplate = Handlebars.compile(document.getElementById('jackett-search-results').textContent);
    element.innerHTML = resultsTemplate(results);

    const rows = element.querySelectorAll('tr.jackett-search-results-row');
    rows.forEach(row => utils.updateReleasesRow(row));
    const settings = { "deadfilter": true };
    const tableElement = element.querySelector('table');
    return $(tableElement).DataTable({
        stateSaveParams(settings, data) {
            data.search.search = ""; // don't save the search filter content
            data.deadfilter = settings.deadfilter;
            return data;
        },
        stateLoadParams(settings, data) {
            if ("deadfilter" in data)
                settings.deadfilter = data.deadfilter;
        },
        layout: {
            topStart: 'pageLength',
            topEnd: {
                features: {
                    div: {
                        className: 'dataTables_deadfilter',
                    },
                    search: {},
                }
            },
            bottomStart: 'info',
            bottomEnd: 'paging'
        },
        stateSave: true,
        stateDuration: 0,
        autoWidth: false,
        pageLength: 20,
        processing: true,
        lengthMenu: [
            [10, 20, 50, 100, 250, 500, -1],
            [10, 20, 50, 100, 250, 500, "All"]
        ],
        order: [[0, "desc"]],
        columnDefs: [
            {
                targets: 0,
                visible: false,
                searchable: false,
                type: 'date'
            },
            {
                targets: 1,
                visible: true,
                searchable: false,
                orderData: 0
            },
            {
                targets: 4,
                visible: false,
                searchable: false,
                type: 'num'
            },
            {
                targets: 5,
                visible: true,
                searchable: false,
                orderData: 4
            }
        ],
        preDrawCallback() {
            const table = this;
            const datalist = element.querySelector("datalist[id=jackett-search-saved-presets]");

            const presets = utils.getSavedPresets();
            if (presets.length > 0 && datalist) {
                datalist.innerHTML = '';
                presets.forEach(preset => {
                    const option = document.createElement('option');
                    option.value = preset;
                    datalist.appendChild(option);
                });
            }

            const inputSearch = element.querySelector("input[type=search]");
            if (inputSearch) {
                utils.setSavePresetsButtonState(table, element, presets.includes(inputSearch.value.trim()));

                if (!inputSearch.hasAttribute("custom")) {
                    const newInputSearch = inputSearch.cloneNode(true);
                    newInputSearch.setAttribute("custom", "true");
                    newInputSearch.setAttribute("data-bs-toggle", "tooltip");
                    newInputSearch.setAttribute("title", "Search query consists of several keywords.\nKeyword starting with \"-\" is considered a negative match.\nKeywords separated by \"|\" are considered as OR filters.");
                    newInputSearch.setAttribute("list", "jackett-search-saved-presets");
                    newInputSearch.addEventListener("input", function() {
                        const newKeywords = [];
                        var filterText = this.value.trim();
                        const presets = utils.getSavedPresets();
                        utils.setSavePresetsButtonState(table, element, presets.includes(filterText));

                        const filterTextKeywords = filterText.split(" ");
                        filterTextKeywords.forEach((keyword) => {
                            if (["", "+", "-"].includes(keyword))
                                return;
                            let newKeyword;
                            if (keyword.startsWith("+"))
                                newKeyword = utils.escape(keyword.substring(1));
                            else if (keyword.startsWith("-"))
                                newKeyword = `^((?!${utils.escape(keyword.substring(1))}).)*$`;
                            else
                                newKeyword = `(${keyword.split('|').map(k => utils.escape(k)).join('|')})`;
                            // fix search filters with "-", "." or "_" characters in the middle of the word => #13628
                            newKeyword = newKeyword.replace("\\-", "\\-\u200B?").replace("\\.", "\\.\u200B?").replace("_", "_\u200B?");
                            newKeywords.push(newKeyword);
                        });
                        var filterText = newKeywords.join(" ");
                        table.api().search(filterText, true, true).draw();
                    });
                    inputSearch.parentNode.replaceChild(newInputSearch, inputSearch);
                }
            }

            const deadfilterdiv = element.querySelector(".dataTables_deadfilter");
            if (deadfilterdiv) {
                let deadfiltercheckbox = deadfilterdiv.querySelector("input");
                if (!deadfiltercheckbox) {
                    const deadfilterlabel = document.createElement('label');
                    deadfilterlabel.innerHTML = '<input type="checkbox" id="jackett-search-results-datatable_deadfilter_checkbox" value="1"> Show dead torrents';
                    deadfilterdiv.appendChild(deadfilterlabel);
                    deadfiltercheckbox = deadfilterlabel.querySelector("input");
                    deadfiltercheckbox.addEventListener("change", function() {
                        settings.deadfilter = this.checked;
                        table.api().draw();
                    });
                    deadfiltercheckbox.checked = settings.deadfilter;

                    const savepresetlabel = document.createElement('button');
                    savepresetlabel.id = "jackett-search-results-datatable_savepreset_button";
                    savepresetlabel.title = "Save Search Preset";
                    savepresetlabel.className = "btn btn-success btn-sm";
                    savepresetlabel.style.marginLeft = "10px";
                    savepresetlabel.innerHTML = '<span class="fa fa-save"></span>';
                    const searchfilterdiv = element.querySelector("#jackett-search-results-datatable_filter");
                    if (searchfilterdiv) {
                        searchfilterdiv.appendChild(savepresetlabel);
                    }
                }
            }
        },
        initComplete() {
            let count = 0;
            this.api().columns().every(function() {
                count++;
                if (!(count === 3 || count === 8)) {
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
}

function clearSearchResultTable() {
    const searchResult = document.getElementById("searchResults");
    searchResult?.querySelector("#jackett-search-results-datatable > tbody")?.replaceChildren();
    searchResult?.querySelector("#jackett-search-results-datatable > tfoot")?.replaceChildren();
    searchResult?.querySelector("#jackett-search-results-datatable_info")?.replaceChildren();
    searchResult?.querySelector("#jackett-search-results-datatable_paginate")?.replaceChildren();
}
