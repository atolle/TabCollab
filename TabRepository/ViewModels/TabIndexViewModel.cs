using System.Collections.Generic;
using TabRepository.Models;

namespace TabRepository.ViewModels
{
    public class TabIndexViewModel
    {
        public IEnumerable<Tab> Tabs { get; set; }

        public string ProjectName { get; set; }

        public int ProjectId { get; set; }
    }
}