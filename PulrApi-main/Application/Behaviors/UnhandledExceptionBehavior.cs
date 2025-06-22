using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Mediatr.Users.Commands.Login;
using Core.Application.Mediatr.Users.Commands.Password;
using Core.Application.Mediatr.Users.Commands.Register;
using System.Collections.Generic;

namespace Core.Application.Behaviors
{
    public class UnhandledExceptionBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<TRequest> _logger;
        private readonly List<string> _forbiddenRequestsForLog = new List<string>() { 
            nameof(LoginCommand), 
            nameof(RegisterCommand), 
            nameof(ChangePasswordFromEmailCommand), 
            nameof(ChangePasswordCommand), 
            // TODO FIX:
            "DashboardLoginCommand"
            };

        public UnhandledExceptionBehaviour(ILogger<TRequest> logger)
        {
            _logger = logger;
        }
        
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            try
            {
                return await next();
            }
            catch (Exception ex)
            {
                var requestName = typeof(TRequest).Name;

                bool skipBody = false;

                if (_forbiddenRequestsForLog.Contains(requestName))
                {
                    skipBody = true;
                }

                object req;
                if (skipBody == true)
                {
                    req = new { sensitiveData = true };
                }
                else
                {
                    req = request;
                }

                _logger.LogError(ex, "PulrApi Request: Unhandled Exception for Request {Name} {@Request}", requestName, req);

                throw new Exception("error :",ex);
            }
        }
    }
}
