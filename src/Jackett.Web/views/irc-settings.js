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
import { HttpClient } from 'aurelia-fetch-client';
import { autoinject } from 'aurelia-framework';
export let IRCSettings = class {
    constructor(httpClient) {
        this.profiles = [];
        this.autodlprofiles = [];
        this.http = httpClient;
        this.http.configure(config => {
            config.useStandardConfiguration();
        });
    }
    activate() {
        return this.http.fetch('../webapi/IRCProfile/AutoDLProfiles')
            .then(response => response.json())
            .then(profiles => { debugger; this.autodlprofiles = profiles; });
        /* return this.http.fetch('../webapi/IRCProfile')
             .then(response => response.json())
             .then(profiles => { this.profiles = profiles });*/
    }
};
IRCSettings = __decorate([
    autoinject, 
    __metadata('design:paramtypes', [HttpClient])
], IRCSettings);
//# sourceMappingURL=irc-settings.js.map