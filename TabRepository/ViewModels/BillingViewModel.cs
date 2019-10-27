using System;
using TabRepository.Models;

namespace TabRepository.ViewModels
{
    public class BillingViewModel
    {
        public double Subtotal { get; set; }

        public double Tax { get; set; }

        public string ReceiptURL { get; set; }

        public DateTime DateCreated { get; set; }

        public DateTime? DateDue { get; set; }

        public DateTime? DatePaid { get; set; }

        public PaymentStatus PaymentStatus { get; set; }

        public string PaymentStatusText { get; set; }

        public SubscriptionInterval Interval { get; set; }
    }
}
