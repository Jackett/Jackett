import {WebClient} from '../Services/WebClient';
import {autoinject} from 'aurelia-framework';

@autoinject
export class IRCService {
    webClient: WebClient;

    constructor(httpClient: WebClient) {
        this.webClient = httpClient;
    }

    getState(): Promise<NetworkState[]> {
        return this.webClient.get('ircstate')
            .then(response => { return JSON.parse(response.response); });
    }

    getMessages(networkId: string, roomId: string): Promise<IRCMessage[]> {
        var url = 'ircmessages/' + networkId;
        if (roomId !== null) {
            url += '/' + roomId;
        }

        return this.webClient.get(url)
            .then(response => {
                var messages = JSON.parse(response.response);
                messages.forEach(m=> {
                   // m.Text = m.Text.replace('\n', '<br />');
                });
                return messages;
            });
    }

    getUsers(networkId: string, roomId: string): Promise<IRCUser[]> {
        return this.webClient.get('ircusers/' + networkId + '/' + roomId)
            .then(response => { return JSON.parse(response.response); });
    }

    processCommand(networkId: string, roomId: string, command: string) {
        return this.webClient.post('irccommand', { Text: command, NetworkId: networkId, ChannelId: roomId });
    }
}

export class NetworkState {
    Id: string;
    Name: string;
    Channels: ChannelState[];
    IsSelected: boolean;
}

export class ChannelState {
    Id: string;
    Name: string;
    IsSelected: boolean;
}

export class IRCMessage {
    Text: string;
    When: Date;
    From: string;
    Type: number;
}

export class IRCUser {
    Nickname: string;
}