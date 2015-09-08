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
import { WebClient } from '../Services/WebClient';
import { autoinject } from 'aurelia-framework';
export let IRCProfileService = class {
    constructor(httpClient) {
        this.webClient = httpClient;
    }
    getAutoDLProfiles() {
        return this.webClient.get('AutoDL/Summary')
            .then(response => { return JSON.parse(response.response); });
    }
    getProfiles() {
        return this.webClient.get('IRCProfile')
            .then(response => { return JSON.parse(response.response); });
    }
    getProfile(name) {
        return this.webClient.get('IRCProfile/' + name)
            .then(response => { return JSON.parse(response.response); });
    }
    setProfile(profile) {
        return this.webClient.put('IRCProfile', profile);
    }
    removeProfile(profile) {
        return this.webClient.delete('IRCProfile/' + profile.Id);
    }
};
IRCProfileService = __decorate([
    autoinject, 
    __metadata('design:paramtypes', [WebClient])
], IRCProfileService);
export class NetworkSummary {
}
export class IRCProfile {
}
//# sourceMappingURL=IRCProfileService.js.map