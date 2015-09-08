export class App {
    configureRouter(config, router) {
        config.title = 'Jackett';
        // Notice: the overall template is hardcoded to routes based on index.
        config.map([
            { route: ['', 'welcome'], name: 'welcome', moduleId: './views/home', title: 'Home' },
            { route: ['indexers'], name: 'indexers', moduleId: './views/indexers', title: 'Indexers' },
            { route: ['irc'], name: 'irc', moduleId: './views/irc', title: 'IRC' },
            { route: ['settings'], name: 'settings', moduleId: './views/settings', title: 'Settings' },
            { route: ['settings'], name: 'settings', moduleId: './views/settings', title: 'Server settings' },
            { route: 'irc-settings', name: 'irc-settings', moduleId: './views/irc-settings', title: 'IRC Settings' },
            { route: 'irc-profile-edit/:name', name: 'irc-profile-edit', moduleId: './views/irc-settings-edit', title: 'Edit Profile' },
            { route: 'irc-profile-create', name: 'irc-profile-create', moduleId: './views/irc-settings-edit', title: 'Create Profile' }
        ]);
        this.router = router;
    }
}
//# sourceMappingURL=app.js.map