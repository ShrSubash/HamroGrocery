using System.ComponentModel.DataAnnotations;

namespace HamroGrocery.Models.Entities
{
    public enum PurchaseOrderStatus
    {
        [Display(Name = "Draft")]
        Draft = 0,

        [Display(Name = "Pending")]
        Pending = 1,

        [Display(Name = "Approved")]
        Approved = 2,

        [Display(Name = "Completed")]
        Completed = 3,

        [Display(Name = "Cancelled")]
        Cancelled = 4
    }
}
