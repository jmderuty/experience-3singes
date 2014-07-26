///<reference path="typings/webrtc/MediaStream.d.ts"/>
///<reference path="typings/stormancer.d.ts"/>
///<reference path="typings/jQuery/jquery.d.ts"/>
///<reference path="communicationPeer.ts"/>
var Experiment;
(function (Experiment) {
    var SubjectLambda = (function () {
        function SubjectLambda(role, video, localVideo) {
            var _this = this;
            this.stmClient = $.stormancer(Stormancer.Configuration.forAccount("43a32f08-d286-452f-ac8a-f3e84ce992bc", "gladostesting"));
            this.stmClient.getPublicScene("session1", { Role: role }).then(function (s) {
                _this.scene = s;

                _this.peerManager = new Experiment.PeerManager(_this.scene);
                getUserMedia({ 'video': true, 'audio': true }, function (s) {
                    localVideo.src = URL.createObjectURL(s);

                    _this.scene.onMessage("gamestate.ready", function (msg) {
                        trace("starting establishing connection with " + msg.role);
                        if (_this.onPlayerConnected) {
                            _this.onPlayerConnected(msg.role);
                        }
                        if (msg.role == "alpha") {
                            _this.peerManager.Streams[msg.id] = {
                                callback: function (s) {
                                    trace("Adding stream from " + msg.role);
                                    video.src = URL.createObjectURL(s);
                                },
                                stream: s
                            };

                            trace("Adding stream to " + msg.role);
                        }
                    });
                    _this.scene.connect().then(function () {
                        _this.scene.send("start", "");
                        trace("Connected");
                    });
                }, function () {
                });
            });
        }
        return SubjectLambda;
    })();
    Experiment.SubjectLambda = SubjectLambda;
})(Experiment || (Experiment = {}));
//# sourceMappingURL=SubjectLambda.js.map
