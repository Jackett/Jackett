import {HttpClient} from 'aurelia-fetch-client'
import  'jquery' 

export class Irc {
    activate() {
        $('body').addClass('jackett-body-fill');
    }

    deactivate() {
        $('body').removeClass('jackett-body-fill');
    }
}