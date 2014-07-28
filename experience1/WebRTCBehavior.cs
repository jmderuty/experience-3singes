using Stormancer.Core;
using Stormancer.Samples.Chat.DTO;
using Stormancer.Samples.Chat.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Stormancer.Samples.Chat
{
    public class WebRTCBehavior : Behavior<Scene>
    {
        NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private HashSet<P2PConnection> _connections = new HashSet<P2PConnection>();
        private Dictionary<IConnection, List<P2PConnection>> _index = new Dictionary<IConnection, List<P2PConnection>>();

        private class PairReadyState
        {
            public string P1;
            public bool P1Ready;

            public string P2;
            public bool P2Ready;

            public TaskCompletionSource<bool> Tcs;
        }
        private Dictionary<int, PairReadyState> _pairInProgress = new Dictionary<int, PairReadyState>();

        private RequestChannel<UpdateSdp> _sdpChannel;
        public IEnumerable<P2PConnection> Connections
        {
            get
            {
                return _connections.AsEnumerable();
            }
        }
        protected override void OnAttached()
        {
            _sdpChannel = new RequestChannel<UpdateSdp>(this.AssociatedObject, "p2p.sdp");
            AssociatedObject.RegisterRoute<P2PReadyDto>("p2p.ready", OnReady);
            AssociatedObject.RegisterRoute<AddCandidate>("p2p.ice", OnIceCandidate);
            AssociatedObject.RegisterRoute<UpdateSdp>("p2p.sdp", OnSdp);
            AssociatedObject.OnDisconnect.Add(OnDisconnect);
        }

        private Task OnDisconnect(IConnection arg)
        {
            return Task.WhenAll(GetP2P(arg).Select(p => CloseConnection(arg.Id + " disconnected from the server", p)));
        }

        private IEnumerable<P2PConnection> GetP2P(IConnection c)
        {
            List<P2PConnection> p2p;
            if (!_index.TryGetValue(c, out p2p))
            {
                p2p = new List<P2PConnection>();
            }
            return p2p.ToArray();
        }

        private object _lock = new object();
        private Task OnReady(RequestMessage<P2PReadyDto> arg)
        {
            Trace("received ready signal from {0} for pair {1}", arg.Content.peerId, arg.Content.pairId.ToString());
            var state = this._pairInProgress[arg.Content.pairId];

            lock (_lock)
            {
                if (state.P1 == arg.Content.peerId)
                {
                    Trace("validated ready signal from {0} for pair {1} 1", arg.Content.peerId, arg.Content.pairId.ToString());
                    state.P1Ready = true;
                }

                if (state.P2 == arg.Content.peerId)
                {
                    Trace("validated ready signal from {0} for pair {1} 2", arg.Content.peerId, arg.Content.pairId.ToString());
                    state.P2Ready = true;
                }
            }
            if (state.P2Ready && state.P1Ready)
            {
                Trace("Pair {0} ready!", arg.Content.pairId.ToString());
                this._pairInProgress.Remove(arg.Content.pairId);
                state.Tcs.SetResult(true);
            }

            return Task.FromResult(true);
        }

        private Task OnIceCandidate(RequestMessage<AddCandidate> rq)
        {
            var remotePeer = this.AssociatedObject.Connections.FirstOrDefault(c => c.Id == rq.Content.destination);
            Trace("Sending Ice candidate from {0} to {1}", rq.Connection.Id, rq.Content.destination);
            return remotePeer.Send("p2p.ice", rq.Content);

        }



        private async Task OnSdp(RequestMessage<UpdateSdp> rq)
        {
            var remotePeer = this.AssociatedObject.Connections.FirstOrDefault(c => c.Id == rq.Content.destination);
            Trace("Sending SDP offer from {0} to {1}", rq.Connection.Id, rq.Content.destination);
            var answer = await _sdpChannel.Send(remotePeer, rq.Content);
            Trace("Received SDP answer from {0} to {1}", remotePeer.Id, rq.Connection.Id);
            await rq.SendResponse(answer);
        }

        protected override void OnDetached()
        {

        }
        int _id;
        private int GenerateId()
        {
            return Interlocked.Increment(ref _id);
        }

        private void AddP2P(P2PConnection connection)
        {
            _connections.Add(connection);

            List<P2PConnection> l;
            if (!_index.TryGetValue(connection.Peer1, out l))
            {
                l = new List<P2PConnection>();
                _index[connection.Peer1] = l;
            }
            l.Add(connection);


            if (!_index.TryGetValue(connection.Peer2, out l))
            {
                l = new List<P2PConnection>();
                _index[connection.Peer2] = l;
            }
            l.Add(connection);

        }
        private void RemoveP2P(P2PConnection connection)
        {
            _connections.Remove(connection);
            List<P2PConnection> l;
            if (_index.TryGetValue(connection.Peer1, out l))
            {
                l.Remove(connection);
            }
            if (_index.TryGetValue(connection.Peer2, out l))
            {
                l.Remove(connection);
            }

        }
        public async Task<P2PConnection> CreateConnection(IConnection p1, IConnection p2)
        {
            if (p1 == null)
            {
                throw new ArgumentNullException("p1");
            }
            if (p2 == null)
            {
                throw new ArgumentNullException("p2");
            }
            var connection = new P2PConnection(p1, p2, GenerateId());
            var tcs = new TaskCompletionSource<bool>();
            _pairInProgress.Add(connection.PairId, new PairReadyState { P1 = p1.Id, P2 = p2.Id, Tcs = tcs });
            logger.Trace("Added Pair in progress " + connection.PairId);
            await connection.Peer1.Send("p2p.opening", new P2POpeningDto { pairId = connection.PairId, isMasterPeer = true, remotePeer = p2.Id, localPeer = p1.Id });
            await connection.Peer2.Send("p2p.opening", new P2POpeningDto { pairId = connection.PairId, isMasterPeer = false, remotePeer = p1.Id, localPeer = p2.Id });

            await tcs.Task;
            AddP2P(connection);
            logger.Trace("Created connection " + connection.PairId);
            
            await connection.Peer1.Send("p2p.start", new P2PStartDto { pairId = connection.PairId, remotePeer = p2.Id });
            await connection.Peer2.Send("p2p.start", new P2PStartDto { pairId = connection.PairId, remotePeer = p1.Id });
            return connection;

        }
        private async Task CloseConnection(string reason, P2PConnection connection)
        {
            logger.Trace("Closing connection " + connection);
            if (connection.Status == P2PConnection.P2PStatus.Closing || connection.Status == P2PConnection.P2PStatus.Closed)
            {
                return;
            }
            connection.Status = P2PConnection.P2PStatus.Closing;
            var tasks = new List<Task>();
            if (this.AssociatedObject.Connections.Contains(connection.Peer1))
            {
                tasks.Add(connection.Peer1.Send("p2p.close", new P2PClosingDto { reason = reason, remotePeer = connection.Peer2.Id }));
            }
            if (this.AssociatedObject.Connections.Contains(connection.Peer2))
            {
                tasks.Add(connection.Peer2.Send("p2p.close", new P2PClosingDto { reason = reason, remotePeer = connection.Peer1.Id }));
            }

            RemoveP2P(connection);
            
            await Task.WhenAll(tasks);

            connection.Status = P2PConnection.P2PStatus.Closed;

        }

        private void Trace(string text, params string[] args)
        {
            logger.Trace(string.Format(text, args));
        }
    }
}
