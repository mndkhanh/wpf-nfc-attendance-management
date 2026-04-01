using System;
using System.Collections.Generic;
using System.Linq;
using WPF.Models;

namespace WPF.Services;

public sealed record StudentFormData(
    string FullName,
    string StudentCode,
    string? NfcUid,
    string? Phone,
    string? Email,
    string? Status,
    string? PhotoPath);

public static class StudentCrudHelper
{
    public static IReadOnlyList<Student> FilterStudents(IEnumerable<Student> students, string? searchText)
    {
        if (students is null)
        {
            return [];
        }

        var normalizedSearch = Normalize(searchText);
        if (string.IsNullOrEmpty(normalizedSearch))
        {
            return students.ToList();
        }

        return students
            .Where(student =>
                Contains(student.StudentCode, normalizedSearch) ||
                Contains(student.FullName, normalizedSearch) ||
                Contains(student.NfcUid, normalizedSearch) ||
                Contains(student.Phone, normalizedSearch) ||
                Contains(student.Email, normalizedSearch) ||
                Contains(student.Status, normalizedSearch))
            .ToList();
    }

    public static void ApplyForm(Student student, StudentFormData formData, DateOnly joinedDate, DateTime now, bool isNewStudent)
    {
        ArgumentNullException.ThrowIfNull(student);

        student.FullName = NormalizeRequired(formData.FullName, nameof(formData.FullName));
        student.StudentCode = NormalizeRequired(formData.StudentCode, nameof(formData.StudentCode));
        student.NfcUid = NormalizeOptional(formData.NfcUid);
        student.Phone = NormalizeOptional(formData.Phone);
        student.Email = NormalizeOptional(formData.Email);
        student.PhotoPath = NormalizeOptional(formData.PhotoPath);
        student.Status = NormalizeOptional(formData.Status) ?? "Active";
        student.UpdatedAt = now;

        if (isNewStudent)
        {
            student.JoinedDate = joinedDate;
            student.CreatedAt = now;
        }
    }

    private static string NormalizeRequired(string? value, string argumentName)
    {
        var normalized = NormalizeOptional(value);
        if (string.IsNullOrEmpty(normalized))
        {
            throw new ArgumentException("Required field was empty.", argumentName);
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static bool Contains(string? source, string searchText)
    {
        return !string.IsNullOrWhiteSpace(source) &&
               source.Contains(searchText, StringComparison.OrdinalIgnoreCase);
    }
}
