import 'jquery';
import 'ms-signalr-client';
import 'jackett-hubs';

export class App {
    constructor() {
        let hub = $.connection.hub.proxies.jacketthub;
        hub.client.transferState = function (item: any) {
            debugger;
        };

        $.connection.hub.start().done(() => {
            debugger;
        });
    }
}