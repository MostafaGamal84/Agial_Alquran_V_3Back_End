SET NOCOUNT ON;
GO

IF EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_CircleReports_TeacherId_IsDeleted_CreationTime'
      AND object_id = OBJECT_ID(N'dbo.CircleReports')
)
BEGIN
    DROP INDEX [IX_CircleReports_TeacherId_IsDeleted_CreationTime] ON dbo.CircleReports;
END
GO

IF COL_LENGTH('dbo.CircleReports', 'TeacherSalaryMinutes') IS NULL
BEGIN
    ALTER TABLE dbo.CircleReports
    ADD TeacherSalaryMinutes DECIMAL(10,2) NULL;
END
ELSE IF EXISTS
(
    SELECT 1
    FROM sys.columns AS c
    INNER JOIN sys.types AS t
        ON t.user_type_id = c.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.CircleReports')
      AND c.name = N'TeacherSalaryMinutes'
      AND
      (
          t.name <> N'decimal'
          OR c.precision <> 10
          OR c.scale <> 2
      )
)
BEGIN
    ALTER TABLE dbo.CircleReports
    ALTER COLUMN TeacherSalaryMinutes DECIMAL(10,2) NULL;
END
GO

IF COL_LENGTH('dbo.CircleReports', 'TeacherSalaryAmount') IS NULL
BEGIN
    ALTER TABLE dbo.CircleReports
    ADD TeacherSalaryAmount DECIMAL(18,2) NULL;
END
ELSE IF EXISTS
(
    SELECT 1
    FROM sys.columns AS c
    INNER JOIN sys.types AS t
        ON t.user_type_id = c.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.CircleReports')
      AND c.name = N'TeacherSalaryAmount'
      AND
      (
          t.name <> N'decimal'
          OR c.precision <> 18
          OR c.scale <> 2
      )
)
BEGIN
    ALTER TABLE dbo.CircleReports
    ALTER COLUMN TeacherSalaryAmount DECIMAL(18,2) NULL;
END
GO

;WITH TeacherReportHistory AS
(
    SELECT
        record.CircleReportId,
        TotalTeacherSalaryMinutes = SUM(CAST(ISNULL(record.Minutes, 0) AS DECIMAL(10,2))),
        TotalTeacherSalaryAmount = SUM(CAST(ISNULL(record.CircleSallary, 0) AS DECIMAL(18,2)))
    FROM dbo.TeacherReportRecord AS record
    WHERE record.CircleReportId IS NOT NULL
      AND ISNULL(record.IsDeleted, 0) = 0
    GROUP BY record.CircleReportId
),
RebuiltSalarySnapshot AS
(
    SELECT
        report.Id,
        TeacherSalaryMinutes =
            CASE
                WHEN report.AttendStatueId IN (1, 3) THEN
                    CASE
                        WHEN report.Minutes IS NOT NULL
                            THEN CAST(ROUND(CAST(report.Minutes AS DECIMAL(10,2)), 2) AS DECIMAL(10,2))
                        ELSE ISNULL(history.TotalTeacherSalaryMinutes, CAST(0 AS DECIMAL(10,2)))
                    END
                ELSE CAST(0 AS DECIMAL(10,2))
            END,
        TeacherSalaryAmount =
            CAST(
                COALESCE(
                    history.TotalTeacherSalaryAmount,
                    report.TeacherSalaryAmount,
                    0
                ) AS DECIMAL(18,2)
            )
    FROM dbo.CircleReports AS report
    LEFT JOIN TeacherReportHistory AS history
        ON history.CircleReportId = report.Id
)
UPDATE report
SET
    report.TeacherSalaryMinutes = snapshot.TeacherSalaryMinutes,
    report.TeacherSalaryAmount = snapshot.TeacherSalaryAmount
FROM dbo.CircleReports AS report
INNER JOIN RebuiltSalarySnapshot AS snapshot
    ON snapshot.Id = report.Id;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_CircleReports_TeacherId_IsDeleted_CreationTime'
      AND object_id = OBJECT_ID(N'dbo.CircleReports')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_CircleReports_TeacherId_IsDeleted_CreationTime]
    ON dbo.CircleReports (TeacherId, IsDeleted, CreationTime)
    INCLUDE (TeacherSalaryMinutes, TeacherSalaryAmount, AttendStatueId, StudentId, CircleId);
END
GO

SELECT
    ReportsCount = COUNT(1),
    ReportsWithSalaryAmount = SUM(CASE WHEN TeacherSalaryAmount IS NOT NULL AND TeacherSalaryAmount > 0 THEN 1 ELSE 0 END),
    ReportsWithFractionalMinutes = SUM(CASE WHEN TeacherSalaryMinutes IS NOT NULL AND TeacherSalaryMinutes <> FLOOR(TeacherSalaryMinutes) THEN 1 ELSE 0 END)
FROM dbo.CircleReports;
GO
