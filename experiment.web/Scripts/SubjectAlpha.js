///<reference path="typings/webrtc/MediaStream.d.ts"/>
///<reference path="typings/stormancer.d.ts"/>
///<reference path="typings/jQuery/jquery.d.ts"/>
///<reference path="communicationPeer.ts"/>
///<reference path="sounds.ts"/>
var Experiment;
(function (Experiment) {
    var SubjectAlpha = (function () {
        function SubjectAlpha(video1, video2, video3, localVideo) {
            var _this = this;
            this.audio = new Experiment.AlphaSounds();
            this.audio.startGeneric.addEventListener('ended', function (ev) {
                _this.audio.start_alpha.play();
                trace("start playing instructions...");
            });
            this.audio.start_alpha.addEventListener('ended', function (ev) {
                trace("updating state...");
                _this.scene.send('state', { state: 1 });
            });
            this.stmClient = $.stormancer(Stormancer.Configuration.forAccount("43a32f08-d286-452f-ac8a-f3e84ce992bc", "gladostesting"));
            this.stmClient.getPublicScene("session1", { Role: "alpha" }).then(function (s) {
                _this.scene = s;
                $('#shocks1').click(function () {
                    _this.scene.send("shock", "subject1");
                });
                $('#shocks2').click(function () {
                    _this.scene.send("shock", "subject2");
                });
                $('#shocks3').click(function () {
                    _this.scene.send("shock", "subject3");
                });

                _this.peerManager = new Experiment.PeerManager(_this.scene);
                getUserMedia({ 'video': true, 'audio': true }, function (s) {
                    //localVideo.src = URL.createObjectURL(s);
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

                    //Shocked
                    _this.scene.onMessage("shock", function () {
                        trace("Shocked!");
                        _this.audio.choc.play();
                    });
                    _this.scene.onMessage("timeleft", function (v) {
                        $("#timeleft").text(v);
                    });
                    _this.scene.onMessage("state", function (i) {
                        if (i == 0) {
                            location.reload();
                        } else if (i == 1) {
                            trace("start playing opening credits...");
                            _this.audio.startGeneric.play();
                        } else if (i == 2) {
                            trace("Test started.");
                            $('#UI').show(1000);
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
