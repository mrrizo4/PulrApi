using System.Threading.Tasks;
using Core.Application.Models;

namespace Core.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendMail(EmailParamsDto emailParamsDto, bool includeAttachments = false);
    }
}
