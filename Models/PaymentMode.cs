namespace ElectricalBilling.Models
{
    /// <summary>Supported ways a customer can pay against an invoice.</summary>
    public enum PaymentMode
    {
        Cash = 0,
        UPI = 1,
        BankTransfer = 2,
        Cheque = 3,
        Other = 4
    }
}
