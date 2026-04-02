DECLARE @MonthStart DATE = '2026-03-01';
DECLARE @TeacherId INT = 1400;

DECLARE @MonthEndExclusive DATE = DATEADD(MONTH, 1, @MonthStart);

/*
Purpose:
1. Compare the financial count source (CircleReports.TeacherId).
2. Compare the old web screen logic source (Student.TeacherId).
3. Show which reports disappear from the original teacher screen after student reassignment.

Usage:
- Set @MonthStart to the first day of the required month.
- Set @TeacherId to the affected teacher id.
- Leave @TeacherId = NULL to inspect all mismatches in the whole month.
*/

IF OBJECT_ID('tempdb..#BaseReports') IS NOT NULL
    DROP TABLE #BaseReports;

SELECT
    cr.Id,
    CAST(cr.CreationTime AS DATE) AS ReportDate,
    cr.CreationTime,
    cr.StudentId,
    student.FullName AS StudentName,
    cr.TeacherId AS ReportTeacherId,
    reportTeacher.FullName AS ReportTeacherName,
    student.TeacherId AS CurrentStudentTeacherId,
    currentTeacher.FullName AS CurrentStudentTeacherName,
    cr.AttendStatueId,
    cr.Minutes,
    cr.IsDeleted
INTO #BaseReports
FROM dbo.CircleReports cr
LEFT JOIN dbo.[User] student
    ON student.Id = cr.StudentId
LEFT JOIN dbo.[User] reportTeacher
    ON reportTeacher.Id = cr.TeacherId
LEFT JOIN dbo.[User] currentTeacher
    ON currentTeacher.Id = student.TeacherId
WHERE cr.CreationTime >= @MonthStart
  AND cr.CreationTime < @MonthEndExclusive
  AND cr.IsDeleted = 0
  AND
  (
      @TeacherId IS NULL
      OR cr.TeacherId = @TeacherId
      OR student.TeacherId = @TeacherId
  );

SELECT
    @MonthStart AS MonthStart,
    @TeacherId AS TeacherId,
    SUM(CASE WHEN @TeacherId IS NULL OR ReportTeacherId = @TeacherId THEN 1 ELSE 0 END) AS FinancialCount_ByReportTeacher,
    SUM(CASE WHEN @TeacherId IS NULL OR CurrentStudentTeacherId = @TeacherId THEN 1 ELSE 0 END) AS WebCount_ByCurrentStudentTeacher,
    SUM(CASE WHEN ISNULL(CurrentStudentTeacherId, -1) <> ISNULL(ReportTeacherId, -1) THEN 1 ELSE 0 END) AS TotalMismatchCountInScope,
    SUM(
        CASE
            WHEN
                @TeacherId IS NOT NULL
                AND ReportTeacherId = @TeacherId
                AND ISNULL(CurrentStudentTeacherId, -1) <> @TeacherId
            THEN 1
            ELSE 0
        END
    ) AS MissingFromOriginalTeacherScreen,
    SUM(
        CASE
            WHEN
                @TeacherId IS NOT NULL
                AND CurrentStudentTeacherId = @TeacherId
                AND ISNULL(ReportTeacherId, -1) <> @TeacherId
            THEN 1
            ELSE 0
        END
    ) AS WronglyShownToCurrentTeacher
FROM #BaseReports;

SELECT
    Id AS CircleReportId,
    ReportDate,
    CreationTime,
    StudentId,
    StudentName,
    ReportTeacherId,
    ReportTeacherName,
    CurrentStudentTeacherId,
    CurrentStudentTeacherName,
    AttendStatueId,
    Minutes
FROM #BaseReports
WHERE ISNULL(CurrentStudentTeacherId, -1) <> ISNULL(ReportTeacherId, -1)
ORDER BY CreationTime, Id;

DROP TABLE #BaseReports;
