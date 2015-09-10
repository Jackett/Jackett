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
import { autoinject } from 'aurelia-framework';
import { EventAggregator } from 'aurelia-event-aggregator';
import 'jquery';
import 'ms-signalr-client';
import 'jackett-hubs';
export let SignalRService = class {
    constructor(ea) {
        this.aggregator = ea;
    }
    start() {
        let hub = $.connection.hub.proxies.jacketthubproxy;
        hub.client.onEvent = (e, d) => {
            this.onEvent(e, d);
        };
        $.connection.hub.start().done(() => {
            console.log('Jackett SignalR connected');
        });
        $.connection.hub.disconnected(() => {
            console.log('Jackett SignalR disconnected');
        });
    }
    onEvent(type, data) {
        debugger;
        this.aggregator.publish(type, data);
    }
};
SignalRService = __decorate([
    autoinject, 
    __metadata('design:paramtypes', [EventAggregator])
], SignalRService);
//# sourceMappingURL=SignalRService.js.map