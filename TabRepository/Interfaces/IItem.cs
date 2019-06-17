using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TabRepository.Interfaces
{
    public interface IItem
    {
        int Id { get; set; }

        string UserId { get; set; }
    }
}
