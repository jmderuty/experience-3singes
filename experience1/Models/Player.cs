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
        public Player()
        {
            Candidates = new List<Candidate>();
        }

        public Player(IConnection connection)
            : this()
        {

            this.Connection = connection;
        }


        public IConnection Connection { get; set; }

        public string Role
        {
            get
            {
                return User.Role;
            }
        }

        public List<Candidate> Candidates { get; set; }

        public SessionDescription Description { get; set; }

       
        public User User
        {
            get
            {
                return Connection.GetUserData<User>();
            }
        }
    }
}
