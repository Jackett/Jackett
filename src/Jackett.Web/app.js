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
import { SignalRService } from './Services/SignalRService';
import { autoinject } from 'aurelia-framework';
export let App = class {
    constructor(sr) {
        sr.start();
    }
    configureRouter(config, router) {
        config.title = 'Jackett';
        // Notice: the overall template is hardcoded to routes based on index.
        config.map([
            { route: ['', 'welcome'], name: 'welcome', moduleId: './views/home', title: 'Home' },
            { route: ['indexers'], name: 'indexers', moduleId: './views/indexers', title: 'Indexers' },
            { route: ['irc'], name: 'irc', moduleId: './views/irc', title: 'IRC' },
            { route: ['settings'], name: 'settings', moduleId: './views/settings', title: 'Settings' },
            { route: ['settings'], name: 'settings', moduleId: './views/settings', title: 'Server settings' },
            { route: 'irc-settings', name: 'irc-settings', moduleId: './views/irc-settings', title: 'IRC Settings' },
            { route: 'irc-profile-edit/:name', name: 'irc-profile-edit', moduleId: './views/irc-settings-edit', title: 'Edit Profile' },
            { route: 'irc-profile-create', name: 'irc-profile-create', moduleId: './views/irc-settings-edit', title: 'Create Profile' },
            { route: 'autodlprofile-configure/:type', name: 'autodlprofile-configure', moduleId: './views/autodlprofile-configure', title: 'AutoDL Profile Config' }
        ]);
        this.router = router;
    }
};
App = __decorate([
    autoinject, 
    __metadata('design:paramtypes', [SignalRService])
], App);
//# sourceMappingURL=app.js.map