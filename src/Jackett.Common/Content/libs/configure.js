import $ from 'jquery';
import Handlebars from 'handlebars';
import { Modal } from 'bootstrap';
import Tagify from '@yaireo/tagify';

import * as utils from './utils.ts';
import { api } from './api.js';
import { reloadIndexers } from '../custom.js';

export function populateConfigItems(configForm, config) {
    // Set flag so we show fields named password as a password input
    for (const item of config) {
        item.ispassword = item.id.toLowerCase() === 'password';
    }
    const $formItemContainer = configForm.find(".config-setup-form");
    $formItemContainer.empty();

    const setupItemTemplate = Handlebars.compile($("#setup-item").html());
    for (const item of config) {
        const setupValueTemplate = Handlebars.compile($(`#setup-item-${item.type}`).html());
        item.value_element = setupValueTemplate(item);
        const template = $(setupItemTemplate(item));
        $formItemContainer.append(template);
        initializeTagifyInput(template, item);
    }
}

function initializeTagifyInput(configItem, item) {
    if (item.type !== "inputtags") {
        return;
    }
    const inputElement = configItem.find("input")[0];
    new Tagify(inputElement, {
        dropdown: {
            enabled: 0,
            position: "text"
        },
        separator: item.separator || ",",
        whitelist: item.whitelist || [],
        blacklist: item.blacklist || [],
        pattern: item.pattern || null,
        delimiters: item.delimiters || item.separator || ",",
        originalInputValueFormat(values) { return values.map(item => item.value.toLowerCase()).join(this.separator); }
    });
}

function setupAlternativeSiteLinks(configForm, indexer) {
    if (indexer.alternativesitelinks.length < 1) {
        return;
    }
    const AlternativeSiteLinksTemplate = Handlebars.compile($("#setup-item-alternativesitelinks").html());
    const template = $(AlternativeSiteLinksTemplate({
        "alternativesitelinks": indexer.alternativesitelinks
    }));
    configForm.find("div[data-id='sitelink']").find('label').first().after(template);
    template.find("a.alternativesitelink").on('click', function(a) {
        $("div[data-id='sitelink'] input").val(this.href);
        return false;
    });
}

function setupTagsInput(configForm) {
    const tagsInput = $("div[data-id='tags'] input", configForm)[0];
    if (tagsInput?.tagify) {
        tagsInput.tagify.settings.whitelist = utils.tags.configured;
        tagsInput.tagify.dropdown.refilter();
    }
}

function newConfigForm(indexer, config) {
    const configTemplate = Handlebars.compile($("#jackett-config-setup-modal").html());
    const configForm = $(configTemplate({
        title: indexer.name,
        caps: indexer.caps,
        link: indexer.site_link,
        description: indexer.description
    }));
    $("#modals").html(configForm);
    populateConfigItems(configForm, config);

    setupAlternativeSiteLinks(configForm, indexer);
    setupTagsInput(configForm);

    return configForm;
}

function getConfigModalJson(configForm) {
    const configJson = [];
    const configElements = configForm[0].querySelectorAll('.config-setup-form > *');
    configElements.forEach(el => {
        const type = el.dataset.type;
        const id = el.dataset.id;
        const itemEntry = { id };
        switch (type) {
            case 'hiddendata':
                itemEntry.value = el.querySelector('.setup-item-hiddendata input').value;
                break;
            case 'inputstring':
                itemEntry.value = el.querySelector('.setup-item-inputstring input').value;
                break;
            case 'password':
                itemEntry.value = el.querySelector('.setup-item-password input').value;
                break;
            case 'inputbool':
                itemEntry.value = el.querySelector('.setup-item-inputbool input').checked;
                break;
            case 'inputcheckbox':
                itemEntry.values = Array.from(
                    el.querySelectorAll('.setup-item-inputcheckbox input:checked')
                ).map(checkbox => checkbox.value);
                break;
            case 'inputselect':
                itemEntry.value = el.querySelector('.setup-item-inputselect select').value;
                break;
            case 'inputtags':
                itemEntry.value = el.querySelector('.setup-item-inputtags input').value;
                break;
        }
        configJson.push(itemEntry);
    });
    return configJson;
}

export function populateSetupForm(indexer, config) {
    const configForm = newConfigForm(indexer, config);
    const configModal = new Modal(configForm[0]);
    const goButton = configForm[0].querySelector(".setup-indexer-go");
    goButton.addEventListener('click', () => {
        const data = getConfigModalJson(configForm);

        const originalBtnText = goButton.innerHTML;
        goButton.disabled = true;
        goButton.innerHTML = "<div class='spinner-border spinner-border-sm'/>";

        api.updateIndexerConfig(indexer.id, data, data => {
            if (data == undefined) {
                configModal.hide();
                reloadIndexers();
                utils.notify(`Successfully configured ${indexer.name}`, "success", "fa fa-check");
                return;
            }
            if (data.result == "error") {
                if (data.config) {
                    populateConfigItems(configForm, data.config);
                }
                utils.notify(`Configuration failed: ${data.error}`, "danger", "fa fa-exclamation-triangle");
            }
        }).fail((data) => {
            utils.notifyIndexerError(indexer.id, data.responseJSON.error, "updating");
        }).always(() => {
            goButton.innerHTML = originalBtnText;
            goButton.disabled = false;
        });
    });

    configForm.on('hidden.bs.modal', () => {
        $('#indexers div.dt-search input').stableFocus();
    });
    configModal.show();
}
