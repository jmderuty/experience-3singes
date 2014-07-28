using Stormancer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Samples.Chat.Models
{
    public class Player
    {
        public IConnection Connection { get; set; }
        public int State { get; set; }

        public Action<int> StateChanged { get; set; }
    }
}
