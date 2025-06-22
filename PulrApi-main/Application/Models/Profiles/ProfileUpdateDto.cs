using Core.Domain.Enums;
using System.Collections.Generic;

namespace Core.Application.Models.Profiles;

public class ProfileUpdateDto
{
    public string Uid { get; set; }
    public string FullName { get; set; }
    public string Username { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string DisplayName { get; set; }
    public string PhoneNumber { get; set; }
    public GenderEnum? Gender { get; set; }
    public string Address { get; set; }
    public string ZipCode { get; set; }
    public string CityName { get; set; }
    public string About { get; set; }
    public string CountryUid { get; set; }
    public string CurrencyUid { get; set; }
    public string WebsiteUrl { get; set; }
    public string InstagramUrl { get; set; }
    public string FacebookUrl { get; set; }
    public string TwitterUrl { get; set; }
    public string TikTokUrl { get; set; }
    public List<ProfileSocialMediaLinkDto> SocialMediaLinks { get; set; } = new List<ProfileSocialMediaLinkDto>();
}