SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ApplyChanges BIT = 0;
DECLARE @ModifiedBy INT = NULL;
DECLARE @NowUtc DATETIME2(7) = SYSUTCDATETIME();

/*
Purpose:
1. Detect duplicate teacher salary invoices per teacher/month.
2. Keep one canonical invoice per teacher/month.
3. Soft-delete the extra invoices by setting IsDeleted = 1.
4. For the kept unpaid invoice, align Sallary with the total salary derived from CircleReports.

How to use:
1. Run the script with @ApplyChanges = 0 first.
   This shows:
   - duplicate groups
   - which invoice will be kept
   - which invoices will be soft-deleted
   - which kept invoices will have salary corrected
2. Review the result carefully.
3. Set @ApplyChanges = 1 and run again to apply the cleanup.

Notes:
- This script does NOT hard-delete anything.
- Paid invoices are preferred as the invoice to keep.
- If no invoice matches the report total, the script still keeps one invoice
  (latest modified/created/highest Id) and updates its salary if unpaid.
*/

IF OBJECT_ID('tempdb..#ReportTotals') IS NOT NULL DROP TABLE #ReportTotals;
IF OBJECT_ID('tempdb..#DuplicateGroups') IS NOT NULL DROP TABLE #DuplicateGroups;
IF OBJECT_ID('tempdb..#CandidateInvoices') IS NOT NULL DROP TABLE #CandidateInvoices;

SELECT
    cr.TeacherId,
    DATEFROMPARTS(YEAR(cr.CreationTime), MONTH(cr.CreationTime), 1) AS MonthStart,
    SUM(CAST(ISNULL(cr.TeacherSalaryAmount, 0) AS DECIMAL(18, 2))) AS ExpectedSalary,
    SUM(CAST(ISNULL(cr.TeacherSalaryMinutes, 0) AS DECIMAL(18, 2))) AS ExpectedMinutes,
    COUNT(*) AS ReportCount
INTO #ReportTotals
FROM dbo.CircleReports AS cr
WHERE cr.IsDeleted = 0
  AND cr.TeacherId IS NOT NULL
GROUP BY
    cr.TeacherId,
    DATEFROMPARTS(YEAR(cr.CreationTime), MONTH(cr.CreationTime), 1);

SELECT
    ts.TeacherId,
    DATEFROMPARTS(YEAR(ts.[Month]), MONTH(ts.[Month]), 1) AS MonthStart,
    COUNT(*) AS InvoiceCount
INTO #DuplicateGroups
FROM dbo.TeacherSallary AS ts
WHERE ts.IsDeleted = 0
  AND ts.TeacherId IS NOT NULL
  AND ts.[Month] IS NOT NULL
GROUP BY
    ts.TeacherId,
    DATEFROMPARTS(YEAR(ts.[Month]), MONTH(ts.[Month]), 1)
HAVING COUNT(*) > 1;

;WITH CandidateBase AS
(
    SELECT
        ts.Id,
        ts.TeacherId,
        u.FullName AS TeacherName,
        DATEFROMPARTS(YEAR(ts.[Month]), MONTH(ts.[Month]), 1) AS MonthStart,
        CAST(ts.Sallary AS DECIMAL(18, 2)) AS InvoiceSalary,
        ts.IsPayed,
        ts.CreatedAt,
        ts.ModefiedAt,
        rt.ExpectedSalary,
        rt.ExpectedMinutes,
        rt.ReportCount,
        CASE
            WHEN rt.ExpectedSalary IS NOT NULL
             AND ABS(CAST(ISNULL(ts.Sallary, 0) AS DECIMAL(18, 2)) - rt.ExpectedSalary) <= 0.01
                THEN 1
            ELSE 0
        END AS MatchesReportTotal
    FROM dbo.TeacherSallary AS ts
    INNER JOIN #DuplicateGroups AS dg
        ON dg.TeacherId = ts.TeacherId
       AND dg.MonthStart = DATEFROMPARTS(YEAR(ts.[Month]), MONTH(ts.[Month]), 1)
    LEFT JOIN #ReportTotals AS rt
        ON rt.TeacherId = ts.TeacherId
       AND rt.MonthStart = DATEFROMPARTS(YEAR(ts.[Month]), MONTH(ts.[Month]), 1)
    LEFT JOIN dbo.[User] AS u
        ON u.Id = ts.TeacherId
    WHERE ts.IsDeleted = 0
),
RankedCandidates AS
(
    SELECT
        cb.*,
        ROW_NUMBER() OVER
        (
            PARTITION BY cb.TeacherId, cb.MonthStart
            ORDER BY
                CASE WHEN cb.IsPayed = 1 THEN 0 ELSE 1 END,
                CASE WHEN cb.MatchesReportTotal = 1 THEN 0 ELSE 1 END,
                ISNULL(cb.ModefiedAt, cb.CreatedAt) DESC,
                cb.Id DESC
        ) AS KeepRank
    FROM CandidateBase AS cb
)
SELECT
    Id,
    TeacherId,
    TeacherName,
    MonthStart,
    InvoiceSalary,
    ExpectedSalary,
    ExpectedMinutes,
    ReportCount,
    MatchesReportTotal,
    IsPayed,
    CreatedAt,
    ModefiedAt,
    KeepRank,
    CASE WHEN KeepRank = 1 THEN 'KEEP' ELSE 'SOFT_DELETE' END AS PlannedAction
INTO #CandidateInvoices
FROM RankedCandidates;

PRINT 'Preview: duplicate teacher salary groups';
SELECT
    TeacherId,
    TeacherName,
    MonthStart,
    COUNT(*) AS DuplicateInvoiceCount,
    MAX(ExpectedSalary) AS ExpectedSalary,
    MAX(ExpectedMinutes) AS ExpectedMinutes,
    MAX(ReportCount) AS ReportCount
FROM #CandidateInvoices
GROUP BY TeacherId, TeacherName, MonthStart
ORDER BY MonthStart DESC, TeacherName;

PRINT 'Preview: invoice-level plan';
SELECT
    Id,
    TeacherId,
    TeacherName,
    MonthStart,
    InvoiceSalary,
    ExpectedSalary,
    MatchesReportTotal,
    IsPayed,
    CreatedAt,
    ModefiedAt,
    PlannedAction
FROM #CandidateInvoices
ORDER BY MonthStart DESC, TeacherName, PlannedAction DESC, Id DESC;

PRINT 'Preview: kept invoices whose salary will be corrected';
SELECT
    Id,
    TeacherId,
    TeacherName,
    MonthStart,
    InvoiceSalary AS CurrentSalary,
    ExpectedSalary AS NewSalary,
    IsPayed
FROM #CandidateInvoices
WHERE KeepRank = 1
  AND IsPayed = 0
  AND ExpectedSalary IS NOT NULL
  AND ABS(ISNULL(InvoiceSalary, 0) - ExpectedSalary) > 0.01
ORDER BY MonthStart DESC, TeacherName;

IF @ApplyChanges = 0
BEGIN
    PRINT 'Dry run only. No changes were applied. Set @ApplyChanges = 1 to apply.';
    RETURN;
END;

BEGIN TRANSACTION;

UPDATE kept
SET
    kept.Sallary = CAST(ci.ExpectedSalary AS FLOAT),
    kept.[Month] = ci.MonthStart,
    kept.ModefiedAt = @NowUtc,
    kept.ModefiedBy = @ModifiedBy
FROM dbo.TeacherSallary AS kept
INNER JOIN #CandidateInvoices AS ci
    ON ci.Id = kept.Id
WHERE ci.KeepRank = 1
  AND ISNULL(kept.IsPayed, 0) = 0
  AND ci.ExpectedSalary IS NOT NULL
  AND
  (
      ABS(CAST(ISNULL(kept.Sallary, 0) AS DECIMAL(18, 2)) - ci.ExpectedSalary) > 0.01
      OR CAST(kept.[Month] AS DATE) <> ci.MonthStart
  );

UPDATE extra
SET
    extra.IsDeleted = 1,
    extra.ModefiedAt = @NowUtc,
    extra.ModefiedBy = @ModifiedBy
FROM dbo.TeacherSallary AS extra
INNER JOIN #CandidateInvoices AS ci
    ON ci.Id = extra.Id
WHERE ci.KeepRank > 1
  AND ISNULL(extra.IsDeleted, 0) = 0;

PRINT 'Applied changes: remaining active invoices for cleaned duplicate groups';
SELECT
    ts.Id,
    ts.TeacherId,
    u.FullName AS TeacherName,
    DATEFROMPARTS(YEAR(ts.[Month]), MONTH(ts.[Month]), 1) AS MonthStart,
    CAST(ts.Sallary AS DECIMAL(18, 2)) AS Salary,
    ts.IsPayed,
    ts.CreatedAt,
    ts.ModefiedAt,
    ts.IsDeleted
FROM dbo.TeacherSallary AS ts
INNER JOIN
(
    SELECT DISTINCT TeacherId, MonthStart
    FROM #CandidateInvoices
) AS cleaned
    ON cleaned.TeacherId = ts.TeacherId
   AND cleaned.MonthStart = DATEFROMPARTS(YEAR(ts.[Month]), MONTH(ts.[Month]), 1)
LEFT JOIN dbo.[User] AS u
    ON u.Id = ts.TeacherId
WHERE ts.IsDeleted = 0
ORDER BY MonthStart DESC, TeacherName, ts.Id DESC;

COMMIT TRANSACTION;

PRINT 'Cleanup completed successfully.';
