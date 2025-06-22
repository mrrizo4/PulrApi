using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Application.Models.Users
{
    public class CheckUsernameRequest
    {
        [Required]
        public string Username { get; set; }
    }
}
