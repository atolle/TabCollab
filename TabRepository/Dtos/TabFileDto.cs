using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TabRepository.Dtos
{
    public class TabFileDto
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public byte[] TabData { get; set; }

        public DateTime DateCreated { get; set; }
    }
}
