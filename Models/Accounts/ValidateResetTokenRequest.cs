using System;
using System.ComponentModel.DataAnnotations;

namespace signup_verification.Models.Accounts
{
    public class ValidateResetTokenRequest
    {
        [Required]
        public string Token { get; set; }
    }
}
