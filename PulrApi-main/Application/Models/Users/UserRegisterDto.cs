using Core.Application.Security.Validation.Attributes;
using Core.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace Core.Application.Models.Users
{
    public class UserRegisterDto
    {
        [Required]
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string DisplayName { get; set; }
        public string PhoneNumber { get; set; }
        [Required]
        public string Username { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
        public string CountryUid { get; internal set; }
        public GenderEnum? Gender { get; internal set; }
        [Required]
        public bool TermsAccepted { get; set; }
        [Required]
        public DateTime DateOfBirth { get; set; }
    }
}
