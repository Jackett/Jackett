import {IRCProfileService, IRCProfile, NetworkSummary} from '../Services/IRCProfileService';
import {autoinject} from 'aurelia-framework';
 
@autoinject 
export class IRCSettings {
    ircService: IRCProfileService;
    profiles: IRCProfile[];
    autodlprofiles: NetworkSummary[];

    constructor(httpClient: IRCProfileService) {
        this.ircService = httpClient;
    }

    activate() {
        return Promise.all([
            this.ircService.getAutoDLProfiles()
                .then(profiles => {
                    this.autodlprofiles = profiles
                }),

            this.ircService.getProfiles().then(profiles => {
                this.profiles = profiles
            })
        ]);
    }

    async remove(item) {
        await this.ircService.removeProfile(item);
        this.profiles = await this.ircService.getProfiles();
    }
} 