using System;
using System.Collections.Generic;

namespace WPF.Models;

public partial class AttendanceSession
{
    public int SessionId { get; set; }

    public string SessionName { get; set; } = null!;

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public string? Status { get; set; }

    public string? Topic { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
}
