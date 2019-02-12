using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace TabRepository.ViewModels
{
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

        [Required(ErrorMessage = "CVV is required")]
        public int CVV { get; set; }

        [Required]
        public string Month { get; set; }

        [Required]
        public int Year { get; set; }

        [Required(ErrorMessage = "Zipcode is required")]
        public string Zip { get; set; }
    }
}
