using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace TabRepository.Models
{
    public class StripeTaxRate
    {
        public string Id { get; set; }

        public string SubscriptionId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Percentage { get; set; }

        public string StripeDescription { get; set; }

        public string StripeJurisdicion { get; set; }

        public string State { get; set; }

        public string Zip { get; set; }

        public virtual StripeSubscription Subscription { get; set; }

        public virtual ICollection<StripeInvoice> Invoices { get; set; }
    }
}
