using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Core.Application.Constants;
using Core.Application.Mediatr.Products.Commands;
using Core.Application.Mediatr.Products.Queries;
using Core.Application.Models;
using Core.Application.Models.Products;
using System.Collections.Generic;

namespace WebApi.Controllers;

public class ProductsController : ApiControllerBase
{
    [AllowAnonymous]
    [HttpGet("{uid}")]
    public async Task<ActionResult<ProductDetailsResponse>> GetProductDetails(string uid, [FromQuery] string currencyCode,
        [FromQuery] string affiliateId)
    {
        var res = await Mediator.Send(new GetProductDetailsQuery()
            { Uid = uid, CurrencyCode = currencyCode, AffiliateId = affiliateId });
        return Ok(res);
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<PagingResponse<ProductPublicResponse>>> GetPublicProducts([FromQuery] GetPublicProductsQuery query)
    {
        var res = await Mediator.Send(query);
        return Ok(res);
    }

    [AllowAnonymous]
    [HttpGet("to-tag")]
    public async Task<ActionResult<PagingResponse<ProductPublicResponse>>> GetProductsToTagPaged([FromQuery] ProductsToTagListQuery query)
    {
        var res = await Mediator.Send(query);
        return Ok(res);
    }

    [AllowAnonymous]
    [HttpGet("similar-products/{productUid}")]
    public async Task<ActionResult<ProductSimilarsResponse>> GetSimilarProduct(string productUid)
    {
        var res = await Mediator.Send(new ProductSimilarsQuery() { ProductUid = productUid });
        return Ok(res);
    }

    [Authorize(Roles = PulrRoles.StoreOwner)]
    [HttpGet("inventory/{storeUid}")]
    public async Task<ActionResult<PagingResponse<ProductInventoryResponse>>> GetInventory([FromQuery] PagingParamsRequest pagingParams, string storeUid)
    {
        var res = await Mediator.Send(new GetProductInventoryQuery() { PagingParams = pagingParams, StoreUid = storeUid });
        return Ok(res);
    }

    [Authorize(Roles = PulrRoles.StoreOwner)]
    [HttpPost]
    public async Task<ActionResult<UidBaseResponse>> CreateProduct(ProductCreateCommand command)
    {
        var uid = await Mediator.Send(command);
        return Ok(new UidBaseResponse() { Uid = uid });
    }

    [Authorize(Roles = PulrRoles.StoreOwner)]
    [HttpPut]
    public async Task<ActionResult> UpdateProduct(ProductUpdateCommand command)
    {
        await Mediator.Send(command);
        return Ok();
    }

    [Authorize(Roles = PulrRoles.StoreOwner)]
    [HttpDelete("{uid}")]
    public async Task<ActionResult> DeleteProduct(string uid)
    {
        await Mediator.Send(new ProductDeleteCommand { Uid = uid });
        return NoContent();
    }

    [Authorize(Roles = PulrRoles.User)]
    [HttpPut("images")]
    public async Task<ActionResult<List<ProductImageUpdateResponse>>> UpdateProductImages([FromForm] UpdateProductImagesCommand command)
    {
        var res = await Mediator.Send(command);
        return Ok(res);
    }

    [Authorize(Roles = PulrRoles.User)]
    [HttpDelete("{productUid}/image/{imageUid}")]
    public async Task<ActionResult> DeleteProductImage(string productUid, string imageUid)
    {
        await Mediator.Send(new DeleteProductImageCommand() { ProductUid = productUid, ImageUid = imageUid });
        return NoContent();
    }

    [Authorize(Roles = PulrRoles.User)]
    [HttpPut("{productUid}/toggle-like")]
    public async Task<ActionResult<ProductToggleLikeResponse>> ToggleProductLike(string productUid)
    {
        var likedByMe = await Mediator.Send(new ToggleProductLikeCommand() { Uid = productUid });
        return Ok(new ProductToggleLikeResponse () { LikedByMe = likedByMe });
    }

    [Authorize(Roles = PulrRoles.StoreOwner)]
    [HttpPost("preferences")]
    public async Task<ActionResult> CreateProductPreferences(ProductPreferencesCreateCommand command)
    {
        await Mediator.Send(command);
        return Ok();
    }

    [Authorize(Roles = PulrRoles.StoreOwner)]
    [HttpPut("preferences")]
    public async Task<ActionResult> UpdateProductPreferences(ProductPreferencesUpdateCommand command)
    {
        await Mediator.Send(command);
        return Ok();
    }

    [Authorize(Roles = PulrRoles.StoreOwner)]
    [HttpGet("preferences")]
    public async Task<ActionResult<ProductOnboardingPreferencesResponse>> GetProductPreferences([FromQuery]GetProductOnboardingPreferencesQuery query)
    {
        var res = await Mediator.Send(query);
        return Ok(res);
    }
}