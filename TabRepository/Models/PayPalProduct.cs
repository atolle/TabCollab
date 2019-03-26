using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TabRepository.Models
{
    public class PayPalProduct
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Json { get; set; }

        public virtual ICollection<PayPalPlan> Plans { get; set; }
    }
}
