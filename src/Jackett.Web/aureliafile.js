var aurelia = require('aurelia-cli');

aurelia.command('bundle', {
    js: {
        "dist/app-bundle": {
            modules: [
              'dist/*',
              'aurelia-bootstrapper',
              'aurelia-http-client',
              'aurelia-router',
              'aurelia-animator-css',
              'github:aurelia/templating-binding@0.14.0',
              'github:aurelia/templating-resources@0.14.0',
              'github:aurelia/templating-router@0.15.0',
              'github:aurelia/loader-default@0.9.5',
              'github:aurelia/history-browser@0.7.0'
            ],
            options: {
                inject: true,
                minify: true
            }
        }
    }
});