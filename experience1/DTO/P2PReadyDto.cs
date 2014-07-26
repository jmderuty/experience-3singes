using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Samples.Chat.DTO
{
    public struct P2PReadyDto
    {
        public int pairId { get; set; }

        public string peerId { get; set; }
    }
}
