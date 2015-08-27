export class App {
    configureRouter(config, router) {
        config.title = 'Aurelia';
        config.map([
            { route: ['', 'welcome'], name: 'welcome', moduleId: './views/home', nav: true, title: 'Home' },
            { route: ['indexers'], name: 'indexers', moduleId: './views/indexers', nav: true, title: 'Indexers' },
            { route: ['irc'], name: 'irc', moduleId: './views/irc', nav: true, title: 'IRC' },
            { route: ['settings'], name: 'settings', moduleId: './views/settings', nav: true, title: 'Settings' },
        ]);
        this.router = router;
    }
}
//# sourceMappingURL=app.js.map