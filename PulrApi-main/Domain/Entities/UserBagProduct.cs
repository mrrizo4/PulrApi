using System;
using System.ComponentModel.DataAnnotations;
using Core.Domain.Entities;

namespace Core.Domain.Entities
{
    public class UserBagProduct
    {
        [Required]
        public int BagProductId { get; set; }
        public Product BagProduct { get; set; }
        [Required]
        public string UserId { get; set; }
        public User User { get; set; }
        public int Quantity { get; set; }


        public int? AffiliateId { get; set; }
        public Affiliate Affiliate { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
