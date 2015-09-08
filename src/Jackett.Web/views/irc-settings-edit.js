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
import { autoinject } from 'aurelia-framework';
import { Validation } from 'aurelia-validation';
import { IRCProfileService, IRCProfile } from '../Services/IRCProfileService';
import 'jquery';
import 'semantic-ui';
import { Router } from 'aurelia-router';
export let IRCSettings = class {
    constructor(httpClient, validation, r) {
        this.ircService = httpClient;
        this.router = r;
        this.validation = validation.on(this, undefined)
            .ensure('autodlnetwork')
            .isNotEmpty()
            .ensure('name')
            .isNotEmpty()
            .ensure('nickname')
            .isNotEmpty();
    }
    activate(params) {
        var actions = [
            this.ircService.getAutoDLProfiles()
                .then(profiles => {
                this.networks = profiles;
            })
        ];
        if (params.name) {
            actions.push(this.ircService.getProfile(params.name).then(profile => {
                this.autodlnetwork = profile.Profile;
                this.name = profile.Name;
                this.nickname = profile.Username;
                this.id = profile.Id;
            }));
        }
        return Promise.all(actions);
    }
    attached() {
        $(this.profileSelect).val(this.autodlnetwork).dropdown().on('change', e => {
            this.autodlnetwork = this.name = e.target.value;
        });
    }
    submit() {
        return __awaiter(this, void 0, Promise, function* () {
            yield this.validation.validate();
            try {
                var profile = new IRCProfile();
                profile.Name = this.name;
                profile.Username = this.nickname;
                profile.Profile = this.autodlnetwork;
                profile.Id = this.id;
                yield this.ircService.setProfile(profile);
                this.router.navigate('irc-settings');
            }
            catch (e) {
                this.error = 'There was an error submiting your changes';
            }
        });
    }
};
IRCSettings = __decorate([
    autoinject, 
    __metadata('design:paramtypes', [IRCProfileService, Validation, Router])
], IRCSettings);
//# sourceMappingURL=irc-settings-edit.js.map