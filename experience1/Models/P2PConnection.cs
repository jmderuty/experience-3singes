using Stormancer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Samples.Chat.Models
{
    public struct P2PConnection
    {
        public P2PConnection(IConnection p1, IConnection p2,int pairId)
        {
            if (p1 == null)
            {
                throw new ArgumentNullException("p1");
            }
            if (p2 == null)
            {
                throw new ArgumentNullException("p2");
            }

          
            Peer1 = p1;
            Peer2 = p2;
            Status = P2PStatus.Closed;
            PairId = pairId;
        }
        public enum P2PStatus
        {
            Opening,
            Open,
            Closing,
            Closed
        }

        public P2PStatus Status;
        public IConnection Peer1;

        public IConnection Peer2;

        public int PairId;
        public override bool Equals(object obj)
        {
            var c2 = (P2PConnection)obj;


            if (c2.Peer1.Id != this.Peer1.Id && c2.Peer1.Id != this.Peer2.Id)
            {
                return false;
            }
            if (c2.Peer2.Id != this.Peer2.Id && c2.Peer2.Id != this.Peer1.Id)
            {
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            return "{" + Peer1.Id + "," + Peer2.Id + "}";
        }

        public override int GetHashCode()
        {
            return Peer1.Id.GetHashCode() * 7 + Peer2.Id.GetHashCode();
        }
    }

}
