/**
 * Custom modifications to the original bootstrap-multiselect plugin.
 * In order to import it as ESM moudle.
 */
/**
 * Bootstrap Multiselect (http://davidstutz.de/bootstrap-multiselect/)
 *
 * Apache License, Version 2.0:
 * Copyright (c) 2012 - 2022 David Stutz
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not
 * use this file except in compliance with the License. You may obtain a
 * copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations
 * under the License.
 *
 * BSD 3-Clause License:
 * Copyright (c) 2012 - 2022 David Stutz
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *    - Redistributions of source code must retain the above copyright notice,
 *      this list of conditions and the following disclaimer.
 *    - Redistributions in binary form must reproduce the above copyright notice,
 *      this list of conditions and the following disclaimer in the documentation
 *      and/or other materials provided with the distribution.
 *    - Neither the name of David Stutz nor the names of its contributors may be
 *      used to endorse or promote products derived from this software without
 *      specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO,
 * THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
 * PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
 * EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 * PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS;
 * OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR
 * OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

import $ from 'jquery';

"use strict";// jshint ;_;

// Knockout.js related code has been removed since window.ko is not available

function forEach(array, callback) {
    for (var index = 0; index < array.length; ++index) {
        callback(array[index], index);
    }
}

var multiselectCount = 0;

/**
 * Constructor to create a new multiselect using the given select.
 *
 * @param {jQuery} select
 * @param {Object} options
 * @returns {Multiselect}
 */
function Multiselect(select, options) {

    this.$select = $(select);
    this.options = this.mergeOptions($.extend({}, options, this.$select.data()));

    // Placeholder via data attributes
    if (this.$select.attr("data-placeholder")) {
        this.options.nonSelectedText = this.$select.data("placeholder");
    }

    // Initialization.
    // We have to clone to create a new reference.
    this.originalOptions = this.$select.clone()[0].options;
    this.query = '';
    this.searchTimeout = null;
    this.lastToggledInput = null;
    this.multiselectId = this.generateUniqueId() + '_' + multiselectCount;
    this.internalIdCount = 0;

    this.options.multiple = this.$select.attr('multiple') === "multiple";
    this.options.onChange = $.proxy(this.options.onChange, this);
    this.options.onSelectAll = $.proxy(this.options.onSelectAll, this);
    this.options.onDeselectAll = $.proxy(this.options.onDeselectAll, this);
    this.options.onDropdownShow = $.proxy(this.options.onDropdownShow, this);
    this.options.onDropdownHide = $.proxy(this.options.onDropdownHide, this);
    this.options.onDropdownShown = $.proxy(this.options.onDropdownShown, this);
    this.options.onDropdownHidden = $.proxy(this.options.onDropdownHidden, this);
    this.options.onInitialized = $.proxy(this.options.onInitialized, this);
    this.options.onFiltering = $.proxy(this.options.onFiltering, this);

    // Build select all if enabled.
    this.buildContainer();
    this.buildButton();
    this.buildDropdown();
    this.buildReset();
    this.buildSelectAll();
    this.buildDropdownOptions();
    this.buildFilter();
    this.buildButtons();

    this.updateButtonText();
    this.updateSelectAll(true);

    if (this.options.enableClickableOptGroups && this.options.multiple) {
        this.updateOptGroups();
    }

    this.options.wasDisabled = this.$select.prop('disabled');
    if (this.options.disableIfEmpty && $('option', this.$select).length <= 0 && !this.options.wasDisabled) {
        this.disable(true);
    }

    this.$select.wrap('<span class="multiselect-native-select" />').after(this.$container);
    this.$select.prop('tabindex', '-1');

    if (this.options.widthSynchronizationMode !== 'never') {
        this.synchronizeButtonAndPopupWidth();
    }

    this.$select.data('multiselect', this);
    this.options.onInitialized(this.$select, this.$container);
}

Multiselect.prototype = {

    defaults: {
        /**
         * Default text function will either print 'None selected' in case no
         * option is selected or a list of the selected options up to a length
         * of 3 selected options.
         *
         * @param {jQuery} options
         * @param {jQuery} select
         * @returns {String}
         */
        buttonText: function(selectedOptions, select) {
            if (this.disabledText.length > 0 && select.prop('disabled')) {
                return this.disabledText;
            }
            else if (selectedOptions.length === 0) {
                return this.nonSelectedText;
            }
            else if (this.allSelectedText
                && selectedOptions.length === $('option', $(select)).length
                && $('option', $(select)).length !== 1
                && this.multiple) {

                if (this.selectAllNumber) {
                    return this.allSelectedText + ' (' + selectedOptions.length + ')';
                }
                else {
                    return this.allSelectedText;
                }
            }
            else if (this.numberDisplayed != 0 && selectedOptions.length > this.numberDisplayed) {
                return selectedOptions.length + ' ' + this.nSelectedText;
            }
            else {
                var selected = '';
                var delimiter = this.delimiterText;

                selectedOptions.each(function() {
                    var label = ($(this).attr('label') !== undefined) ? $(this).attr('label') : $(this).text();
                    selected += label + delimiter;
                });

                return selected.substr(0, selected.length - this.delimiterText.length);
            }
        },
        /**
         * Updates the title of the button similar to the buttonText function.
         *
         * @param {jQuery} options
         * @param {jQuery} select
         * @returns {@exp;selected@call;substr}
         */
        buttonTitle: function(options, select) {
            if (options.length === 0) {
                return this.nonSelectedText;
            }
            else {
                var selected = '';
                var delimiter = this.delimiterText;

                options.each(function() {
                    var label = ($(this).attr('label') !== undefined) ? $(this).attr('label') : $(this).text();
                    selected += label + delimiter;
                });
                return selected.substr(0, selected.length - this.delimiterText.length);
            }
        },
        checkboxName: function(option) {
            return false; // no checkbox name
        },
        /**
         * Create a label.
         *
         * @param {jQuery} element
         * @returns {String}
         */
        optionLabel: function(element) {
            return $(element).attr('label') || $(element).text();
        },
        /**
         * Create a class.
         *
         * @param {jQuery} element
         * @returns {String}
         */
        optionClass: function(element) {
            return $(element).attr('class') || '';
        },
        /**
         * Triggered on change of the multiselect.
         *
         * Not triggered when selecting/deselecting options manually.
         *
         * @param {jQuery} option
         * @param {Boolean} checked
         */
        onChange: function(option, checked) {

        },
        /**
         * Triggered when the dropdown is shown.
         *
         * @param {jQuery} event
         */
        onDropdownShow: function(event) {

        },
        /**
         * Triggered when the dropdown is hidden.
         *
         * @param {jQuery} event
         */
        onDropdownHide: function(event) {

        },
        /**
         * Triggered after the dropdown is shown.
         *
         * @param {jQuery} event
         */
        onDropdownShown: function(event) {

        },
        /**
         * Triggered after the dropdown is hidden.
         *
         * @param {jQuery} event
         */
        onDropdownHidden: function(event) {

        },
        /**
         * Triggered on select all.
         */
        onSelectAll: function(selectedOptions) {

        },
        /**
         * Triggered on deselect all.
         */
        onDeselectAll: function(deselectedOptions) {

        },
        /**
         * Triggered after initializing.
         *
         * @param {jQuery} $select
         * @param {jQuery} $container
         */
        onInitialized: function($select, $container) {

        },
        /**
         * Triggered on filtering.
         *
         * @param {jQuery} $filter
         */
        onFiltering: function($filter) {

        },
        enableHTML: false,
        buttonClass: 'custom-select',
        inheritClass: false,
        buttonWidth: 'auto',
        buttonContainer: '<div class="btn-group" />',
        dropRight: false,
        dropUp: false,
        selectedClass: 'active',
        // Maximum height of the dropdown menu.
        // If maximum height is exceeded a scrollbar will be displayed.
        maxHeight: null,
        includeSelectAllOption: false,
        includeSelectAllIfMoreThan: 0,
        selectAllText: ' Select all',
        selectAllValue: 'multiselect-all',
        selectAllName: false,
        selectAllNumber: true,
        selectAllJustVisible: true,
        enableFiltering: false,
        enableCaseInsensitiveFiltering: false,
        enableFullValueFiltering: false,
        enableClickableOptGroups: false,
        enableCollapsibleOptGroups: false,
        collapseOptGroupsByDefault: false,
        filterPlaceholder: 'Search',
        // possible options: 'text', 'value', 'both'
        filterBehavior: 'text',
        includeFilterClearBtn: true,
        preventInputChangeEvent: false,
        nonSelectedText: 'None selected',
        nSelectedText: 'selected',
        allSelectedText: 'All selected',
        resetButtonText: 'Reset',
        numberDisplayed: 3,
        disableIfEmpty: false,
        disabledText: '',
        delimiterText: ', ',
        includeResetOption: false,
        includeResetDivider: false,
        resetText: 'Reset',
        indentGroupOptions: true,
        // possible options: 'never', 'always', 'ifPopupIsSmaller', 'ifPopupIsWider'
        widthSynchronizationMode: 'never',
        // possible options: 'left', 'center', 'right'
        buttonTextAlignment: 'center',
        enableResetButton: false,
        templates: {
            button: '<button type="button" class="multiselect form-select dropdown-toggle" data-bs-toggle="dropdown"><span class="multiselect-selected-text"></span></button>',
            popupContainer: '<div class="multiselect-container dropdown-menu"></div>',
            filter: '<div class="multiselect-filter d-flex align-items-center"><i class="fas fa-sm fa-search text-body-secondary"></i><input type="search" class="multiselect-search form-control" /></div>',
            buttonGroup: '<div class="multiselect-buttons btn-group" style="display:flex;"></div>',
            buttonGroupReset: '<button type="button" class="multiselect-reset btn btn-secondary w-100"></button>',
            option: '<button type="button" class="multiselect-option dropdown-item"></button>',
            divider: '<div class="dropdown-divider"></div>',
            optionGroup: '<button type="button" class="multiselect-group dropdown-item"></button>',
            resetButton: '<div class="multiselect-reset text-center p-2"><button type="button" class="btn btn-sm w-100 btn-outline-secondary"></button></div>'
        }
    },

    constructor: Multiselect,

    /**
     * Builds the container of the multiselect.
     */
    buildContainer: function() {
        this.$container = $(this.options.buttonContainer);
        if (this.options.widthSynchronizationMode !== 'never') {
            this.$container.on('show.bs.dropdown', $.proxy(function() {
                // the width needs to be synchronized again in case the width of the button changed in between
                this.synchronizeButtonAndPopupWidth();
                this.options.onDropdownShow();
            }, this));
        }
        else {
            this.$container.on('show.bs.dropdown', this.options.onDropdownShow);
        }
        this.$container.on('hide.bs.dropdown', this.options.onDropdownHide);
        this.$container.on('shown.bs.dropdown', this.options.onDropdownShown);
        this.$container.on('hidden.bs.dropdown', this.options.onDropdownHidden);
    },

    /**
     * Builds the button of the multiselect.
     */
    buildButton: function() {
        this.$button = $(this.options.templates.button).addClass(this.options.buttonClass);
        if (this.$select.attr('class') && this.options.inheritClass) {
            this.$button.addClass(this.$select.attr('class'));
        }
        // Adopt active state.
        if (this.$select.prop('disabled')) {
            this.disable();
        }
        else {
            this.enable();
        }

        // Manually add button width if set.
        if (this.options.buttonWidth && this.options.buttonWidth !== 'auto') {
            this.$button.css({
                'width': '100%' //this.options.buttonWidth,
            });
            this.$container.css({
                'width': this.options.buttonWidth
            });
        }

        if (this.options.buttonTextAlignment) {
            switch (this.options.buttonTextAlignment) {
                case 'left':
                    this.$button.addClass('text-start');
                    break;
                case 'center':
                    this.$button.addClass('text-center');
                    break;
                case 'right':
                    this.$button.addClass('text-end');
                    break;
            }
        }

        // Keep the tab index from the select.
        var tabindex = this.$select.attr('tabindex');
        if (tabindex) {
            this.$button.attr('tabindex', tabindex);
        }

        this.$container.prepend(this.$button);
    },

    /**
     * Builds the popup container representing the dropdown menu.
     */
    buildDropdown: function() {

        // Build popup container.
        this.$popupContainer = $(this.options.templates.popupContainer);

        if (this.options.dropRight) {
            this.$container.addClass('dropend');
        }
        else if (this.options.dropUp) {
            this.$container.addClass("dropup");
        }

        // Set max height of dropdown menu to activate auto scrollbar.
        if (this.options.maxHeight) {
            // TODO: Add a class for this option to move the css declarations.
            this.$popupContainer.css({
                'max-height': this.options.maxHeight + 'px',
                'overflow-y': 'auto',
                'overflow-x': 'hidden'
            });
        }

        if (this.options.widthSynchronizationMode !== 'never') {
            this.$popupContainer.css('overflow-x', 'hidden');
        }

        this.$popupContainer.on("touchstart click", function(e) {
            e.stopPropagation();
        });

        this.$container.append(this.$popupContainer);
    },

    synchronizeButtonAndPopupWidth: function() {
        if (!this.$popupContainer || this.options.widthSynchronizationMode === 'never') {
            return;
        }

        var buttonWidth = this.$button.outerWidth();
        switch (this.options.widthSynchronizationMode) {
            case 'always':
                this.$popupContainer.css('min-width', buttonWidth);
                this.$popupContainer.css('max-width', buttonWidth);
                break;
            case 'ifPopupIsSmaller':
                this.$popupContainer.css('min-width', buttonWidth);
                break;
            case 'ifPopupIsWider':
                this.$popupContainer.css('max-width', buttonWidth);
                break;
        }
    },

    /**
     * Build the dropdown options and binds all necessary events.
     *
     * Uses createDivider and createOptionValue to create the necessary options.
     */
    buildDropdownOptions: function() {

        this.$select.children().each($.proxy(function(index, element) {

            var $element = $(element);
            // Support optgroups and options without a group simultaneously.
            var tag = $element.prop('tagName')
                .toLowerCase();

            if ($element.prop('value') === this.options.selectAllValue) {
                return;
            }

            if (tag === 'optgroup') {
                this.createOptgroup(element);
            }
            else if (tag === 'option') {

                if ($element.data('role') === 'divider') {
                    this.createDivider();
                }
                else {
                    this.createOptionValue(element, false);
                }

            }

            // Other illegal tags will be ignored.
        }, this));

        // Bind the change event on the dropdown elements.
        $(this.$popupContainer).off('change', '> *:not(.multiselect-group) input[type="checkbox"], > *:not(.multiselect-group) input[type="radio"]');
        $(this.$popupContainer).on('change', '> *:not(.multiselect-group) input[type="checkbox"], > *:not(.multiselect-group) input[type="radio"]', $.proxy(function(event) {
            var $target = $(event.target);

            var checked = $target.prop('checked') || false;
            var isSelectAllOption = $target.val() === this.options.selectAllValue;

            // Apply or unapply the configured selected class.
            if (this.options.selectedClass) {
                if (checked) {
                    $target.closest('.multiselect-option')
                        .addClass(this.options.selectedClass);
                }
                else {
                    $target.closest('.multiselect-option')
                        .removeClass(this.options.selectedClass);
                }
            }

            // Get the corresponding option.
            var id = $target.attr('id');
            var $option = this.getOptionById(id);

            var $optionsNotThis = $('option', this.$select).not($option);
            var $checkboxesNotThis = $('input', this.$container).not($target);

            if (isSelectAllOption) {

                if (checked) {
                    this.selectAll(this.options.selectAllJustVisible, true);
                }
                else {
                    this.deselectAll(this.options.selectAllJustVisible, true);
                }
            }
            else {
                if (checked) {
                    $option.prop('selected', true);

                    if (this.options.multiple) {
                        // Simply select additional option.
                        $option.prop('selected', true);
                    }
                    else {
                        // Unselect all other options and corresponding checkboxes.
                        if (this.options.selectedClass) {
                            $($checkboxesNotThis).closest('.dropdown-item').removeClass(this.options.selectedClass);
                        }

                        $($checkboxesNotThis).prop('checked', false);
                        $optionsNotThis.prop('selected', false);

                        // It's a single selection, so close.
                        this.$button.click();
                    }

                    if (this.options.selectedClass === "active") {
                        $optionsNotThis.closest(".dropdown-item").css("outline", "");
                    }
                }
                else {
                    // Unselect option.
                    $option.prop('selected', false);
                }

                // To prevent select all from firing onChange: #575
                this.options.onChange($option, checked);

                // Do not update select all or optgroups on select all change!
                this.updateSelectAll();

                if (this.options.enableClickableOptGroups && this.options.multiple) {
                    this.updateOptGroups();
                }
            }

            this.$select.change();
            this.updateButtonText();

            if (this.options.preventInputChangeEvent) {
                return false;
            }
        }, this));

        $('.multiselect-option', this.$popupContainer).off('mousedown');
        $('.multiselect-option', this.$popupContainer).on('mousedown', function(e) {
            if (e.shiftKey) {
                // Prevent selecting text by Shift+click
                return false;
            }
        });

        $(this.$popupContainer).off('touchstart click', '.multiselect-option, .multiselect-all, .multiselect-group');
        $(this.$popupContainer).on('touchstart click', '.multiselect-option, .multiselect-all, .multiselect-group', $.proxy(function(event) {
            event.stopPropagation();

            var $target = $(event.target);

            if (event.shiftKey && this.options.multiple) {
                if (!$target.is("input")) { // Handles checkbox selection manually (see https://github.com/davidstutz/bootstrap-multiselect/issues/431)
                    event.preventDefault();
                    $target = $target.closest(".multiselect-option").find("input");
                    $target.prop("checked", !$target.prop("checked"));
                }
                var checked = $target.prop('checked') || false;

                if (this.lastToggledInput !== null && this.lastToggledInput !== $target) { // Make sure we actually have a range
                    var from = this.$popupContainer.find(".multiselect-option:visible").index($target.closest(".multiselect-option"));
                    var to = this.$popupContainer.find(".multiselect-option:visible").index(this.lastToggledInput.closest(".multiselect-option"));

                    if (from > to) { // Swap the indices
                        var tmp = to;
                        to = from;
                        from = tmp;
                    }

                    // Make sure we grab all elements since slice excludes the last index
                    ++to;

                    // Change the checkboxes and underlying options
                    var range = this.$popupContainer.find(".multiselect-option:not(.multiselect-filter-hidden)").slice(from, to).find("input");

                    range.prop('checked', checked);

                    if (this.options.selectedClass) {
                        range.closest('.multiselect-option')
                            .toggleClass(this.options.selectedClass, checked);
                    }

                    for (var i = 0, j = range.length; i < j; i++) {
                        var $checkbox = $(range[i]);

                        var $option = this.getOptionById($checkbox.attr('id'));

                        $option.prop('selected', checked);
                    }
                }

                // Trigger the select "change" event
                $target.trigger("change");
            }
            else if (!$target.is('input')) {
                var $checkbox = $target.closest('.multiselect-option, .multiselect-all').find('.form-check-input');
                if ($checkbox.length > 0) {
                    if (this.options.multiple || !$checkbox.prop('checked')) {
                        $checkbox.prop('checked', !$checkbox.prop('checked'));
                        $checkbox.change();
                    }
                }
                else if (this.options.enableClickableOptGroups && this.options.multiple && !$target.hasClass("caret-container")) {
                    var groupItem = $target;
                    if (!groupItem.hasClass("multiselect-group")) {
                        groupItem = $target.closest('.multiselect-group');
                    }
                    $checkbox = groupItem.find(".form-check-input");
                    if ($checkbox.length > 0) {
                        $checkbox.prop('checked', !$checkbox.prop('checked'));
                        $checkbox.change();
                    }
                }

                event.preventDefault();
            }

            // Remembers last clicked option
            var $input = $target.closest(".multiselect-option").find("input[type='checkbox'], input[type='radio']");
            if ($input.length > 0) {
                this.lastToggledInput = $target;
            }
            else {
                this.lastToggledInput = null;
            }

            $target.blur();
        }, this));

        //Keyboard support.
        this.$container.off('keydown.multiselect').on('keydown.multiselect', $.proxy(function(event) {
            var $items = $(this.$container).find(".multiselect-option:not(.disabled), .multiselect-group:not(.disabled), .multiselect-all").filter(":visible");
            var index = $items.index($items.filter(':focus'));
            var $search = $('.multiselect-search', this.$container);

            // keyCode 9 == Tab
            if (event.keyCode === 9 && this.$container.hasClass('show')) {
                this.$button.click();
            }
            // keyCode 13 = Enter
            else if (event.keyCode == 13) {
                var $current = $items.eq(index);
                setTimeout(function() {
                    $current.focus();
                }, 1);
            }
            // keyCode 38 = Arrow Up
            else if (event.keyCode == 38) {
                if (index == 0 && !$search.is(':focus')) {
                    setTimeout(function() {
                        $search.focus();
                    }, 1);
                }
            }
            // keyCode 40 = Arrow Down
            else if (event.keyCode == 40) {
                if ($search.is(':focus')) {
                    var $first = $items.eq(0);
                    setTimeout(function() {
                        $search.blur();
                        $first.focus();
                    }, 1);
                }
                else if (index == -1) {
                    setTimeout(function() {
                        $search.focus();
                    }, 1);
                }
            }
        }, this));

        if (this.options.enableClickableOptGroups && this.options.multiple) {
            $(".multiselect-group input", this.$popupContainer).off("change");
            $(".multiselect-group input", this.$popupContainer).on("change", $.proxy(function(event) {
                event.stopPropagation();

                var $target = $(event.target);
                var checked = $target.prop('checked') || false;

                var $item = $(event.target).closest('.dropdown-item');
                var $group = $item.nextUntil(".multiselect-group")
                    .not('.multiselect-filter-hidden')
                    .not('.disabled');

                var $inputs = $group.find("input");

                var $options = [];

                if (this.options.selectedClass) {
                    if (checked) {
                        $item.addClass(this.options.selectedClass);
                    }
                    else {
                        $item.removeClass(this.options.selectedClass);
                    }
                }

                $.each($inputs, $.proxy(function(index, input) {
                    var $input = $(input);
                    var id = $input.attr('id');
                    var $option = this.getOptionById(id);

                    if (checked) {
                        $input.prop('checked', true);
                        $input.closest('.dropdown-item')
                            .addClass(this.options.selectedClass);

                        $option.prop('selected', true);
                    }
                    else {
                        $input.prop('checked', false);
                        $input.closest('.dropdown-item')
                            .removeClass(this.options.selectedClass);

                        $option.prop('selected', false);
                    }

                    $options.push($option);
                }, this));

                // Cannot use select or deselect here because it would call updateOptGroups again.

                this.options.onChange($options, checked);

                this.$select.change();
                this.updateButtonText();
                this.updateSelectAll();
            }, this));
        }

        if (this.options.enableCollapsibleOptGroups) {
            let clickableSelector = this.options.enableClickableOptGroups
                ? ".multiselect-group .caret-container"
                : ".multiselect-group";

            $(clickableSelector, this.$popupContainer).off("click");
            $(clickableSelector, this.$popupContainer).on("click", $.proxy(function(event) {
                var $group = $(event.target).closest('.multiselect-group');
                var $inputs = $group.nextUntil(".multiselect-group").not('.multiselect-filter-hidden');

                var visible = true;
                $inputs.each(function() {
                    visible = visible && !$(this).hasClass('multiselect-collapsible-hidden');
                });

                if (visible) {
                    $inputs.hide().addClass('multiselect-collapsible-hidden');
                    $group.get(0).classList.add("closed");
                } else {
                    $inputs.show().removeClass('multiselect-collapsible-hidden');
                    $group.get(0).classList.remove("closed");
                }
            }, this));
        }
    },

    /**
     * Create a checkbox container with input and label based on given values
     * @param {JQuery} $item
     * @param {String} label
     * @param {String} name
     * @param {String} value
     * @param {String} inputType
     * @returns {JQuery}
     */
    createCheckbox: function($item, labelContent, name, value, title, inputType, internalId) {
        var $wrapper = $('<span />');
        $wrapper.addClass("form-check");

        var $checkboxLabel = $('<label class="form-check-label" />');
        if (this.options.enableHTML && $(labelContent).length > 0) {
            $checkboxLabel.html(labelContent);
        }
        else {
            $checkboxLabel.text(labelContent);
        }
        $wrapper.append($checkboxLabel);

        var $checkbox = $('<input class="form-check-input"/>').attr('type', inputType);
        $checkbox.val(value);
        $wrapper.prepend($checkbox);

        if (internalId) {
            $checkbox.attr('id', internalId);
            $checkboxLabel.attr('for', internalId);
        }

        if (name) {
            $checkbox.attr('name', name);
        }

        $item.prepend($wrapper);
        $item.attr("title", title || labelContent);

        return $checkbox;
    },

    /**
     * Create an option using the given select option.
     *
     * @param {jQuery} element
     */
    createOptionValue: function(element, isGroupOption) {
        var $element = $(element);
        if ($element.is(':selected')) {
            $element.prop('selected', true);
        }

        // Support the label attribute on options.
        var label = this.options.optionLabel(element);
        var classes = this.options.optionClass(element);
        var value = $element.val();
        var inputType = this.options.multiple ? "checkbox" : "radio";
        var title = $element.attr('title');

        var $option = $(this.options.templates.option);
        $option.addClass(classes);

        if (isGroupOption && this.options.indentGroupOptions) {
            if (this.options.enableCollapsibleOptGroups) {
                $option.addClass("multiselect-group-option-indented-full");
            }
            else {
                $option.addClass("multiselect-group-option-indented");
            }
        }

        // Hide all children items when collapseOptGroupsByDefault is true
        if (this.options.collapseOptGroupsByDefault && $(element).parent().prop("tagName").toLowerCase() === "optgroup") {
            $option.addClass("multiselect-collapsible-hidden");
            $option.hide();
        }

        var name = this.options.checkboxName($element);

        var checkboxId = this.createAndApplyUniqueId($element);
        var $checkbox = this.createCheckbox($option, label, name, value, title, inputType, checkboxId);

        var selected = $element.prop('selected') || false;

        if (value === this.options.selectAllValue) {
            $option.addClass("multiselect-all");
            $option.removeClass("multiselect-option");
            $checkbox.parent().parent()
                .addClass('multiselect-all');
        }

        this.$popupContainer.append($option);

        if ($element.is(':disabled')) {
            $checkbox.attr('disabled', 'disabled')
                .prop('disabled', true)
                .closest('.dropdown-item')
                .addClass('disabled');
        }

        $checkbox.prop('checked', selected);

        if (selected && this.options.selectedClass) {
            $checkbox.closest('.dropdown-item')
                .addClass(this.options.selectedClass);
        }
    },

    /**
     * Creates a divider using the given select option.
     *
     * @param {jQuery} element
     */
    createDivider: function(element) {
        var $divider = $(this.options.templates.divider);
        this.$popupContainer.append($divider);
    },

    /**
     * Creates an optgroup.
     *
     * @param {jQuery} group
     */
    createOptgroup: function(group) {
        var $group = $(group);
        var label = $group.attr("label");
        var value = $group.attr("value");
        var title = $group.attr('title');

        var $groupOption = $("<span class='multiselect-group dropdown-item-text'></span>");

        if (this.options.enableClickableOptGroups && this.options.multiple) {
            $groupOption = $(this.options.templates.optionGroup);
            var checkboxId = this.createAndApplyUniqueId($group);
            var $checkbox = this.createCheckbox($groupOption, label, null, value, title, "checkbox", checkboxId);
        }
        else {
            if (this.options.enableHTML) {
                $groupOption.html(" " + label);
            }
            else {
                $groupOption.text(" " + label);
            }
        }

        var classes = this.options.optionClass(group);
        $groupOption.addClass(classes);

        if (this.options.enableCollapsibleOptGroups) {
            $groupOption.find('.form-check').addClass('d-inline-block');
            $groupOption.get(0).insertAdjacentHTML("afterbegin", '<span class="caret-container dropdown-toggle"></span>');
        }

        if ($group.is(':disabled')) {
            $groupOption.addClass('disabled');
        }

        this.$popupContainer.append($groupOption);

        $("option", group).each($.proxy(function($, group) {
            this.createOptionValue(group, true);
        }, this));
    },

    /**
     * Build the reset.
     *
     */
    buildReset: function() {
        if (this.options.includeResetOption) {

            // Check whether to add a divider after the reset.
            if (this.options.includeResetDivider) {
                var divider = $(this.options.templates.divider);
                divider.addClass("mt-0");
                this.$popupContainer.prepend(divider);
            }

            var $resetButton = $(this.options.templates.resetButton);

            if (this.options.enableHTML) {
                $('button', $resetButton).html(this.options.resetText);
            }
            else {
                $('button', $resetButton).text(this.options.resetText);
            }

            $('button', $resetButton).click($.proxy(function() {
                this.clearSelection();
            }, this));

            this.$popupContainer.prepend($resetButton);
        }
    },

    /**
     * Build the select all.
     *
     * Checks if a select all has already been created.
     */
    buildSelectAll: function() {
        if (typeof this.options.selectAllValue === 'number') {
            this.options.selectAllValue = this.options.selectAllValue.toString();
        }

        var alreadyHasSelectAll = this.hasSelectAll();

        if (!alreadyHasSelectAll && this.options.includeSelectAllOption && this.options.multiple
            && $('option', this.$select).length > this.options.includeSelectAllIfMoreThan) {

            // Check whether to add a divider after the select all.
            if (this.options.includeSelectAllDivider) {
                this.$popupContainer.prepend($(this.options.templates.divider));
            }

            var $option = $(this.options.templates.option);
            var $checkbox = this.createCheckbox($option, this.options.selectAllText, this.options.selectAllName,
                this.options.selectAllValue, this.options.selectAllText, "checkbox", this.createAndApplyUniqueId(null));

            $option.addClass("multiselect-all");
            $option.removeClass("multiselect-option");
            $option.find(".form-check-label").addClass("font-weight-bold");

            this.$popupContainer.prepend($option);

            $checkbox.prop('checked', false);
        }
    },

    /**
     * Builds the filter.
     */
    buildFilter: function() {

        // Build filter if filtering OR case insensitive filtering is enabled and the number of options exceeds (or equals) enableFilterLength.
        if (this.options.enableFiltering || this.options.enableCaseInsensitiveFiltering) {
            var enableFilterLength = Math.max(this.options.enableFiltering, this.options.enableCaseInsensitiveFiltering);

            if (this.$select.find('option').length >= enableFilterLength) {

                this.$filter = $(this.options.templates.filter);
                $('input', this.$filter).attr('placeholder', this.options.filterPlaceholder);

                // Handles optional filter clear button
                if (!this.options.includeFilterClearBtn) {
                    this.$filter.find(".multiselect-search").attr("type", "text");

                    // Remove clear button if the old design of the filter with input groups and separated clear button is used
                    this.$filter.find(".multiselect-clear-filter").remove();
                }
                else {
                    // Firefox does not support a clear button in search inputs right now therefore it must be added manually
                    if (this.isFirefox() && this.$filter.find(".multiselect-clear-filter").length === 0) {
                        this.$filter.append("<i class='fas fa-times text-body-secondary multiselect-clear-filter multiselect-moz-clear-filter'></i>");
                    }

                    this.$filter.find(".multiselect-clear-filter").on('click', $.proxy(function(event) {
                        clearTimeout(this.searchTimeout);

                        this.query = '';
                        this.$filter.find('.multiselect-search').val('');
                        $('.dropdown-item', this.$popupContainer).show().removeClass('multiselect-filter-hidden');

                        this.updateSelectAll();

                        if (this.options.enableClickableOptGroups && this.options.multiple) {
                            this.updateOptGroups();
                        }

                    }, this));
                }

                this.$popupContainer.prepend(this.$filter);

                this.$filter.val(this.query).on('click', function(event) {
                    event.stopPropagation();
                }).on('input keydown', $.proxy(function(event) {
                    // Cancel enter key default behaviour
                    if (event.which === 13) {
                        event.preventDefault();
                    }

                    if (this.isFirefox() && this.options.includeFilterClearBtn) {
                        if (event.target.value) {
                            this.$filter.find(".multiselect-moz-clear-filter").show();
                        }
                        else {
                            this.$filter.find(".multiselect-moz-clear-filter").hide();
                        }
                    }

                    // This is useful to catch "keydown" events after the browser has updated the control.
                    clearTimeout(this.searchTimeout);

                    this.searchTimeout = this.asyncFunction($.proxy(function() {

                        if (this.query !== event.target.value) {
                            this.query = event.target.value;

                            var currentGroup, currentGroupVisible;
                            $.each($('.multiselect-option, .multiselect-group', this.$popupContainer), $.proxy(function(index, element) {
                                var value = $('input', element).length > 0 ? $('input', element).val() : "";
                                var text = $('.form-check-label', element).text();

                                var filterCandidate = '';
                                if ((this.options.filterBehavior === 'text')) {
                                    filterCandidate = text;
                                }
                                else if ((this.options.filterBehavior === 'value')) {
                                    filterCandidate = value;
                                }
                                else if (this.options.filterBehavior === 'both') {
                                    filterCandidate = text + '\n' + value;
                                }

                                if (value !== this.options.selectAllValue && text) {

                                    // By default lets assume that element is not
                                    // interesting for this search.
                                    var showElement = false;

                                    if (this.options.enableCaseInsensitiveFiltering) {
                                        filterCandidate = filterCandidate.toLowerCase();
                                        this.query = this.query.toLowerCase();
                                    }

                                    if (this.options.enableFullValueFiltering && this.options.filterBehavior !== 'both') {
                                        var valueToMatch = filterCandidate.trim().substring(0, this.query.length);
                                        if (this.query.indexOf(valueToMatch) > -1) {
                                            showElement = true;
                                        }
                                    }
                                    else if (filterCandidate.indexOf(this.query) > -1) {
                                        showElement = true;
                                    }

                                    // Toggle current element (group or group item) according to showElement boolean.
                                    if (!showElement) {
                                        $(element).css('display', 'none');
                                        $(element).addClass('multiselect-filter-hidden');
                                    }
                                    if (showElement) {
                                        $(element).css('display', 'block');
                                        $(element).removeClass('multiselect-filter-hidden');
                                    }

                                    // Differentiate groups and group items.
                                    if ($(element).hasClass('multiselect-group')) {
                                        // Remember group status.
                                        currentGroup = element;
                                        currentGroupVisible = showElement;
                                    }
                                    else {
                                        // Show group name when at least one of its items is visible.
                                        if (showElement) {
                                            $(currentGroup).show()
                                                .removeClass('multiselect-filter-hidden');
                                        }

                                        // Show all group items when group name satisfies filter.
                                        if (!showElement && currentGroupVisible) {
                                            $(element).show()
                                                .removeClass('multiselect-filter-hidden');
                                        }
                                    }
                                }
                            }, this));
                        }

                        this.updateSelectAll();

                        if (this.options.enableClickableOptGroups && this.options.multiple) {
                            this.updateOptGroups();
                        }

                        this.updatePopupPosition();

                        this.options.onFiltering(event.target);

                    }, this), 300, this);
                }, this));
            }
        }
    },

    /**
     * Builds the filter.
     */
    buildButtons: function() {
        if (this.options.enableResetButton) {
            var $buttonGroup = $(this.options.templates.buttonGroup);
            this.$buttonGroupReset = $(this.options.templates.buttonGroupReset).text(this.options.resetButtonText);
            $buttonGroup.append(this.$buttonGroupReset);
            this.$popupContainer.prepend($buttonGroup);

            // We save all options that were previously selected.
            this.defaultSelection = {};
            $('option', this.$select).each($.proxy(function(index, element) {
                var $option = $(element);
                this.defaultSelection[$option.val()] = $option.prop('selected');
            }, this));

            this.$buttonGroupReset.on('click', $.proxy(function(event) {
                $('option', this.$select).each($.proxy(function(index, element) {
                    var $option = $(element);
                    $option.prop('selected', this.defaultSelection[$option.val()]);
                }, this));
                this.refresh();

                if (this.options.enableFiltering) {
                    this.$filter.trigger('keydown');
                    $('input', this.$filter).val('');
                }
            }, this));
        }
    },

    updatePopupPosition: function() {
        // prevent gaps between popup and select when filter is used (#1199)
        var transformMatrix = this.$popupContainer.css("transform");
        var matrixType = transformMatrix.substring(0, transformMatrix.indexOf('('));
        var values = transformMatrix.substring(transformMatrix.indexOf('(') + 1, transformMatrix.length - 1);
        var valuesArray = values.split(',');

        var valueIndex = 5;
        if (matrixType === "matrix3d") {
            valueIndex = 13;
        }

        if (valuesArray.length < valueIndex) {
            return;
        }

        var yTransformation = valuesArray[valueIndex];
        // Need to check to avoid errors when testing and in some other situations.
        yTransformation = typeof yTransformation === 'undefined' ? 0 : yTransformation.trim();
        if (yTransformation < 0) {
            yTransformation = this.$popupContainer.css("height").replace('px', '') * -1;
            valuesArray[valueIndex] = yTransformation;
            transformMatrix = matrixType + '(' + valuesArray.join(',') + ')';
            this.$popupContainer.css("transform", transformMatrix);
        }
    },

    /**
     * Unbinds the whole plugin.
     */
    destroy: function() {
        this.$container.remove();
        this.$select.unwrap();
        this.$select.show();

        // reset original state
        this.$select.prop('disabled', this.options.wasDisabled);
        this.$select.find('option, optgroup').removeAttr('data-multiselectid');
        this.$select.data('multiselect', null);
    },

    /**
     * Refreshs the multiselect based on the selected options of the select.
     */
    refresh: function() {
        var inputs = {};
        $('.multiselect-option input', this.$popupContainer).each(function() {
            inputs[$(this).val()] = $(this);
        });

        $('option', this.$select).each($.proxy(function(index, element) {
            var $elem = $(element);
            var $input = inputs[$(element).val()];

            if ($elem.is(':selected')) {
                $input.prop('checked', true);

                if (this.options.selectedClass) {
                    $input.closest('.multiselect-option')
                        .addClass(this.options.selectedClass);
                }
            }
            else {
                $input.prop('checked', false);

                if (this.options.selectedClass) {
                    $input.closest('.multiselect-option')
                        .removeClass(this.options.selectedClass);
                }
            }

            if ($elem.is(":disabled")) {
                $input.attr('disabled', 'disabled')
                    .prop('disabled', true)
                    .closest('.multiselect-option')
                    .addClass('disabled');
            }
            else {
                $input.prop('disabled', false)
                    .closest('.multiselect-option')
                    .removeClass('disabled');
            }
        }, this));

        this.updateButtonText();
        this.updateSelectAll();

        if (this.options.enableClickableOptGroups && this.options.multiple) {
            this.updateOptGroups();
        }
    },

    /**
     * Select all options of the given values.
     *
     * If triggerOnChange is set to true, the on change event is triggered if
     * and only if one value is passed.
     *
     * @param {Array} selectValues
     * @param {Boolean} triggerOnChange
     */
    select: function(selectValues, triggerOnChange) {
        if (!$.isArray(selectValues)) {
            selectValues = [selectValues];
        }

        for (var i = 0; i < selectValues.length; i++) {
            var value = selectValues[i];

            if (value === null || value === undefined) {
                continue;
            }

            var $checkboxes = this.getInputsByValue(value);
            if (!$checkboxes || $checkboxes.length === 0) {
                continue;
            }

            for (var checkboxIndex = 0; checkboxIndex < $checkboxes.length; ++checkboxIndex) {
                var $checkbox = $checkboxes[checkboxIndex];

                var $option = this.getOptionById($checkbox.attr('id'));
                if ($option === undefined) {
                    continue;
                }

                if (this.options.selectedClass) {
                    $checkbox.closest('.dropdown-item')
                        .addClass(this.options.selectedClass);
                }

                $checkbox.prop('checked', true);
                $option.prop('selected', true);

                if (!this.options.multiple) {
                    var $checkboxesNotThis = $('input', this.$container).not($checkbox);
                    $($checkboxesNotThis).prop('checked', false);
                    $($checkboxesNotThis).closest('.multiselect-option').removeClass("active");

                    var $optionsNotThis = $('option', this.$select).not($option);
                    $optionsNotThis.prop('selected', false);
                }

                if (triggerOnChange) {
                    this.options.onChange($option, true);
                }
            }
        }

        this.updateButtonText();
        this.updateSelectAll();

        if (this.options.enableClickableOptGroups && this.options.multiple) {
            this.updateOptGroups();
        }
    },

    /**
     * Clears all selected items.
     */
    clearSelection: function() {
        this.deselectAll(false);
        this.updateButtonText();
        this.updateSelectAll();

        if (this.options.enableClickableOptGroups && this.options.multiple) {
            this.updateOptGroups();
        }
    },

    /**
     * Deselects all options of the given values.
     *
     * If triggerOnChange is set to true, the on change event is triggered, if
     * and only if one value is passed.
     *
     * @param {Array} deselectValues
     * @param {Boolean} triggerOnChange
     */
    deselect: function(deselectValues, triggerOnChange) {
        if (!this.options.multiple) {
            // In single selection mode at least on option needs to be selected
            return;
        }

        if (!$.isArray(deselectValues)) {
            deselectValues = [deselectValues];
        }

        for (var i = 0; i < deselectValues.length; i++) {
            var value = deselectValues[i];

            if (value === null || value === undefined) {
                continue;
            }

            var $checkboxes = this.getInputsByValue(value);
            if (!$checkboxes || $checkboxes.length === 0) {
                continue;
            }

            for (var checkboxIndex = 0; checkboxIndex < $checkboxes.length; ++checkboxIndex) {
                var $checkbox = $checkboxes[checkboxIndex];

                var $option = this.getOptionById($checkbox.attr('id'));
                if (!$option) {
                    continue;
                }

                if (this.options.selectedClass) {
                    $checkbox.closest('.dropdown-item')
                        .removeClass(this.options.selectedClass);
                }

                $checkbox.prop('checked', false);
                $option.prop('selected', false);

                if (triggerOnChange) {
                    this.options.onChange($option, false);
                }
            }
        }

        this.updateButtonText();
        this.updateSelectAll();

        if (this.options.enableClickableOptGroups && this.options.multiple) {
            this.updateOptGroups();
        }
    },

    /**
     * Selects all enabled & visible options.
     *
     * If justVisible is true or not specified, only visible options are selected.
     *
     * @param {Boolean} justVisible
     * @param {Boolean} triggerOnSelectAll
     */
    selectAll: function(justVisible, triggerOnSelectAll) {
        if (!this.options.multiple) {
            // In single selection mode only one option can be selected at a time
            return;
        }

        // Record all changes, i.e., options selected that were not selected before.
        var selected = [];
        var justVisible = typeof justVisible === 'undefined' ? true : justVisible;

        if (justVisible) {
            var visibleOptions = $(".multiselect-option:not(.disabled):not(.multiselect-filter-hidden)", this.$popupContainer);
            $('input:enabled', visibleOptions).prop('checked', true);
            visibleOptions.addClass(this.options.selectedClass);

            $('input:enabled', visibleOptions).each($.proxy(function(index, element) {
                var id = $(element).attr('id');
                var option = this.getOptionById(id);
                if (!$(option).prop('selected')) {
                    selected.push(option);
                }
                $(option).prop('selected', true);
            }, this));
        }
        else {
            var allOptions = $(".multiselect-option:not(.disabled)", this.$popupContainer);
            $('input:enabled', allOptions).prop('checked', true);
            allOptions.addClass(this.options.selectedClass);

            $('input:enabled', allOptions).each($.proxy(function(index, element) {
                var id = $(element).attr('id');
                var option = this.getOptionById(id);
                if (!$(option).prop('selected')) {
                    selected.push(option);
                }
                $(option).prop('selected', true);
            }, this));
        }

        $('.multiselect-option input[value="' + this.options.selectAllValue + '"]', this.$popupContainer).prop('checked', true);

        if (this.options.enableClickableOptGroups && this.options.multiple) {
            this.updateOptGroups();
        }

        this.updateButtonText();
        this.updateSelectAll();

        if (triggerOnSelectAll) {
            this.options.onSelectAll(selected);
        }
    },

    /**
     * Deselects all options.
     *
     * If justVisible is true or not specified, only visible options are deselected.
     *
     * @param {Boolean} justVisible
     */
    deselectAll: function(justVisible, triggerOnDeselectAll) {
        if (!this.options.multiple) {
            // In single selection mode at least on option needs to be selected
            return;
        }

        // Record changes, i.e., those options that are deselected but were not deselected before.
        var deselected = [];
        var justVisible = typeof justVisible === 'undefined' ? true : justVisible;

        if (justVisible) {
            var visibleOptions = $(".multiselect-option:not(.disabled):not(.multiselect-filter-hidden)", this.$popupContainer);
            $('input[type="checkbox"]:enabled', visibleOptions).prop('checked', false);
            visibleOptions.removeClass(this.options.selectedClass);

            $('input[type="checkbox"]:enabled', visibleOptions).each($.proxy(function(index, element) {
                var id = $(element).attr('id');
                var option = this.getOptionById(id);
                if ($(option).prop('selected')) {
                    deselected.push(option);
                }
                $(option).prop('selected', false);
            }, this));
        }
        else {
            var allOptions = $(".multiselect-option:not(.disabled):not(.multiselect-group)", this.$popupContainer);
            $('input[type="checkbox"]:enabled', allOptions).prop('checked', false);
            allOptions.removeClass(this.options.selectedClass);

            $('input[type="checkbox"]:enabled', allOptions).each($.proxy(function(index, element) {
                var id = $(element).attr('id');
                var option = this.getOptionById(id);
                if ($(option).prop('selected')) {
                    deselected.push(option);
                }
                $(option).prop('selected', false);
            }, this));
        }

        $('.multiselect-all input[value="' + this.options.selectAllValue + '"]', this.$popupContainer).prop('checked', false);

        if (this.options.enableClickableOptGroups && this.options.multiple) {
            this.updateOptGroups();
        }

        this.updateButtonText();
        this.updateSelectAll();

        if (triggerOnDeselectAll) {
            this.options.onDeselectAll(deselected);
        }
    },

    /**
     * Rebuild the plugin.
     *
     * Rebuilds the dropdown, the filter and the select all option.
     */
    rebuild: function() {
        this.internalIdCount = 0;

        this.$popupContainer.html('');
        this.$select.find('option, optgroup').removeAttr('data-multiselectid');

        // Important to distinguish between radios and checkboxes.
        this.options.multiple = this.$select.attr('multiple') === "multiple";

        this.buildSelectAll();
        this.buildDropdownOptions();
        this.buildFilter();
        this.buildButtons();

        this.updateButtonText();
        this.updateSelectAll(true);

        if (this.options.enableClickableOptGroups && this.options.multiple) {
            this.updateOptGroups();
        }

        if (this.options.disableIfEmpty) {
            if ($('option', this.$select).length <= 0) {
                if (!this.$select.prop('disabled')) {
                    this.disable(true);
                }
            }
            else if (this.$select.data("disabled-by-option")) {
                this.enable();
            }
        }

        if (this.options.dropRight) {
            this.$container.addClass('dropend');
        }
        else if (this.options.dropUp) {
            this.$container.addClass('dropup');
        }

        if (this.options.widthSynchronizationMode !== 'never') {
            this.synchronizeButtonAndPopupWidth();
        }
    },

    /**
     * The provided data will be used to build the dropdown.
     */
    dataprovider: function(dataprovider) {

        var groupCounter = 0;
        var $select = this.$select.empty();

        $.each(dataprovider, function(index, option) {
            var $tag;

            if ($.isArray(option.children)) { // create optiongroup tag
                groupCounter++;

                $tag = $('<optgroup/>').attr({
                    label: option.label || 'Group ' + groupCounter,
                    disabled: !!option.disabled,
                    value: option.value
                });

                forEach(option.children, function(subOption) { // add children option tags
                    var attributes = {
                        value: subOption.value,
                        label: subOption.label !== undefined && subOption.label !== null ? subOption.label : subOption.value,
                        title: subOption.title,
                        class: subOption.class,
                        selected: !!subOption.selected,
                        disabled: !!subOption.disabled
                    };

                    //Loop through attributes object and add key-value for each attribute
                    for (var key in subOption.attributes) {
                        attributes['data-' + key] = subOption.attributes[key];
                    }
                    //Append original attributes + new data attributes to option
                    $tag.append($('<option/>').attr(attributes));
                });
            }
            else {
                var attributes = {
                    'value': option.value,
                    'label': option.label !== undefined && option.label !== null ? option.label : option.value,
                    'title': option.title,
                    'class': option['class'],
                    'selected': !!option['selected'],
                    'disabled': !!option['disabled']
                };
                //Loop through attributes object and add key-value for each attribute
                for (var key in option.attributes) {
                    attributes['data-' + key] = option.attributes[key];
                }
                //Append original attributes + new data attributes to option
                $tag = $('<option/>').attr(attributes);

                $tag.text(option.label !== undefined && option.label !== null ? option.label : option.value);
            }

            $select.append($tag);
        });

        this.rebuild();
    },

    /**
     * Enable the multiselect.
     */
    enable: function() {
        this.$select.prop('disabled', false);
        this.$button.prop('disabled', false)
            .removeClass('disabled');

        this.updateButtonText();
    },

    /**
     * Disable the multiselect.
     */
    disable: function(disableByOption) {
        this.$select.prop('disabled', true);
        this.$button.prop('disabled', true)
            .addClass('disabled');

        if (disableByOption) {
            this.$select.data("disabled-by-option", true);
        }
        else {
            this.$select.data("disabled-by-option", null);
        }

        this.updateButtonText();
    },

    /**
     * Set the options.
     *
     * @param {Array} options
     */
    setOptions: function(options) {
        this.options = this.mergeOptions(options);
    },

    /**
     * Merges the given options with the default options.
     *
     * @param {Array} options
     * @returns {Array}
     */
    mergeOptions: function(options) {
        return $.extend(true, {}, this.defaults, this.options, options);
    },

    /**
     * Checks whether a select all checkbox is present.
     *
     * @returns {Boolean}
     */
    hasSelectAll: function() {
        return $('.multiselect-all', this.$popupContainer).length > 0;
    },

    /**
     * Update opt groups.
     */
    updateOptGroups: function() {
        var $groups = $('.multiselect-group', this.$popupContainer);
        var selectedClass = this.options.selectedClass;

        $groups.each(function() {
            var $options = $(this).nextUntil('.multiselect-group')
                .not('.multiselect-filter-hidden')
                .not('.disabled');

            var checked = true;
            $options.each(function() {
                var $input = $('input', this);

                if (!$input.prop('checked')) {
                    checked = false;
                }
            });

            if (selectedClass) {
                if (checked) {
                    $(this).addClass(selectedClass);
                }
                else {
                    $(this).removeClass(selectedClass);
                }
            }

            $('input', this).prop('checked', checked);
        });
    },

    /**
     * Updates the select all checkbox based on the currently displayed and selected checkboxes.
     */
    updateSelectAll: function(notTriggerOnSelectAll) {
        if (this.hasSelectAll()) {
            var allBoxes = $(".multiselect-option:not(.multiselect-filter-hidden):not(.multiselect-group):not(.disabled) input:enabled", this.$popupContainer);
            var allBoxesLength = allBoxes.length;
            var checkedBoxesLength = allBoxes.filter(":checked").length;
            var selectAllItem = $(".multiselect-all", this.$popupContainer);
            var selectAllInput = selectAllItem.find("input");

            if (checkedBoxesLength > 0 && checkedBoxesLength === allBoxesLength) {
                selectAllInput.prop("checked", true);
                selectAllItem.addClass(this.options.selectedClass);
            }
            else {
                selectAllInput.prop("checked", false);
                selectAllItem.removeClass(this.options.selectedClass);
            }
        }
    },

    /**
     * Update the button text and its title based on the currently selected options.
     */
    updateButtonText: function() {
        var options = this.getSelected();

        // First update the displayed button text.
        if (this.options.enableHTML) {
            $('.multiselect .multiselect-selected-text', this.$container).html(this.options.buttonText(options, this.$select));
        }
        else {
            $('.multiselect .multiselect-selected-text', this.$container).text(this.options.buttonText(options, this.$select));
        }

        // Now update the title attribute of the button.
        $('.multiselect', this.$container).attr('title', this.options.buttonTitle(options, this.$select));
        this.$button.trigger('change');
    },

    /**
     * Get all selected options.
     *
     * @returns {jQUery}
     */
    getSelected: function() {
        return $('option', this.$select).filter(":selected");
    },

    /**
     * Gets a select option by its id
     * @param {String} id
     * @returns {JQuery}
     */
    getOptionById: function(id) {
        if (!id) {
            return null;
        }

        return this.$select.find('option[data-multiselectid=' + id + '], optgroup[data-multiselectid=' + id + ']');
    },

    /**
     * Get the input (radio/checkbox) by its value.
     *
     * @param {String} value
     * @returns {jQuery}
     */
    getInputsByValue: function(value) {
        var checkboxes = $('.multiselect-option input:not(.multiselect-search)', this.$popupContainer);
        var valueToCompare = value.toString();

        var matchingCheckboxes = [];
        for (var i = 0; i < checkboxes.length; i = i + 1) {
            var checkbox = checkboxes[i];
            if (checkbox.value === valueToCompare) {
                matchingCheckboxes.push($(checkbox));
            }
        }

        return matchingCheckboxes;
    },

    /**
     * Used for knockout integration.
     */
    updateOriginalOptions: function() {
        this.originalOptions = this.$select.clone()[0].options;
    },

    asyncFunction: function(callback, timeout, self) {
        var args = Array.prototype.slice.call(arguments, 3);
        return setTimeout(function() {
            callback.apply(self || window, args);
        }, timeout);
    },

    setAllSelectedText: function(allSelectedText) {
        this.options.allSelectedText = allSelectedText;
        this.updateButtonText();
    },

    isFirefox: function() {
        var firefoxIdentifier = 'firefox';
        var valueNotFoundIndex = -1;

        if (navigator && navigator.userAgent) {
            return navigator.userAgent.toLocaleLowerCase().indexOf(firefoxIdentifier) > valueNotFoundIndex;
        }

        return false;
    },

    /**
     * Generate a unique identifier inside the multiselect namespace and adds it as an data attribute to the related element
     * @param {JQuery} $relatedElement
     * @returns unique id
     */
    createAndApplyUniqueId: function($relatedElement) {
        var id = 'multiselect_' + this.multiselectId + '_' + this.internalIdCount++;
        if (!!$relatedElement) {
            $relatedElement[0].dataset.multiselectid = id;
        }
        return id;
    },

    /**
     * Generate a unique identifier
     * @returns unique id
     */
    generateUniqueId: function() {
        return Math.random().toString(36).substr(2);
    }
};

export default function($) {
    $.fn.multiselect = function(option, parameter, extraOptions) {
        return this.each(function() {
            let data = $(this).data('multiselect');
            const options = typeof option === 'object' && option;

            // Initialize the multiselect.
            if (!data) {
                data = new Multiselect(this, options);
            }

            // Call multiselect method.
            if (typeof option === 'string') {
                data[option](parameter, extraOptions);
            }
        });
    };

    $(() => {
        $("select[data-role=multiselect]").multiselect();
    });
}
