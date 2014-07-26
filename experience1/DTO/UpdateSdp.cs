using Stormancer.Samples.Chat.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Samples.Chat.DTO
{
    public class UpdateSdp
    {
        public UpdateSdp() { }
        public UpdateSdp(string origin, string destination, SessionDescription description)
        {
            this.origin = origin;
            this.destination = destination;
            sdp = description;
        }
      
        public SessionDescription sdp { get; set; }

        public string origin { get; set; }

        public string destination { get; set; }
    }

    public class AddCandidate
    {
        public AddCandidate() { }
        public AddCandidate(string origin, string destination, Candidate candidate)
        {
            this.origin = origin;
            this.destination = destination;
            this.candidate = candidate;
        }

        public string origin { get; set; }
        public string destination { get; set; }

        public Candidate candidate { get; set; }
    }
}
