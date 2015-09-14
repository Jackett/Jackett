import 'jquery';
import 'ms-signalr-client';
import 'jackett-hubs';
import {AutoDLService, AutoDLProfile} from '../Services/AutoDLService'
import {autoinject} from 'aurelia-framework';

@autoinject 
export class Indexers {
    autodlService: AutoDLService;
    autodlProfiles: AutoDLProfile[];

    constructor(a: AutoDLService) {
        this.autodlService = a;
    }

    activate(params: any) {
        return this.autodlService.getProfiles()
            .then(profiles => {
                this.autodlProfiles = profiles;
            })
    }
}