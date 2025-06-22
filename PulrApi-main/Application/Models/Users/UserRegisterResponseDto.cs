using Core.Domain.Entities;

namespace Core.Application.Models.Users
{
    public class UserRegisterResponseDto
    {
        public bool IsSuccess { get; set; }
        public User User { get; set; }
        public string Message { get; set; }
    }
}
