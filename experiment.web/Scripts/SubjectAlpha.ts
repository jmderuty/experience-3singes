module Experiment {

    export class SubjectAlpha {
        private peerManager: PeerManager;
        private stmClient: Stormancer.IClient;
        private scene: Stormancer.IScene;
        private peer1: Peer;
        private peer2: Peer;
        private peer3: Peer;

        public constructor(video1: HTMLVideoElement, video2: HTMLVideoElement, video3: HTMLVideoElement, localVideo: HTMLVideoElement) {
            this.stmClient = $.stormancer(Stormancer.Configuration.forAccount("43a32f08-d286-452f-ac8a-f3e84ce992bc", "gladostesting"));
            this.stmClient.getPublicScene("session1", { Role: "alpha" }).then(s=> {
                this.scene = s;


                this.peerManager = new PeerManager(this.scene);
                getUserMedia({ 'video': true, 'audio': true }, s=> {
                    localVideo.src = URL.createObjectURL(s);

                    this.scene.onMessage("gamestate.ready", msg=> {
                        trace("starting establishing connection with " + msg.role);
                        if (this.onPlayerConnected) {
                            this.onPlayerConnected(msg.role);
                        }
                        if (msg.role == "subject1") {
                            this.peerManager.Streams[msg.id] = {
                                callback: s=> {
                                    trace("Adding stream from " + msg.role);
                                    video1.src = URL.createObjectURL(s);
                                },
                                stream : s
                            };
                            
                            trace("Adding stream to " + msg.role);
                           

                        }
                        if (msg.role == "subject2") {
                            this.peerManager.Streams[msg.id] = {
                                callback: s=> {
                                    trace("Adding stream from " + msg.role);
                                    video2.src = URL.createObjectURL(s);
                                },
                                stream: s
                            };
                            trace("Adding stream to " + msg.role);
                            
                        }
                        if (msg.role == "subject3") {
                            this.peerManager.Streams[msg.id] = {
                                callback: s=> {
                                    trace("Adding stream from " + msg.role);
                                    video3.src = URL.createObjectURL(s);
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

