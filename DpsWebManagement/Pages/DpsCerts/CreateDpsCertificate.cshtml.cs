using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.Xml;

namespace DpsWebManagement.Pages.DpsCerts
{
    public class CreateDpsCertificateModel : PageModel
    {
        [BindProperty]
        public string? Message { get; set; }

        [BindProperty]
        [Required]
        public string Name { get; set; } = string.Empty;

        public IActionResult OnGet()
        {
            return Page();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                // Something failed. Redisplay the form.
                return OnGet();
            }

            //var cert = GetCertificateWithPrivateKey();

            Message = $"{Name}";

            // Redisplay the form.
            return OnGet();
        }
    }
}
