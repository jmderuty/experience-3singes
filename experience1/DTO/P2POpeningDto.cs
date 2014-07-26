using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Samples.Chat.DTO
{
    public struct P2POpeningDto
    {
        public string remotePeer;
        public string localPeer;
        public int pairId;

        public bool isMasterPeer;
    }
}
