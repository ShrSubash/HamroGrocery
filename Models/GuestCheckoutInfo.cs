using System;

namespace HamroGrocery.Models
{
	/// <summary>
	/// Guest checkout information stored in cart session
	/// Serialized to JSON and stored in Cart.GuestInfoJson
	/// </summary>
	public class GuestCheckoutInfo
	{
		/// <summary>
		/// Guest email address (required for order confirmation)
		/// </summary>
		public string Email { get; set; } = string.Empty;

		/// <summary>
		/// Guest full name
		/// </summary>
		public string FullName { get; set; } = string.Empty;

		/// <summary>
		/// Guest contact phone number
		/// </summary>
		public string Phone { get; set; } = string.Empty;

		/// <summary>
		/// Delivery street address
		/// </summary>
		public string Address { get; set; } = string.Empty;

		/// <summary>
		/// Delivery city
		/// </summary>
		public string City { get; set; } = string.Empty;

		/// <summary>
		/// Delivery state (e.g., NSW, VIC)
		/// </summary>
		public string State { get; set; } = string.Empty;

		/// <summary>
		/// Delivery postcode (4 digits for Australia)
		/// </summary>
		public string Postcode { get; set; } = string.Empty;

		/// <summary>
		/// Timestamp when guest info was saved
		/// </summary>
		public DateTime SavedAt { get; set; } = DateTime.UtcNow;
	}
}