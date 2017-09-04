using System;

namespace TabRepository.Models
{
    public class TabFile
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public byte[] TabData { get; set; }

        public DateTime DateCreated { get; set; }

        public virtual TabVersion TabVersion { get; set; }
    }
}