import {autoinject} from 'aurelia-framework';
import {IRCService, NetworkState, ChannelState, IRCMessage, IRCUser} from '../Services/IRCService';
import {HttpClient} from 'aurelia-fetch-client'
import {activationStrategy} from 'aurelia-router'
import {EventAggregator} from 'aurelia-event-aggregator';
import 'jquery' 

@autoinject 
export class Irc {
    ircService: IRCService;
    eventAggregator: EventAggregator;

    networkStates: NetworkState[];
    messages: IRCMessage[];
    users: IRCUser[];

    selectedId: string;
    selectedChannelId: string;
    selectedNetworkId: string;

    commandInput: any;
    ircMessageSubscription: any;
    ircUsersSubscription: any;
    ircStateSubscription: any;

    constructor(ircs: IRCService, ea: EventAggregator) {
        this.ircService = ircs;
        this.eventAggregator = ea;
    }

    // Force aurelia to redraw the page
    determineActivationStrategy() {
        return activationStrategy.replace;
    }

    attached() {
        var irc = this;
        this.ircMessageSubscription = this.eventAggregator.subscribe('IRC-Message', msg => {
            if (msg.Id === irc.selectedId) {
                this.ircService.getMessages(irc.selectedNetworkId, irc.selectedChannelId).then(m=> {
                    this.messages = m;
                });
            }
        });
        this.ircUsersSubscription = this.eventAggregator.subscribe('IRC-Users', msg => {
            if (msg.Id === irc.selectedId) {
                this.ircService.getUsers(irc.selectedNetworkId, irc.selectedChannelId).then(users=> {
                    this.users = users;
                });
            }
        });

        this.ircStateSubscription = this.eventAggregator.subscribe('IRC-State', msg => {
            irc.getState();
        });
    }

    detached() {
        this.ircMessageSubscription();
        this.ircUsersSubscription();
        this.ircStateSubscription();
    }

    getState() {
        return this.ircService.getState().then(state=> {
            this.networkStates = state;
            if (this.networkStates.length > 0) {
                this.onNetworkClick(this.networkStates[0]);
            }
        });
    }

    activate() {
        this.getState();
        $('body').addClass('jackett-body-fill');
    }

    deactivate() {
        $('body').removeClass('jackett-body-fill');
    }

    onNetworkClick(network: NetworkState) {
        this.selectById(network.Id);
        this.selectedChannelId = null;
        this.selectedNetworkId = network.Id;
        this.users = [];
        this.ircService.getMessages(network.Id, null).then(m=> {
                this.messages = m;
        });
        this.ircService.getUsers(network.Id, 'server').then(users=> {
            this.users = users;
        });
        return false;
    }

    onChannelClick(channel: ChannelState, network: NetworkState) {
        this.selectById(channel.Id);
        this.selectedChannelId = channel.Id;
        this.selectedNetworkId = network.Id;
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