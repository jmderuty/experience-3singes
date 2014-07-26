var Experiment;
(function (Experiment) {
    var SubjectAlpha = (function () {
        function SubjectAlpha(video1, video2, video3, localVideo) {
            var _this = this;
            this.stmClient = $.stormancer(Stormancer.Configuration.forAccount("43a32f08-d286-452f-ac8a-f3e84ce992bc", "gladostesting"));
            this.stmClient.getPublicScene("session1", { Role: "alpha" }).then(function (s) {
                _this.scene = s;

                _this.peerManager = new Experiment.PeerManager(_this.scene);
                getUserMedia({ 'video': true, 'audio': true }, function (s) {
                    localVideo.src = URL.createObjectURL(s);

                    _this.scene.onMessage("gamestate.ready", function (msg) {
                        trace("starting establishing connection with " + msg.role);
                        if (_this.onPlayerConnected) {
                            _this.onPlayerConnected(msg.role);
                        }
                        if (msg.role == "subject1") {
                            _this.peerManager.Streams[msg.id] = {
                                callback: function (s) {
                                    trace("Adding stream from " + msg.role);
                                    video1.src = URL.createObjectURL(s);
                                },
                                stream: s
                            };

                            trace("Adding stream to " + msg.role);
                        }
                        if (msg.role == "subject2") {
                            _this.peerManager.Streams[msg.id] = {
                                callback: function (s) {
                                    trace("Adding stream from " + msg.role);
                                    video2.src = URL.createObjectURL(s);
                                },
                                stream: s
                            };
                            trace("Adding stream to " + msg.role);
                        }
                        if (msg.role == "subject3") {
                            _this.peerManager.Streams[msg.id] = {
                                callback: function (s) {
                                    trace("Adding stream from " + msg.role);
                                    video3.src = URL.createObjectURL(s);
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
        return SubjectAlpha;
    })();
    Experiment.SubjectAlpha = SubjectAlpha;
})(Experiment || (Experiment = {}));
//# sourceMappingURL=SubjectAlpha.js.map
