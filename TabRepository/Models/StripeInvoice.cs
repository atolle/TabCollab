using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TabRepository.Models
{
    public class StripeInvoice
    {
        public string Id { get; set; }

        public string SubscriptionId { get; set; }

        public string ChargeId { get; set; }

        public double Subtotal { get; set; }

        public double Tax { get; set; }

        public string ReceiptURL { get; set; }

        public DateTime DateCreated { get; set; }

        public DateTime? DateDue { get; set; }

        public DateTime? DatePaid { get; set; }

        public PaymentStatus PaymentStatus { get; set; }

        public string PaymentStatusText { get; set; }

        public virtual StripeSubscription Subscription { get; set; }
    }

    public enum PaymentStatus
    {
        Unpaid,
        Paid,
        Failed
    }
}
