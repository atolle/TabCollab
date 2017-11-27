using System.Collections.Generic;
using TabRepository.Models;

namespace TabRepository.ViewModels
{
    public class TabVersionIndexViewModel
    {
        public IEnumerable<TabVersionViewModel> TabVersions { get; set; }

        public string TabName { get; set; }

        public int TabId { get; set; }

        public int AlbumId { get; set; }
    }
}