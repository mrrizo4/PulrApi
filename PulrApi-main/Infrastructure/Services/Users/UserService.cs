using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Core.Application.Constants;
using Core.Application.Constants.Http;
using Core.Application.Exceptions;
using Core.Application.Helpers;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Application.Models.Currencies;
using Core.Application.Models.External.Apple;
using Core.Application.Models.Profiles;
using Core.Application.Models.Stores;
using Core.Application.Models.Users;
using Core.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using shortid;
using shortid.Configuration;

namespace Core.Infrastructure.Services.Users
{
    public class UserService : IUserService
    {
        private readonly ILogger<UserService> _logger;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly IFacebookAuthService _facebookAuthService;
        private readonly IGoogleAuthService _googleAuthService;
        private readonly IProfileService _profileService;
        private readonly IMapper _mapper;
        private readonly JWT _jwt;
        private readonly IAppleAuthService _appleAuthService;

        public UserService(
            ILogger<UserService> logger,
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            IOptions<JWT> jwt,
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService,
            IConfiguration configuration,
            IEmailService emailService,
            IFacebookAuthService facebookAuthService,
            IGoogleAuthService googleAuthService,
            IProfileService profileService,
            IMapper mapper,
            IAppleAuthService appleAuthService)
        {
            _logger = logger;
            _userManager = userManager;
            _roleManager = roleManager;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _configuration = configuration;
            _emailService = emailService;
            _facebookAuthService = facebookAuthService;
            _googleAuthService = googleAuthService;
            _profileService = profileService;
            _mapper = mapper;
            _jwt = jwt.Value;
            _appleAuthService = appleAuthService;
        }

        public async Task<string> GetRoleIdAsync(string roleName)
        {
            var role = await _dbContext.Set<IdentityRole>()
                .SingleOrDefaultAsync(r => r.Name == roleName);

            return role.Id;
        }

        public IQueryable<IdentityUserRole<string>> IsUserInRoleQuery(string userId, string roleId)
        {
            var userRoleQuery = _dbContext.Set<IdentityUserRole<string>>()
                .Where(ur => ur.UserId == userId && ur.RoleId == roleId);

            return userRoleQuery;
        }

        public async Task<UserRegisterResponseDto> RegisterAsync(UserRegisterDto model)
        {
            bool isSuccess = false;
            string message = "";

            User user = null;
            var userWithSameEmail = await _userManager.FindByEmailAsync(model.Email);
            if (userWithSameEmail != null)
            {
                if (userWithSameEmail.IsSuspended)
                {
                    // Check if the suspension period has expired
                    if (userWithSameEmail.SuspendedUntil.HasValue && userWithSameEmail.SuspendedUntil.Value > DateTime.UtcNow)
                    {
                        // User is still within the 30-day period, reactivate their account
                        await ReactivateUserAsync(userWithSameEmail);
                        message = "Your account has been reactivated. You can now log in.";
                        return new UserRegisterResponseDto()
                        {
                            IsSuccess = true,
                            Message = message,
                            User = userWithSameEmail
                        };
                    }
                    else
                    {
                        // Suspension period has expired, allow new registration
                        // Update the suspended user's email to free up the email address
                        var timestamp = DateTime.UtcNow.Ticks;
                        userWithSameEmail.Email = $"suspended_{timestamp}_{userWithSameEmail.Email}";
                        userWithSameEmail.UserName = $"suspended_{timestamp}_{userWithSameEmail.UserName}";
                        await _userManager.UpdateAsync(userWithSameEmail);

                        // Create a new user with the original email
                        user = new User
                        {
                            FirstName = model.FirstName,
                            LastName = string.IsNullOrWhiteSpace(model.LastName) ? null : model.LastName,
                            DisplayName = !string.IsNullOrEmpty(model.DisplayName) ? model.DisplayName : await GenerateUniqueDisplayName(model.FirstName, model.LastName),
                            UserName = UsernameHelper.Normalize(model.Username),
                            Email = model.Email,
                            PhoneNumber = model.PhoneNumber,
                            TermsAccepted = model.TermsAccepted,
                            CreatedAt = DateTime.UtcNow,
                            IsSuspended = false,
                            IsVerified = true // Set to true since email is already verified
                        };
                        var result = await _userManager.CreateAsync(user, model.Password);
                        if (result.Succeeded)
                        {
                            await _userManager.AddToRoleAsync(user, PulrRoles.User);
                            isSuccess = true;
                        }
                        else
                        {
                            message = string.Join(", ", result.Errors.Select(e => e.Description));
                        }
                    }
                }
                else if (userWithSameEmail.EmailConfirmed && !userWithSameEmail.IsVerified)
                {
                    // This is a temporary user created for email verification
                    // Delete it and create the actual user
                    await _userManager.DeleteAsync(userWithSameEmail);

                    user = new User
                    {
                        FirstName = model.FirstName,
                        LastName = string.IsNullOrWhiteSpace(model.LastName) ? null : model.LastName,
                        DisplayName = !string.IsNullOrEmpty(model.DisplayName) ? model.DisplayName : await GenerateUniqueDisplayName(model.FirstName, model.LastName),
                        UserName = UsernameHelper.Normalize(model.Username),
                        Email = model.Email,
                        PhoneNumber = model.PhoneNumber,
                        TermsAccepted = model.TermsAccepted,
                        CreatedAt = DateTime.UtcNow,
                        IsSuspended = false,
                        IsVerified = true,
                        EmailConfirmed = true
                    };
                    var result = await _userManager.CreateAsync(user, model.Password);
                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(user, PulrRoles.User);
                        isSuccess = true;
                    }
                    else
                    {
                        message = string.Join(", ", result.Errors.Select(e => e.Description));
                    }
                }
                else if (userWithSameEmail.IsVerified)
                {
                    message = "User already Registered with this Email";
                }
                else
                {
                    message = HttpErrorMessages.EmailTaken;
                }
            }
            else
            {
                message = "Please verify your email address before registering.";
                return new UserRegisterResponseDto()
                {
                    IsSuccess = false,
                    Message = message,
                    User = null
                };
            }

            if (isSuccess)
            {
                if (model.CountryUid != null)
                {
                    var country = await _dbContext.Countries.Where(c => c.Uid == model.CountryUid).FirstOrDefaultAsync();
                    if (country != null)
                    {
                        user.Country = country;
                    }
                }

                var affiliateId = ShortId.Generate(new GenerationOptions(true, false));
                user.Affiliate = new Affiliate() { AffiliateId = affiliateId };
                await _dbContext.SaveChangesAsync(CancellationToken.None);
            }

            return new UserRegisterResponseDto()
            {
                IsSuccess = isSuccess,
                Message = message,
                User = isSuccess ? user : null
            };
        }

        public async Task SendEmailConfirmationToken(User user)
        {
            try
            {
                // var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                // var confirmationLink =
                //     $"{_configuration["ApiUrl"]}/users/confirm-email?email={user.Email}&token={token}";
                var random = new Random();
                var code = random.Next(100000, 999999).ToString();
                user.EmailVerificationCode = code;
                user.EmailVerificationCodeExpiry = DateTime.UtcNow.AddMinutes(15); // 15 min expiry
                await _dbContext.SaveChangesAsync(CancellationToken.None);

                var logoUrl = "https://prod-pulr-logo.s3.me-south-1.amazonaws.com/Pulr+Logo+Purple.svg";
                var emailContent = $@"
<div style=""font-family: Arial, sans-serif; text-align: center; background: #fff; padding: 32px;"">
  <img src=""{logoUrl}"" alt=""Your Logo"" style=""width: 80px; margin-bottom: 24px;"">
  <h2>Verify your email address</h2>
  <p>Use the following code to verify your email address:</p>
  <div style=""font-size: 2em; font-weight: bold; letter-spacing: 8px; margin: 24px 0;"">{code}</div>
  <p>This code will expire in 15 minutes.</p>
  <p>If you didn't create an account, you can ignore this email.</p>
</div>
";
                await _emailService.SendMail(new EmailParamsDto()
                {
                    From = _configuration["PulrEmails:Support"],
                    Subject = "Confirmation link for Pulr.co to verify your email address",
                    Content = emailContent,
                    To = new List<string>() { user.Email },
                    IsTemplateFromFile = false,
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task<AuthModel> GetTokenAsync(TokenRequest request)
        {
            var authModel = new AuthModel();
            User user;
            if (request.IsEmail)
            {
                user = await _userManager.FindByEmailAsync(request.Username);              
            }
            else
            {
                var normalizedUsername = UsernameHelper.Normalize(request.Username);
                user = await _userManager.FindByNameAsync(normalizedUsername);
            }

            if (user == null)
            {
                authModel.IsAuthenticated = false;
                authModel.Message = HttpErrorMessages.WrongCredentials;
                return authModel;
            }

            if (user.IsSuspended)
            {
                // Check if the suspension period has expired
                if (user.SuspendedUntil.HasValue && user.SuspendedUntil.Value > DateTime.UtcNow)
                {
                    // User is still within the 30-day period, reactivate their account
                    await ReactivateUserAsync(user);
                }
                else
                {
                    // Suspension period has expired, keep the account suspended
                    authModel.IsAuthenticated = false;
                    authModel.Message = HttpErrorMessages.AccountSuspended;
                    return authModel;
                }
            }

            // Check if user's profile is active
            var profile = await _dbContext.Profiles.SingleOrDefaultAsync(p => p.UserId == user.Id);
            if (profile == null || !profile.IsActive)
            {
                authModel.IsAuthenticated = false;
                authModel.Message = "User account is deactivated";
                return authModel;
            }

            if (await _userManager.CheckPasswordAsync(user, request.Password))
            {
                authModel = await CreateSuccessAuthModel(user);
                return authModel;
            }

            authModel.IsAuthenticated = false;
            authModel.Message = HttpErrorMessages.WrongCredentials;
            return authModel;
        }

        private async Task<AuthModel> CreateSuccessAuthModel(User user)
        {
            try
            {
                var authModel = new AuthModel();
                authModel.IsAuthenticated = true;
                authModel.UserId = user.Id;
                JwtSecurityToken jwtSecurityToken = await CreateJwtToken(user);
                authModel.Token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
                authModel.Email = user.Email;
                authModel.Username = user.UserName;
                var rolesList = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
                authModel.Roles = rolesList.ToList();
                return authModel;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        private async Task<JwtSecurityToken> CreateJwtToken(User user)
        {
            var userClaims = await _userManager.GetClaimsAsync(user);
            var roles = await _userManager.GetRolesAsync(user);
            var roleClaims = new List<Claim>();
            for (int i = 0; i < roles.Count; i++)
            {
                roleClaims.Add(new Claim("roles", roles[i]));
            }

            var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email)
                }
                .Union(userClaims)
                .Union(roleClaims);
            var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
            var signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);
            var jwtSecurityToken = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwt.DurationInMinutes),
                signingCredentials: signingCredentials);
            return jwtSecurityToken;
        }

        public async Task DeactivateAsync(User currentUser)
        {
            try
            {
                // Suspend the user
                currentUser.IsSuspended = true;
                currentUser.SuspendedAt = DateTime.UtcNow;
                currentUser.SuspendedUntil = DateTime.UtcNow.AddDays(30);
                await _userManager.UpdateAsync(currentUser);

                // Deactivate the user's profile
                var profile = await _dbContext.Profiles.SingleOrDefaultAsync(p => p.UserId == currentUser.Id);
                if (profile != null)
                {
                    profile.IsActive = false;
                }

                // Suspend all user's stores
                var stores = await _dbContext.Stores.Where(s => s.UserId == currentUser.Id).ToListAsync();
                foreach (var store in stores)
                {
                    store.IsActive = false;
                }

                // Suspend all user's posts
                var posts = await _dbContext.Posts.Where(p => p.User.Id == currentUser.Id).ToListAsync();
                foreach (var post in posts)
                {
                    post.IsActive = false;
                }

                // Suspend all user's comments
                var comments = await _dbContext.Comments.Where(c => c.CommentedBy.UserId == currentUser.Id).ToListAsync();
                foreach (var comment in comments)
                {
                    comment.IsActive = false;
                }

                // Suspend all user's stories
                var stories = await _dbContext.Stories.Where(s => s.UserId == currentUser.Id).ToListAsync();
                foreach (var story in stories)
                {
                    story.IsActive = false;
                }

                await _dbContext.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task ReactivateUserAsync(User user)
        {
            try
            {
                // Reactivate the user
                user.IsSuspended = false;
                user.SuspendedAt = null;
                user.SuspendedUntil = null;
                await _userManager.UpdateAsync(user);

                // Reactivate the user's profile
                var profile = await _dbContext.Profiles.SingleOrDefaultAsync(p => p.UserId == user.Id);
                if (profile != null)
                {
                    profile.IsActive = true;
                }

                // Reactivate all user's stores
                var stores = await _dbContext.Stores.Where(s => s.UserId == user.Id).ToListAsync();
                foreach (var store in stores)
                {
                    store.IsActive = true;
                }

                // Reactivate all user's posts
                var posts = await _dbContext.Posts.Where(p => p.User.Id == user.Id).ToListAsync();
                foreach (var post in posts)
                {
                    post.IsActive = true;
                }

                // Reactivate all user's comments
                var comments = await _dbContext.Comments.Where(c => c.CommentedBy.UserId == user.Id).ToListAsync();
                foreach (var comment in comments)
                {
                    comment.IsActive = true;
                }

                // Reactivate all user's stories
                var stories = await _dbContext.Stories.Where(s => s.UserId == user.Id).ToListAsync();
                foreach (var story in stories)
                {
                    story.IsActive = true;
                }

                await _dbContext.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task ManagePasswordResetRequest(string email)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    throw new NotFoundException("User not found.");
                }
                // var userId = await _userManager.GetUserIdAsync(user);
                // var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                // code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                // var passwordResetUrl =
                // $"{_configuration["ConsumerUrls:WebApp"]}/password-reset?email={email}&token={code}";
                var random = new Random();
                var code = random.Next(100000, 999999).ToString();
                user.PasswordResetCode = code;
                user.PasswordResetCodeExpiry = DateTime.UtcNow.AddMinutes(15); // 15 min expiry
                await _dbContext.SaveChangesAsync(CancellationToken.None);

                var logoUrl = "https://prod-pulr-logo.s3.me-south-1.amazonaws.com/Pulr+Logo+Purple.svg"; // TODO: Replace with your actual logo URL
                var emailContent = $@"
<div style=""font-family: Arial, sans-serif; text-align: center; background: #fff; padding: 32px;"">
  <img src=""{logoUrl}"" alt=""Your Logo"" style=""width: 80px; margin-bottom: 24px;"">
  <h2>Reset your password</h2>
  <p>Use the following code to reset your password:</p>
  <div style=""font-size: 2em; font-weight: bold; letter-spacing: 8px; margin: 24px 0;"">{code}</div>
  <p>If you didn't request a password reset, you can ignore this email.</p>
</div>
";
                await _emailService.SendMail(new EmailParamsDto()
                {
                    // Subject = "Password reset request",
                    // Content = $"Visit this url to reset your password: \n{passwordResetUrl}",
                    Subject = "Password reset code",
                    Content = emailContent,
                    To = new List<string>() { email },
                    IsTemplateFromFile = false,                   
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task AssignRole(string storeOwner)
        {
            try
            {
                if (PulrRoles.RoleExists(storeOwner) == false)
                {
                    throw new ForbiddenException($"Role {storeOwner} doesn't exist.");
                }

                var user = await _userManager.FindByIdAsync(_currentUserService.GetUserId());

                if (await _userManager.IsInRoleAsync(user, storeOwner))
                {
                    return;
                }

                var res = await _userManager.AddToRoleAsync(user, storeOwner);

                if (!res.Succeeded)
                {
                    throw new Exception(string.Join(",", res.Errors.Select(e => e.Description)));
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task<LoginResponse> LoginAsync(LoginDto loginDto)
        {
            try
            {
                var normalizedUsername = loginDto.IsEmail ? loginDto.Username : UsernameHelper.Normalize(loginDto.Username);                
                var res = await GetTokenAsync(new TokenRequest()
                {
                    IsEmail = loginDto.IsEmail,
                    Username = normalizedUsername,
                    Password = loginDto.Password
                });

                if (!res.IsAuthenticated)
                {
                    _logger.LogWarning($"Login failed for user {normalizedUsername} - {res.Message}");
                    throw new NotAuthenticatedException(res.Message);
                }
                var loginResponse = new LoginResponse()
                {
                    Id = res.UserId,
                    Roles = res.Roles,
                    Token = res.Token,
                    Username = res.Username,
                    Email = res.Email,
                    ImageUrl = null,
                };
                
                var profile = await _dbContext.Profiles
                    .Where(p => p.UserId == res.UserId)
                    .Select(p => new LoginResponse
                    {
                        ProfileUid = p.Uid,
                        FullName = p.User.FirstName,
                        FirstName = p.User.FirstName,
                        LastName = p.User.LastName,
                        Username = p.User.UserName,
                        ImageUrl = p.ImageUrl,
                        Currency = _mapper.Map<CurrencyDetailsResponse>(p.Currency),
                        StoreUids = p.User.Stores.Select(s => s.Uid).ToList()
                    }).SingleOrDefaultAsync();


                if (profile != null)
                {
                    loginResponse.ProfileUid = profile.ProfileUid;
                    loginResponse.ImageUrl = profile.ImageUrl;
                    loginResponse.StoreUids = profile.StoreUids;
                    loginResponse.FullName = profile.FirstName;
                    loginResponse.FirstName = profile.FirstName;
                    loginResponse.LastName = profile.LastName;
                    loginResponse.PhoneNumber = profile.PhoneNumber;
                    loginResponse.Currency = profile.Currency;
                }

                return loginResponse;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task<string> GenerateUniqueUsername()
        {
            try
            {
                string generatedPart = ShortId.Generate(new GenerationOptions(true, false, 8));

                bool usernameExists = true;

                while (usernameExists)
                {
                    var uniqueUsername = "user_" + generatedPart;
                    usernameExists = await _dbContext.Users.AnyAsync(u => u.UserName == uniqueUsername);
                    if (!usernameExists)
                    {
                        return uniqueUsername;
                    }

                    generatedPart = ShortId.Generate(new GenerationOptions(true, false, 8));
                }

                throw new Exception("GenerateUniqueUsername failed");
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task<LoginResponse> LoginWithFacebookAsync(string accessToken)
        {
            try
            {
                var validatedTokenResult = await _facebookAuthService.ValidateAccessTokenAsync(accessToken);

                if (!validatedTokenResult.Data.IsValid)
                {
                    throw new NotAuthenticatedException("Invalid facebook token.");
                }

                var userInfo = await _facebookAuthService.GetUserInfoAsync(accessToken);
                var user = await _userManager.FindByEmailAsync(userInfo.Email);

                if (user != null && user.IsSuspended)
                {
                    throw new NotAuthenticatedException("This account has been suspended");
                }

                AuthModel authResult = null;

                if (user == null)
                {
                    var newUser = new User()
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserName = await GenerateUniqueUsername(),
                        Email = userInfo.Email,
                        FirstName = userInfo.FirstName,
                        LastName = userInfo.LastName,
                    };
                    var userCreateResult = await _userManager.CreateAsync(newUser);

                    if (!userCreateResult.Succeeded)
                    {
                        throw new NotAuthenticatedException("Failed to create user from facebook.");
                    }

                    var addToRoleRes = await _userManager.AddToRoleAsync(newUser, PulrRoles.User);
                    if (!addToRoleRes.Succeeded)
                    {
                        throw new NotAuthenticatedException("Failed to create user ROLE from facebook user.");
                    }

                    await _profileService.Create(newUser);

                    authResult = await CreateSuccessAuthModel(newUser);
                }

                if (authResult == null)
                {
                    authResult = await CreateSuccessAuthModel(user);
                }

                _logger.LogInformation($"User '{authResult.Username}' logged in");

                var loginResponse = new LoginResponse()
                {
                    Id = authResult.UserId,
                    Roles = authResult.Roles,
                    Token = authResult.Token,
                    Username = authResult.Username,
                    Email = authResult.Email,
                    ImageUrl = null,
                };

                var profile = await _dbContext.Profiles
                                   .Where(p => p.UserId == user.Id)
                                   .Select(p => new LoginResponse
                                   {
                                       ProfileUid = p.Uid,
                                       FullName = p.User.FirstName,
                                       FirstName = p.User.FirstName,
                                       LastName = p.User.LastName,
                                       Username = p.User.UserName,
                                       ImageUrl = p.ImageUrl,
                                       Currency = _mapper.Map<CurrencyDetailsResponse>(p.Currency),
                                       StoreUids = p.User.Stores.Select(s => s.Uid).ToList()
                                   }).SingleOrDefaultAsync();


                if (profile != null)
                {
                    loginResponse.ProfileUid = profile.ProfileUid;
                    loginResponse.ImageUrl = profile.ImageUrl;
                    loginResponse.StoreUids = profile.StoreUids;
                    loginResponse.FullName = profile.FirstName;
                    loginResponse.FirstName = profile.FirstName;
                    loginResponse.LastName = profile.LastName;
                    loginResponse.PhoneNumber = profile.PhoneNumber;
                }
                return loginResponse;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task<LoginResponse> LoginWithGoogleAsync(string accessToken, string firstName = null, string lastName = null, string pictureUrl = null, bool isEmailVerified = false)
        {
            try
            {
                var userInfo = await _googleAuthService.GetUserInfoAsync(accessToken);
                var user = await _userManager.FindByEmailAsync(userInfo.Email);

                AuthModel authResult = null;

                if (user == null || user.IsSuspended)
                {
                    if (user != null && user.IsSuspended)
                    {
                        // Update the suspended user's email to free up the email address
                        var timestamp = DateTime.UtcNow.Ticks;
                        user.Email = $"suspended_{timestamp}_{user.Email}";
                        user.UserName = $"suspended_{timestamp}_{user.UserName}";
                        await _userManager.UpdateAsync(user);
                    }

                    var newUser = new User()
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserName = await GenerateUniqueUsername(),
                        Email = userInfo.Email,
                        FirstName = userInfo.Given_Name,
                        LastName = userInfo.Family_Name,
                        DisplayName = GenerateUniqueDisplayName(userInfo.Given_Name, userInfo.Family_Name).Result,
                        EmailConfirmed = userInfo.EmailVerified,
                        CreatedAt = DateTime.UtcNow,
                        IsSuspended = false
                    };
                    var userCreateResult = await _userManager.CreateAsync(newUser);

                    if (!userCreateResult.Succeeded)
                    {
                        var errors = string.Join(", ", userCreateResult.Errors.Select(e => e.Description));
                        throw new NotAuthenticatedException($"Failed to create user from Google: {errors}");
                    }

                    var addToRoleRes = await _userManager.AddToRoleAsync(newUser, PulrRoles.User);
                    if (!addToRoleRes.Succeeded)
                    {
                        throw new NotAuthenticatedException("Failed to create user ROLE from Google user.");
                    }

                    await _profileService.Create(newUser);

                    // Update profile with picture URL if provided
                    if (!string.IsNullOrEmpty(pictureUrl) || !string.IsNullOrEmpty(userInfo.Picture))
                    {
                        var userProfile = await _dbContext.Profiles.FirstOrDefaultAsync(p => p.UserId == newUser.Id);
                        if (userProfile != null)
                        {
                            userProfile.ImageUrl = !string.IsNullOrEmpty(pictureUrl) ? pictureUrl : userInfo.Picture;
                            await _dbContext.SaveChangesAsync(CancellationToken.None);
                        }
                    }

                    authResult = await CreateSuccessAuthModel(newUser);
                }
                else
                {
                    // Update existing user's information if provided
                    if (!string.IsNullOrEmpty(firstName))
                        user.FirstName = firstName;
                    if (!string.IsNullOrEmpty(lastName))
                        user.LastName = lastName;
                    if (isEmailVerified)
                        user.EmailConfirmed = true;

                    // Update display name if first name or last name changed
                    if (!string.IsNullOrEmpty(firstName) || !string.IsNullOrEmpty(lastName))
                    {
                        user.DisplayName = $"{user.FirstName} {user.LastName}";
                    }

                    await _userManager.UpdateAsync(user);

                    // Update profile with picture URL if provided
                    if (!string.IsNullOrEmpty(pictureUrl) || !string.IsNullOrEmpty(userInfo.Picture))
                    {
                        var profilen = await _dbContext.Profiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
                        if (profilen != null)
                        {
                            profilen.ImageUrl = !string.IsNullOrEmpty(pictureUrl) ? pictureUrl : userInfo.Picture;
                            await _dbContext.SaveChangesAsync(CancellationToken.None);
                        }
                    }

                    authResult = await CreateSuccessAuthModel(user);
                }

                _logger.LogInformation($"User '{authResult.Username}' logged in with Google");

                var loginResponse = new LoginResponse()
                {
                    Id = authResult.UserId,
                    Roles = authResult.Roles,
                    Token = authResult.Token,
                    Username = authResult.Username,
                    Email = authResult.Email,
                    ImageUrl = null,
                };

                var profile = await _dbContext.Profiles
                           .Where(p => p.UserId == authResult.UserId)
                           .Select(p => new LoginResponse
                           {
                               ProfileUid = p.Uid,
                               FullName = p.User.FirstName,
                               FirstName = p.User.FirstName,
                               LastName = p.User.LastName,
                               Username = p.User.UserName,
                               ImageUrl = p.ImageUrl,
                               Currency = _mapper.Map<CurrencyDetailsResponse>(p.Currency),
                               StoreUids = p.User.Stores.Select(s => s.Uid).ToList()
                           }).SingleOrDefaultAsync();

                if (profile != null)
                {
                    loginResponse.ProfileUid = profile.ProfileUid;
                    loginResponse.ImageUrl = profile.ImageUrl;
                    loginResponse.StoreUids = profile.StoreUids;
                    loginResponse.FullName = profile.FirstName;
                    loginResponse.FirstName = profile.FirstName;
                    loginResponse.LastName = profile.LastName;
                    loginResponse.PhoneNumber = profile.PhoneNumber;
                    loginResponse.Currency = profile.Currency;
                }
                else
                {
                    // If no profile exists, use the user's information directly
                    loginResponse.FullName = user.FirstName;
                    loginResponse.FirstName = user.FirstName;
                    loginResponse.LastName = user.LastName;
                }

                return loginResponse;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task<LoginResponse> LoginWithAppleAsync(string identityToken, AppleNameInfo fullName = null)
        {
            try
            {
                var userInfo = await _appleAuthService.GetUserInfoAsync(identityToken);
                if (string.IsNullOrEmpty(userInfo.Email))
                {
                    throw new NotAuthenticatedException("Email not provided by Apple.");
                }

                var user = await _userManager.FindByEmailAsync(userInfo.Email);

                AuthModel authResult = null;

                if (user == null || user.IsSuspended)
                {
                    _logger.LogInformation("Creating new user from Apple Sign In");
                    
                    if (user != null && user.IsSuspended)
                    {
                        // Update the suspended user's email to free up the email address
                        var timestamp = DateTime.UtcNow.Ticks;
                        user.Email = $"suspended_{timestamp}_{user.Email}";
                        user.UserName = $"suspended_{timestamp}_{user.UserName}";
                        await _userManager.UpdateAsync(user);
                    }
                    
                    // Use the provided fullName or fall back to userInfo.NameInfo
                    var nameInfo = fullName ?? userInfo.NameInfo;
                    
                    user = new User
                    {
                        UserName = userInfo.Email,
                        Email = userInfo.Email,
                        EmailConfirmed = userInfo.EmailVerified,
                        FirstName = nameInfo?.GivenName ?? "AppleUser",
                        LastName = nameInfo?.FamilyName ?? "User",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsSuspended = false
                    };

                    var result = await _userManager.CreateAsync(user);
                    if (!result.Succeeded)
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        _logger.LogError($"Failed to create user: {errors}");
                        throw new NotAuthenticatedException($"Failed to create user from Apple: {errors}");
                    }

                    // Add to User role
                    await _userManager.AddToRoleAsync(user, PulrRoles.User);

                    // Create profile for new user
                    await _profileService.Create(user);

                    authResult = await CreateSuccessAuthModel(user);
                }
                else
                {
                    // Update existing user's name if not set
                    if (string.IsNullOrEmpty(user.FirstName) || string.IsNullOrEmpty(user.LastName))
                    {
                        var nameInfo = fullName ?? userInfo.NameInfo;
                        if (nameInfo != null)
                        {
                            user.FirstName = nameInfo.GivenName ?? user.FirstName;
                            user.LastName = nameInfo.FamilyName ?? user.LastName;
                            await _userManager.UpdateAsync(user);
                        }
                    }

                    authResult = await CreateSuccessAuthModel(user);
                }

                _logger.LogInformation($"User '{authResult.Username}' logged in with Apple");

                var loginResponse = new LoginResponse()
                {
                    Id = authResult.UserId,
                    Roles = authResult.Roles,
                    Token = authResult.Token,
                    Username = authResult.Username,
                    Email = authResult.Email,
                    ImageUrl = null,
                };

                try
                {
                    // Get profile information with a simpler query
                    var profile = await _dbContext.Profiles
                        .Include(p => p.User)
                        .Include(p => p.Currency)
                        .Include(p => p.User.Stores)
                        .FirstOrDefaultAsync(p => p.UserId == user.Id);

                    if (profile != null)
                    {
                        _logger.LogInformation($"Profile found for user {user.Id}");
                        loginResponse.ProfileUid = profile.Uid;
                        loginResponse.ImageUrl = profile.ImageUrl;
                        loginResponse.StoreUids = profile.User.Stores.Select(s => s.Uid).ToList();
                        loginResponse.FullName = profile.User.FirstName;
                        loginResponse.FirstName = profile.User.FirstName;
                        loginResponse.LastName = profile.User.LastName;
                        loginResponse.PhoneNumber = profile.User.PhoneNumber;
                        loginResponse.Currency = _mapper.Map<CurrencyDetailsResponse>(profile.Currency);
                    }
                    else
                    {
                        _logger.LogWarning($"No profile found for user {user.Id}");
                        // If no profile exists, use the user's information directly
                        loginResponse.FullName = user.FirstName;
                        loginResponse.FirstName = user.FirstName;
                        loginResponse.LastName = user.LastName;
                        loginResponse.PhoneNumber = user.PhoneNumber;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error retrieving profile for user {user.Id}");
                    // Continue with the login response even if profile retrieval fails
                    loginResponse.FullName = user.FirstName;
                    loginResponse.FirstName = user.FirstName;
                    loginResponse.LastName = user.LastName;
                    loginResponse.PhoneNumber = user.PhoneNumber;
                }

                return loginResponse;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        private async Task<string> GenerateUniqueDisplayName(string firstName, string lastName)
        {
            try
            {
                // Generate a random number between 10000 and 99999
                Random random = new Random();
                int randomNumber = random.Next(10000, 99999);
                
                // Create base username from first name only
                string baseUsername = firstName.ToLower();
                
                // Remove any special characters and spaces
                baseUsername = new string(baseUsername.Where(c => char.IsLetterOrDigit(c)).ToArray());
                
                // Take first 6 characters if longer
                baseUsername = baseUsername.Length > 6 ? baseUsername.Substring(0, 6) : baseUsername;
                
                string displayName = $"@{baseUsername}{randomNumber}";
                bool displayNameExists = true;
                int attempts = 0;
                const int maxAttempts = 5;

                while (displayNameExists && attempts < maxAttempts)
                {
                    displayNameExists = await _dbContext.Users.AnyAsync(u => u.DisplayName == displayName);
                    if (!displayNameExists)
                    {
                        return displayName;
                    }
                    
                    // Generate new random number if display name exists
                    randomNumber = random.Next(10000, 99999);
                    displayName = $"@{baseUsername}{randomNumber}";
                    attempts++;
                }

                // If we couldn't find a unique name after max attempts, throw exception
                throw new Exception("Failed to generate unique display name after multiple attempts");
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task DeactivateAccountAsync(User user)
        {
            try
            {
                // Set the user's profile as inactive
                var profile = await _dbContext.Profiles.SingleOrDefaultAsync(p => p.UserId == user.Id);
                if (profile != null)
                {
                    profile.IsActive = false;
                }

                // Set all user's stores as inactive
                var stores = await _dbContext.Stores.Where(s => s.UserId == user.Id).ToListAsync();
                foreach (var store in stores)
                {
                    store.IsActive = false;
                }

                // Set all user's posts as inactive
                var posts = await _dbContext.Posts.Where(p => p.User.Id == user.Id).ToListAsync();
                foreach (var post in posts)
                {
                    post.IsActive = false;
                }

                // Set all user's comments as inactive
                var comments = await _dbContext.Comments.Where(c => c.CommentedBy.UserId == user.Id).ToListAsync();
                foreach (var comment in comments)
                {
                    comment.IsActive = false;
                }

                // Set all user's stories as inactive
                var stories = await _dbContext.Stories.Where(s => s.UserId == user.Id).ToListAsync();
                foreach (var story in stories)
                {
                    story.IsActive = false;
                }

                await _dbContext.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task<List<LoginActivityDto>> GetLoginActivityAsync()
        {
            var userId = _currentUserService.GetUserId();
            var activities = await _dbContext.UserLoginActivities
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.Timestamp)
                .Select(a => new LoginActivityDto
                {
                    DeviceName = a.ModelName,
                    Action = a.Action,
                    Timestamp = a.Timestamp
                })
                .ToListAsync();
            return activities;
        }

        public async Task<List<RecognisedDeviceDto>> GetRecognisedDevicesAsync()
        {
            var userId = _currentUserService.GetUserId();
            var activities = await _dbContext.UserLoginActivities
                .Where(a => a.UserId == userId)
                .Include(a => a.User)
                .ToListAsync();

            var devices = activities
                .GroupBy(a => a.DeviceIdentifier)
                .Select(g => g.OrderByDescending(a => a.Timestamp).FirstOrDefault())
                .Where(a => a != null && a.Action == "Logged in")
                .Select(a => new RecognisedDeviceDto
                {
                    DeviceName = a.ModelName,
                    DeviceIdentifier = a.DeviceIdentifier,
                    Username = a.User?.UserName
                })
                .ToList();
            return devices;
        }

        public async Task SaveLoginActivityAsync(string userId, string brand, string modelName, string osVersion, string deviceIdentifier, string action)
        {
            var activity = new UserLoginActivity
            {
                UserId = userId,
                Brand = brand,
                ModelName = modelName,
                OsVersion = osVersion,
                DeviceIdentifier = deviceIdentifier,
                Action = action,
                Timestamp = DateTime.UtcNow
            };
            _dbContext.UserLoginActivities.Add(activity);
            await _dbContext.SaveChangesAsync(CancellationToken.None);
        }

        public async Task SignOutDeviceAsync(int activityId)
        {
            var userId = _currentUserService.GetUserId();
            var activity = await _dbContext.UserLoginActivities
                .Where(a => a.UserId == userId && a.Id == activityId)
                .FirstOrDefaultAsync();
            if (activity != null && activity.Action == "Logged in")
            {
                _dbContext.UserLoginActivities.Add(new UserLoginActivity
                {
                    UserId = userId,
                    ModelName = activity.ModelName,
                    DeviceIdentifier = activity.DeviceIdentifier,
                    Action = "Logged out",
                    Timestamp = DateTime.UtcNow
                });
                await _dbContext.SaveChangesAsync(CancellationToken.None);
            }
        }

        public async Task SignOutAllDevicesAsync()
        {
            var userId = _currentUserService.GetUserId();
            var devices = await _dbContext.UserLoginActivities
                .Where(a => a.UserId == userId)
                .GroupBy(a => a.DeviceIdentifier)
                .Select(g => g.OrderByDescending(a => a.Timestamp).FirstOrDefault())
                .Where(a => a.Action == "Logged in")
                .ToListAsync();
            foreach (var device in devices)
            {
                _dbContext.UserLoginActivities.Add(new UserLoginActivity
                {
                    UserId = userId,
                    ModelName = device.ModelName,
                    DeviceIdentifier = device.DeviceIdentifier,
                    Action = "Logged out",
                    Timestamp = DateTime.UtcNow
                });
            }
            await _dbContext.SaveChangesAsync(CancellationToken.None);
        }

        public async Task<UserNotificationSettingDto> GetNotificationSettingsAsync()
        {
            var userId = _currentUserService.GetUserId();
            var settings = await _dbContext.UserNotificationSettings.FirstOrDefaultAsync(x => x.UserId == userId);
            if (settings == null)
            {
                // Return default settings if not set
                return new UserNotificationSettingDto
                {
                    Likes = true,
                    Comments = true,
                    Mentions = true,
                    Follows = true,
                    SavedPosts = true,
                    ShopActivity = true,
                    DirectMessages = true,
                    EmailNotification = true
                };
            }
            return new UserNotificationSettingDto
            {
                Likes = settings.Likes,
                Comments = settings.Comments,
                Mentions = settings.Mentions,
                Follows = settings.Follows,
                SavedPosts = settings.SavedPosts,
                ShopActivity = settings.ShopActivity,
                DirectMessages = settings.DirectMessages,
                EmailNotification = settings.EmailNotification
            };
        }

        public async Task UpdateNotificationSettingsAsync(UserNotificationSettingDto dto)
        {
            var userId = _currentUserService.GetUserId();
            var settings = await _dbContext.UserNotificationSettings.FirstOrDefaultAsync(x => x.UserId == userId);
            if (settings == null)
            {
                settings = new UserNotificationSetting
                {
                    UserId = userId
                };
                _dbContext.UserNotificationSettings.Add(settings);
            }
            if (dto.Likes.HasValue) settings.Likes = dto.Likes.Value;
            if (dto.Comments.HasValue) settings.Comments = dto.Comments.Value;
            if (dto.Mentions.HasValue) settings.Mentions = dto.Mentions.Value;
            if (dto.Follows.HasValue) settings.Follows = dto.Follows.Value;
            if (dto.SavedPosts.HasValue) settings.SavedPosts = dto.SavedPosts.Value;
            if (dto.ShopActivity.HasValue) settings.ShopActivity = dto.ShopActivity.Value;
            if (dto.DirectMessages.HasValue) settings.DirectMessages = dto.DirectMessages.Value;
            if (dto.EmailNotification.HasValue) settings.EmailNotification = dto.EmailNotification.Value;
            await _dbContext.SaveChangesAsync(CancellationToken.None);
        }
    }
}