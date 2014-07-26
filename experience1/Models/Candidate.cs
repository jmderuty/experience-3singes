using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Samples.Chat.Models
{
    public class Candidate
    {
        public string candidate { get; set; }
        public int sdpMLineIndex { get; set; }
        public string sdpMid { get; set; }
    }
}
