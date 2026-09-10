using System.ComponentModel.DataAnnotations;

namespace ElectricalBilling.ViewModels
{
    public class ServiceViewModel
    {
        public int ServiceId { get; set; }

        [Required(ErrorMessage = "Service name is required.")]
        [MaxLength(150)]
        [Display(Name = "Service Name")]
        public string ServiceName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Range(0, 9999999, ErrorMessage = "Default rate must be zero or more.")]
        [Display(Name = "Default Rate (₹)")]
        public decimal? DefaultRate { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
