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
import { IRCProfileService } from '../Services/IRCProfileService';
import { autoinject } from 'aurelia-framework';
export let IRCSettings = class {
    constructor(httpClient) {
        this.ircService = httpClient;
    }
    activate() {
        return Promise.all([
            this.ircService.getAutoDLProfiles()
                .then(profiles => {
                this.autodlprofiles = profiles;
            }),
            this.ircService.getProfiles().then(profiles => {
                this.profiles = profiles;
            })
        ]);
    }
    remove(item) {
        return __awaiter(this, void 0, Promise, function* () {
            yield this.ircService.removeProfile(item);
            this.profiles = yield this.ircService.getProfiles();
        });
    }
};
IRCSettings = __decorate([
    autoinject, 
    __metadata('design:paramtypes', [IRCProfileService])
], IRCSettings);
//# sourceMappingURL=irc-settings.js.map