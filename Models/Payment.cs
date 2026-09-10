using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectricalBilling.Models
{
    /// <summary>
    /// One payment received against an invoice. An invoice can have many
    /// payments (partial payments); the invoice's Status and outstanding
    /// amount are always derived by summing these rows, never stored
    /// redundantly, so they can never drift out of sync.
    /// </summary>
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        [Required]
        public int InvoiceId { get; set; }

        public Invoice? Invoice { get; set; }

        /// <summary>Always positive; never persist zero/negative amounts.</summary>
        [Column(TypeName = "decimal(12,2)")]
        public decimal Amount { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.Today;

        public PaymentMode PaymentMode { get; set; } = PaymentMode.Cash;

        [MaxLength(100)]
        public string? ReferenceNumber { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
