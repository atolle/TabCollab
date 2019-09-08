using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace TabRepository.Models.AccountViewModels
{
    public enum AccountType
    {
        Free,
        Pro,
        Composer
    }
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Display(Name = "Bio")]
        public string Bio { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        public IFormFile Image { get; set; }

        public IFormFile CroppedImage { get; set; }

        [FileExtensions(Extensions = "png,gif,jpeg,jpg,nofile", ErrorMessage = "Invalid file type")]
        public string FileName
        {
            get
            {
                if (Image != null)
                    return Image.FileName;
                else
                    return ".nofile";
            }
        }

        [Required(ErrorMessage = "Account Type is required")]
        [Display(Name = "Account Type")]
        public AccountType? AccountType { get; set; }

        [Required]
        [Display(Name = "I agree to the Terms of Service and Privacy Policy")]
        public bool AgreeToTerms { get; set; }

        [Required]
        public string ReCaptchaToken { get; set; }
    }
}
