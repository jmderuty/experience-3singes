using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Samples.Chat.Models
{
    public class SessionDescription
    {
        public string sdp { get; set; }

        /// <summary>
        /// SDP description type: offer or answer.
        /// </summary>
        public string type { get; set; }
    }
}
