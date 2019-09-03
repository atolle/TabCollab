using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace TabRepository.ViewModels
{
    public enum SubscriptionRecurrence
    {
        Monthly,
        Yearly
    }

    public class CreditCardFormViewModel
    {
        public string UserId { get; set; }

        [Required(ErrorMessage = "Card Holder is required")]
        [Display(Name = "Card Holder")]
        public string CardName { get; set; }

        [Required(ErrorMessage = "Card Number is required")]
        [CreditCard]
        [Display(Name = "Card Number")]
        
        public string CardNumber { get; set; }

        [Required(ErrorMessage = "CVC is required")]
        public int CVC { get; set; }

        [Required]
        public string Month { get; set; }

        [Required]
        public int Year { get; set; }

        [Required(ErrorMessage = "Zip Code is required")]        
        [Display(Name = "Zip Code")]
        public string Zip { get; set; }

        [Required]
        public string PaymentToken { get; set; }

        [Required(ErrorMessage = "Recurrence is required")]
        public SubscriptionRecurrence Recurrence { get; set; }
    }
}
