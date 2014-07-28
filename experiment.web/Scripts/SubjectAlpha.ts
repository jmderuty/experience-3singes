///<reference path="typings/webrtc/MediaStream.d.ts"/>
///<reference path="typings/stormancer.d.ts"/>
///<reference path="typings/jQuery/jquery.d.ts"/>
///<reference path="communicationPeer.ts"/>
///<reference path="sounds.ts"/>

module Experiment {

    export class SubjectAlpha {
        private peerManager: PeerManager;
        private stmClient: Stormancer.IClient;
        private scene: Stormancer.IScene;
        private peer1: Peer;
        private peer2: Peer;
        private peer3: Peer;
        private audio: AlphaSounds;
        public constructor(video1: HTMLVideoElement, video2: HTMLVideoElement, video3: HTMLVideoElement, localVideo: HTMLVideoElement) {
            this.audio = new AlphaSounds();
            this.audio.startGeneric.addEventListener('ended', ev=> {
                this.audio.start_alpha.play();
                trace("start playing instructions...");
            });
            this.audio.start_alpha.addEventListener('ended', ev=> {
                trace("updating state...");
                this.scene.send('state', { state: 1 });
            });
            this.stmClient = $.stormancer(Stormancer.Configuration.forAccount("43a32f08-d286-452f-ac8a-f3e84ce992bc", "gladostesting"));
            this.stmClient.getPublicScene("session1", { Role: "alpha" }).then(s=> {
                this.scene = s;
                $('#shocks1').click(() => {
                    this.scene.send("shock", "subject1");
                });
                $('#shocks2').click(() => {
                    this.scene.send("shock", "subject2");
                });
                $('#shocks3').click(() => {
                    this.scene.send("shock", "subject3");
                });

                this.peerManager = new PeerManager(this.scene);
                getUserMedia({ 'video': true, 'audio': true }, s=> {
                    //localVideo.src = URL.createObjectURL(s);

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
                                stream: s
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
                    //Shocked
                    this.scene.onMessage("shock", () => {
                        trace("Shocked!");
                        this.audio.choc.play();
                    });
                    this.scene.onMessage("timeleft", (v) => {
                        $("#timeleft").text(v);
                    });
                    this.scene.onMessage("state", (i) => {
                        if (i == 0) {
                            location.reload();
                        }
                        else if (i == 1) {
                            trace("start playing opening credits...");
                            this.audio.startGeneric.play();

                        }
                        else if (i == 2) {
                            trace("Test started.");
                            $('#UI').show(1000);
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

