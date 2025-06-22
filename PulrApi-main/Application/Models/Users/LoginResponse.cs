using System.Collections.Generic;
using Core.Application.Models.Currencies;

namespace Core.Application.Models.Users
{
    public class LoginResponse
    {
        public string Id { get; set; }
        public List<string> Roles { get; set; }
        public string Token { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string ImageUrl { get; set; }
        public List<string> StoreUids { get; set; }
        public string FullName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string ProfileUid { get; set; }
        public CurrencyDetailsResponse Currency { get; set; }
    }
}
