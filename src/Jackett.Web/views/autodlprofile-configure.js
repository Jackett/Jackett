var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") return Reflect.decorate(decorators, target, key, desc);
    switch (arguments.length) {
        case 2: return decorators.reduceRight(function(o, d) { return (d && d(o)) || o; }, target);
        case 3: return decorators.reduceRight(function(o, d) { return (d && d(target, key)), void 0; }, void 0);
        case 4: return decorators.reduceRight(function(o, d) { return (d && d(target, key, o)) || o; }, desc);
    }
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, Promise, generator) {
    return new Promise(function (resolve, reject) {
        generator = generator.call(thisArg, _arguments);
        function cast(value) { return value instanceof Promise && value.constructor === Promise ? value : new Promise(function (resolve) { resolve(value); }); }
        function onfulfill(value) { try { step("next", value); } catch (e) { reject(e); } }
        function onreject(value) { try { step("throw", value); } catch (e) { reject(e); } }
        function step(verb, value) {
            var result = generator[verb](value);
            result.done ? resolve(result.value) : cast(result.value).then(onfulfill, onreject);
        }
        step("next", void 0);
    });
};
import 'jquery';
import 'ms-signalr-client';
import 'jackett-hubs';
import { AutoDLService } from '../Services/AutoDLService';
import { autoinject } from 'aurelia-framework';
import { Router } from 'aurelia-router';
export let AutoDLProfileConfigure = class {
    constructor(a, r) {
        this.autodlService = a;
        this.router = r;
    }
    activate(params) {
        return this.autodlService.getProfiles()
            .then(profiles => {
            this.profile = profiles.filter(p => p.Type == params.type)[0];
            this.profile.Options.forEach(opt => {
                if (opt.Value === null) {
                    opt.Value = opt.DefaultValue;
                }
                if (opt.Type === 'Bool' && opt.Value !== null) {
                    opt.Value = opt.Value.toLowerCase() === 'true';
                }
            });
        });
    }
    attached() {
        var vm = this;
        $(this.form).find('input').on('paste', function (e) {
            // We get this event before the data is applied, so wait a bit :/
            setTimeout(() => {
                var fieldName = $(e.target).attr('data-fieldname');
                var value = $(e.target).val();
                // Find by fieldname
                vm.profile.Options.forEach(opt => {
                    if (opt.Name === fieldName) {
                        // If we have a paste group
                        if (opt.PasteGroup !== null) {
                            opt.PasteGroup.split(',').forEach(i => {
                                i = $.trim(i);
                                // Loop over each item in the paste group and apply
                                vm.profile.Options.forEach(o => {
                                    if (o.Name === i && o.PasteRegex !== null) {
                                        var regex = new RegExp(o.PasteRegex, "i");
                                        var match = regex.exec(value);
                                        debugger;
                                        if (match !== null && match.length > 0) {
                                            o.Value = match[1];
                                        }
                                    }
                                });
                            });
                        }
                    }
                });
            }, 50);
        });
    }
    submit() {
        return __awaiter(this, void 0, Promise, function* () {
            //await this.validation.validate();
            //  try {
            yield this.autodlService.saveProfile(this.profile);
            this.router.navigate('indexers');
            // } catch (e) {
            //this.error = 'There was an error submiting your changes';
            //  }
        });
    }
};
AutoDLProfileConfigure = __decorate([
    autoinject, 
    __metadata('design:paramtypes', [AutoDLService, Router])
], AutoDLProfileConfigure);
//# sourceMappingURL=autodlprofile-configure.js.map