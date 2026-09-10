using System.ComponentModel.DataAnnotations;

namespace ElectricalBilling.ViewModels
{
    public class BusinessSettingViewModel
    {
        public int BusinessSettingId { get; set; }

        [Required(ErrorMessage = "Business name is required.")]
        [MaxLength(200)]
        [Display(Name = "Business Name")]
        public string BusinessName { get; set; } = string.Empty;

        [MaxLength(500)]
        [Display(Name = "Business Description")]
        public string? Description { get; set; }

        [MaxLength(300)]
        public string? Address { get; set; }

        [Phone(ErrorMessage = "Enter a valid mobile number.")]
        [MaxLength(20)]
        public string? Mobile { get; set; }

        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [MaxLength(150)]
        public string? Email { get; set; }

        [MaxLength(20)]
        [Display(Name = "GSTIN")]
        public string? GSTIN { get; set; }

        [Required(ErrorMessage = "Invoice prefix is required.")]
        [MaxLength(20)]
        [Display(Name = "Invoice Prefix")]
        public string InvoicePrefix { get; set; } = "ELB";

        [Display(Name = "Enable GST")]
        public bool GstEnabled { get; set; }

        [Range(0, 100, ErrorMessage = "GST % must be between 0 and 100.")]
        [Display(Name = "Default GST %")]
        public decimal DefaultGstPercent { get; set; }

        [Range(0, 100, ErrorMessage = "CGST % must be between 0 and 100.")]
        [Display(Name = "CGST %")]
        public decimal CgstPercent { get; set; }

        [Range(0, 100, ErrorMessage = "SGST % must be between 0 and 100.")]
        [Display(Name = "SGST %")]
        public decimal SgstPercent { get; set; }

        [MaxLength(500)]
        [Display(Name = "Invoice Footer")]
        public string? InvoiceFooter { get; set; }

        [MaxLength(500)]
        [Display(Name = "Bank Details")]
        public string? BankDetails { get; set; }

        [MaxLength(100)]
        [Display(Name = "UPI ID")]
        public string? UpiId { get; set; }
    }
}
