using System.ComponentModel.DataAnnotations;
using ElectricalBilling.Models;

namespace ElectricalBilling.ViewModels
{
    public class InvoiceViewModel
    {
        public int InvoiceId { get; set; }

        /// <summary>Display only — generated server-side, never bound from the form.</summary>
        public string? InvoiceNumber { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Please select a customer.")]
        public int CustomerId { get; set; }

        public string? CustomerName { get; set; }

        public string? CustomerMobile { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Invoice Date")]
        public DateTime InvoiceDate { get; set; } = DateTime.Today;

        [Display(Name = "Add GST")]
        public bool GstEnabled { get; set; }

        [Range(0, 100, ErrorMessage = "CGST % must be between 0 and 100.")]
        [Display(Name = "CGST %")]
        public decimal CgstPercent { get; set; }

        [Range(0, 100, ErrorMessage = "SGST % must be between 0 and 100.")]
        [Display(Name = "SGST %")]
        public decimal SgstPercent { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;

        public List<InvoiceItemInputViewModel> Items { get; set; } = new();

        // Totals are always recomputed server-side from Items; these are
        // only populated for display after a successful save, never bound
        // in from the posted form.
        public decimal SubTotal { get; set; }
        public decimal CgstAmount { get; set; }
        public decimal SgstAmount { get; set; }
        public decimal GrandTotal { get; set; }
    }
}
