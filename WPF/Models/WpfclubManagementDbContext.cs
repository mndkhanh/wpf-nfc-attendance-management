using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace WPF.Models;

public partial class WpfclubManagementDbContext : DbContext
{
    public WpfclubManagementDbContext()
    {
    }

    public WpfclubManagementDbContext(DbContextOptions<WpfclubManagementDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Attendance> Attendances { get; set; }

    public virtual DbSet<AttendanceSession> AttendanceSessions { get; set; }

    public virtual DbSet<FinancialTransaction> FinancialTransactions { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    private string GetConnectionString()
    {
        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", true, true)
            .Build();

        var connectionString = config.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        throw new InvalidOperationException("Khong tim thay ConnectionStrings:DefaultConnection trong appsettings.json.");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(GetConnectionString());    
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(e => e.AttendanceId).HasName("PK__Attendan__8B69263C34237212");

            entity.ToTable("Attendance");

            entity.HasIndex(e => new { e.SessionId, e.StudentId }, "UQ_Attendance_Session_Student").IsUnique();

            entity.Property(e => e.AttendanceId).HasColumnName("AttendanceID");
            entity.Property(e => e.CheckInTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Note).HasMaxLength(255);
            entity.Property(e => e.SessionId).HasColumnName("SessionID");
            entity.Property(e => e.StudentId).HasColumnName("StudentID");

            entity.HasOne(d => d.Session).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.SessionId)
                .HasConstraintName("FK_Attendance_Sessions");

            entity.HasOne(d => d.Student).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK_Attendance_Students");
        });

        modelBuilder.Entity<AttendanceSession>(entity =>
        {
            entity.HasKey(e => e.SessionId).HasName("PK__Attendan__C9F49270C02B3671");

            entity.Property(e => e.SessionId).HasColumnName("SessionID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.EndTime).HasColumnType("datetime");
            entity.Property(e => e.SessionName).HasMaxLength(255);
            entity.Property(e => e.StartTime).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Active");
            entity.Property(e => e.Topic).HasMaxLength(255);
        });

        modelBuilder.Entity<FinancialTransaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("PK__Financia__55433A4B7F9F12D3");

            entity.ToTable("Financial_Transactions");

            entity.HasIndex(e => e.TransactionDate, "IX_Financial_Date");

            entity.HasIndex(e => e.StudentId, "IX_Financial_StudentID");

            entity.HasIndex(e => new { e.TransactionType, e.Category }, "IX_Financial_Type_Category");

            entity.Property(e => e.TransactionId).HasColumnName("TransactionID");
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(20)
                .HasDefaultValue("Paid");
            entity.Property(e => e.StudentId).HasColumnName("StudentID");
            entity.Property(e => e.TransactionDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TransactionType).HasMaxLength(20);

            entity.HasOne(d => d.Student).WithMany(p => p.FinancialTransactions)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Financial_Students");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.StudentId).HasName("PK__Students__32C52A7900A98BD2");

            entity.HasIndex(e => e.StudentCode, "UQ__Students__1FC88604D7215965").IsUnique();

            entity.HasIndex(e => e.NfcUid, "UQ__Students__9AA649A404FEC607").IsUnique();

            entity.Property(e => e.StudentId).HasColumnName("StudentID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.JoinedDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.NfcUid)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("NFC_UID");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.PhotoPath).HasMaxLength(260);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Active");
            entity.Property(e => e.StudentCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
