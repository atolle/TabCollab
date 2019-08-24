using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TabRepository.ViewModels
{
    public class TabSelectionViewModel
    {
        public int AlbumId { get; set; }

        public List<TabViewModel> Tabs { get; set; }
    }
}
