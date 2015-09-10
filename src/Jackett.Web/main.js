import { LogManager } from 'aurelia-framework';
import { ConsoleAppender } from 'aurelia-logging-console';
import 'aurelia-validation';
import { ValidateCustomAttributeViewStrategy } from 'aurelia-validation';
LogManager.addAppender(new ConsoleAppender());
LogManager.setLevel(LogManager.logLevel.debug);
export function configure(aurelia) {
    aurelia.use.standardConfiguration()
        .plugin('./resources/MomentValueConverter', undefined)
        .developmentLogging()
        .plugin('aurelia-validation', (config) => { config.useViewStrategy(ValidateCustomAttributeViewStrategy.TWBootstrapAppendToMessage); });
    aurelia.start().then(a => a.setRoot('app', document.body));
}
//# sourceMappingURL=main.js.map