using System;
using System.ComponentModel.DataAnnotations;

namespace signup_verification.Models.Accounts
{
    public class RegisterRequest
    {
        [Required]
        public string Title { get; set; }
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        [MinLength(6)]
        public string Password { get; set; }
        [Required]
        [Compare(nameof(Password))]
        public string ConfirmPassword { get; set; }
        [Range(typeof(bool), "true", "true")]
        public bool AcceptTerms { get; set; }
    }
}
