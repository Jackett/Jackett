import { LogManager } from 'aurelia-framework';
import { ConsoleAppender } from 'aurelia-logging-console';
LogManager.addAppender(new ConsoleAppender());
LogManager.setLevel(LogManager.logLevel.debug);
export function configure(aurelia) {
    aurelia.use.standardConfiguration()
        .developmentLogging();
    aurelia.start().then(a => a.setRoot('views/jackett'));
}
//# sourceMappingURL=main.js.map