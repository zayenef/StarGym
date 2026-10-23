namespace StarGymApp.Core.Models;

public class Payment
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public string Status { get; set; } = "Paid";
    public string Notes { get; set; } = string.Empty;
}
