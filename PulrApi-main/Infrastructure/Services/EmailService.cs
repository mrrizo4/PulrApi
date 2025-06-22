using Amazon;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Helpers;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Infrastructure.Services;

namespace Core.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;
        private readonly AmazonSesEmailConfig _emailConfig;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
            if (_emailConfig == null)
            {
                _emailConfig = new AmazonSesEmailConfig(config);
            }
        }

        public async Task SendMail(EmailParamsDto emailParams, bool includeAttachments = false)
        {
            try
            {
                //emailParams.Bcc.Add("IF EVER NEEDED");
                if (emailParams.Attachments.Count > 0 && includeAttachments == true)
                {
                    await SendEmailWithAttachments(emailParams);
                    return;
                }

                await SendSimpleEmail(emailParams);

            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        private async Task SendSimpleEmail(EmailParamsDto emailParams)
        {
            using var sender = new AmazonSimpleEmailServiceClient(_emailConfig,
                                                                  RegionEndpoint.MESouth1);
            string from = String.IsNullOrWhiteSpace(emailParams.From) ? _config["PulrEmails:Support"] : emailParams.From;
            var destination = new Destination()
            {
                BccAddresses = emailParams.Bcc,
                CcAddresses = emailParams.Cc,
                ToAddresses = emailParams.To
            };
            var body = new Body
            {
                Html = new Content(emailParams.Content),
                Text = new Content("If you can't see this email, please use a mail client that supports HTML.") // fallback

            };
            var message = new Message()
            {
                // Body = new Body(new Content(emailParams.Content)),
                Body = body,
                Subject = new Content(emailParams.Subject)
            };
            var emailRequest = new SendEmailRequest(from, destination, message);
            await sender.SendEmailAsync(emailRequest, new CancellationToken());
        }

        private async Task SendEmailWithAttachments(EmailParamsDto emailParams)
        {
            try
            {
                using (var client = new AmazonSimpleEmailServiceClient(_emailConfig, RegionEndpoint.MESouth1))
                {

                    var bodyBuilder = new BodyBuilder();

                    bodyBuilder.HtmlBody = emailParams.Content;
                    bodyBuilder.TextBody = emailParams.Content;

                    foreach (var attachment in emailParams.Attachments)
                    {
                        var byteArray = FileHelper.streamToByteArray(attachment.ContentStream);
                        bodyBuilder.Attachments.Add(attachment.Name, byteArray);
                    }

                    var mimeMessage = new MimeMessage();
                    mimeMessage.From.Add(new MailboxAddress("", emailParams.From));
                    mimeMessage.To.AddRange(emailParams.To.ConvertAll(email => new MailboxAddress("", email)));
                    mimeMessage.Bcc.AddRange(emailParams.Bcc.ConvertAll(email => new MailboxAddress("", email)));

                    mimeMessage.Subject = emailParams.Subject;
                    mimeMessage.Body = bodyBuilder.ToMessageBody();
                    using (var messageStream = new MemoryStream())
                    {
                        await mimeMessage.WriteToAsync(messageStream);
                        var sendRequest = new SendRawEmailRequest { RawMessage = new RawMessage(messageStream) };
                        var response = await client.SendRawEmailAsync(sendRequest);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

    }
}
