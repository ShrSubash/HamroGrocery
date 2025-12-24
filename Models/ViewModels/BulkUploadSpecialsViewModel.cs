using HamroGrocery.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace HamroGrocery.Models.ViewModels
{
    public class BulkUploadSpecialsViewModel
    {
        public IFormFile? CsvFile { get; set; }

        [Display(Name = "Default Start Date")]
        public DateTime DefaultStartDate { get; set; } = DateTime.Now;

        [Display(Name = "Default End Date")]
        public DateTime DefaultEndDate { get; set; } = DateTime.Now.AddDays(7);

        [Display(Name = "Default Discount Type")]
        public DiscountType DefaultDiscountType { get; set; } = DiscountType.Percentage;

        [Display(Name = "Auto Activate")]
        public bool AutoActivate { get; set; } = true;

        [Display(Name = "Make Featured")]
        public bool MakeFeatured { get; set; } = false;
    }
}
