using System;
using System.ComponentModel.DataAnnotations;

namespace signup_verification.Models.Accounts
{
    public class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
