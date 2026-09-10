using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectricalBilling.Models
{
    /// <summary>
    /// One line item on an invoice (S.No / Particular / Qty / Rate / Amount).
    /// ServiceId is optional: the user may pick a Service Master entry as a
    /// quick-fill, or type a fully custom Particular by hand.
    /// </summary>
    public class InvoiceItem
    {
        [Key]
        public int InvoiceItemId { get; set; }

        [Required]
        public int InvoiceId { get; set; }

        public Invoice? Invoice { get; set; }

        public int? ServiceId { get; set; }

        public Service? Service { get; set; }

        public int SNo { get; set; }

        [Required]
        [MaxLength(300)]
        public string Particular { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal Qty { get; set; } = 1;

        [Column(TypeName = "decimal(10,2)")]
        public decimal Rate { get; set; }

        /// <summary>Always Qty * Rate, computed and stored server-side — never trusted from the client.</summary>
        [Column(TypeName = "decimal(12,2)")]
        public decimal Amount { get; set; }
    }
}
