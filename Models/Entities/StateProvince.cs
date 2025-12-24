using DocumentFormat.OpenXml.Bibliography;

namespace HamroGrocery.Models.Entities
{
    public class StateProvince : BaseEntity
    {
        public string StateName { get; set; } = string.Empty;
        public int CountryId { get; set; }

        // Navigation Properties
        public Country? Country { get; set; }
        public ICollection<City> Cities { get; set; } = new List<City>();
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}