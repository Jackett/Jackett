declare module 'aurelia-logging-console' {
  export class ConsoleAppender {
    debug(logger: Object, ...rest: any[]): void;
    info(logger: Object, ...rest: any[]): void;
    warn(logger: Object, ...rest: any[]): void;
    error(logger: Object, ...rest: any[]): void;
  }
}