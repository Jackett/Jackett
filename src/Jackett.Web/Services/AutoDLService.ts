import {WebClient} from '../Services/WebClient';
import {autoinject} from 'aurelia-framework';

@autoinject
export class AutoDLService {
    webClient: WebClient;

    constructor(httpClient: WebClient) {
        this.webClient = httpClient;
    }

    getProfiles(): Promise<AutoDLProfile[]> {
        return this.webClient.get('AutoDL')
            .then(response => { return JSON.parse(response.response); });
    }

    saveProfile(profile: AutoDLProfile): Promise<Response> {
        return this.webClient.put('AutoDL', profile);
    }
}

export class AutoDLConfigOption {
    Name: string;
    Label: string;
    Type: string;
    Value: any;
    DefaultValue: string;
    EmptyText: string;
    Tooltip: string;
    PasteGroup: string;
    PasteRegex: string;
    MinValue: string;
    MaxValue: string;
    IsDownloadVar: boolean;
}

export class AutoDLProfile {
    Type: string;
    ShortName: string;
    LongName: string;
    SiteName: string;
    Options: AutoDLConfigOption[];
}
