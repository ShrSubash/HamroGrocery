using Microsoft.Build.Framework;

namespace HamroGrocery.Models.ViewModels
{
    public class ComplaintViewModel
    {
        public string? OrderNumber { get; set; }
        [Required] public string Subject { get; set; } = string.Empty;
        [Required] public string Message { get; set; } = string.Empty;
    }
}
