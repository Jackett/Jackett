import {autoinject} from 'aurelia-framework';
import {EventAggregator} from 'aurelia-event-aggregator';
import 'jquery';
import 'ms-signalr-client';
import 'jackett-hubs';
import {IRCMessageCommand} from '../scripts/typings/JackettSignalr';

@autoinject
export class SignalRService {
    aggregator: EventAggregator;

    constructor(ea: EventAggregator) {
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

    onEvent(type: string, data: any) {
        debugger;
        this.aggregator.publish(type, data);
    }
}