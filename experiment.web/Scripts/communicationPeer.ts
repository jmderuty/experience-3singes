///<reference path="typings/webrtc/RTCPeerConnection.d.ts"/>
///<reference path="typings/webrtc/MediaStream.d.ts"/>
///<reference path="typings/stormancer.d.ts"/>
///<reference path="typings/jQuery/jquery.d.ts"/>
declare function getUserMedia(constraints: any, success: (s: MediaStream) => void, failure: () => void): void;
declare function trace(text: string): void;
module Experiment {
    export class Peer {
        public remoteId: string;
        public localId: string;
        private scene: Stormancer.IScene;
        private isMaster: boolean;
        private callback: (stream: MediaStream) => void;

        public constructor(localId: string, remoteId: string, isMaster: boolean, scene: Stormancer.IScene,streamConfig: StreamConfiguration) {
            this.remoteId = remoteId;
            this.localId = localId;
            this.scene = scene;

            this.isMaster = isMaster;
            this.peer = new RTCPeerConnection(null);
            if (streamConfig) {
                this.peer.addStream(streamConfig.stream);
                this.callback = streamConfig.callback;
            }
            this.scene.onMessage("p2p.ice", data=> {
                if (data.origin = this.remoteId) {
                    trace("received ICE candidate from " + this.remoteId);
                    if (data.candidate) {
                        this.peer.addIceCandidate(new RTCIceCandidate(data.candidate));
                    }
                }
            });

            this.scene.onMessage("p2p.sdp.request", msg=> {
                var data = msg.data;
                if (data.origin == this.remoteId && !this.isMaster) {
                    trace("received SDP offer from " + this.remoteId);
                    this.peer.setRemoteDescription(new RTCSessionDescription(data.sdp));

                    this.peer.createAnswer(sdp=> {
                        this.peer.setLocalDescription(sdp);
                        msg.data = { origin: this.localId, destination: this.remoteId, sdp: sdp };
                        trace("sent SDP answer to " + this.remoteId);
                        this.scene.send("p2p.sdp.answer", msg);

                    }, this.OnFailure);
                }
            });

        }

        public close(): void {
            this.peer.close();
            this.peer = null;
        }
        private peer: webkitRTCPeerConnection;

        public AddStream(stream: MediaStream): void {
            if (this.peer == null) {
                throw "Peer closed";
            }
            this.peer.addStream(stream);

        }

        public OnAddStream(callback: (stream: MediaStream) => void) {
            this.callback = callback;
        }


        public start(): void {
            if (this.peer == null) {
                throw "Peer closed";
            }



            this.peer.onaddstream = e=> {
                this.callback(e.stream);
            };
            this.peer.onicecandidate = e=> {
                trace("sending ICE candidate to " + this.remoteId);
                this.scene.send("p2p.ice", { origin: this.localId, destination: this.remoteId, candidate: e.candidate });
            };

            if (this.isMaster) {
                this.peer.createOffer(description=> {
                    this.peer.setLocalDescription(description);
                    trace("sending SDP offer to " + this.remoteId);
                    this.scene.sendRequest("p2p.sdp", { origin: this.localId, destination: this.remoteId, sdp: description },
                        answer=> {
                            trace("received SDP answer from " + this.remoteId);
                            this.peer.setRemoteDescription(new RTCSessionDescription(answer.sdp));
                        }, () => { });
                }, this.OnFailure);
            }


        }

        private OnFailure(event: any): void {
        }
    }
    export class StreamConfiguration {
        stream: MediaStream;
        callback: (MediaStream) => void
    }
    export class PeerManager {

        private scene: Stormancer.IScene;

        public Peers: { [remoteId: string]: Peer };

        public Streams: { [remoteId: string]: StreamConfiguration };

        public onPeerAdded: (remoteId: string, peer: Peer) => void;
        public onPeerRemoved: (remoteId: string, reason: string) => void;

        public constructor(scene: Stormancer.IScene) {
            this.Peers = {};
            this.Streams = {};
            this.scene = scene;
            scene.onMessage("p2p.opening", msg=> {
                trace("I am " + msg.localPeer);
                trace("opening P2P connection with " + msg.remotePeer);
                var peer = new Peer(msg.localPeer, msg.remotePeer, msg.isMasterPeer, this.scene,this.Streams[msg.remotePeer]);
                this.Peers[msg.remotePeer] = peer;
                var answer = { pairId: msg.pairId, peerId: msg.localPeer };
                trace("Sending ready state." + JSON.stringify(answer));
                scene.send("p2p.ready", answer);
            });
            scene.onMessage("p2p.start", msg=> {
                var remotePeer = msg.remotePeer;
                var peer = this.Peers[remotePeer];
                trace("P2P connection synchronized with " + msg.remotePeer);
                peer.start();
                if (this.onPeerAdded) {
                    this.onPeerAdded(msg.remotePeer, peer);
                }
            });
            scene.onMessage("p2p.close", msg=> {
                var peer = this.Peers[msg.remotePeer];
                if (peer) {
                    trace("closing P2P connection with " + msg.remotePeer);
                    peer.close();
                    delete this.Peers[msg.RemotePeer];
                }
                if (this.onPeerRemoved) {
                    this.onPeerRemoved(msg.remotePeer, msg.reason);
                }
            });
        }
    }
}