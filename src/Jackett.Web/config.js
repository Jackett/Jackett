System.config({
  baseURL: "/dev",
  defaultJSExtensions: true,
  transpiler: "babel",
  babelOptions: {
    "optional": [
      "runtime",
      "optimisation.modules.system"
    ]
  },
  paths: {
    "jackett-hubs": "/signalr/hubs",
    "github:*": "jspm_packages/github/*",
    "npm:*": "jspm_packages/npm/*"
  },
  bundles: {
    "dist/app-bundle.js": [
      "github:aurelia/history-browser@0.7.0",
      "github:aurelia/history-browser@0.7.0/aurelia-history-browser",
      "npm:core-js@0.9.18",
      "github:aurelia/history@0.6.1",
      "github:aurelia/history@0.6.1/aurelia-history",
      "npm:core-js@0.9.18/index",
      "npm:core-js@0.9.18/shim",
      "npm:core-js@0.9.18/modules/core.dict",
      "npm:core-js@0.9.18/modules/core.iter-helpers",
      "npm:core-js@0.9.18/modules/core.$for",
      "npm:core-js@0.9.18/modules/core.delay",
      "npm:core-js@0.9.18/modules/core.function.part",
      "npm:core-js@0.9.18/modules/core.object",
      "npm:core-js@0.9.18/modules/core.array.turn",
      "npm:core-js@0.9.18/modules/core.number.iterator",
      "npm:core-js@0.9.18/modules/core.number.math",
      "npm:core-js@0.9.18/modules/core.string.escape-html",
      "npm:core-js@0.9.18/modules/core.date",
      "npm:core-js@0.9.18/modules/core.global",
      "npm:core-js@0.9.18/modules/core.log",
      "npm:core-js@0.9.18/modules/$",
      "npm:core-js@0.9.18/modules/es5",
      "npm:core-js@0.9.18/modules/es6.symbol",
      "npm:core-js@0.9.18/modules/es6.object.assign",
      "npm:core-js@0.9.18/modules/es6.object.is",
      "npm:core-js@0.9.18/modules/es6.object.set-prototype-of",
      "npm:core-js@0.9.18/modules/es6.object.to-string",
      "npm:core-js@0.9.18/modules/es6.object.statics-accept-primitives",
      "npm:core-js@0.9.18/modules/es6.function.name",
      "npm:core-js@0.9.18/modules/es6.function.has-instance",
      "npm:core-js@0.9.18/modules/es6.number.constructor",
      "npm:core-js@0.9.18/modules/es6.number.statics",
      "npm:core-js@0.9.18/modules/es6.math",
      "npm:core-js@0.9.18/modules/es6.string.from-code-point",
      "npm:core-js@0.9.18/modules/es6.string.raw",
      "npm:core-js@0.9.18/modules/es6.string.iterator",
      "npm:core-js@0.9.18/modules/es6.string.code-point-at",
      "npm:core-js@0.9.18/modules/es6.string.ends-with",
      "npm:core-js@0.9.18/modules/es6.string.includes",
      "npm:core-js@0.9.18/modules/es6.string.repeat",
      "npm:core-js@0.9.18/modules/es6.string.starts-with",
      "npm:core-js@0.9.18/modules/es6.array.from",
      "npm:core-js@0.9.18/modules/es6.array.of",
      "npm:core-js@0.9.18/modules/es6.array.iterator",
      "npm:core-js@0.9.18/modules/es6.array.species",
      "npm:core-js@0.9.18/modules/es6.array.copy-within",
      "npm:core-js@0.9.18/modules/es6.array.fill",
      "npm:core-js@0.9.18/modules/es6.array.find",
      "npm:core-js@0.9.18/modules/es6.array.find-index",
      "npm:core-js@0.9.18/modules/es6.regexp",
      "npm:core-js@0.9.18/modules/es6.promise",
      "npm:core-js@0.9.18/modules/es6.map",
      "npm:core-js@0.9.18/modules/es6.set",
      "npm:core-js@0.9.18/modules/es6.weak-map",
      "npm:core-js@0.9.18/modules/es6.weak-set",
      "npm:core-js@0.9.18/modules/es6.reflect",
      "npm:core-js@0.9.18/modules/es7.array.includes",
      "npm:core-js@0.9.18/modules/es7.string.at",
      "npm:core-js@0.9.18/modules/es7.string.lpad",
      "npm:core-js@0.9.18/modules/es7.string.rpad",
      "npm:core-js@0.9.18/modules/es7.regexp.escape",
      "npm:core-js@0.9.18/modules/es7.object.get-own-property-descriptors",
      "npm:core-js@0.9.18/modules/es7.object.to-array",
      "npm:core-js@0.9.18/modules/es7.map.to-json",
      "npm:core-js@0.9.18/modules/es7.set.to-json",
      "npm:core-js@0.9.18/modules/js.array.statics",
      "npm:core-js@0.9.18/modules/web.timers",
      "npm:core-js@0.9.18/modules/web.immediate",
      "npm:core-js@0.9.18/modules/web.dom.iterable",
      "npm:core-js@0.9.18/modules/$.ctx",
      "npm:core-js@0.9.18/modules/$.def",
      "npm:core-js@0.9.18/modules/$.assign",
      "npm:core-js@0.9.18/modules/$.keyof",
      "npm:core-js@0.9.18/modules/$.uid",
      "npm:core-js@0.9.18/modules/$.assert",
      "npm:core-js@0.9.18/modules/$.iter",
      "npm:core-js@0.9.18/modules/$.for-of",
      "npm:core-js@0.9.18/modules/$.iter-call",
      "npm:core-js@0.9.18/modules/$.mix",
      "npm:core-js@0.9.18/modules/$.partial",
      "npm:core-js@0.9.18/modules/$.own-keys",
      "npm:core-js@0.9.18/modules/$.cof",
      "npm:core-js@0.9.18/modules/$.unscope",
      "npm:core-js@0.9.18/modules/$.iter-define",
      "npm:core-js@0.9.18/modules/$.invoke",
      "npm:core-js@0.9.18/modules/$.replacer",
      "npm:core-js@0.9.18/modules/$.fw",
      "github:jspm/nodelibs-process@0.1.1",
      "npm:core-js@0.9.18/modules/$.dom-create",
      "npm:core-js@0.9.18/modules/$.array-methods",
      "npm:core-js@0.9.18/modules/$.array-includes",
      "npm:core-js@0.9.18/modules/$.throws",
      "npm:core-js@0.9.18/modules/$.shared",
      "npm:core-js@0.9.18/modules/$.redef",
      "npm:core-js@0.9.18/modules/$.enum-keys",
      "npm:core-js@0.9.18/modules/$.get-names",
      "npm:core-js@0.9.18/modules/$.wks",
      "npm:core-js@0.9.18/modules/$.same",
      "npm:core-js@0.9.18/modules/$.set-proto",
      "npm:core-js@0.9.18/modules/$.string-at",
      "npm:core-js@0.9.18/modules/$.string-repeat",
      "npm:core-js@0.9.18/modules/$.iter-detect",
      "npm:core-js@0.9.18/modules/$.species",
      "npm:core-js@0.9.18/modules/$.task",
      "npm:core-js@0.9.18/modules/$.collection-strong",
      "npm:core-js@0.9.18/modules/$.collection",
      "npm:core-js@0.9.18/modules/$.collection-weak",
      "npm:core-js@0.9.18/modules/$.string-pad",
      "npm:core-js@0.9.18/modules/$.collection-to-json",
      "github:jspm/nodelibs-process@0.1.1/index",
      "npm:process@0.10.1",
      "npm:process@0.10.1/browser",
      "github:aurelia/loader-default@0.9.5",
      "github:aurelia/loader-default@0.9.5/aurelia-loader-default",
      "github:aurelia/metadata@0.7.3",
      "github:aurelia/loader@0.8.7",
      "github:aurelia/metadata@0.7.3/aurelia-metadata",
      "github:aurelia/loader@0.8.7/aurelia-loader",
      "github:aurelia/path@0.8.1",
      "github:aurelia/path@0.8.1/aurelia-path",
      "github:aurelia/templating-router@0.15.0",
      "github:aurelia/templating-router@0.15.0/aurelia-templating-router",
      "github:aurelia/router@0.11.0",
      "github:aurelia/router@0.11.0/aurelia-router",
      "github:aurelia/logging@0.6.4",
      "github:aurelia/dependency-injection@0.9.2",
      "github:aurelia/route-recognizer@0.6.2",
      "github:aurelia/event-aggregator@0.7.0",
      "github:aurelia/logging@0.6.4/aurelia-logging",
      "github:aurelia/dependency-injection@0.9.2/aurelia-dependency-injection",
      "github:aurelia/route-recognizer@0.6.2/aurelia-route-recognizer",
      "github:aurelia/event-aggregator@0.7.0/aurelia-event-aggregator",
      "github:aurelia/templating-router@0.15.0/route-loader",
      "github:aurelia/templating@0.14.4",
      "github:aurelia/templating@0.14.4/aurelia-templating",
      "github:aurelia/binding@0.8.6",
      "github:aurelia/task-queue@0.6.2",
      "github:aurelia/task-queue@0.6.2/aurelia-task-queue",
      "github:aurelia/binding@0.8.6/aurelia-binding",
      "github:aurelia/templating-router@0.15.0/router-view",
      "github:aurelia/templating-router@0.15.0/route-href",
      "github:aurelia/templating-resources@0.14.0",
      "github:aurelia/templating-resources@0.14.0/aurelia-templating-resources",
      "github:aurelia/templating-resources@0.14.0/if",
      "github:aurelia/templating-resources@0.14.0/with",
      "github:aurelia/templating-resources@0.14.0/compose",
      "github:aurelia/templating-resources@0.14.0/show",
      "github:aurelia/templating-resources@0.14.0/global-behavior",
      "github:aurelia/templating-resources@0.14.0/repeat",
      "github:aurelia/templating-resources@0.14.0/sanitize-html",
      "github:aurelia/templating-resources@0.14.0/replaceable",
      "github:aurelia/templating-resources@0.14.0/focus",
      "github:aurelia/templating-resources@0.14.0/compile-spy",
      "github:aurelia/templating-resources@0.14.0/view-spy",
      "github:aurelia/templating-binding@0.14.0",
      "github:aurelia/templating-binding@0.14.0/aurelia-templating-binding",
      "github:aurelia/animator-css@0.15.0",
      "github:aurelia/animator-css@0.15.0/aurelia-animator-css",
      "github:aurelia/http-client@0.11.0",
      "github:aurelia/http-client@0.11.0/aurelia-http-client",
      "github:aurelia/path@0.9.0",
      "github:aurelia/path@0.9.0/aurelia-path",
      "github:aurelia/bootstrapper@0.16.0",
      "github:aurelia/bootstrapper@0.16.0/aurelia-bootstrapper",
      "github:aurelia/framework@0.15.0",
      "github:aurelia/logging-console@0.6.2",
      "github:aurelia/framework@0.15.0/aurelia-framework",
      "github:aurelia/logging-console@0.6.2/aurelia-logging-console"
    ]
  },

  map: {
    "aurelia-animator-css": "github:aurelia/animator-css@0.15.0",
    "aurelia-bootstrapper": "github:aurelia/bootstrapper@0.16.0",
    "aurelia-dependency-injection": "github:aurelia/dependency-injection@0.9.2",
    "aurelia-fetch-client": "github:aurelia/fetch-client@0.2.0",
    "aurelia-framework": "github:aurelia/framework@0.15.0",
    "aurelia-http-client": "github:aurelia/http-client@0.11.0",
    "aurelia-router": "github:aurelia/router@0.11.0",
    "aurelia-validation": "github:aurelia/validation@0.2.8",
    "babel": "npm:babel-core@5.8.23",
    "babel-runtime": "npm:babel-runtime@5.8.20",
    "core-js": "npm:core-js@1.1.4",
    "font-awesome": "npm:font-awesome@4.4.0",
    "jquery": "github:components/jquery@2.1.4",
    "moment": "npm:moment@2.10.6",
    "ms-signalr-client": "npm:ms-signalr-client@2.2.2",
    "polymer/mutationobservers": "github:polymer/mutationobservers@0.4.2",
    "semantic-ui": "github:Semantic-Org/Semantic-UI@2.1.3",
    "whatwg-fetch": "npm:whatwg-fetch@0.9.0",
    "github:Semantic-Org/Semantic-UI@2.1.3": {
      "css": "github:systemjs/plugin-css@0.1.16",
      "jquery": "github:components/jquery@2.1.4"
    },
    "github:aurelia/animator-css@0.15.0": {
      "aurelia-metadata": "github:aurelia/metadata@0.7.3",
      "aurelia-templating": "github:aurelia/templating@0.14.4"
    },
    "github:aurelia/binding@0.8.6": {
      "aurelia-dependency-injection": "github:aurelia/dependency-injection@0.9.2",
      "aurelia-metadata": "github:aurelia/metadata@0.7.3",
      "aurelia-task-queue": "github:aurelia/task-queue@0.6.2",
      "core-js": "npm:core-js@0.9.18"
    },
    "github:aurelia/bootstrapper@0.16.0": {
      "aurelia-event-aggregator": "github:aurelia/event-aggregator@0.7.0",
      "aurelia-framework": "github:aurelia/framework@0.15.0",
      "aurelia-history": "github:aurelia/history@0.6.1",
      "aurelia-history-browser": "github:aurelia/history-browser@0.7.0",
      "aurelia-loader-default": "github:aurelia/loader-default@0.9.5",
      "aurelia-logging-console": "github:aurelia/logging-console@0.6.2",
      "aurelia-router": "github:aurelia/router@0.11.0",
      "aurelia-templating": "github:aurelia/templating@0.14.4",
      "aurelia-templating-binding": "github:aurelia/templating-binding@0.14.0",
      "aurelia-templating-resources": "github:aurelia/templating-resources@0.14.0",
      "aurelia-templating-router": "github:aurelia/templating-router@0.15.0",
      "core-js": "npm:core-js@0.9.18"
    },
    "github:aurelia/dependency-injection@0.9.2": {
      "aurelia-logging": "github:aurelia/logging@0.6.4",
      "aurelia-metadata": "github:aurelia/metadata@0.7.3",
      "core-js": "npm:core-js@0.9.18"
    },
    "github:aurelia/event-aggregator@0.7.0": {
      "aurelia-logging": "github:aurelia/logging@0.6.4"
    },
    "github:aurelia/fetch-client@0.2.0": {
      "core-js": "npm:core-js@0.9.18"
    },
    "github:aurelia/framework@0.15.0": {
      "aurelia-binding": "github:aurelia/binding@0.8.6",
      "aurelia-dependency-injection": "github:aurelia/dependency-injection@0.9.2",
      "aurelia-loader": "github:aurelia/loader@0.8.7",
      "aurelia-logging": "github:aurelia/logging@0.6.4",
      "aurelia-metadata": "github:aurelia/metadata@0.7.3",
      "aurelia-path": "github:aurelia/path@0.8.1",
      "aurelia-task-queue": "github:aurelia/task-queue@0.6.2",
      "aurelia-templating": "github:aurelia/templating@0.14.4",
      "core-js": "npm:core-js@0.9.18"
    },
    "github:aurelia/history-browser@0.7.0": {
      "aurelia-history": "github:aurelia/history@0.6.1",
      "core-js": "npm:core-js@0.9.18"
    },
    "github:aurelia/http-client@0.11.0": {
      "aurelia-path": "github:aurelia/path@0.9.0",
      "core-js": "npm:core-js@0.9.18"
    },
    "github:aurelia/loader-default@0.9.5": {
      "aurelia-loader": "github:aurelia/loader@0.8.7",
      "aurelia-metadata": "github:aurelia/metadata@0.7.3"
    },
    "github:aurelia/loader@0.8.7": {
      "aurelia-html-template-element": "github:aurelia/html-template-element@0.2.0",
      "aurelia-metadata": "github:aurelia/metadata@0.7.3",
      "aurelia-path": "github:aurelia/path@0.8.1",
      "core-js": "npm:core-js@0.9.18",
      "webcomponentsjs": "github:webcomponents/webcomponentsjs@0.6.3"
    },
    "github:aurelia/logging-console@0.6.2": {
      "aurelia-logging": "github:aurelia/logging@0.6.4"
    },
    "github:aurelia/metadata@0.7.3": {
      "core-js": "npm:core-js@0.9.18"
    },
    "github:aurelia/route-recognizer@0.6.2": {
      "core-js": "npm:core-js@0.9.18"
    },
    "github:aurelia/router@0.11.0": {
      "aurelia-dependency-injection": "github:aurelia/dependency-injection@0.9.2",
      "aurelia-event-aggregator": "github:aurelia/event-aggregator@0.7.0",
      "aurelia-history": "github:aurelia/history@0.6.1",
      "aurelia-logging": "github:aurelia/logging@0.6.4",
      "aurelia-path": "github:aurelia/path@0.8.1",
      "aurelia-route-recognizer": "github:aurelia/route-recognizer@0.6.2",
      "core-js": "npm:core-js@0.9.18"
    },
    "github:aurelia/templating-binding@0.14.0": {
      "aurelia-binding": "github:aurelia/binding@0.8.6",
      "aurelia-logging": "github:aurelia/logging@0.6.4",
      "aurelia-templating": "github:aurelia/templating@0.14.4"
    },
    "github:aurelia/templating-resources@0.14.0": {
      "aurelia-binding": "github:aurelia/binding@0.8.6",
      "aurelia-dependency-injection": "github:aurelia/dependency-injection@0.9.2",
      "aurelia-logging": "github:aurelia/logging@0.6.4",
      "aurelia-task-queue": "github:aurelia/task-queue@0.6.2",
      "aurelia-templating": "github:aurelia/templating@0.14.4",
      "core-js": "npm:core-js@0.9.18"
    },
    "github:aurelia/templating-router@0.15.0": {
      "aurelia-dependency-injection": "github:aurelia/dependency-injection@0.9.2",
      "aurelia-metadata": "github:aurelia/metadata@0.7.3",
      "aurelia-path": "github:aurelia/path@0.8.1",
      "aurelia-router": "github:aurelia/router@0.11.0",
      "aurelia-templating": "github:aurelia/templating@0.14.4"
    },
    "github:aurelia/templating@0.14.4": {
      "aurelia-binding": "github:aurelia/binding@0.8.6",
      "aurelia-dependency-injection": "github:aurelia/dependency-injection@0.9.2",
      "aurelia-html-template-element": "github:aurelia/html-template-element@0.2.0",
      "aurelia-loader": "github:aurelia/loader@0.8.7",
      "aurelia-logging": "github:aurelia/logging@0.6.4",
      "aurelia-metadata": "github:aurelia/metadata@0.7.3",
      "aurelia-path": "github:aurelia/path@0.8.1",
      "aurelia-task-queue": "github:aurelia/task-queue@0.6.2",
      "core-js": "npm:core-js@0.9.18"
    },
    "github:aurelia/validation@0.2.8": {
      "aurelia-binding": "github:aurelia/binding@0.8.6",
      "aurelia-dependency-injection": "github:aurelia/dependency-injection@0.9.2",
      "aurelia-logging": "github:aurelia/logging@0.6.4",
      "aurelia-templating": "github:aurelia/templating@0.14.4"
    },
    "github:jspm/nodelibs-process@0.1.1": {
      "process": "npm:process@0.10.1"
    },
    "npm:babel-runtime@5.8.20": {
      "process": "github:jspm/nodelibs-process@0.1.1"
    },
    "npm:core-js@0.9.18": {
      "fs": "github:jspm/nodelibs-fs@0.1.2",
      "process": "github:jspm/nodelibs-process@0.1.1",
      "systemjs-json": "github:systemjs/plugin-json@0.1.0"
    },
    "npm:core-js@1.1.4": {
      "fs": "github:jspm/nodelibs-fs@0.1.2",
      "process": "github:jspm/nodelibs-process@0.1.1",
      "systemjs-json": "github:systemjs/plugin-json@0.1.0"
    },
    "npm:font-awesome@4.4.0": {
      "css": "github:systemjs/plugin-css@0.1.16"
    },
    "npm:jquery@2.1.4": {
      "process": "github:jspm/nodelibs-process@0.1.1"
    },
    "npm:moment@2.10.6": {
      "process": "github:jspm/nodelibs-process@0.1.1"
    },
    "npm:ms-signalr-client@2.2.2": {
      "jquery": "npm:jquery@2.1.4"
    }
  }
});
