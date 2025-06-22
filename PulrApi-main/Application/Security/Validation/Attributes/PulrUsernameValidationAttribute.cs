using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Core.Application.Security.Validation.Attributes
{
    public class PulrUsernameValidationAttribute : ValidationAttribute
    {
        private readonly bool _allowNullValue;
        private readonly Regex _minLengthPattern = new Regex("^.{3,}$");
        private readonly Regex _consecutiveCharsPattern = new Regex("(.)\\1{2,}");

        public PulrUsernameValidationAttribute(bool allowNullValue = false)
        {
            _allowNullValue = allowNullValue;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (_allowNullValue && value == null)
            {
                return ValidationResult.Success;
            }

            string username = value?.ToString();

            if (string.IsNullOrWhiteSpace(username))
            {
                return new ValidationResult("Username cannot be empty.");
            }

            if (!_minLengthPattern.IsMatch(username))
            {
                return new ValidationResult("Username must be at least 3 characters long.");
            }

            if (_consecutiveCharsPattern.IsMatch(username))
            {
                return new ValidationResult("Username cannot contain 3 or more consecutive identical characters.");
            }

            return ValidationResult.Success;
        }
    }
} 