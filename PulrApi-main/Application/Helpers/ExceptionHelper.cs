﻿using Microsoft.AspNetCore.Http;
using System;
using System.Net;
using Core.Application.Exceptions;

namespace Core.Application.Helpers
{
    public static class ExceptionHelper
    {
        public static int SetHttpStatusCodeBasedOnExceptionType(Exception exception)
        {
            if (exception == null)
            {
                return (int)HttpStatusCode.InternalServerError;
            }

            if (exception is BadRequestException)
            {
                return StatusCodes.Status400BadRequest;
            }
            if (exception is NotAuthenticatedException)
            {
                return StatusCodes.Status401Unauthorized;
            }
            if (exception is ForbiddenException)
            {
                return StatusCodes.Status403Forbidden;
            }
            if (exception is NotFoundException)
            {
                return StatusCodes.Status404NotFound;
            }

            if(exception is ValidationException || exception is FluentValidation.ValidationException)
            {
                return StatusCodes.Status422UnprocessableEntity;
            }

            return StatusCodes.Status500InternalServerError;
        }
    }
}
