import {HttpClient} from 'aurelia-fetch-client'
import {autoinject} from 'aurelia-framework';

 
@autoinject 
export class IRCSettings {
    http: HttpClient;
    profiles = [];
    autodlprofiles = [];

    constructor(httpClient: HttpClient) {
        this.http = httpClient;
        this.http.configure(config=> {
            config.useStandardConfiguration();
        });
    }

    activate() {
        return this.http.fetch('../webapi/IRCProfile/AutoDLProfiles')
            .then(response => response.json())
            .then(profiles => { debugger; this.autodlprofiles = profiles })

       /* return this.http.fetch('../webapi/IRCProfile')
            .then(response => response.json())
            .then(profiles => { this.profiles = profiles });*/
     
    }
} 