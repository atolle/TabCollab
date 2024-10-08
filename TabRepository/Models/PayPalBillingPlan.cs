﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TabRepository.Models
{
    // This class is deprecated as it is meant for PayPal's old Subscription plan API
    public class PayPalBillingPlan
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Json { get; set; }

        public string State { get; set; }

        public virtual ICollection<PayPalBillingAgreement> BillingAgreements { get; set; }
    }
}
