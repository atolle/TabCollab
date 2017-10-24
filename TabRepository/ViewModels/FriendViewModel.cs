using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TabRepository.Models;

namespace TabRepository.ViewModels
{
    public class FriendViewModel
    {
        public string FromUsername { get; set; }

        public string ToUsername { get; set; }

        public string Username { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public FriendStatus Status { get; set; }

        public Direction Direction { get; set; }

        public bool IsCurrentUser { get; set; }
    }
}
