using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using TabRepository.Models.AccountViewModels;

namespace TabRepository.ViewModels
{
    public enum SubscriptionInterval
    {
        Monthly,
        Yearly,
        None 
    }

    public class CreditCardFormViewModel
    {
        public string UserId { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [Display(Name = "Name")]
        public string CardName { get; set; }

        [Required(ErrorMessage = "Address is required")]
        public string Address { get; set; }

        [Required(ErrorMessage = "City is required")]
        public string City { get; set; }

        [Required(ErrorMessage = "State is required")]
        public string State { get; set; }

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

        [Required(ErrorMessage = "Interval is required")]
        public SubscriptionInterval Interval { get; set; }

        [Required(ErrorMessage = "Account Type is required")]
        [Display(Name = "Account Type")]
        public AccountType AccountType { get; set; }
    }
}
