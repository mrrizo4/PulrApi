using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Core.Application.Helpers;
using Core.Application.Models;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System;

namespace WebApi.Controllers
{
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ErrorsController : ControllerBase
    {
        [Route("errors")]
        public IActionResult ErrorDev(
        [FromServices] IWebHostEnvironment webHostEnvironment)
        {
            try
            {
                var context = HttpContext.Features.Get<IExceptionHandlerFeature>();
                if (context == null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new ExceptionResponseDto
                    {
                        StatusCode = StatusCodes.Status500InternalServerError,
                        Message = "An error occurred but no exception details are available."
                    });
                }

                var exception = context.Error;
                if (exception == null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new ExceptionResponseDto
                    {
                        StatusCode = StatusCodes.Status500InternalServerError,
                        Message = "An error occurred but no exception was found."
                    });
                }

                var statusCode = ExceptionHelper.SetHttpStatusCodeBasedOnExceptionType(exception);
                Response.StatusCode = statusCode;

                var exceptionRes = new ExceptionResponseDto
                {
                    StatusCode = statusCode,
                    Message = exception.Message ?? "An error occurred",
                    Details = webHostEnvironment?.EnvironmentName != "Development" 
                        ? "To see details, use dev mode." 
                        : exception.StackTrace
                };

                // Handle validation errors
                if (exception is ValidationException validationException)
                {
                    Response.StatusCode = StatusCodes.Status400BadRequest;
                    exceptionRes.StatusCode = StatusCodes.Status400BadRequest;
                    exceptionRes.Errors = new Dictionary<string, string[]>
                    {
                        { "File", new[] { validationException.Message ?? "File validation failed" } }
                    };
                }
                else if (statusCode == StatusCodes.Status422UnprocessableEntity)
                {
                    var validationEx = exception as dynamic;
                    if (validationEx?.Errors != null)
                    {
                        exceptionRes.Errors = validationEx.Errors;
                    }
                }

                return StatusCode(Response.StatusCode, exceptionRes);
            }
            catch (Exception ex)
            {
                // If something goes wrong in the error handler itself
                return StatusCode(StatusCodes.Status500InternalServerError, new ExceptionResponseDto
                {
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = "An error occurred while processing the request",
                    Details = webHostEnvironment?.EnvironmentName == "Development" ? ex.ToString() : null
                });
            }
        }
    }
}
