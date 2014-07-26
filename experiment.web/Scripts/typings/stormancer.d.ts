
declare module Stormancer {
    class jQueryWrapper {
        static $: JQueryStatic;
        static initWrapper(jquery: JQueryStatic): void;
    }
    class SceneEndpoint {
        public Endpoint: string;
        public SceneId: string;
        public Token: string;
    }
    interface IScene {
        send(route: string, data: any): void;
        sendRequest(route: string, data: any, handler: (data: any) => void, completed: () => void): void;
        onMessage(route: string, handler: (data: any) => void): void;
        connect(): JQueryPromise<any>;
        disconnect(): JQueryPromise<any>;
        connected: boolean;
    }
    interface IClient {
        getScene(token: any): JQueryPromise<IScene>;
        getPublicScene(sceneId: string, userData: any): JQueryPromise<IScene>;
        state: ClientState;
    }
    class ClientState {
        public stateChanged(callback: (ConnectionState: any) => void): void;
        public state: ConnectionState;
        public capabilities: any;
        public latency: number;
        public events: {
            stateChanged: string;
        };
    }
    enum ConnectionState {
        connecting = 0,
        connected = 1,
        reconnecting = 2,
        disconnected = 4,
    }
    interface IApiClient {
        getSceneEndpoint(sceneId: string, userData: any): JQueryPromise<SceneEndpoint>;
    }
    class ApiClient implements IApiClient {
        private configuration;
        constructor(configuration: Configuration);
        public getSceneEndpoint(sceneId: string, userData: any): JQueryXHR;
    }
    class Configuration {
        static forLocalDev(applicationName: string): Configuration;
        static forAccount(accountId: string, applicationName: string): Configuration;
        public getApiEndpoint(): string;
        static apiEndpoint: string;
        static localDevEndpoint: string;
        public isLocalDev: boolean;
        public localDevProxy: string;
        public account: string;
        public application: string;
        public timeout: number;
    }
    class Client implements IClient {
        private static clients;
        private apiClient;
        private connections;
        private connectionsBySceneId;
        private configuration;
        private currentId;
        static CreateClient(configuration: Configuration): IClient;
        public state: ClientState;
        constructor(configuration: Configuration);
        public getPublicScene(sceneId: string, userData: any): JQueryPromise<{}>;
        public getScene(token: any): JQueryPromise<{}>;
        private startConnection(endpoint);
        private routeHandlers;
        private responseHandlers;
        private requestCompleteHandlers;
        private dataReceived(data);
        public observeMessages(sceneId: string, route: string, handler: (data: any) => void): void;
        private encodeMsg(sceneId, route, data, requestId?);
        public send(sceneId: string, route: string, data: any[]): void;
        public sendHostRequest(sceneId: string, route: string, message: any, handler: (data: any) => void, completed: () => void): void;
        public sendRequest(sceneId: string, route: string, data: any[], handler: (data: any) => void, completed: () => void): void;
        private sendRequestInternal(connection, sceneId, route, data, handler, completed);
    }
}
interface JQueryStatic {
    stormancer: (configuration: Stormancer.Configuration) => Stormancer.IClient;
}
