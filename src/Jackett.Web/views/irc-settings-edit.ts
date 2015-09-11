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
    servers: string[];

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
                this.servers = profile.Servers;
            }));
        }
        return Promise.all(actions);
    }

    attached() {
        var vm = this;
        $(this.profileSelect).val(this.autodlnetwork).dropdown().on('change', e => {
            this.autodlnetwork = this.name = e.target.value;
            vm.servers = [];
            vm.networks.forEach(n=> {
                if (n.Name === e.target.value) {
                    vm.servers = n.Servers;
                }
            });
        });
    }

    removeServer(index: number) {
        this.servers.splice(index, 1);
    }

    addButton() {
        if (this.servers === undefined) {
            this.servers = [];
        }

        this.servers.push('');
    }

    async submit() {

        await this.validation.validate();

        try {
            var profile = new IRCProfile();
            profile.Name = this.name;
            profile.Username = this.nickname;
            profile.Profile = this.autodlnetwork;
            profile.Id = this.id;
            profile.Servers = this.servers;
            await this.ircService.setProfile(profile);

            this.router.navigate('irc-settings');

        } catch (e) {
            this.error = 'There was an error submiting your changes';
        }
    }
} 