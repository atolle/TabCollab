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

        [Required]
        [Display(Name = "Card Holder")]
        public string CardName { get; set; }

        [Required]
        [CreditCard]
        [Display(Name = "Card Number")]
        public string CardNumber { get; set; }

        [Required]
        public int CVV { get; set; }

        [Required]
        public string Month { get; set; }

        [Required]
        public int Year { get; set; }
    }
}
