using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TabRepository.Models
{
    public class StripePlan
    {
        public string Id { get; set; }

        public string Nickname { get; set; }

        public string ProductId { get; set; }

        public virtual StripeProduct Product { get; set; }

        public virtual ICollection<StripeSubscription> Subscriptions { get; set; }
    }
}
