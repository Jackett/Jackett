import {LogManager} from 'aurelia-framework';
import {ConsoleAppender} from 'aurelia-logging-console';
import {Aurelia} from 'aurelia-framework';

LogManager.addAppender(new ConsoleAppender());
LogManager.setLevel(LogManager.logLevel.debug);

export function configure(aurelia: Aurelia) {
    aurelia.use.standardConfiguration()
               .developmentLogging();
   
    aurelia.start().then(a => a.setRoot('views/jackett'));
}