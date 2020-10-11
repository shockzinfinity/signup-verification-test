using System;
using System.ComponentModel.DataAnnotations;

namespace signup_verification.Models.Accounts
{
    public class VerifyEmailRequest
    {
        [Required]
        public string Token { get; set; }
    }
}
