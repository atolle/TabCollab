﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace TabRepository.ViewModels
{
    public class StripePaymentViewModel
    {
        public string UserId { get; set; }

        [Required]
        public string PaymentToken { get; set; }

        [Required]
        public SubscriptionInterval StripeInterval { get; set; }

        [Required]
        public double Subtotal { get; set; }

        [Required]
        public double Tax { get; set; }

        [Required]
        public string Zipcode { get; set; }
    }
}
