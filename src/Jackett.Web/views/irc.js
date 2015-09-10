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
import { IRCService } from '../Services/IRCService';
import { activationStrategy } from 'aurelia-router';
import { EventAggregator } from 'aurelia-event-aggregator';
import 'jquery';
export let Irc = class {
    constructor(ircs, ea) {
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
                this.ircService.getMessages(irc.selectedNetworkId, irc.selectedChannelId).then(m => {
                    this.messages = m;
                });
            }
        });
        this.ircUsersSubscription = this.eventAggregator.subscribe('IRC-Users', msg => {
            if (msg.Id === irc.selectedId) {
                this.ircService.getUsers(irc.selectedNetworkId, irc.selectedChannelId).then(users => {
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
        return this.ircService.getState().then(state => {
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
    onNetworkClick(network) {
        this.selectById(network.Id);
        this.selectedChannelId = null;
        this.selectedNetworkId = network.Id;
        this.users = [];
        this.ircService.getMessages(network.Id, null).then(m => {
            this.messages = m;
        });
        return false;
    }
    onChannelClick(channel, network) {
        this.selectById(channel.Id);
        this.selectedChannelId = channel.Id;
        this.selectedNetworkId = network.Id;
        this.ircService.getMessages(network.Id, channel.Id).then(m => {
            this.messages = m;
        });
        this.ircService.getUsers(network.Id, channel.Id).then(users => {
            this.users = users;
        });
        return false;
    }
    performCommand() {
        var networkId = null;
        var channelId = null;
        this.networkStates.forEach(n => {
            if (n.Id == this.selectedId) {
                networkId = n.Id;
            }
            n.Channels.forEach(c => {
                if (c.Id == this.selectedId) {
                    networkId = n.Id;
                    channelId = c.Id;
                }
            });
        });
        this.ircService.processCommand(networkId, channelId, this.commandInput.value);
        this.commandInput.value = '';
    }
    selectById(id) {
        this.selectedId = id;
        this.networkStates.forEach(n => {
            n.IsSelected = n.Id == this.selectedId;
            n.Channels.forEach(c => {
                c.IsSelected = c.Id == this.selectedId;
            });
        });
    }
};
Irc = __decorate([
    autoinject, 
    __metadata('design:paramtypes', [IRCService, EventAggregator])
], Irc);
//# sourceMappingURL=irc.js.map