/* This file contains the signalr services Jackett provides */

interface SignalR {
    sync: HubProxy;
}

interface HubProxy {
    client: ISyncClient;
}

interface ISyncClient {
    onChange();
}