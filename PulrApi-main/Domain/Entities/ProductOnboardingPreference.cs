using System;
using System.ComponentModel.DataAnnotations;

namespace Core.Domain.Entities
{
    public class ProductOnboardingPreference
    {
        [Required]
        public int ProductId { get; set; }
        public Product Product { get; set; }

        [Required]
        public int OnboardingPreferenceId { get; set; }
        public OnboardingPreference OnboardingPreference { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
