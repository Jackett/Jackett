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
        return this.http.fetch('../webapi/IRCProfile/AutoDLProfiles', {
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            }
        })
            .then(response => { return response.json(); })
            .then(profiles => { this.autodlprofiles = profiles })

       /* return this.http.fetch('../webapi/IRCProfile')
            .then(response => response.json())
            .then(profiles => { this.profiles = profiles });*/
     
    }
} 