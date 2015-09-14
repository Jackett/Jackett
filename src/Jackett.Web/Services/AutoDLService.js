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
export let AutoDLService = class {
    constructor(httpClient) {
        this.webClient = httpClient;
    }
    getProfiles() {
        return this.webClient.get('AutoDL')
            .then(response => { return JSON.parse(response.response); });
    }
    saveProfile(profile) {
        return this.webClient.put('AutoDL', profile);
    }
};
AutoDLService = __decorate([
    autoinject, 
    __metadata('design:paramtypes', [WebClient])
], AutoDLService);
export class AutoDLConfigOption {
}
export class AutoDLProfile {
}
//# sourceMappingURL=AutoDLService.js.map