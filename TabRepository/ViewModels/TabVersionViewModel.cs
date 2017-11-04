using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TabRepository.Models;

namespace TabRepository.ViewModels
{
    public class TabVersionViewModel
    {
        public TabVersion TabVersion { get; set; }

        public bool IsOwner { get; set; }       
    }
}
