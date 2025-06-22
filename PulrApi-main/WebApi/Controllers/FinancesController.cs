using System.Threading.Tasks;
using Core.Application.Mediatr.Finances.Commands.Delete;
using Core.Application.Mediatr.Finances.Commands.Update;
using Core.Application.Mediatr.Finances.Commands.Verify;
using Core.Application.Mediatr.Finances.Queries;
using Core.Application.Models.StripeModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

public class FinancesController : ApiControllerBase
{
    [HttpGet("stripe-verification-status/{accountId}")]
    [AllowAnonymous]
    public async Task<ActionResult<StripeIndividualVerificationStatusResponse>> GetStripeVerificationStatus(string accountId)
    {
        var verificationStatus = await Mediator.Send(new GetStripeVerificationStatusQuery() { AccountId = accountId });
        return Ok(verificationStatus);
    }

    [HttpPost("verify-user")]
    public async Task<ActionResult> VerifyUser([FromBody] VerifyStripeIndividualCommand command)
    {
        await Mediator.Send(command);
        return Ok();
    }

    [HttpPut("user-info")]
    public async Task<ActionResult> UpdateUserInfo([FromBody] UpdateStripeIndividualCommand command)
    {
        await Mediator.Send(command);
        return Ok();
    }

    // da se zamijeni za individual
    [HttpPost("verify-company")]
    public async Task<ActionResult> StripeVerifyCompany([FromBody] VerifyStripeCompanyCommand command)
    {
        await Mediator.Send(command);
        return Ok();
    }

    [HttpPut("company-info")]
    public async Task<ActionResult> UpdateCompanyInfo([FromBody] UpdateStripeCompanyCommand command)
    {
        await Mediator.Send(command);
        return Ok();
    }

    [HttpGet("user-info/{username?}")]
    public async Task<ActionResult<StripeInfoIndividualResponse>> GetExternalUserInfo(string username)
    {
        var res = await Mediator.Send(new GetStripeInfoIndividualQuery() { Username = username });
        return Ok(res);
    }

    // TODO later
    //[HttpPut]
    //public async Task<ActionResult> UpdateFinance([FromBody] UpdateFinanceCommand command)
    //{
    //    await Mediator.Send(command);
    //    return Ok();
    //}


    [HttpDelete("user/{username}/external-account/{externalAccountId}")]
    public async Task<ActionResult> DeleteFinance(string username, string externalAccountId)
    {
        await Mediator.Send(new DeleteStripeExternalAccountCommand { Username = username, ExternalAccountId = externalAccountId });
        return NoContent();
    }
}