using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TabRepository.Models;

namespace TabRepository.ViewModels
{
    public class TabIndexViewModel
    {
        public List<ProjectIndexViewModel> ProjectIndexViewModels { get; set; }

        public bool TabTutorialShown { get; set; }

        public bool AllowNewTabs { get; set; }

        public bool SubscriptionExpired { get; set; }

        public DateTime? SubscriptionExpiration { get; set; }
    }
}