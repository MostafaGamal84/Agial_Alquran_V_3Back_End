BEGIN TRY
    BEGIN TRAN;

    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'IX_TeacherReportRecord_CreatedAt_IsDeleted_CircleReportId'
          AND object_id = OBJECT_ID(N'dbo.TeacherReportRecord')
    )
    BEGIN
        DROP INDEX [IX_TeacherReportRecord_CreatedAt_IsDeleted_CircleReportId]
        ON [dbo].[TeacherReportRecord];
    END

    ALTER TABLE [dbo].[TeacherReportRecord]
    ALTER COLUMN [CircleSallary] DECIMAL(18,2) NULL;

    CREATE NONCLUSTERED INDEX [IX_TeacherReportRecord_CreatedAt_IsDeleted_CircleReportId]
    ON [dbo].[TeacherReportRecord]
    (
        [CreatedAt] ASC,
        [IsDeleted] ASC,
        [CircleReportId] ASC
    )
    INCLUDE ([TeacherId], [Minutes], [CircleSallary]);

    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK;

    THROW;
END CATCH;
