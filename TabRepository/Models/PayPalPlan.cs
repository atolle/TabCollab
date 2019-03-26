using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TabRepository.Models
{
    public class PayPalPlan
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Json { get; set; }

        public string Status { get; set; }

        public string ProductId { get; set; }

        public virtual PayPalProduct Product { get; set; }
    }
}
