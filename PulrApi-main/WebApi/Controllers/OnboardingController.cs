using Core.Application.Mediatr.Onboarding.Commands;
using Core.Application.Mediatr.Onboarding.Queries;
using Core.Application.Models.Onboarding;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace WebApi.Controllers
{
    public class OnboardingController : ApiControllerBase
    {
        [AllowAnonymous]
        [HttpGet("preferences/all")]
        public async Task<ActionResult<OnboardingPreferencesResponse>> GetAllPreferences()
        {
            var res = await Mediator.Send(new GetAllOnboardingPreferencesQuery());
            return Ok(res);
        }

        [HttpGet("preferences")]
        public async Task<ActionResult<OnboardingPreferencesResponse>> GetPreferences()
        {
            var res = await Mediator.Send(new GetMyOnboardingPreferencesQuery());
            return Ok(res);
        }

        [HttpPut("preferences")]
        public async Task<ActionResult> UpdatePreferences([FromBody]OnboardingPreferencesCommand command)
        {
            await Mediator.Send(command);
            return Ok();
        }

        [HttpPost("complete")]
        public async Task<IActionResult> CompleteOnboarding()
        {
            await Mediator.Send(new CompleteOnboardingCommand());
            return Ok();
        }

        [HttpPost("who-to-follow")]
        public async Task<ActionResult<OnboardingWhoToFollowResponse>> WhoToFollow([FromBody] GetOnboardingWhoToFollowQuery query)
        {
            var res = await Mediator.Send(query);
            return Ok(res);
        }
    }
}
