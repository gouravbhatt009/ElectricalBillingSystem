namespace ElectricalBilling.ViewModels
{
    /// <summary>
    /// Qty/Rate are nullable so that a leftover blank row (e.g. user added
    /// a row, then changed their mind but forgot to remove it) binds
    /// cleanly instead of failing model binding on an empty numeric field.
    /// InvoiceService.GetValidItems treats any row with no Particular or a
    /// non-positive Qty as blank and silently drops it before saving.
    /// </summary>
    public class InvoiceItemInputViewModel
    {
        public int? ServiceId { get; set; }

        public string? Particular { get; set; }

        public decimal? Qty { get; set; } = 1;

        public decimal? Rate { get; set; }
    }
}
