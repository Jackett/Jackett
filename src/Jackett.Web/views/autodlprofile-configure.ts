import 'jquery';
import 'ms-signalr-client';
import 'jackett-hubs';
import {AutoDLService, AutoDLProfile} from '../Services/AutoDLService'
import {autoinject} from 'aurelia-framework';
import {Router} from 'aurelia-router';

@autoinject
export class AutoDLProfileConfigure {
    autodlService: AutoDLService;
    profile: AutoDLProfile;
    router: Router;

    form: any;

    constructor(a: AutoDLService, r: Router) {
        this.autodlService = a;
        this.router = r;
    }

    activate(params: any) {
        return this.autodlService.getProfiles()
            .then(profiles => {
                this.profile = profiles.filter(p=> p.Type == params.type)[0];

                this.profile.Options.forEach(opt=> {
                    if (opt.Value === null) {
                        opt.Value = opt.DefaultValue;
                    }

                    if (opt.Type === 'Bool' && opt.Value!==null) {
                        opt.Value = opt.Value.toLowerCase() === 'true';
                    }
                });
            })
    }

    attached() {
        var vm = this;
        $(this.form).find('input').on('paste', function (e) {
            // We get this event before the data is applied, so wait a bit :/
            setTimeout(() => {
                var fieldName = $(e.target).attr('data-fieldname');
                var value = $(e.target).val();
                // Find by fieldname
                vm.profile.Options.forEach(opt=> {
                    if (opt.Name === fieldName) {
                        // If we have a paste group
                        if (opt.PasteGroup !== null) {
                            opt.PasteGroup.split(',').forEach(i=> {
                                i = $.trim(i);
                                // Loop over each item in the paste group and apply
                                vm.profile.Options.forEach(o=> {
                                    if (o.Name === i && o.PasteRegex !== null) {
                                        var regex = new RegExp(o.PasteRegex, "i");
                                        var match = regex.exec(value);
                                        debugger;
                                        if (match!==null&& match.length > 0) {
                                            o.Value = match[1];
                                        }
                                    }
                                });
                            });
                        }
                    }
                });
            },50);
        });
    }


    async submit() {

        //await this.validation.validate();

      //  try {
            await this.autodlService.saveProfile(this.profile);
            this.router.navigate('indexers');

       // } catch (e) {
            //this.error = 'There was an error submiting your changes';
      //  }
    }
}