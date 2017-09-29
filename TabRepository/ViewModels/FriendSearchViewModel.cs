using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TabRepository.Models;

namespace TabRepository.ViewModels
{
    public class FriendSearchViewModel
    {
        public string Username { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public FriendStatus Status { get; set; }
    }
}
