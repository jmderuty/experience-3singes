///<reference path="typings/webrtc/MediaStream.d.ts"/>
///<reference path="typings/stormancer.d.ts"/>
///<reference path="typings/jQuery/jquery.d.ts"/>
///<reference path="communicationPeer.ts"/>

module Experiment {
    export class SubjectLambda {
        private peerManager: PeerManager;
        private stmClient: Stormancer.IClient;
        private scene: Stormancer.IScene;
        private peer: Peer;

        public constructor(role: string, video: HTMLVideoElement,localVideo:HTMLVideoElement) {
            this.stmClient = $.stormancer(Stormancer.Configuration.forAccount("43a32f08-d286-452f-ac8a-f3e84ce992bc", "gladostesting"));
            this.stmClient.getPublicScene("session1", { Role: role }).then(s=> {
                this.scene = s;

                this.peerManager = new PeerManager(this.scene);
                getUserMedia({ 'video': true, 'audio': true }, s=> {
                    localVideo.src = URL.createObjectURL(s);

                    this.scene.onMessage("gamestate.ready", msg=> {
                        trace("starting establishing connection with " + msg.role);
                        if (this.onPlayerConnected) {
                            this.onPlayerConnected(msg.role);
                        }
                        if (msg.role == "alpha") {
                            this.peerManager.Streams[msg.id] = {
                                callback: s=> {
                                    trace("Adding stream from " + msg.role);
                                    video.src = URL.createObjectURL(s);
                                },
                                stream: s
                            };
                           
                            trace("Adding stream to " + msg.role);
                        }
                    });
                    this.scene.connect().then(() => {
                        this.scene.send("start", "");
                        trace("Connected");
                    });
                }, () => { });

            });

        }

        public onPlayerConnected: (role: string) => void;

    }

    
}
