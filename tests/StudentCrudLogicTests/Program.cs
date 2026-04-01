using WPF.Models;
using WPF.Services;

return TestRunner.Run();

internal static class TestRunner
{
    private static int _failed;

    public static int Run()
    {
        RunTest("FilterStudents matches code name phone email and NFC UID", FilterStudents_MatchesAcrossFields);
        RunTest("ApplyForm trims values and sets timestamps for new student", ApplyForm_SetsFieldsForNewStudent);
        RunTest("ApplyForm keeps created date when editing existing student", ApplyForm_KeepsCreateFieldsForExistingStudent);
        RunTest("ApplyForm also assigns photo path", ApplyForm_AssignsPhotoPath);
        RunTest("StudentPhotoStorage saves file into target folder and returns stored name", StudentPhotoStorage_SavesPhotoIntoStorageRoot);

        if (_failed == 0)
        {
            Console.WriteLine("All tests passed.");
            return 0;
        }

        Console.WriteLine($"{_failed} test(s) failed.");
        return 1;
    }

    private static void RunTest(string name, Action test)
    {
        try
        {
            test();
            Console.WriteLine($"PASS: {name}");
        }
        catch (Exception ex)
        {
            _failed++;
            Console.WriteLine($"FAIL: {name}");
            Console.WriteLine(ex.Message);
        }
    }

    private static void FilterStudents_MatchesAcrossFields()
    {
        var students = new[]
        {
            new Student { StudentId = 1, StudentCode = "SE150111", FullName = "Nguyen Van An", NfcUid = "04:AA:BB", Phone = "0901234567", Email = "an@test.com", Status = "Active" },
            new Student { StudentId = 2, StudentCode = "SE150222", FullName = "Tran Thi Bich", NfcUid = "04:CC:DD", Phone = "0912345678", Email = "bich@test.com", Status = "Inactive" },
        };

        var resultByCode = StudentCrudHelper.FilterStudents(students, "150111");
        AssertEqual(1, resultByCode.Count, "Expected filter by student code to return one student.");

        var resultByPhone = StudentCrudHelper.FilterStudents(students, "091234");
        AssertEqual(1, resultByPhone.Count, "Expected filter by phone to return one student.");

        var resultByEmail = StudentCrudHelper.FilterStudents(students, "bich@test");
        AssertEqual(1, resultByEmail.Count, "Expected filter by email to return one student.");

        var resultByUid = StudentCrudHelper.FilterStudents(students, "04:AA");
        AssertEqual(1, resultByUid.Count, "Expected filter by NFC UID to return one student.");
    }

    private static void ApplyForm_SetsFieldsForNewStudent()
    {
        var student = new Student();
        var now = new DateTime(2026, 4, 1, 9, 30, 0);
        var joinedDate = new DateOnly(2026, 4, 1);
        var form = new StudentFormData("  Nguyen Van An  ", " SE150111 ", " 04:AA ", " 0901 ", " an@test.com ", " Active ", "photo-a.png");

        StudentCrudHelper.ApplyForm(student, form, joinedDate, now, isNewStudent: true);

        AssertEqual("Nguyen Van An", student.FullName, "Expected full name to be trimmed.");
        AssertEqual("SE150111", student.StudentCode, "Expected student code to be trimmed.");
        AssertEqual("04:AA", student.NfcUid, "Expected NFC UID to be trimmed.");
        AssertEqual("0901", student.Phone, "Expected phone to be trimmed.");
        AssertEqual("an@test.com", student.Email, "Expected email to be trimmed.");
        AssertEqual("Active", student.Status, "Expected status to be trimmed.");
        AssertEqual("photo-a.png", student.PhotoPath, "Expected photo path to be assigned.");
        AssertEqual(joinedDate, student.JoinedDate, "Expected joined date to be assigned for new student.");
        AssertEqual(now, student.CreatedAt, "Expected created at to be assigned for new student.");
        AssertEqual(now, student.UpdatedAt, "Expected updated at to be assigned.");
    }

    private static void ApplyForm_KeepsCreateFieldsForExistingStudent()
    {
        var createdAt = new DateTime(2025, 12, 31, 10, 0, 0);
        var joinedDate = new DateOnly(2025, 12, 15);
        var student = new Student
        {
            StudentId = 10,
            StudentCode = "OLD",
            FullName = "Old Name",
            JoinedDate = joinedDate,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

        var now = new DateTime(2026, 4, 1, 10, 30, 0);
        var form = new StudentFormData("New Name", "SE888", "", "", "", "Inactive", "");

        StudentCrudHelper.ApplyForm(student, form, new DateOnly(2026, 4, 1), now, isNewStudent: false);

        AssertEqual("New Name", student.FullName, "Expected existing student name to update.");
        AssertEqual("SE888", student.StudentCode, "Expected existing student code to update.");
        AssertNull(student.NfcUid, "Expected blank NFC UID to become null.");
        AssertNull(student.Phone, "Expected blank phone to become null.");
        AssertNull(student.Email, "Expected blank email to become null.");
        AssertEqual("Inactive", student.Status, "Expected status to update.");
        AssertNull(student.PhotoPath, "Expected blank photo path to become null.");
        AssertEqual(joinedDate, student.JoinedDate, "Expected joined date to stay unchanged when editing.");
        AssertEqual(createdAt, student.CreatedAt, "Expected created at to stay unchanged when editing.");
        AssertEqual(now, student.UpdatedAt, "Expected updated at to change when editing.");
    }

    private static void ApplyForm_AssignsPhotoPath()
    {
        var student = new Student();
        var form = new StudentFormData("Photo User", "SE0099", null, null, null, "Active", "avatars\\photo.png");

        StudentCrudHelper.ApplyForm(student, form, new DateOnly(2026, 4, 1), new DateTime(2026, 4, 1, 12, 0, 0), isNewStudent: true);

        AssertEqual("avatars\\photo.png", student.PhotoPath, "Expected photo path to be stored on the student.");
    }

    private static void StudentPhotoStorage_SavesPhotoIntoStorageRoot()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"student-photo-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);

        try
        {
            var sourceFilePath = Path.Combine(tempRoot, "source.jpg");
            File.WriteAllText(sourceFilePath, "fake image bytes");

            var storedName = StudentPhotoStorage.SavePhoto(sourceFilePath, tempRoot, "SE150111");
            var resolvedPath = StudentPhotoStorage.ResolvePhotoPath(storedName, tempRoot);

            AssertEqual(true, File.Exists(resolvedPath), "Expected saved image file to exist.");
            AssertEqual(".jpg", Path.GetExtension(resolvedPath), "Expected saved image extension to be preserved.");
            AssertNotEqual("source.jpg", Path.GetFileName(resolvedPath), "Saved image should not reuse original file name directly.");
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message} Expected: {expected}. Actual: {actual}.");
        }
    }

    private static void AssertNull(object? value, string message)
    {
        if (value is not null)
        {
            throw new InvalidOperationException($"{message} Actual: {value}.");
        }
    }

    private static void AssertNotEqual<T>(T notExpected, T actual, string message)
    {
        if (EqualityComparer<T>.Default.Equals(notExpected, actual))
        {
            throw new InvalidOperationException($"{message} Actual: {actual}.");
        }
    }
}
