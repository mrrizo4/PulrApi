﻿using Core.Application.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Dashboard
{
    [Authorize(Roles = PulrRoles.Administrator)]
    [Route("api/dashboard/[controller]")]
    public class DashboardApiControllerBase : ApiControllerBase
    {
    }
}
