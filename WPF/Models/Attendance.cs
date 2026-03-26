using System;
using System.Collections.Generic;

namespace WPF.Models;

public partial class Attendance
{
    public int AttendanceId { get; set; }

    public int SessionId { get; set; }

    public int StudentId { get; set; }

    public DateTime CheckInTime { get; set; }

    public string? Note { get; set; }

    public virtual AttendanceSession Session { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}
