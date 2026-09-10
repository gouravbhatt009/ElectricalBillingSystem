using System.ComponentModel.DataAnnotations;

namespace ElectricalBilling.Models
{
    /// <summary>
    /// Holds business-wide configuration so it is never hardcoded into
    /// views, controllers, or the PDF template. Designed as a single-row
    /// table for a single-business installation.
    /// </summary>
    public class BusinessSetting
    {
        [Key]
        public int BusinessSettingId { get; set; }

        [Required]
        [MaxLength(200)]
        public string BusinessName { get; set; } = "LEELADHAR BHATT";

        [MaxLength(500)]
        public string? Description { get; set; } =
            "Power Factor, Electric Work, HT/LT Cabling, Earth Testing, Transformer Oil Testing APFC Penal & Electrical Guidance";

        [MaxLength(300)]
        public string? Address { get; set; } = "Plot No.56, Goutam Nagar-8, Khora Bisal, JAIPUR (Raj.)";

        [MaxLength(20)]
        public string? Mobile { get; set; } = "9413600320";

        [MaxLength(150)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? GSTIN { get; set; }

        [MaxLength(20)]
        public string InvoicePrefix { get; set; } = "ELB";

        [MaxLength(300)]
        public string? LogoPath { get; set; }

        [MaxLength(300)]
        public string? SignaturePath { get; set; }

        public bool GstEnabled { get; set; } = false;

        [Range(0, 100)]
        public decimal DefaultGstPercent { get; set; } = 18.0m;

        [Range(0, 100)]
        public decimal CgstPercent { get; set; } = 9.0m;

        [Range(0, 100)]
        public decimal SgstPercent { get; set; } = 9.0m;

        [MaxLength(500)]
        public string? InvoiceFooter { get; set; } = "Thank you for your business.";

        [MaxLength(500)]
        public string? BankDetails { get; set; }

        [MaxLength(100)]
        public string? UpiId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
