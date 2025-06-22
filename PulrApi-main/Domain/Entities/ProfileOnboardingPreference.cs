using System;
using System.ComponentModel.DataAnnotations;

namespace Core.Domain.Entities
{
    public class ProfileOnboardingPreference
    {
        [Required]
        public int ProfileId { get; set; }
        public Profile Profile { get; set; }
        [Required]
        public int OnboardingPreferenceId { get; set; }
        public OnboardingPreference OnboardingPreference { get; set; }
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
