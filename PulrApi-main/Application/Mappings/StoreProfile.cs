using System.Linq;
using AutoMapperProfile = AutoMapper.Profile;
using Core.Application.Mediatr.Products.Commands;
using Core.Application.Mediatr.Products.Queries;
using Core.Application.Mediatr.Stores.Commands;
using Core.Application.Models;
using Core.Application.Models.Currencies;
using Core.Application.Models.Products;
using Core.Application.Models.Stores;
using Core.Domain.Entities;

namespace Core.Application.Mappings
{
    public class StoreProfile : AutoMapperProfile
    {
        public StoreProfile()
        {
            CreateMap<Currency, CurrencyDetailsResponse>();

            CreateMap<CreateStoreCommand, StoreCreateDto>();
            CreateMap<UpdateStoreCommand,StoreUpdateDto>();

            CreateMap<PagedList<StoreResponse>, PagingResponse<StoreResponse>>().ForMember(
                           dest => dest.Items, opt => opt.MapFrom(src => src));

            CreateMap<GetPublicProductsQuery, ProductPublicListRequestParams>(); 
            CreateMap<ProductCreateCommand, ProductCreateDto>();
            CreateMap<ProductUpdateCommand, ProductUpdateDto>();
            CreateMap<UpdateProductImagesCommand, ProductImagesUpdateDto>().ForMember(
                dest => dest.ImagePriorities, opt => opt.MapFrom(src => src.ImagePriorities.Select(p => int.Parse(p))));
            CreateMap<DeleteProductImageCommand, ProductImageDeleteDto>();
            CreateMap<Product, ProductDetailsResponse>();
            CreateMap<Product, ProductInventoryResponse>()
                //TODO fix this mapping for category listing 
                /*.ForMember(
                dest => dest.CategoryTitle, opt => opt.MapFrom(src => src.ProductCategory.Category.Name))*/
                .ForMember(
                dest => dest.ImageUrl, opt => opt.MapFrom(src => src.ProductMediaFiles.Where(pmf => pmf.MediaFile.Priority == 0).FirstOrDefault().MediaFile.Url));
            CreateMap<PagedList<Product>, PagingResponse<ProductInventoryResponse>>().ForMember(
                            dest => dest.Items, opt => opt.MapFrom(src => src));

            CreateMap<Product, ProductPublicResponse>().ForMember(dest => dest.StoreName, opt => opt.MapFrom(src => src.Store.Name));

            CreateMap<PagedList<Product>, PagingResponse<ProductPublicResponse>>().ForMember(
                            dest => dest.Items, opt => opt.MapFrom(src => src));
        }
    }
}
