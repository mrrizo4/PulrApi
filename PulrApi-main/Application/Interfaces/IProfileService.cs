using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Models.Profiles;
using Core.Domain.Entities;

namespace Core.Application.Interfaces
{
    public interface IProfileService
    {
        Task<(string username, string uid)> ProfileToggleFollow(string profileUid);
        Task Create(User user, Domain.Enums.GenderEnum? gender = null);
        Task<string> ProfileUpdateAvatarImage(Profile profile, IFormFile image);
        Task<MyProfileDetailsResponse> GetMy();
        Task Update(ProfileUpdateDto profileUpdateDto);
        Task<List<ProfileResponse>> MapProfileResponseList(IQueryable<Profile> profiles, CancellationToken ct);
        Task<List<string>> SearchHandles(string search);
    }
}
