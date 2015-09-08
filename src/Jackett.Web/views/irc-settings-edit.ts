import {HttpClient} from 'aurelia-fetch-client'
import {autoinject} from 'aurelia-framework';
import {Validation} from 'aurelia-validation';
import {IRCProfileService, IRCProfile, NetworkSummary} from '../Services/IRCProfileService';
import 'jquery' 
import 'semantic-ui';
import {Router} from 'aurelia-router';

@autoinject 
export class IRCSettings {
    ircService: IRCProfileService;

    networks: NetworkSummary[];
    autodlnetwork: string;
    name: string;
    nickname: string;
    error: string;
    id: string;

    profileSelect: any;
    validation: any;
    router: Router;

    constructor( httpClient: IRCProfileService, validation: Validation, r: Router) {
        this.ircService = httpClient;
        this.router = r;

        this.validation = validation.on(this, undefined)
            .ensure('autodlnetwork')
            .isNotEmpty()

            .ensure('name')
            .isNotEmpty()

            .ensure('nickname')
            .isNotEmpty();
    }

    activate(params: any) {
        var actions = [
            this.ircService.getAutoDLProfiles()
                .then(profiles => {
                    this.networks = profiles;
                })
        ];
        if (params.name) {
            actions.push(this.ircService.getProfile(params.name).then(profile=> {
                this.autodlnetwork = profile.Profile;
                this.name = profile.Name;
                this.nickname = profile.Username;
                this.id = profile.Id;
            }));
        }
        return Promise.all(actions);
    }

    attached() {
        $(this.profileSelect).val(this.autodlnetwork).dropdown().on('change', e => {
            this.autodlnetwork = this.name = e.target.value;
        });
    }

    async submit() {

        await this.validation.validate();

        try {
            var profile = new IRCProfile();
            profile.Name = this.name;
            profile.Username = this.nickname;
            profile.Profile = this.autodlnetwork;
            profile.Id = this.id;
            await this.ircService.setProfile(profile);

            this.router.navigate('irc-settings');

        } catch (e) {
            this.error = 'There was an error submiting your changes';
        }
    }
} 