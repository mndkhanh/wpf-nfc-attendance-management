using System;
using System.Collections.Generic;

namespace WPF.Models;

public partial class Student
{
    public int StudentId { get; set; }

    public string StudentCode { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? NfcUid { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? PhotoPath { get; set; }

    public string? Status { get; set; }

    public DateOnly? JoinedDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual ICollection<FinancialTransaction> FinancialTransactions { get; set; } = new List<FinancialTransaction>();
}
