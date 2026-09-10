using System.ComponentModel.DataAnnotations;

namespace ElectricalBilling.ViewModels
{
    public class CustomerViewModel
    {
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "Customer name is required.")]
        [MaxLength(150)]
        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; } = string.Empty;

        [MaxLength(150)]
        [Display(Name = "Company Name")]
        public string? CompanyName { get; set; }

        [MaxLength(300)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(100)]
        public string? State { get; set; }

        [RegularExpression(@"^\d{6}$", ErrorMessage = "Pincode must be 6 digits.")]
        [Display(Name = "Pincode")]
        public string? Pincode { get; set; }

        [Required(ErrorMessage = "Mobile number is required.")]
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid 10-digit mobile number.")]
        public string Mobile { get; set; } = string.Empty;

        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid 10-digit mobile number.")]
        [Display(Name = "Alternate Mobile")]
        public string? AlternateMobile { get; set; }

        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string? Email { get; set; }

        [MaxLength(20)]
        [Display(Name = "GSTIN")]
        public string? GSTIN { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
