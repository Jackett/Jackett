declare module 'aurelia-http-client' {
  import core from 'core-js';
  import { join, buildQueryString }  from 'aurelia-path';
  export class Headers {
    constructor(headers?: any);
    add(key: any, value: any): any;
    get(key: any): any;
    clear(): any;
    configureXHR(xhr: any): any;
    
    /**
       * XmlHttpRequest's getAllResponseHeaders() method returns a string of response
       * headers according to the format described here:
       * http://www.w3.org/TR/XMLHttpRequest/#the-getallresponseheaders-method
       * This method parses that string into a user-friendly key/value pair object.
       */
    static parse(headerStr: any): any;
  }
  export class RequestMessage {
    constructor(method: any, url: any, content: any, headers: any);
    buildFullUrl(): any;
  }
  
  /*jshint -W093 */
  export class HttpResponseMessage {
    constructor(requestMessage: any, xhr: any, responseType: any, reviver: any);
    content(): any;
  }
  
  /**
   * MimeTypes mapped to responseTypes
   *
   * @type {Object}
   */
  export var mimeTypes: any;
  export class RequestMessageProcessor {
    constructor(xhrType: any, xhrTransformers: any);
    abort(): any;
    process(client: any, message: any): any;
  }
  export function timeoutTransformer(client: any, processor: any, message: any, xhr: any): any;
  export function callbackParameterNameTransformer(client: any, processor: any, message: any, xhr: any): any;
  export function credentialsTransformer(client: any, processor: any, message: any, xhr: any): any;
  export function progressTransformer(client: any, processor: any, message: any, xhr: any): any;
  export function responseTypeTransformer(client: any, processor: any, message: any, xhr: any): any;
  export function headerTransformer(client: any, processor: any, message: any, xhr: any): any;
  export function contentTransformer(client: any, processor: any, message: any, xhr: any): any;
  export class JSONPRequestMessage extends RequestMessage {
    constructor(url: any, callbackParameterName: any);
  }
  class JSONPXHR {
    open(method: any, url: any): any;
    send(): any;
    abort(): any;
    setRequestHeader(): any;
  }
  export function createJSONPRequestMessageProcessor(): any;
  export class HttpRequestMessage extends RequestMessage {
    constructor(method: any, url: any, content: any, headers: any);
  }
  export function createHttpRequestMessageProcessor(): any;
  
  /**
   * A builder class allowing fluent composition of HTTP requests.
   *
   * @class RequestBuilder
   * @constructor
   */
  export class RequestBuilder {
    constructor(client: any);
    
    /**
       * Adds a user-defined request transformer to the RequestBuilder.
       *
       * @method addHelper
       * @param {String} name The name of the helper to add.
       * @param {Function} fn The helper function.
       * @chainable
       */
    static addHelper(name: any, fn: any): any;
    
    /**
       * Sends the request.
       *
       * @method send
       * @return {Promise} A cancellable promise object.
       */
    send(): any;
  }
  
  /**
  * The main HTTP client object.
  *
  * @class HttpClient
  * @constructor
  */
  export class HttpClient {
    constructor();
    
    /**
       * Configure this HttpClient with default settings to be used by all requests.
       *
       * @method configure
       * @param {Function} fn A function that takes a RequestBuilder as an argument.
       * @chainable
       */
    configure(fn: any): any;
    
    /**
       * Returns a new RequestBuilder for this HttpClient instance that can be used to build and send HTTP requests.
       *
       * @method createRequest
       * @param url The target URL.
       * @type RequestBuilder
       */
    createRequest(url: any): any;
    
    /**
       * Sends a message using the underlying networking stack.
       *
       * @method send
       * @param message A configured HttpRequestMessage or JSONPRequestMessage.
       * @param {Array} transformers A collection of transformers to apply to the HTTP request.
       * @return {Promise} A cancellable promise object.
       */
    send(message: any, transformers: any): any;
    
    /**
       * Sends an HTTP DELETE request.
       *
       * @method delete
       * @param {String} url The target URL.
       * @return {Promise} A cancellable promise object.
       */
    delete(url: any): any;
    
    /**
       * Sends an HTTP GET request.
       *
       * @method get
       * @param {String} url The target URL.
       * @return {Promise} A cancellable promise object.
       */
    get(url: any): any;
    
    /**
       * Sends an HTTP HEAD request.
       *
       * @method head
       * @param {String} url The target URL.
       * @return {Promise} A cancellable promise object.
       */
    head(url: any): any;
    
    /**
       * Sends a JSONP request.
       *
       * @method jsonp
       * @param {String} url The target URL.
       * @return {Promise} A cancellable promise object.
       */
    jsonp(url: any, callbackParameterName?: any): any;
    
    /**
       * Sends an HTTP OPTIONS request.
       *
       * @method options
       * @param {String} url The target URL.
       * @return {Promise} A cancellable promise object.
       */
    options(url: any): any;
    
    /**
       * Sends an HTTP PUT request.
       *
       * @method put
       * @param {String} url The target URL.
       * @param {Object} url The request payload.
       * @return {Promise} A cancellable promise object.
       */
    put(url: any, content: any): any;
    
    /**
       * Sends an HTTP PATCH request.
       *
       * @method patch
       * @param {String} url The target URL.
       * @param {Object} url The request payload.
       * @return {Promise} A cancellable promise object.
       */
    patch(url: any, content: any): any;
    
    /**
       * Sends an HTTP POST request.
       *
       * @method post
       * @param {String} url The target URL.
       * @param {Object} url The request payload.
       * @return {Promise} A cancellable promise object.
       */
    post(url: any, content: any): any;
  }
}