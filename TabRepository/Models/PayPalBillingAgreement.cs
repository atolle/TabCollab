using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TabRepository.Models
{
    public class PayPalBillingAgreement
    {
        public string Token { get; set; }

        public string Id { get; set; }

        public string Description { get; set; }

        public string Json { get; set; }

        public string State { get; set; }

        public string ExecuteURL { get; set; }

        public string RequestToken { get; set; }

        // This is stored separately from the Id because we won't have the BillingAgreementId until the
        // BillingAgreement is executed by the user. It's created first, then executed.
        public string BillingAgreementId { get; set; }

        public string PlanId { get; set; }

        public string UserId { get; set; }

        public virtual PayPalBillingPlan Plan { get; set; }

        public virtual ApplicationUser User { get; set; }
    }
}
