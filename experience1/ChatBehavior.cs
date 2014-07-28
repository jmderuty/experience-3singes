using Stormancer.Core;
using Stormancer.Samples.Chat.DTO;
using Stormancer.Samples.Chat.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Stormancer.Samples.Chat
{
    public class ChatBehavior : Behavior<Scene>
    {
        NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private Dictionary<string, Player> _players = new Dictionary<string, Player>();
        //private Player GetPlayer(IConnection connection)
        //{
        //    return _players.Values.FirstOrDefault(p => p.Connection == connection);
        //}

        #region Behavior
        protected override void OnAttached()
        {
            AssociatedObject.RegisterRoute<string>("message", MessageSent);
            AssociatedObject.RegisterApiRoute<string>("action", OnAdminAction);
            AssociatedObject.RegisterApiRoute<string, GameState>("getGameState", OnAdminRequestGameState);
            //AssociatedObject.RegisterRoute<Candidate>("candidate", OnCandidate);
            //AssociatedObject.RegisterRoute<SessionDescription>("sdp", OnSdp);
            AssociatedObject.RegisterRoute<StateUpdate>("state", OnStart);
            AssociatedObject.RegisterRoute<string>("shock", OnShock);
            AssociatedObject.OnConnect.Add(OnConnect);
            AssociatedObject.OnDisconnect.Add(OnDisconnect);
            AssociatedObject.OnStarting.Add(OnStarting);
            AssociatedObject.OnShutdown.Add(OnShutDown);
        }

        private Task OnShock(RequestMessage<string> arg)
        {
            if (_players["alpha"].Connection.Id == arg.Connection.Id)
            {
                logger.Trace("alpha shocked " + arg);
            }
            var target = _players[arg.Content];
            _shocked = true;
            _shocks.Add(new Choc { Target = arg.Content, Date = _currentTimeLeft });
            return target.Connection.Send("shock", true);
        }

        private List<Choc> _shocks = new List<Choc>();
        private Task<GameState> OnAdminRequestGameState(string arg)
        {

            var state = new GameState
            {
                Chocs = _shocks,
                AlphaConnected = _players.ContainsKey("alpha"),
                Subject1Connected = _players.ContainsKey("subject1"),
                Subject2Connected = _players.ContainsKey("subject2"),
                Subject3Connected = _players.ContainsKey("subject3"),
                State = this.gameState
            };
            return Task.FromResult(state);
        }

        private Task OnAdminAction(string action)
        {
            if (action == "stop")
            {
                if (_gameCTS != null && !_gameCTS.IsCancellationRequested)
                {
                    _gameCTS.Cancel();

                }
                _isGameRunning = false;
                gameState = null;
            }
            if (action == "start")
            {
                _gameCTS = new CancellationTokenSource();
                var _ = Task.Run(() => RunGame(_gameCTS.Token));
            }
            return Task.FromResult(true);
        }
        public struct StateUpdate
        {
            public int state;
        }
        private Task OnStart(RequestMessage<StateUpdate> arg)
        {
            var user = arg.Connection.GetUserData<User>();
            var player = this._players[user.Role].State = arg.Content.state;
            Trace("{0} updated its state to {1}", user.Role, arg.Content.ToString());
            return Task.FromResult(true);
        }

        //private Task OnSdp(RequestMessage<SessionDescription> rq)
        //{
        //    var player = GetPlayer(rq.Connection);
        //    player.Description = rq.Content;
        //    if (player.Role == "alpha")
        //    {
        //        return Task.WhenAll(_players.Values.Where(p => p.Role != "alpha").Select(p => p.Connection.Send("sdp", new[] { new UpdateSdp(player) })));

        //    }
        //    else
        //    {
        //        Player alpha;
        //        if (_players.TryGetValue("alpha",out alpha))
        //        {
        //            return alpha.Connection.Send("sdp", new[] { new UpdateSdp(player) });
        //        }
        //        else
        //        {
        //            return Task.FromResult(false);
        //        }
        //    }
        //}


        //private Task OnCandidate(RequestMessage<Candidate> rq)
        //{

        //    var player = GetPlayer(rq.Connection);
        //    player.Candidates.Add(rq.Content);
        //    logger.Trace("Adding candidates for " + player.Role + " candidate: " + rq.Content.candidate);
        //    if (player.Role == "alpha")
        //    {
        //        return Task.WhenAll(_players.Values.Where(p => p.Role != "alpha").Select(p => p.Connection.Send("candidate.added", new[] { new AddCandidate(player, rq.Content) })));

        //    }
        //    else
        //    {
        //        Player alpha;
        //        if (_players.TryGetValue("alpha", out alpha))
        //        {
        //            return alpha.Connection.Send("candidate.add", new[] { new AddCandidate(player, rq.Content) });
        //        }
        //        else
        //        {
        //            return Task.FromResult(false);
        //        }
        //    }


        //}

        private WebRTCBehavior _p2pManager;
        private async Task OnStarting(string parameter)
        {
            _p2pManager = this.AssociatedObject.Behaviors.OfType<WebRTCBehavior>().First();
        }

        private async Task OnShutDown(Core.Infrastructure.ShutdownArgument arg)
        {
            if (arg.HasData)
            {
                var data = arg.GetData<Message>();
                //do your shutdown logic
            }
        }

        protected override void OnDetached()
        {
            AssociatedObject.OnConnect.Remove(OnConnect);
            AssociatedObject.OnDisconnect.Remove(OnDisconnect);
        }
        #endregion

        private async Task MessageSent(RequestMessage<string> message)
        {
            var user = message.Connection.GetUserData<User>();
            var msg = new Message { Text = message.Content, UserId = user.Role };
            await message.SendResponse("Broadcasting " + message.Content);

            await AssociatedObject.Broadcast("message", msg);
        }

        private async Task OnConnect(IConnection connection)
        {
            await connection.Send("user.Add", this.AssociatedObject.Connections
                .Where(con => con != connection)
                .Select(con => con.GetUserData<User>()).ToArray());

            await AssociatedObject.Broadcast("user.Add", new[] { connection.GetUserData<User>() });

            this._players[connection.GetUserData<User>().Role] = new Player { Connection = connection };



        }
        private Dictionary<int, P2PConnection> _p2p = new Dictionary<int, P2PConnection>();


        private Task TryStartConnections()
        {
            return Task.WhenAll(StartSubject(1), StartSubject(2), StartSubject(3));
        }
        private async Task StartSubject(int id)
        {

            var subjectId = "subject" + id;

            if (!this._p2p.ContainsKey(id) && _players.ContainsKey("alpha") && _players.ContainsKey(subjectId))//Si alpha & subject i connectés mais connexion non établie...
            {
                var alpha = _players["alpha"].Connection;
                var subject = _players[subjectId].Connection;
                Trace("Creating P2P connection {0}<->{1}", alpha.Id, subject.Id);

                Trace("Sending gameState.Ready to {0}<->{1}", alpha.Id, subject.Id);
                await Task.WhenAll(
                    alpha.Send("gamestate.ready", new GameStateChanged { id = subject.Id, role = subjectId }),
                    subject.Send("gamestate.ready", new GameStateChanged { id = alpha.Id, role = "alpha" }));
                this._p2p[id] = await _p2pManager.CreateConnection(alpha, subject);
            }


        }


        //private async Task StopSubject(int id)
        //{
        //    var subjectId = "subject" + id;
        //    if (this._p2p.ContainsKey(id) && _players.ContainsKey("alpha") && _players.ContainsKey(subjectId))//Si alpha & subject i connectés mais connexion non établie...
        //    {
        //        var alpha = _players["alpha"];
        //        var subject = _players[subjectId];
        //        this._p2p.Remove(id);

        //        await Task.WhenAll(
        //            alpha.Send("gameState.stop", new GameStateChanged { id = subject.Id, role = subjectId }),
        //            subject.Send("gameState.stop", new GameStateChanged { id = alpha.Id, role = "alpha" }));
        //    }
        //}




        private async Task OnDisconnect(IConnection connection)
        {
            var data = connection.GetUserData<User>();
            //var player = GetPlayer(connection);
            _players.Remove(data.Role);
            foreach (var p2p in _p2p.Where(p => p.Value.Peer1.Id == connection.Id || p.Value.Peer2.Id == connection.Id).ToArray())
            {
                _p2p.Remove(p2p.Key);
            }
            await AssociatedObject.Broadcast("user.Remove", data.Role);
        }

        private bool _isGameRunning;
        private void Trace(string text, params string[] args)
        {
            logger.Trace(string.Format(text, args));
        }

        private string gameState;
        private string _currentTimeLeft;
        public async Task RunGame(CancellationToken token)
        {
            try
            {
                if (_isGameRunning)
                {
                    return;
                }
                logger.Trace("Starting game");
                _isGameRunning = true;
                await this.AssociatedObject.Broadcast("state", 0);
                await Task.Delay(1000);
                _shocks.Clear();
                gameState = "En attente des sujets de test";
                logger.Trace("Waiting for test subjects");
                await AllPlayerConnected(token);
                token.ThrowIfCancellationRequested();
                gameState = "Générique de debut";
                logger.Trace("playing opening credits");
                await AllPlayerInState(1,token);//Tous les joueurs ont terminé le generique.
                logger.Trace("Connexions P2P");
                token.ThrowIfCancellationRequested();

                await TryStartConnections();
                logger.Trace("Debut de la partie");
                gameState = "Test en cours...";
                await this.AssociatedObject.Broadcast("state", 2);
                while (!token.IsCancellationRequested)
                {
                    var end = DateTime.UtcNow.AddMinutes(10);
                    DateTime current;
                    while ((current = DateTime.UtcNow) < end && !token.IsCancellationRequested)
                    {
                        _currentTimeLeft = (end - current).ToString(@"mm\:ss");
                        await this.AssociatedObject.Broadcast("timeleft", _currentTimeLeft);
                        await Task.Delay(1000);
                        if (_shocked)
                        {
                            break;
                        }
                    }
                    if (!_shocked && !token.IsCancellationRequested)
                    {
                        _shocks.Add(new Choc { Date = _currentTimeLeft, Target = "Tous" });
                        await this.AssociatedObject.Broadcast("shock", true);//Choque tout le monde
                    }
                    _shocked = false;
                }


                gameState = "Test terminé.";
            }
            catch (Exception ex)
            {
                logger.Error("Game stopped", ex);
            }
            finally
            {
                gameState = null;
                _isGameRunning = false;
            }
        }
        private bool _shocked;

        private CancellationTokenSource _gameCTS;

        private async Task AllPlayerInState(int i,CancellationToken token)
        {
            this.AssociatedObject.Broadcast("state", i);
            while (this._players.Values.Any(p => p.State < i) && !token.IsCancellationRequested)
            {
                await Task.Delay(1000, token);
            }

        }


        private async Task AllPlayerConnected(CancellationToken token)
        {
            Trace(_players.Count.ToString());
            while (_players.Count < 4 && !token.IsCancellationRequested)
            {
                Trace(_players.Count.ToString());
                await Task.Delay(1000, token);
            }

        }
    }
}
