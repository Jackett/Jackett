declare module 'aurelia-framework' {
  import core from 'core-js';
  import * as TheLogManager from 'aurelia-logging';
  import { Metadata }  from 'aurelia-metadata';
  import { Container }  from 'aurelia-dependency-injection';
  import { Loader }  from 'aurelia-loader';
  import { join, relativeToFile }  from 'aurelia-path';
  import { BindingLanguage, ViewEngine, ViewSlot, ResourceRegistry, CompositionEngine, Animator, DOMBoundary }  from 'aurelia-templating';
  
  /**
   * Manages loading and configuring plugins.
   *
   * @class Plugins
   * @constructor
   * @param {Aurelia} aurelia An instance of Aurelia.
   */
  export class Plugins {
    constructor(aurelia: Aurelia);
    
    /**
       * Configures an internal feature plugin before Aurelia starts.
       *
       * @method feature
       * @param {string} plugin The folder for the internal plugin to configure (expects an index.js in that folder).
       * @param {config} config The configuration for the specified plugin.
       * @return {Plugins} Returns the current Plugins instance.
      */
    feature(plugin: string, config: any): Plugins;
    
    /**
       * Configures an external, 3rd party plugin before Aurelia starts.
       *
       * @method plugin
       * @param {string} plugin The ID of the 3rd party plugin to configure.
       * @param {config} config The configuration for the specified plugin.
       * @return {Plugins} Returns the current Plugins instance.
     */
    plugin(plugin: string, config: any): Plugins;
    
    /**
       * Plugs in the default binding language from aurelia-templating-binding.
       *
       * @method defaultBindingLanguage
       * @return {Plugins} Returns the current Plugins instance.
      */
    defaultBindingLanguage(): Plugins;
    
    /**
       * Plugs in the router from aurelia-templating-router.
       *
       * @method router
       * @return {Plugins} Returns the current Plugins instance.
      */
    router(): Plugins;
    
    /**
       * Plugs in the default history implementation from aurelia-history-browser.
       *
       * @method history
       * @return {Plugins} Returns the current Plugins instance.
      */
    history(): Plugins;
    
    /**
       * Plugs in the default templating resources (if, repeat, show, compose, etc.) from aurelia-templating-resources.
       *
       * @method defaultResources
       * @return {Plugins} Returns the current Plugins instance.
      */
    defaultResources(): Plugins;
    
    /**
       * Plugs in the event aggregator from aurelia-event-aggregator.
       *
       * @method eventAggregator
       * @return {Plugins} Returns the current Plugins instance.
      */
    eventAggregator(): Plugins;
    
    /**
       * Sets up the Aurelia configuration. This is equivalent to calling `.defaultBindingLanguage().defaultResources().history().router().eventAggregator();`
       *
       * @method standardConfiguration
       * @return {Plugins} Returns the current Plugins instance.
      */
    standardConfiguration(): Plugins;
    
    /**
       * Plugs in the ConsoleAppender and sets the log level to debug.
       *
       * @method developmentLogging
       * @return {Plugins} Returns the current Plugins instance.
      */
    developmentLogging(): Plugins;
  }
  
  /**
   * The framework core that provides the main Aurelia object.
   *
   * @class Aurelia
   * @constructor
   * @param {Loader} loader The loader for this Aurelia instance to use. If a loader is not specified, Aurelia will use a defaultLoader.
   * @param {Container} container The dependency injection container for this Aurelia instance to use. If a container is not specified, Aurelia will create an empty container.
   * @param {ResourceRegistry} resources The resource registry for this Aurelia instance to use. If a resource registry is not specified, Aurelia will create an empty registry.
   */
  export class Aurelia {
    loader: Loader;
    container: Container;
    resources: ResourceRegistry;
    use: Plugins;
    constructor(loader?: Loader, container?: Container, resources?: ResourceRegistry);
    
    /**
       * Adds an existing object to the framework's dependency injection container.
       *
       * @method withInstance
       * @param {Class} type The object type of the dependency that the framework will inject.
       * @param {Object} instance The existing instance of the dependency that the framework will inject.
       * @return {Aurelia} Returns the current Aurelia instance.
       */
    withInstance(type: any, instance: any): Aurelia;
    
    /**
       * Adds a singleton to the framework's dependency injection container.
       *
       * @method withSingleton
       * @param {Class} type The object type of the dependency that the framework will inject.
       * @param {Object} implementation The constructor function of the dependency that the framework will inject.
       * @return {Aurelia} Returns the current Aurelia instance.
       */
    withSingleton(type: any, implementation?: Function): Aurelia;
    
    /**
       * Adds a transient to the framework's dependency injection container.
       *
       * @method withTransient
       * @param {Class} type The object type of the dependency that the framework will inject.
       * @param {Object} implementation The constructor function of the dependency that the framework will inject.
       * @return {Aurelia} Returns the current Aurelia instance.
       */
    withTransient(type: any, implementation?: Function): Aurelia;
    
    /**
       * Adds globally available view resources to be imported into the Aurelia framework.
       *
       * @method globalizeResources
       * @param {Object|Array} resources The relative module id to the resource. (Relative to the plugin's installer.)
       * @return {Aurelia} Returns the current Aurelia instance.
       */
    globalizeResources(resources: string | string[]): Aurelia;
    
    /**
       * Renames a global resource that was imported.
       *
       * @method renameGlobalResource
       * @param {String} resourcePath The path to the resource.
       * @param {String} newName The new name.
       * @return {Aurelia} Returns the current Aurelia instance.
       */
    renameGlobalResource(resourcePath: string, newName: string): Aurelia;
    
    /**
       * Adds an async function that runs before the plugins are run.
       *
       * @method addPreStartTask
       * @param {Function} task The function to run before start.
       * @return {Aurelia} Returns the current Aurelia instance.
       */
    addPreStartTask(task: Function): Aurelia;
    
    /**
       * Adds an async function that runs after the plugins are run.
       *
       * @method addPostStartTask
       * @param {Function} task The function to run after start.
       * @return {Aurelia} Returns the current Aurelia instance.
       */
    addPostStartTask(task: Function): Aurelia;
    
    /**
       * Loads plugins, then resources, and then starts the Aurelia instance.
       *
       * @method start
       * @return {Promise<Aurelia>} Returns the started Aurelia instance.
       */
    start(): Promise<Aurelia>;
    
    /**
       * Enhances the host's existing elements with behaviors and bindings.
       *
       * @method enhance
       * @param {Object} bindingContext A binding context for the enhanced elements.
       * @param {string|Object} applicationHost The DOM object that Aurelia will enhance.
       * @return {Promise<Aurelia>} Returns the current Aurelia instance.
       */
    enhance(bindingContext?: Object, applicationHost?: any): Promise<Aurelia>;
    
    /**
       * Instantiates the root view-model and view and add them to the DOM.
       *
       * @method setRoot
       * @param {Object} root The root view-model to load upon bootstrap.
       * @param {string|Object} applicationHost The DOM object that Aurelia will attach to.
       * @return {Promise<Aurelia>} Returns the current Aurelia instance.
       */
    setRoot(root?: string, applicationHost?: any): Promise<Aurelia>;
  }
  
  /**
   * The aurelia framework brings together all the required core aurelia libraries into a ready-to-go application-building platform.
   *
   * @module framework
   */
  export * from 'aurelia-dependency-injection';
  export * from 'aurelia-binding';
  export * from 'aurelia-metadata';
  export * from 'aurelia-templating';
  export * from 'aurelia-loader';
  export * from 'aurelia-task-queue';
  export * from 'aurelia-path';
  export var LogManager: any;
}