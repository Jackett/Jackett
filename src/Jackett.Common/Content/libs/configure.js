import $ from 'jquery';
import Handlebars from 'handlebars';
import { Modal } from 'bootstrap';
import Tagify from '@yaireo/tagify';
import { api } from './api.js';
import * as utils from './utils.ts';
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

function newConfigForm(title, config, caps, link, alternativesitelinks, description) {
    const configTemplate = Handlebars.compile($("#jackett-config-setup-modal").html());
    const configForm = $(configTemplate({
        title,
        caps,
        link,
        description
    }));
    $("#modals").html(configForm);
    populateConfigItems(configForm, config);

    if (alternativesitelinks.length >= 1) {
        const AlternativeSiteLinksTemplate = Handlebars.compile($("#setup-item-alternativesitelinks").html());
        const template = $(AlternativeSiteLinksTemplate({
            "alternativesitelinks": alternativesitelinks
        }));
        configForm.find("div[data-id='sitelink']").after(template);
        template.find("a.alternativesitelink").on('click', function(a) {
            $("div[data-id='sitelink'] input").val(this.href);
            return false;
        });
    }

    const tagsInput = $("div[data-id='tags'] input", configForm)[0];
    if (tagsInput?.tagify) {
        tagsInput.tagify.settings.whitelist = utils.tags.configured;
        tagsInput.tagify.dropdown.refilter();
    }

    return configForm;
}

function getConfigModalJson(configForm) {
    const configJson = [];
    configForm.find(".config-setup-form").children().each((i, el) => {
        const $el = $(el);
        const type = $el.data("type");
        const id = $el.data("id");
        const itemEntry = {
            id
        };
        switch (type) {
            case "hiddendata":
                itemEntry.value = $el.find(".setup-item-hiddendata input").val();
                break;
            case "inputstring":
                itemEntry.value = $el.find(".setup-item-inputstring input").val();
                break;
            case "password":
                itemEntry.value = $el.find(".setup-item-password input").val();
                break;
            case "inputbool":
                itemEntry.value = $el.find(".setup-item-inputbool input").is(":checked");
                break;
            case "inputcheckbox":
                itemEntry.values = [];
                $el.find(".setup-item-inputcheckbox input:checked").each(function() {
                    itemEntry.values.push($(this).val());
                });
                break;
            case "inputselect":
                itemEntry.value = $el.find(".setup-item-inputselect select").val();
                break;
            case "inputtags":
                itemEntry.value = $el.find(".setup-item-inputtags input").val();
                break;
        }
        configJson.push(itemEntry);
    });
    return configJson;
}

export function populateSetupForm(indexerId, name, config, caps, link, alternativesitelinks, description) {
    const configForm = newConfigForm(name, config, caps, link, alternativesitelinks, description);
    const configModal = new Modal(configForm[0]);
    const $goButton = configForm.find(".setup-indexer-go");
    $goButton.on('click', () => {
        const data = getConfigModalJson(configForm);

        const originalBtnText = $goButton.html();
        $goButton.prop('disabled', true);
        $goButton.html("<div class='spinner-border spinner-border-sm'/>");

        api.updateIndexerConfig(indexerId, data, data => {
            if (data == undefined) {
                configModal.hide();
                reloadIndexers();
                utils.notify(`Successfully configured ${name}`, "success", "fa fa-check");
                return;
            }
            if (data.result == "error") {
                if (data.config) {
                    populateConfigItems(configForm, data.config);
                }
                utils.notify(`Configuration failed: ${data.error}`, "danger", "fa fa-exclamation-triangle");
            }
        }).fail((data) => {
            utils.notifyError(indexerId, data.responseJSON.error, "updating");
        }).always(() => {
            $goButton.html(originalBtnText);
            $goButton.prop('disabled', false);
        });
    });

    configForm.on('hidden.bs.modal', (e) => {
        $('#indexers div.dataTables_filter input').stableFocus();
    });
    configModal.show();
}
