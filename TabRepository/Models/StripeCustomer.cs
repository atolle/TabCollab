using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TabRepository.Models
{
    public class StripeCustomer
    {
        public string Id { get; set; }

        public string UserId { get; set; }

        public virtual ApplicationUser User { get; set; }

        public virtual ICollection<StripeSubscription> Subscriptions { get; set; }

        public virtual ICollection<StripeInvoice> Invoice { get; set; }
    }
}
