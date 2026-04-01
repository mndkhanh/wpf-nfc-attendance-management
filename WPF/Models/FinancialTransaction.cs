using System;
using System.Collections.Generic;

namespace WPF.Models;

public partial class FinancialTransaction
{
    public int TransactionId { get; set; }

    public int? StudentId { get; set; }

    public string TransactionType { get; set; } = null!;

    public string Category { get; set; } = null!;

    public decimal Amount { get; set; }

    public DateTime TransactionDate { get; set; }

    public string? Description { get; set; }

    public string? PaymentMethod { get; set; }

    public string PaymentStatus { get; set; } = "Paid";

    public DateTime? CreatedAt { get; set; }

    public virtual Student? Student { get; set; }
}
