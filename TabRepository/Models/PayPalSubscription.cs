using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TabRepository.Models
{
    public class PayPalSubscription
    {
        public string Id { get; set; }

        public string Description { get; set; }

        public string Json { get; set; }

        public string Status { get; set; }

        public string PlanId { get; set; }

        public string UserId { get;set; }

        public string SubscriptionToken { get; set; }

        public virtual PayPalPlan Plan { get; set; }

        public virtual ApplicationUser User { get; set; }
    }
}
