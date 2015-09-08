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
import { HttpClient } from 'aurelia-http-client';
import { autoinject } from 'aurelia-framework';
export let WebClient = class {
    constructor(httpClient) {
        this.http = httpClient;
        this.http.configure(config => {
            config.withHeader('Content-Type', 'application/json');
            config.withHeader('Accept', 'application/json');
        });
    }
    get(url) {
        return this.http.get('../webapi/' + url + '?_t=' + new Date().getTime());
    }
    put(url, obj) {
        return this.http.put('../webapi/' + url + '?_t=' + new Date().getTime(), JSON.stringify(obj));
    }
    delete(url) {
        return this.http.delete('../webapi/' + url + '?_t=' + new Date().getTime());
    }
};
WebClient = __decorate([
    autoinject, 
    __metadata('design:paramtypes', [HttpClient])
], WebClient);
//# sourceMappingURL=WebClient.js.map