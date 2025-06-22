using AutoMapperProfile = AutoMapper.Profile;
using Core.Application.Models.BagItems;
using Core.Application.Models.ShippingDetails;
using Core.Domain.Entities;
using Core.Application.Models.StripeModels;
using Core.Application.Mediatr.Finances.Commands.Verify;
using Core.Application.Mediatr.Finances.Commands.Update;

namespace Core.Application.Mappings
{
    public class FinanceProfile : AutoMapperProfile
    {
        public FinanceProfile()
        {
            CreateMap<BagProductExtendedDto, BagProductResponse>();
            CreateMap<Product, BagProductExtendedDto>();
            CreateMap<ShippingDetails, ShippingDetailsResponse>()
                .ReverseMap();

            CreateMap<StripeExternalAccountModel, StripeExternalAccountDto>();
            CreateMap<VerifyStripeIndividualCommand, StripeIndividualVerificationDetailsDto>();
            CreateMap<VerifyStripeCompanyCommand, StripeCompanyVerificationDetailsDto>();
            CreateMap<VerifyStripeCompanyCommand, StripeAddress>();

            CreateMap<UpdateStripeIndividualCommand, StripeIndividualUpdateDto>();

            CreateMap<UpdateStripeCompanyCommand, StripeCompanyUpdateDto>();

        }
    }
}
