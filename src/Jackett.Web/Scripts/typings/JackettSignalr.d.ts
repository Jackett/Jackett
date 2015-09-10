/* This file contains the signalr services Jackett provides */
    export interface SignalR {
        sync: HubProxy;
    }

    export interface HubProxy {
        client: ISyncClient;
    }

    export interface ISyncClient {
        onChange();
        onIRCMessage(m: IRCMessageCommand);
    }

    export class IRCMessageCommand {
        Id: string;
    }
