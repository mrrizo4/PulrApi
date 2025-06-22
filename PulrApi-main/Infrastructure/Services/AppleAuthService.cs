using Core.Application.Interfaces;
using Core.Application.Models.External.Apple;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Core.Infrastructure.Services
{
    class AppleAuthService : IAppleAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<AppleAuthService> _logger;

        public AppleAuthService(IConfiguration configuration, HttpClient httpClient, ILogger<AppleAuthService> logger)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger;
        }
        public Task<AppleUserInfo> GetUserInfoAsync(string identityToken, string fullResponse = null)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(identityToken);

                // Log all claims for debugging
                _logger.LogInformation("Extracting claims from Apple identity token:");
                foreach (var claim in token.Claims)
                {
                    _logger.LogInformation($"Claim Type: {claim.Type}, Value: {claim.Value}");
                }

                AppleUserInfo userInfo;

                // If we have the full response, use it
                if (!string.IsNullOrEmpty(fullResponse))
                {
                    try
                    {
                        userInfo = JsonSerializer.Deserialize<AppleUserInfo>(fullResponse);
                        _logger.LogInformation($"Successfully parsed full response with name: {userInfo.NameInfo?.GivenName} {userInfo.NameInfo?.FamilyName}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Failed to parse full response: {ex.Message}");
                        userInfo = new AppleUserInfo();
                    }
                }
                else
                {
                    userInfo = new AppleUserInfo();
                }

                // Always extract token claims as they are more reliable for basic info
                userInfo.Sub = token.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
                userInfo.Email = token.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
                userInfo.EmailVerified = token.Claims.FirstOrDefault(c => c.Type == "email_verified")?.Value == "true";
                userInfo.IsPrivateEmail = token.Claims.FirstOrDefault(c => c.Type == "is_private_email")?.Value == "true";
                userInfo.RealUserStatus = int.Parse(token.Claims.FirstOrDefault(c => c.Type == "real_user_status")?.Value ?? "0");
                userInfo.User = token.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

                // Try to get name from the token header
                var nameHeader = token.Header.FirstOrDefault(h => h.Key == "name");
                if (nameHeader.Value != null)
                {
                    try
                    {
                        var nameData = JsonSerializer.Deserialize<AppleNameInfo>(nameHeader.Value.ToString());
                        userInfo.NameInfo = nameData;
                        _logger.LogInformation($"Extracted name from token header: {nameData.GivenName} {nameData.FamilyName}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Failed to parse name from token header: {ex.Message}");
                    }
                }

                // If name not found in header, try to get it from claims
                if (userInfo.NameInfo == null)
                {
                    var nameClaim = token.Claims.FirstOrDefault(c => c.Type == "name");
                    if (nameClaim != null)
                    {
                        try
                        {
                            var nameData = JsonSerializer.Deserialize<AppleNameInfo>(nameClaim.Value);
                            userInfo.NameInfo = nameData;
                            _logger.LogInformation($"Extracted name from token claim: {nameData.GivenName} {nameData.FamilyName}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"Failed to parse name from token claim: {ex.Message}");
                        }
                    }
                }

                // If still no name, try to get it from the token payload
                if (userInfo.NameInfo == null)
                {
                    try
                    {
                        var payload = token.Payload;
                        if (payload.ContainsKey("name"))
                        {
                            var nameData = JsonSerializer.Deserialize<AppleNameInfo>(payload["name"].ToString());
                            userInfo.NameInfo = nameData;
                            _logger.LogInformation($"Extracted name from token payload: {nameData.GivenName} {nameData.FamilyName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Failed to parse name from token payload: {ex.Message}");
                    }
                }

                _logger.LogInformation($"Final Apple user info - Email: {userInfo.Email}, Name: {userInfo.NameInfo?.GivenName} {userInfo.NameInfo?.FamilyName}, User: {userInfo.User}");

                return Task.FromResult(userInfo);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error getting Apple user info");
                throw;
            }
        }
        public async Task<bool> ValidateIdentityTokenAsync(string accessToken)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(accessToken);

                var response = await _httpClient.GetAsync($"https://appleid.apple.com/auth/keys");
                if(!response.IsSuccessStatusCode)
                {
                    return false;
                }

                var keyJson = await response.Content.ReadAsStringAsync();
                var keys = JsonSerializer.Deserialize<AppleKeysResponse>(keyJson);

                // Find the key that matches the token's key ID
                var keyId = token.Header.Kid;
                var key = keys.Keys.FirstOrDefault(k => k.Kid == keyId);

                if(key == null)
                {
                    return false;
                }

                // Create validation parameters
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new ECDsaSecurityKey(ECDsa.Create()),
                    ValidateIssuer = true,
                    ValidIssuer = "https://appleid.apple.com",
                    ValidateAudience = true,
                    ValidAudience = _configuration["AppleAuth:ClientId"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                // Validate the token
                var principal = handler.ValidateToken(accessToken, validationParameters, out _);
                return principal != null;
            }
            catch(Exception ex)
            {
                // Log the exception
                //return false;
                _logger.LogError(ex, "Error validating Apple identity token");
                throw;
            }
        }
    }
    
}

public class AppleKeysResponse
{
    [JsonPropertyName("keys")]
    public List<AppleKey> Keys { get; set; }
}

public class AppleKey
{
    [JsonPropertyName("kty")]
    public string Kty { get; set; }

    [JsonPropertyName("kid")]
    public string Kid { get; set; }

    [JsonPropertyName("use")]
    public string Use { get; set; }

    [JsonPropertyName("alg")]
    public string Alg { get; set; }

    [JsonPropertyName("n")]
    public string N { get; set; }

    [JsonPropertyName("e")]
    public string E { get; set; }
}
