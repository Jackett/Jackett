export class App {
    configureRouter(config, router) {
        config.title = 'Jackett';
        // Notice: the overall template is hardcoded to routes based on index.
        config.map([
            { route: ['', 'welcome'], name: 'welcome', moduleId: './views/home', nav: true, title: 'Home' },
            { route: ['indexers'], name: 'indexers', moduleId: './views/indexers', nav: true, title: 'Indexers' },
            { route: ['irc'], name: 'irc', moduleId: './views/irc', nav: true, title: 'IRC' },
            { route: ['settings'], name: 'settings', moduleId: './views/settings', nav: true, title: 'Settings' },
            { route: ['settings'], name: 'settings', moduleId: './views/settings', nav: true, title: 'Server settings' },
            { route: 'irc-settings', name: 'irc-settings', moduleId: './views/irc-settings', nav: true, title: 'IRC Settings' },
            { route: 'irc-settings-edit', name: 'irc-settings-edit', moduleId: './views/irc-settings-edit', nav: true, title: 'Edit Profile' }
        ]);
        this.router = router;
    }
}
//# sourceMappingURL=app.js.map