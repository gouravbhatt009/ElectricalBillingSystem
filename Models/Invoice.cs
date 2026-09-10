using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectricalBilling.Models
{
    /// <summary>
    /// A single bill raised against a customer. GST fields are snapshotted
    /// at creation time (copied from BusinessSetting, but editable per
    /// invoice) so that changing the business GST% later never alters the
    /// amounts on a previously issued bill.
    /// </summary>
    public class Invoice
    {
        [Key]
        public int InvoiceId { get; set; }

        /// <summary>Format: {Prefix}-{Year}-{0000}, e.g. ELB-2026-0001. Never reused.</summary>
        [Required]
        [MaxLength(30)]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Required]
        public int CustomerId { get; set; }

        public Customer? Customer { get; set; }

        public DateTime InvoiceDate { get; set; } = DateTime.Today;

        public bool GstEnabled { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal CgstPercent { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal SgstPercent { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal CgstAmount { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal SgstAmount { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal GrandTotal { get; set; }

        public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();

        /// <summary>All payments recorded against this invoice (Phase 5). Never trust a client-supplied total — always sum these.</summary>
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
