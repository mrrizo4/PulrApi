using System.Linq;
using System.Threading.Tasks;
using Core.Application.Models.Users;
using Core.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Core.Application.Models.External.Apple;
using System.Collections.Generic;

namespace Core.Application.Interfaces
{
    public interface IUserService
    {
        Task<string> GetRoleIdAsync(string roleName);
        IQueryable<IdentityUserRole<string>> IsUserInRoleQuery(string userId, string roleId);
        Task<LoginResponse> LoginAsync(LoginDto loginDto);
        Task<UserRegisterResponseDto> RegisterAsync(UserRegisterDto model);
        Task<AuthModel> GetTokenAsync(TokenRequest request);
        Task DeactivateAsync(User currentUser);
        Task ReactivateUserAsync(User user);
        Task DeactivateAccountAsync(User user);
        Task ManagePasswordResetRequest(string email);
        Task AssignRole(string storeOwner);
        Task<LoginResponse> LoginWithFacebookAsync(string accessToken);
        Task<LoginResponse> LoginWithGoogleAsync(string accessToken, string firstName = null, string lastName = null, string pictureUrl = null, bool isEmailVerified = false);
        Task<LoginResponse> LoginWithAppleAsync(string identityToken, AppleNameInfo fullName = null);
        Task SendEmailConfirmationToken(User user);
        Task<List<LoginActivityDto>> GetLoginActivityAsync();
        Task<List<RecognisedDeviceDto>> GetRecognisedDevicesAsync();
        Task SignOutDeviceAsync(int activityId);
        Task SignOutAllDevicesAsync();
        Task<UserNotificationSettingDto> GetNotificationSettingsAsync();
        Task UpdateNotificationSettingsAsync(UserNotificationSettingDto dto);
        Task SaveLoginActivityAsync(string userId, string brand, string modelName, string osVersion, string deviceIdentifier, string action);
    }
}
