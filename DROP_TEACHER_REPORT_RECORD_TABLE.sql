SET NOCOUNT ON;
GO

IF COL_LENGTH('dbo.CircleReports', 'TeacherSalaryMinutes') IS NULL
   OR COL_LENGTH('dbo.CircleReports', 'TeacherSalaryAmount') IS NULL
BEGIN
    RAISERROR(N'CircleReports salary snapshot columns are missing. Run CIRCLE_REPORT_TEACHER_SALARY_MIGRATION.sql first.', 16, 1);
    RETURN;
END
GO

IF OBJECT_ID(N'dbo.TeacherReportRecord', N'U') IS NULL
BEGIN
    PRINT N'TeacherReportRecord table does not exist. Nothing to drop.';
    RETURN;
END
GO

SELECT
    TeacherReportRecordRows = COUNT(1),
    DistinctCircleReports = COUNT(DISTINCT CircleReportId),
    DistinctTeachers = COUNT(DISTINCT TeacherId)
FROM dbo.TeacherReportRecord
WHERE ISNULL(IsDeleted, 0) = 0;
GO

DECLARE @DropForeignKeysSql NVARCHAR(MAX) = N'';

SELECT @DropForeignKeysSql = @DropForeignKeysSql +
    N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id)) + N'.' + QUOTENAME(OBJECT_NAME(parent_object_id)) +
    N' DROP CONSTRAINT ' + QUOTENAME(name) + N';' + CHAR(13) + CHAR(10)
FROM sys.foreign_keys
WHERE parent_object_id = OBJECT_ID(N'dbo.TeacherReportRecord')
   OR referenced_object_id = OBJECT_ID(N'dbo.TeacherReportRecord');

IF LEN(@DropForeignKeysSql) > 0
BEGIN
    EXEC sp_executesql @DropForeignKeysSql;
END
GO

DROP TABLE dbo.TeacherReportRecord;
GO

PRINT N'TeacherReportRecord table was dropped successfully.';
GO
