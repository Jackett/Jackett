import {autoinject} from 'aurelia-framework';
import {IRCService, NetworkState, ChannelState, IRCMessage, IRCUser} from '../Services/IRCService';
import {HttpClient} from 'aurelia-fetch-client'
import 'jquery' 

@autoinject 
export class Irc {
    ircService: IRCService;
    networkStates: NetworkState[];
    messages: IRCMessage[];
    users: IRCUser[];

    selectedId: string;
    commandInput: any;

    constructor(ircs: IRCService) {
        this.ircService = ircs;
    }

    activate() {
        return this.ircService.getState().then(state=> {
            this.networkStates = state;
            if (this.networkStates.length > 0) {
                this.onNetworkClick(this.networkStates[0]);
            }
        });
        $('body').addClass('jackett-body-fill');
    }

    deactivate() {
        $('body').removeClass('jackett-body-fill');
    }

    onNetworkClick(network: NetworkState) {
        this.selectById(network.Id);
        this.users = [];
        this.ircService.getMessages(network.Id, null).then(m=> {
                this.messages = m;
        });
        return false;
    }

    onChannelClick(channel: ChannelState, network: NetworkState) {
        this.selectById(channel.Id);
        this.ircService.getMessages(network.Id, channel.Id).then(m=> {
                this.messages = m;
        });
        this.ircService.getUsers(network.Id, channel.Id).then(users=> {
            this.users = users;
        });
        return false;
    }

    performCommand() {
        var networkId = null;
        var channelId = null;
        this.networkStates.forEach(n=> {
            if (n.Id == this.selectedId) {
                networkId = n.Id;
            }
            n.Channels.forEach(c=> {
                if (c.Id == this.selectedId) {
                    networkId = n.Id;
                    channelId = c.Id;
                }
            });
        });

        this.ircService.processCommand(networkId, channelId, this.commandInput.value);
        this.commandInput.value = '';
    }

    private selectById(id: string) {
        this.selectedId = id; 
        this.networkStates.forEach(n=> {
            n.IsSelected = n.Id == this.selectedId;
            n.Channels.forEach(c=> {
                c.IsSelected = c.Id == this.selectedId;
            });
        });
    }
}