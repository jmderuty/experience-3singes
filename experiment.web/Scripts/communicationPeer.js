///<reference path="typings/webrtc/RTCPeerConnection.d.ts"/>
///<reference path="typings/webrtc/MediaStream.d.ts"/>
///<reference path="typings/stormancer.d.ts"/>
///<reference path="typings/jQuery/jquery.d.ts"/>

var Experiment;
(function (Experiment) {
    var Peer = (function () {
        function Peer(localId, remoteId, isMaster, scene, streamConfig) {
            var _this = this;
            this.remoteId = remoteId;
            this.localId = localId;
            this.scene = scene;

            this.isMaster = isMaster;
            this.peer = new RTCPeerConnection(null);
            if (streamConfig) {
                this.peer.addStream(streamConfig.stream);
                this.callback = streamConfig.callback;
            }
            this.scene.onMessage("p2p.ice", function (data) {
                if (data.origin == _this.remoteId) {
                    trace("received ICE candidate from " + _this.remoteId);
                    try  {
                        if (data.candidate) {
                            _this.peer.addIceCandidate(new RTCIceCandidate(data.candidate));
                        }
                    } catch (e) {
                        trace(e);
                    }
                }
            });

            this.scene.onMessage("p2p.sdp.request", function (msg) {
                var data = msg.data;
                if (data.origin == _this.remoteId && !_this.isMaster) {
                    trace("received SDP offer from " + _this.remoteId);
                    _this.peer.setRemoteDescription(new RTCSessionDescription(data.sdp));

                    _this.peer.createAnswer(function (sdp) {
                        _this.peer.setLocalDescription(sdp);
                        msg.data = { origin: _this.localId, destination: _this.remoteId, sdp: sdp };
                        trace("sent SDP answer to " + _this.remoteId);
                        _this.scene.send("p2p.sdp.answer", msg);
                    }, _this.OnFailure);
                }
            });
        }
        Peer.prototype.close = function () {
            this.peer.close();
            this.peer = null;
        };

        Peer.prototype.AddStream = function (stream) {
            if (this.peer == null) {
                throw "Peer closed";
            }
            this.peer.addStream(stream);
        };

        Peer.prototype.OnAddStream = function (callback) {
            this.callback = callback;
        };

        Peer.prototype.start = function () {
            var _this = this;
            if (this.peer == null) {
                throw "Peer closed";
            }

            this.peer.onaddstream = function (e) {
                _this.callback(e.stream);
            };
            this.peer.onicecandidate = function (e) {
                trace("sending ICE candidate to " + _this.remoteId);
                _this.scene.send("p2p.ice", { origin: _this.localId, destination: _this.remoteId, candidate: e.candidate });
            };
            var deferred = $.Deferred();
            if (this.isMaster) {
                this.peer.createOffer(function (description) {
                    _this.peer.setLocalDescription(description);
                    trace("sending SDP offer to " + _this.remoteId);
                    _this.scene.sendRequest("p2p.sdp", { origin: _this.localId, destination: _this.remoteId, sdp: description }, function (answer) {
                        trace("received SDP answer from " + _this.remoteId);
                        _this.peer.setRemoteDescription(new RTCSessionDescription(answer.sdp));
                        deferred.resolve();
                    }, function () {
                    });
                }, this.OnFailure);
            } else {
                deferred.resolve();
            }
            return deferred.promise();
        };

        Peer.prototype.OnFailure = function (event) {
            console.error(event);
        };
        return Peer;
    })();
    Experiment.Peer = Peer;
    var StreamConfiguration = (function () {
        function StreamConfiguration() {
        }
        return StreamConfiguration;
    })();
    Experiment.StreamConfiguration = StreamConfiguration;
    var PeerManager = (function () {
        function PeerManager(scene) {
            var _this = this;
            this.Peers = {};
            this.Streams = {};
            this.scene = scene;
            this.tasks = new Array();
            scene.onMessage("p2p.opening", function (msg) {
                trace("I am " + msg.localPeer);
                trace("opening P2P connection with " + msg.remotePeer);
                var peer = new Peer(msg.localPeer, msg.remotePeer, msg.isMasterPeer, _this.scene, _this.Streams[msg.remotePeer]);
                _this.Peers[msg.remotePeer] = peer;
                var answer = { pairId: msg.pairId, peerId: msg.localPeer };
                trace("Sending ready state." + JSON.stringify(answer));
                scene.send("p2p.ready", answer);
            });
            scene.onMessage("p2p.start", function (msg) {
                var remotePeer = msg.remotePeer;
                var peer = _this.Peers[remotePeer];
                trace("P2P connection synchronized with " + msg.remotePeer);
                _this.runTask(function () {
                    return peer.start();
                });
                if (_this.onPeerAdded) {
                    _this.onPeerAdded(msg.remotePeer, peer);
                }
            });
            scene.onMessage("p2p.close", function (msg) {
                var peer = _this.Peers[msg.remotePeer];
                if (peer) {
                    trace("closing P2P connection with " + msg.remotePeer);
                    peer.close();
                    delete _this.Peers[msg.RemotePeer];
                }
                if (_this.onPeerRemoved) {
                    _this.onPeerRemoved(msg.remotePeer, msg.reason);
                }
            });
        }
        PeerManager.prototype.runTask = function (task) {
            this.tasks.push(task);
            if (!this._taskRunning) {
                this.runTaskLoop();
            }
        };
        PeerManager.prototype.runTaskLoop = function () {
            var _this = this;
            this._taskRunning = true;
            var task = this.tasks.pop();
            if (task != null) {
                task().then(function () {
                    _this.runTaskLoop();
                });
            } else {
                this._taskRunning = false;
            }
        };
        return PeerManager;
    })();
    Experiment.PeerManager = PeerManager;
})(Experiment || (Experiment = {}));
//# sourceMappingURL=communicationPeer.js.map
