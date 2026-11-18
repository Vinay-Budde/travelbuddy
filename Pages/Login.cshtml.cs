using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TravelBuddy.Models;

namespace TravelBuddy.Pages
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public LoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [BindProperty]
        public string Email { get; set; } = "passenger@travelbuddy.local";

        [BindProperty]
        public string Password { get; set; } = "Passenger@123456";

        public string? ErrorMessage { get; set; }

        public void OnGet(string? returnUrl = null)
        {
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            var user = await _userManager.FindByEmailAsync(Email);
            if (user == null)
            {
                ErrorMessage = "Invalid credentials";
                return Page();
            }

            var result = await _signInManager.PasswordSignInAsync(user, Password, isPersistent: true, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Admin")) return RedirectToPage("/Admin");
                if (roles.Contains("Operator")) return RedirectToPage("/Operator");
                return RedirectToPage("/MyPasses");
            }

            ErrorMessage = "Invalid email or password.";
            return Page();
        }
    }
}
