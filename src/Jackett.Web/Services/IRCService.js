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
export let IRCService = class {
    constructor(httpClient) {
        this.webClient = httpClient;
    }
    getState() {
        return this.webClient.get('ircstate')
            .then(response => { return JSON.parse(response.response); });
    }
    getMessages(networkId, roomId) {
        var url = 'ircmessages/' + networkId;
        if (roomId !== null) {
            url += '/' + roomId;
        }
        return this.webClient.get(url)
            .then(response => {
            var messages = JSON.parse(response.response);
            messages.forEach(m => {
                // m.Text = m.Text.replace('\n', '<br />');
            });
            return messages;
        });
    }
    getUsers(networkId, roomId) {
        return this.webClient.get('ircusers/' + networkId + '/' + roomId)
            .then(response => { return JSON.parse(response.response); });
    }
    processCommand(networkId, roomId, command) {
        return this.webClient.post('irccommand', { Text: command, NetworkId: networkId, ChannelId: roomId });
    }
};
IRCService = __decorate([
    autoinject, 
    __metadata('design:paramtypes', [WebClient])
], IRCService);
export class NetworkState {
}
export class ChannelState {
}
export class IRCMessage {
}
export class IRCUser {
}
//# sourceMappingURL=IRCService.js.map