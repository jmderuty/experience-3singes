using Stormancer.Core;
using Stormancer.Samples.Chat.DTO;
using Stormancer.Samples.Chat.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Stormancer.Samples.Chat
{
    public class ChatBehavior : Behavior<Scene>
    {
        NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private Dictionary<string, IConnection> _players = new Dictionary<string, IConnection>();
        //private Player GetPlayer(IConnection connection)
        //{
        //    return _players.Values.FirstOrDefault(p => p.Connection == connection);
        //}

        #region Behavior
        protected override void OnAttached()
        {
            AssociatedObject.RegisterRoute<string>("message", MessageSent);
            //AssociatedObject.RegisterRoute<Candidate>("candidate", OnCandidate);
            //AssociatedObject.RegisterRoute<SessionDescription>("sdp", OnSdp);
            AssociatedObject.RegisterRoute<string>("start", OnStart);
            AssociatedObject.OnConnect.Add(OnConnect);
            AssociatedObject.OnDisconnect.Add(OnDisconnect);
            AssociatedObject.OnStarting.Add(OnStarting);
            AssociatedObject.OnShutdown.Add(OnShutDown);
        }

        private Task OnStart(RequestMessage<string> arg)
        {
            return this.TryStartConnections();
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

            this._players[connection.GetUserData<User>().Role] = connection;

            

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
                var alpha = _players["alpha"];
                var subject = _players[subjectId];
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

        private void Trace(string text, params string[] args)
        {
            logger.Trace(string.Format(text, args));
        }
    }
}
