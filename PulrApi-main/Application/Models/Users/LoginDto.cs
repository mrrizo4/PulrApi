using System.ComponentModel.DataAnnotations;

namespace Core.Application.Models.Users
{
    public class DeviceInfoDto
    {
        public string Brand { get; set; }
        public string ModelName { get; set; }
        public string OsVersion { get; set; }
        public string DeviceIdentifier { get; set; }
    }

    public class LoginDto
    {
        [Required]
        public string Username { get; set; }
        [Required]
        public bool IsEmail { get; set; }
        [Required]
        public string Password { get; set; }
        [Required]
        public DeviceInfoDto Device { get; set; }
    }
}
