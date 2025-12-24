using System.ComponentModel.DataAnnotations;

namespace HamroGrocery.Models.Entities
{
    public enum DiscountType
    {
        [Display(Name = "Percentage Discount")]
        Percentage=0,

        [Display(Name = "Fixed Amount Off")]
        FixedAmount=1,

        [Display(Name = "Buy One Get One")]
        BuyOneGetOne=2,

        [Display(Name = "Buy Two Get One")]
        BuyTwoGetOne=3
    }
}
