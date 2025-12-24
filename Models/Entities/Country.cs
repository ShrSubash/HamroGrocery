using DocumentFormat.OpenXml.Bibliography;

namespace HamroGrocery.Models.Entities
{
    public class Country : BaseEntity
    {
        public string CountryName { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;

        // Navigation Properties
        public ICollection<StateProvince> StateProvinces { get; set; } = new List<StateProvince>();
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}