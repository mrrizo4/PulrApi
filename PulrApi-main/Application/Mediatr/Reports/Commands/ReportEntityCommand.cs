using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Core.Application.Exceptions;
using Microsoft.Extensions.Configuration;
using Core.Application.Models.Reports;

namespace Core.Application.Mediatr.Reports.Commands
{
    public class ReportEntityCommand : IRequest<ReportResponse>
    {
        [Required]
        public string EntityUid { get; set; }
        
        [Required]
        public ReportTypeEnum Type { get; set; }
    }

    public class ReportEntityCommandHandler : IRequestHandler<ReportEntityCommand, ReportResponse>
    {
        private readonly ILogger<ReportEntityCommandHandler> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public ReportEntityCommandHandler(
            ILogger<ReportEntityCommandHandler> logger,
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService,
            IEmailService emailService,
            IConfiguration configuration)
        {
            _logger = logger;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _emailService = emailService;
            _configuration = configuration;
        }

        public async Task<ReportResponse> Handle(ReportEntityCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = await _currentUserService.GetUserAsync();
                string entityType = request.Type.ToString();
                string subject = $"{entityType} Report Notification";
                string emailContent;

                // Check if user has already reported this entity
                var existingReport = await _dbContext.Reports
                    .FirstOrDefaultAsync(r =>
                        r.EntityUid == request.EntityUid &&
                        r.ReportType == request.Type &&
                        r.ReportedById == currentUser.Id,
                        cancellationToken);

                if (existingReport != null)
                {
                    return new ReportResponse
                    {
                        Success = false,
                        Message = $"You have already reported this {entityType.ToLower()}."
                    };
                }

                if (request.Type == ReportTypeEnum.Post)
                {
                    var post = await _dbContext.Posts
                        .Include(p => p.User)
                        .FirstOrDefaultAsync(p => p.Uid == request.EntityUid, cancellationToken);

                    if (post == null)
                    {
                        throw new BadRequestException($"Post with uid {request.EntityUid} not found.");
                    }

                    emailContent = $@"<div style='font-family: Arial, sans-serif; line-height: 1.6;'>
<h2>Post Report Notification</h2>

<p>A user has reported a post. Below are the details:</p>

<p>🔸 Reported Post ID: {post.Uid}</p>

<p>Post Owner:<br>
- Username: {post.User.UserName}<br>
- Email: {post.User.Email}</p>

<p>Reported By:<br>
- Username: {currentUser.UserName}<br>
- Email: {currentUser.Email}</p>

<p>Please review this report and take appropriate action.</p>

<p>Best regards,<br>
Pulr Team</p>
</div>";

                    var report = new Report
                    {
                        EntityUid = request.EntityUid,
                        ReportType = request.Type,
                        ReportedById = currentUser.Id
                    };

                    _dbContext.Reports.Add(report);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    await _emailService.SendMail(new EmailParamsDto
                    {
                        To = new List<string> { "irfan@pulr.co" },
                        Subject = subject,
                        Content = emailContent
                    });
                }
                else if (request.Type == ReportTypeEnum.Profile)
                {
                    var profile = await _dbContext.Profiles
                        .Include(p => p.User)
                        .FirstOrDefaultAsync(p => p.Uid == request.EntityUid, cancellationToken);

                    if (profile == null)
                    {
                        throw new BadRequestException($"Profile with uid {request.EntityUid} not found.");
                    }

                    emailContent = $@"<div style='font-family: Arial, sans-serif; line-height: 1.6;'>
<h2>Profile Report Notification</h2>

<p>A user has reported a profile. Below are the details:</p>

<p>🔸 Reported Profile ID: {profile.Uid}</p>

<p>Profile Owner:<br>
- Username: {profile.User.UserName}<br>
- Email: {profile.User.Email}</p>

<p>Reported By:<br>
- Username: {currentUser.UserName}<br>
- Email: {currentUser.Email}</p>

<p>Please review this report and take appropriate action.</p>

<p>Best regards,<br>
Pulr Team</p>
</div>";

                    var report = new Report
                    {
                        EntityUid = request.EntityUid,
                        ReportType = request.Type,
                        ReportedById = currentUser.Id
                    };

                    _dbContext.Reports.Add(report);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    await _emailService.SendMail(new EmailParamsDto
                    {
                        To = new List<string> { "irfan@pulr.co" },
                        Subject = subject,
                        Content = emailContent
                    });
                }
                else if (request.Type == ReportTypeEnum.Story)
                {
                    var story = await _dbContext.Stories
                        .Include(s => s.User)
                        .FirstOrDefaultAsync(s => s.Uid == request.EntityUid, cancellationToken);

                    if (story == null)
                    {
                        throw new BadRequestException($"Story with uid {request.EntityUid} not found.");
                    }

                    emailContent = $@"<div style='font-family: Arial, sans-serif; line-height: 1.6;'>
<h2>Story Report Notification</h2>

<p>A user has reported a story. Below are the details:</p>

<p>🔸 Reported Story ID: {story.Uid}</p>

<p>Story Owner:<br>
- Username: {story.User.UserName}<br>
- Email: {story.User.Email}</p>

<p>Reported By:<br>
- Username: {currentUser.UserName}<br>
- Email: {currentUser.Email}</p>

<p>Please review this report and take appropriate action.</p>

<p>Best regards,<br>
Pulr Team</p>
</div>";

                    var report = new Report
                    {
                        EntityUid = request.EntityUid,
                        ReportType = request.Type,
                        ReportedById = currentUser.Id
                    };

                    _dbContext.Reports.Add(report);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    await _emailService.SendMail(new EmailParamsDto
                    {
                        To = new List<string> { "irfan@pulr.co" },
                        Subject = subject,
                        Content = emailContent
                    });
                }

                return new ReportResponse
                {
                    Success = true,
                    Message = $"{entityType} reported successfully. Our team will review the report."
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
} 