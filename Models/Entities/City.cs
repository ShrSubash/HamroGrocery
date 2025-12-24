namespace HamroGrocery.Models.Entities
{
    public class City : BaseEntity
    {
        public string CityName { get; set; } = string.Empty;
        public int StateProvinceId { get; set; }
        public int CountryId { get; set; }

        // Navigation Properties
        public StateProvince? StateProvince { get; set; }
        public Country? Country { get; set; }
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}