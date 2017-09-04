using System.Collections.Generic;
using TabRepository.Models;

namespace TabRepository.ViewModels
{
    public class TabVersionIndexViewModel
    {
        public IEnumerable<TabVersion> TabVersions { get; set; }

        public string TabName { get; set; }

        public int TabId { get; set; }

        public int ProjectId { get; set; }
    }
}