using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Application.Models.StripeModels
{
    public class StripeCompanyOwnerDto
    {
        public string ParentId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public StripeAddress Address { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string JobTitle { get; set; }
        public DateTime DateOfBirth { get; set; }
        public decimal? OwnershipPercent { get; set; }
    }
}
